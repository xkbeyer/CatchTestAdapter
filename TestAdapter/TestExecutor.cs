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
        public const string ExecutorUriString = "executor://CatchTestRunner";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private string SolutionDirectory { get; set; } = "";
        private IFrameworkHandle frameworkHandle = null;
        private IList<TestResult> results = new List<TestResult>();

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Runs the test of an given executable. This implies to run the discover for these sources.
        /// </summary>
        /// <param name="sources">The full qualified name of an executable to run</param>
        /// <param name="runContext">Test context</param>
        /// <param name="frameworkHandle">Test frame work handle</param>
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

        /// <summary>
        /// Run all given test cases.
        /// </summary>
        /// <param name="testsToRun">List of test cases</param>
        /// <param name="runContext">Run context</param>
        /// <param name="frameworkHandle">Test frame work handle</param>
        public void RunTests(IEnumerable<TestCase> testsToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            SolutionDirectory = runContext.SolutionDirectory;
            this.frameworkHandle = frameworkHandle;

            var tests = testsToRun.ToList();
#if DEBUG
            frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Run tests :");
            foreach (var t in tests) {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"\tDisplayName={t.DisplayName} Full={t.FullyQualifiedName} ID={t.Id}");
            }
#endif
            var timer = Stopwatch.StartNew();

            var listOfExes = tests.Select(t => t.Source).Distinct();
            foreach(var CatchExe in listOfExes)
            {

                var listOfTestCasesOfSource = from test in tests where test.Source == CatchExe select test.DisplayName;
                var listOfTestCases = listOfTestCasesOfSource.Aggregate("", (acc, name) => acc + name + Environment.NewLine);

#if DEBUG
                frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Run {CatchExe} with Tests:{Environment.NewLine}{listOfTestCases}");
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

                ComposeResults(output_text, tests.ToList(), frameworkHandle);

                // Remove the temporary input file.
                System.IO.File.Delete(caseFile);
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
                    foreach (var r in results)
                    {
#if DEBUG
                        frameworkHandle.SendMessage(TestMessageLevel.Informational, $"Report TestResult {r.DisplayName}");
#endif
                        frameworkHandle.RecordResult(r);
                    }

                    // And remove the test from the list of outstanding tests
                    vsTests.Remove(test);
                }
            }
            catch (InvalidOperationException ex)
            {
                if (vsTests.Count != 0)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Error, $"While running test {vsTests.First().Source}, caught exception in adapter: {ex}");
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