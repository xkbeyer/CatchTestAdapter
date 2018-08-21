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
            var tests = testsToRun.ToList();
#if DEBUG
            foreach (var t in tests) {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"DisplayName={t.DisplayName} Full={t.FullyQualifiedName} Prop={t.GetPropertyValue(Section)} ID={t.Id}");
            }
#endif
            SolutionDirectory = runContext.SolutionDirectory;
            var CatchExe = tests.First().Source;
            var timer = Stopwatch.StartNew();
            // Use the directory of the executable as the working directory.
            string workingDirectory = System.IO.Path.GetDirectoryName(CatchExe);
            if (workingDirectory == "")
                workingDirectory = ".";

            IList<string> output_text;

            // Get a list of all test case names
            var listOfTestCases = tests.Aggregate("", (acc, test) => acc + test.DisplayName + Environment.NewLine);
            // Find all tests which are section results.
            var testsWithSections = tests.Where((test) => test.GetPropertyValue(Section) != null );
            if (testsWithSections.Count() > 0 )
            {
                listOfTestCases = "";
                foreach (var test in testsWithSections)
                {
                    var section = test.GetPropertyValue(Section);
                    var sectionNames = section.ToString().Split('/');
                    listOfTestCases += sectionNames.First() + Environment.NewLine;
                    string listOfSections = "";
                    foreach (var n in sectionNames.Skip(1))
                    {
                        listOfSections += $" -c \"{n}\"";
                    }
                    string args = $"-r xml --durations yes \"{sectionNames.First()}\" {listOfSections}" ;
                    if (runContext.IsBeingDebugged)
                    {
                        output_text = ProcessRunner.RunDebugProcess(frameworkHandle, CatchExe, args, workingDirectory);
                    }
                    else
                    {
                        output_text = ProcessRunner.RunProcess(CatchExe, args, workingDirectory);
                    }
#if DEBUG
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Call: {args}");
#endif
                    List<TestCase> copyOftests = new List<TestCase>();
                    copyOftests.Add(test);
                    ComposeResults(output_text, copyOftests, frameworkHandle);
                }
                timer.Stop();
                frameworkHandle.SendMessage(TestMessageLevel.Informational, "Overall time " + timer.Elapsed.ToString());
            }
            var remainingTests = tests.Except(testsWithSections);
            tests = remainingTests.ToList();
            listOfTestCases = remainingTests.Aggregate("", (acc,test)=> acc + test.DisplayName + Environment.NewLine);
            // Write them to the input file for Catch runner
            string caseFile = "test.cases";
            System.IO.File.WriteAllText(workingDirectory + System.IO.Path.DirectorySeparatorChar + caseFile, listOfTestCases);
            string originalDirectory = Directory.GetCurrentDirectory();

            // Execute the tests

            string arguments = "-r xml --durations yes --input-file=" + caseFile ;
            if (runContext.IsBeingDebugged)
            {
                output_text = ProcessRunner.RunDebugProcess(frameworkHandle, CatchExe, arguments, workingDirectory);
            }
            else
            {
                output_text = ProcessRunner.RunProcess(CatchExe, arguments, workingDirectory);
            }
            timer.Stop();
            frameworkHandle.SendMessage(TestMessageLevel.Informational, "Overall time " + timer.Elapsed.ToString());

            // Output as a single string.
            ComposeResults(output_text, tests, frameworkHandle);

            // Remove the temporary input file.
            System.IO.File.Delete(caseFile);
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
            var testCaseSection = testCase.GetPropertyValue(Section)?? "";
            if ( name != testResult.TestCase.DisplayName && testCaseSection.ToString() != name)
            {
                // This is a testcase which hasn't run before. It is really a new one, so create it.
                testCase = new TestCase(testCase.FullyQualifiedName, testResult.TestCase.ExecutorUri, testResult.TestCase.Source);
                testCase.CodeFilePath = testResult.TestCase.CodeFilePath;
                testCase.DisplayName = name.Replace('/', ' ');
                testCase.LineNumber = testResult.TestCase.LineNumber;
                testCase.Source = testResult.TestCase.Source;
                testCase.Traits.Concat( testResult.TestCase.Traits );
                testCase.Id = EqtHash.GuidFromString(testCase.FullyQualifiedName + testResult.TestCase.ExecutorUri + testResult.TestCase.Source + name);
                testCase.SetPropertyValue(Section, name);
#if DEBUG
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Found new TestCase {name} ID={testCase.Id}");
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
                var reader = XmlReader.Create(stream);
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
                    if( specificTest.Count() == 1 && test.DisplayName != test.FullyQualifiedName)
                    {
                        var r = specificTest.First();
                        frameworkHandle.RecordStart(r.TestCase);
                        frameworkHandle.RecordResult(r);
                        frameworkHandle.RecordEnd(r.TestCase, r.Outcome);
                    }
                    else
                    {
                        foreach (var r in results)
                        {
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