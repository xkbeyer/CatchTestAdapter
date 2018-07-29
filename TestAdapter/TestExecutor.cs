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

namespace Catch.TestAdapter
{

    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://CatchTestRunner/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private string SolutionDirectory { get; set; } = "";
        private IFrameworkHandle frameworkHandle = null;

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            this.frameworkHandle = frameworkHandle;
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
            this.frameworkHandle = frameworkHandle;
            var tests = testsToRun.ToList();

            SolutionDirectory = runContext.SolutionDirectory;
            var CatchExe = tests.First().Source;
            var timer = Stopwatch.StartNew();

            // Get a list of all test case names
            var listOfTestCases = tests.Aggregate("", (acc, test) => acc + test.DisplayName + "\n");

            // Use the directory of the executable as the working directory.
            string workingDirectory = System.IO.Path.GetDirectoryName(CatchExe);
            if (workingDirectory == "")
                workingDirectory = ".";

            // Write them to the input file for Catch runner
            string caseFile = "test.cases";
            System.IO.File.WriteAllText(workingDirectory + System.IO.Path.DirectorySeparatorChar + caseFile, listOfTestCases);
            string originalDirectory = Directory.GetCurrentDirectory();

            // Execute the tests
            IList<string> output_text;

            string arguments = "-r xml --durations yes --input-file=" + caseFile;
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
        
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="element"></param>
        /// <param name="testResult"></param>
        /// <param name="name"></param>
        void TryGetFailure(Tests.TestCase element, TestResult testResult, string name)
        {
            // Get current level's name.
            name += element.Name;
            // Try to find the failure from this element.
            frameworkHandle.RecordStart(testResult.TestCase);
            var subResult = new TestResult(testResult.TestCase);
            subResult.Outcome = testResult.Outcome;
            subResult.DisplayName = name;
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
                subResult.ErrorStackTrace += $"at #{i} - {testResult.DisplayName}() in {FilePath}:line {LineNumber}{Environment.NewLine}";
            }

            frameworkHandle.RecordEnd(subResult.TestCase, testResult.Outcome);
            frameworkHandle.RecordResult(subResult);

            // Try to find the failure from a subsection of this element.
            foreach (var section in element.Sections)
            {
                // Try to find a failure in this section.
                TryGetFailure(section, testResult, name + "\n");
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
                    var test = tests.Where((test_case) => test_case.DisplayName == xmlResult.Name).First();
                    var testResult = new TestResult(test);

                    // Add the test execution time provided by Catch to the result.
                    var testTime = xmlResult.Result.Duration;
                    testResult.Duration = TimeSpan.FromSeconds(Double.Parse(testTime, CultureInfo.InvariantCulture));

                    if (xmlResult.Result.Success == "true")
                    {
                        testResult.Outcome = TestOutcome.Passed;
                        frameworkHandle.RecordResult(testResult);
                    }
                    else
                    {
                        // Mark failure.
                        testResult.Outcome = TestOutcome.Failed;

                        // Parse the failure to a flat result.
                        TryGetFailure(xmlResult, testResult, "");
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