using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Diagnostics;

using TestAdapter.Settings;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;

namespace Catch.TestAdapter
{

    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public static readonly TestProperty Section = TestProperty.Register("TestCase.Section", "Section", typeof(string), typeof(TestCase));
        public const string ExecutorUriString = "executor://CatchTestRunner/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private string SolutionDirectory { get; set; } = "";
        private IFrameworkHandle frameworkHandle = null;
        private IList<TestResult> results = new List<TestResult>();

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // Load settings from the context.
            var settings = CatchSettingsProvider.LoadSettings(runContext.RunSettings);
            frameworkHandle.SendMessage(TestMessageLevel.Informational, "CatchAdapter::RunTests... ");

            // Run tests in all included executables.
            foreach (var exeName in sources.Where(name => settings.IncludeTestExe(name)))
            {
                // Wrap execution in try to stop one executable's exceptions from stopping the others from being run.
                try
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, "RunTest with source " + exeName);
                    var tests = TestDiscoverer.CreateTestCases(exeName, runContext.SolutionDirectory);

                    RunTests(tests, runContext, frameworkHandle);
                }
                catch (Exception ex)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, "Exception running tests: " + ex.Message);
                    frameworkHandle.SendMessage(TestMessageLevel.Error, "Exception stack: " + ex.StackTrace);
                }
            }
        }

        public void RunTests(IEnumerable<TestCase> testsToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            SolutionDirectory = runContext.SolutionDirectory;
            this.frameworkHandle = frameworkHandle;

            var tests = testsToRun.ToList();
#if DEBUG
            foreach (var t in tests) {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"DisplayName={t.DisplayName} Full={t.FullyQualifiedName} Prop={t.GetPropertyValue(Section)} ID={t.Id}");
            }
#endif
            var timer = Stopwatch.StartNew();

            // Find all tests of section results and run them.
            var testsWithSections = tests.Where((test) => test.GetPropertyValue(Section) != null );
            RunTestSections(testsWithSections, runContext.IsBeingDebugged);

            // Get and run the remaining test cases.
            var remainingTests = tests.Except(testsWithSections);
            if(remainingTests.Count() == 0)
            {
                return; // Done no further tests to run.
            }
            var listOfTestCases = remainingTests.Aggregate("", (acc,test)=> acc + test.DisplayName + Environment.NewLine);
#if DEBUG
            frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Remaining Tests: {listOfTestCases}");
#endif

            // Use the directory of the executable as the working directory.
            var CatchExe = remainingTests.First().Source;
            string workingDirectory = System.IO.Path.GetDirectoryName(CatchExe);
            if (workingDirectory == "")
                workingDirectory = ".";
            // Write them to the input file for Catch runner
            const string caseFile = "test.cases";
            System.IO.File.WriteAllText(workingDirectory + System.IO.Path.DirectorySeparatorChar + caseFile, listOfTestCases);

            // Execute the tests
            string arguments = "-r xml --durations yes --input-file=" + caseFile ;
            var output_text = ProcessRunner.RunProcess(frameworkHandle, CatchExe, arguments, workingDirectory, runContext.IsBeingDebugged);

            timer.Stop();
            frameworkHandle.SendMessage(TestMessageLevel.Informational, "Overall time " + timer.Elapsed.ToString());

            ComposeResults(output_text, remainingTests.ToList(), frameworkHandle);

            // Remove the temporary input file.
            System.IO.File.Delete(caseFile);
        }

        /// <summary>
        /// Calls Catch to run the section of a test.
        /// Catch can only execute sections via the command line and only sections of one test.
        /// </summary>
        /// <param name="testsWithSections">All the test which have sections to be run</param>
        /// <param name="isBeingDebugged">Is the Catch Runner being debugged?</param>
        private void RunTestSections(IEnumerable<TestCase> testsWithSections, bool isBeingDebugged)
        {
            if (testsWithSections.Count() == 0)
            {
                return;
            }
            foreach (var test in testsWithSections)
            {
                var CatchExe = test.Source;
                string workingDirectory = System.IO.Path.GetDirectoryName(CatchExe);
                if (workingDirectory == "")
                    workingDirectory = ".";
                var section = test.GetPropertyValue(Section);
                var sectionNames = section.ToString().Split('/');
                string listOfSections = "";
                foreach (var n in sectionNames.Skip(1))
                {
                    listOfSections += $" -c \"{n}\"";
                }
                string args = $"-r xml --durations yes \"{sectionNames.First()}\"{listOfSections}";
                var output_text = ProcessRunner.RunProcess(frameworkHandle, CatchExe, args, workingDirectory, isBeingDebugged);
#if DEBUG
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Call: {args}");
#endif
                List<TestCase> listOfTests = new List<TestCase> { test };
                ComposeResults(output_text, listOfTests, frameworkHandle);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="Expresions"></param>
        /// <param name="testResult"></param>
        private int ConstructResult(Tests.Expression[] Expresions, TestResult testResult)
        {
            int i = 0;
            foreach (var expression in Expresions.Where(expr => expr.Success == "false"))
            {
                ++i;
                string expanded = expression.Expanded.Trim();
                string original = expression.Original.Trim();
                string type = expression.Type;
                var FilePath = TestDiscoverer.ResolvePath(expression.Filename, SolutionDirectory);
                var LineNumber = Int32.Parse(expression.Line);
                testResult.ErrorMessage += $"#{i} - {type}({original}) with expansion: ({expanded}){Environment.NewLine}";
                testResult.ErrorStackTrace += $"at #{i} - {testResult.DisplayName}() in {FilePath}:line {LineNumber}{Environment.NewLine}";
            }
            return i;
        }
        
        private void CreateResult(Tests.TestCase element, TestResult testResult, TestCase testCase, string name)
        {
            var subResult = new TestResult(testCase);
            subResult.Outcome = testResult.Outcome;
            subResult.DisplayName = testCase.DisplayName;
            subResult.ErrorMessage = $"{element.Name}{Environment.NewLine}";
            subResult.ErrorStackTrace = "";


            int i = ConstructResult(element.Expressions, subResult);

            foreach (var s in (element.Warning ?? new string[] { }))
            {
                var Expression = $"WARN: {s.Trim()}{ Environment.NewLine }";
                subResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, Expression));
            }

            foreach (var s in (element.Info ?? new string[] { }))
            {
                var Expression = $"INFO: {s.Trim()}{ Environment.NewLine }";
                subResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, Expression));
            }

            // Check if this element is a failure generated by FAIL().
            if (element.Failure != null)
            {
                ++i;
                var LineNumber = Int32.Parse(element.Failure.Line);
                var FilePath = TestDiscoverer.ResolvePath(element.Failure.Filename, SolutionDirectory);
                subResult.ErrorMessage += $"#{i} - FAIL({element.Failure.text.Trim()}){Environment.NewLine}";
                subResult.ErrorStackTrace += $"at #{i} - {name}() in {FilePath}:line {LineNumber}{Environment.NewLine}";
            }
            if (i == 0)
            {
                // No further failed cases found. That can especially happen in SECTIONS.
                subResult.Outcome = TestOutcome.Passed;
            }
            else
            {
                subResult.Outcome = TestOutcome.Failed;
            }
            results.Add(subResult);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="element"></param>
        /// <param name="testResult"></param>
        /// <param name="name"></param>
        void TryGetFailure(Tests.TestCase element, TestResult testResult, string name)
        {
            name += element.Name;
            if( element.Result != null )
                testResult.Duration = TimeSpan.FromSeconds(Double.Parse(element.Result.Duration, CultureInfo.InvariantCulture)); 
            var testCase = testResult.TestCase;
#if DEBUG
            var testCaseSection = testCase.GetPropertyValue(Section) ?? "";
            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                $"Handle TestCase Element={name} FQN={testCase.FullyQualifiedName} DisplayName={testCase.DisplayName} Section={testCaseSection} ID={testCase.Id}");
#endif
            var Id = EqtHash.GuidFromString(testCase.FullyQualifiedName + testResult.TestCase.ExecutorUri + testResult.TestCase.Source + name);
            if (testCase.Id != Id)
            {
                // This is a testcase which hasn't run before. It is really a new one, so create it.
                testCase = new TestCase(testCase.FullyQualifiedName, testResult.TestCase.ExecutorUri, testResult.TestCase.Source);
                testCase.CodeFilePath = testResult.TestCase.CodeFilePath;
                testCase.DisplayName = name.Replace('/', ' ');
                testCase.LineNumber = testResult.TestCase.LineNumber;
                testCase.Source = testResult.TestCase.Source;
                testCase.Traits.Concat( testResult.TestCase.Traits );
                testCase.Id = EqtHash.GuidFromString(testCase.FullyQualifiedName + testResult.TestCase.ExecutorUri + testResult.TestCase.Source + name);
                if(name.Contains('/'))
                    testCase.SetPropertyValue(Section, name);
#if DEBUG
                frameworkHandle.SendMessage(TestMessageLevel.Informational, 
                    $"Found new TestCase FQN={testCase.FullyQualifiedName} Name={name} ID={testCase.Id}");
#endif
            }

            // Try to find the failure from this element.
            CreateResult(element, testResult, testCase, name);
            // Try to find the failure from a subsection of this element.
            foreach (var section in element.Sections)
            {
                // Try to find a failure in this section.
                TryGetFailure(section, testResult, name + "/");
            }
        }

        /// <summary>
        /// Reports all results from the XML output to the framework.
        /// </summary>
        /// <param name="output_text">The text lines of the output</param>
        /// <param name="tests">List of tests from the discoverer</param>
        /// <param name="frameworkHandle"></param>
        protected virtual void ComposeResults(IList<string> output_text, IList<TestCase> tests, IFrameworkHandle frameworkHandle)
        {
            this.frameworkHandle = frameworkHandle;
            var xmlresult = string.Join("", output_text);
            var stream = new System.IO.MemoryStream(System.Text.Encoding.ASCII.GetBytes(xmlresult));
            var testCaseSerializer = new XmlSerializer(typeof(Catch.TestAdapter.Tests.TestCase));
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreComments = true;
                settings.IgnoreProcessingInstructions = true;
                settings.IgnoreWhitespace = true;
                var reader = XmlReader.Create(stream, settings);
                while (reader.Read())
                {
                    if (reader.Depth != 2 || reader.Name != "TestCase")
                        continue;

                    var xmlResult = (Tests.TestCase)testCaseSerializer.Deserialize(reader.ReadSubtree());

                    // Find the matching test case
                    var test = tests.Where((test_case) => test_case.FullyQualifiedName == xmlResult.Name).First();
                    var testResult = new TestResult(test);
                    results.Clear();
                    TryGetFailure(xmlResult, testResult, "");
                    var specificTest = results.Where((test_result) => test_result.TestCase.Id == test.Id);
                    if(specificTest.Count() == 1 && test.DisplayName != test.FullyQualifiedName)
                    {
                        var r = specificTest.First();
#if DEBUG
                        frameworkHandle.SendMessage(TestMessageLevel.Informational,
                            $"Report special TestCase FQN={r.TestCase.FullyQualifiedName} DisplayName={r.TestCase.DisplayName} ID={r.TestCase.Id}");
#endif
                        frameworkHandle.RecordStart(r.TestCase);
                        frameworkHandle.RecordResult(r);
                        frameworkHandle.RecordEnd(r.TestCase, r.Outcome);
                    }
                    else
                    {
                        foreach (var r in results)
                        {
#if DEBUG
                            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                                $"Report TestCase FQN={r.TestCase.FullyQualifiedName} DisplayName={r.TestCase.DisplayName} ID={r.TestCase.Id}");
#endif
                            frameworkHandle.RecordStart(r.TestCase);
                            frameworkHandle.RecordResult(r);
                            frameworkHandle.RecordEnd(r.TestCase, r.Outcome);
                        }
                    }

                    // And remove the test from the list of outstanding tests
                    tests.Remove(test);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (tests.Count != 0)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, $"Running test {tests.First().Source}, exception in adapter: {ex}");
                    frameworkHandle.SendMessage(TestMessageLevel.Error, "  Test output, all remaining test cases marked as inconclusive: ");
                    foreach (var s in output_text)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Error, s);
                    }
                    frameworkHandle.SendMessage(TestMessageLevel.Error, "===============================");

                    foreach (var missingTest in tests)
                    {
                        var testResult = new TestResult(missingTest)
                        {
                            Outcome = TestOutcome.None
                        };
                        frameworkHandle.RecordResult(testResult);
                    }
                }
            }
        }
    }
}