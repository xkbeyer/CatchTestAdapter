using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Xml;
using System.Globalization;
using System.Diagnostics;

using TestAdapter.Settings;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using System.IO;

namespace Catch.TestAdapter
{

    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public static readonly TestProperty Section = TestProperty.Register("TestCase.Section", "Section", typeof(string), typeof(TestCase));
        public const string ExecutorUriString = "executor://CatchTestRunner";
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
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, "RunTest of source " + exeName);
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
            frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Run tests :");
            foreach (var t in tests) {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"\tDisplayName={t.DisplayName} Full={t.FullyQualifiedName} Prop={t.GetPropertyValue(Section)} ID={t.Id}");
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

            var listOfExes = remainingTests.Select(t => t.Source).Distinct();
            foreach(var CatchExe in listOfExes)
            {

                var listOfTestCasesOfSource = from test in remainingTests where test.Source == CatchExe select test.DisplayName;
                var listOfTestCases = listOfTestCasesOfSource.Aggregate("", (acc, name) => acc + name + Environment.NewLine);

#if DEBUG
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"{CatchExe} with Tests:{Environment.NewLine}{listOfTestCases}");
#endif
                // Use the directory of the executable as the working directory.
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
                    string name = n;
                    if (n.Contains("Given"))
                        name = "    " + n;
                    if (n.Contains("And"))
                        name = "      " + n;
                    if (n.Contains("When") || n.Contains("Then"))
                        name = "     " + n;
                    listOfSections += $" -c \"{name}\"";
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
        /// Constructs a test result (messages and stack trace) from the given Expressions.
        /// </summary>
        /// <param name="Expresions">Catch expressions</param>
        /// <param name="testResult">Test result of the expressions</param>
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
        
        private void CreateResult(Tests.TestCase element, TestCase testCase, string name)
        {
            var subResult = new TestResult(testCase)
            {
                DisplayName = name.Replace(".", "\n\t"),
                ErrorMessage = $"{element.Name}{Environment.NewLine}",
                ErrorStackTrace = "",
            };
            if (element.Result != null)
                subResult.Duration = TimeSpan.FromSeconds(Double.Parse(element.Result.Duration, CultureInfo.InvariantCulture));

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
#if DEBUG1
            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                $"Created TestResult Name={subResult.DisplayName} element={element.Name} TC={subResult.TestCase.FullyQualifiedName} Error={subResult.ErrorMessage}");
            foreach (var msg in subResult.Messages)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"\tMsg={msg.Text}");
            }
#endif
        }

        /// <summary>
        /// Extracts the test results from the Catch test cases.
        /// </summary>
        /// <param name="element">Catch test case element</param>
        /// <param name="testCase">Current test case</param>
        /// <param name="name">Constructed name (including section names)</param>
        private void TryGetFailure(Tests.TestCase element, TestCase testCase, string name)
        {
            name += element.Name;
#if DEBUG1
            var testCaseSection = testCase.GetPropertyValue(Section) ?? "";
            frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Handle TestCase Name={name} FQN={testCase.FullyQualifiedName} DisplayName={testCase.DisplayName} Section={testCaseSection} ID={testCase.Id}");
#endif
            // Try to find the failure from this element.
            CreateResult(element, testCase, name);
            // Try to find the failure from a subsection of this element.
            foreach (var section in element.Sections)
            {
                // Try to find a failure in this section.
                TryGetFailure(section, testCase, name + ".");
            }
        }

        /// <summary>
        /// Reports all results from the XML output to the framework.
        /// </summary>
        /// <param name="output_text">The text lines of the output</param>
        /// <param name="vsTests">List of tests from the discoverer/TestExplorer</param>
        /// <param name="frameworkHandle"></param>
        protected virtual void ComposeResults(IList<string> output_text, IList<TestCase> vsTests, IFrameworkHandle frameworkHandle)
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
                    var test = vsTests.Where((test_case) => test_case.FullyQualifiedName == xmlResult.Name).First();
                    results.Clear();
                    TryGetFailure(xmlResult, test, "");
                    var specificTest = results.Where((test_result) => test_result.TestCase.Id == test.Id);
                    if(specificTest.Count() == 1 && test.DisplayName != test.FullyQualifiedName)
                    {
                        var r = specificTest.First();
#if DEBUG
                        frameworkHandle.SendMessage(TestMessageLevel.Informational,
                            $"Report special TestCase FQN={r.TestCase.FullyQualifiedName} DisplayName={r.TestCase.DisplayName} ID={r.TestCase.Id}");
#endif
                        frameworkHandle.RecordResult(r);
                    }
                    else
                    {
                        foreach (var r in results)
                        {
#if DEBUG
                            frameworkHandle.SendMessage(TestMessageLevel.Informational,
                                $"Report TestResult {r.DisplayName} FQN={r.TestCase.FullyQualifiedName} DisplayName={r.TestCase.DisplayName} ID={r.TestCase.Id}");
#endif
                            frameworkHandle.RecordResult(r);
                        }
                    }

                    // And remove the test from the list of outstanding tests
                    vsTests.Remove(test);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (vsTests.Count != 0)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, $"While running test {vsTests.First().Source}, catched exception in adapter: {ex}");
                    frameworkHandle.SendMessage(TestMessageLevel.Error, "  Test output, all remaining test cases marked as inconclusive: ");
                    foreach (var s in output_text)
                    {
                        frameworkHandle.SendMessage(TestMessageLevel.Error, s);
                    }
                    frameworkHandle.SendMessage(TestMessageLevel.Error, "===============================");

#if DEBUG
                    frameworkHandle.SendMessage(TestMessageLevel.Warning, $"Mark the following test as not run:");
#endif
                    var listOfTestCasesOfSource = from test in vsTests where test.Source == vsTests.First().Source select test;

                    foreach (var missingTest in listOfTestCasesOfSource)
                    {
                        var testResult = new TestResult(missingTest)
                        {
                            Outcome = TestOutcome.None
                        };
#if DEBUG
                        frameworkHandle.SendMessage(TestMessageLevel.Warning, $"{missingTest} / {testResult.DisplayName}");
#endif
                        frameworkHandle.RecordResult(testResult);
                    }
                }
            }
        }
    }
}