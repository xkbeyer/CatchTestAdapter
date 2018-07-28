﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Xml;
using System.Xml.Linq;
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

        /// <summary>
        /// Describes the result of an expression, with the section path flattened to a string.
        /// </summary>
        struct FlatResult
        {
            public string SectionPath;
            public string Expression;
            public int LineNumber;
            public string FilePath;
        }
        private List<FlatResult> result = new List<FlatResult>();
        /// <summary>
        /// Tries to find a failure in the section tree of a test case.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="name"></param>
        void TryGetFailure(Tests.TestCase element, string name)
        {
            // Get current level's name.
            name += element.Name;
            // Try to find the failure from this element.
            foreach (var expression in element.Expressions)
            {
                // Map the failure to a flat result.
                if (expression.Success == "false")
                {
                    string expanded = expression.Expanded.Trim();
                    string original = expression.Original.Trim();
                    string type = expression.Type;
                    var res = new FlatResult()
                    {
                        // The path will be expanded by preceding stack frames.
                        SectionPath = name,
                        Expression = $"{type}({original}) with expansion: ({expanded})",
                        LineNumber = Int32.Parse(expression.Line),
                        FilePath = TestDiscoverer.ResolvePath(expression.Filename, SolutionDirectory)
                    };
                    result.Add(res);
                }
            }

            if( element.Warning != null )
            {
                foreach (var s in element.Warning)
                {
                    var res = new FlatResult()
                    {
                        SectionPath = name,
                        Expression = $"WARN: {s.Trim()}{ Environment.NewLine }",
                        FilePath = TestDiscoverer.ResolvePath(element.Filename, SolutionDirectory)
                    };
                    result.Add(res);
                }
            }

            if (element.Info != null)
            {
                foreach(var s in element.Info)
                {
                    var res = new FlatResult()
                    {
                        SectionPath = name,
                        Expression = $"INFO: {s.Trim()}{ Environment.NewLine }",
                        FilePath = TestDiscoverer.ResolvePath(element.Filename, SolutionDirectory)
                    };
                    result.Add(res);
                }
            }

            // Try to find the failure from a subsection of this element.
            foreach (var section in element.Sections)
            {
                // Try to find a failure in this section.
                TryGetFailure(section, name + "\n");
            }

            // Check if this element is a failure generated by FAIL().
            if (element.Failure != null)
            {
                var res = new FlatResult()
                {
                    SectionPath = name,
                    Expression = $"FAIL({element.Failure.text.Trim()})",
                    LineNumber = Int32.Parse(element.Failure.Line),
                    FilePath = TestDiscoverer.ResolvePath(element.Failure.Filename, SolutionDirectory)
                };
                result.Add(res);
            }
        }

        /// <summary>
        /// Finds a failure in a test case and flattens the section path that leads to it.
        /// </summary>
        /// <param name="testCase"></param>
        /// <returns>A list of all failure found.</returns>
        List<FlatResult> GetFlatFailure(Tests.TestCase testCase)
        {
            result.Clear();
            TryGetFailure(testCase, "");
            return result;
        }

        public void RunTests(IEnumerable<TestCase> testsToRun, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
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
        /// Reports all results from the XML output to the framework.
        /// </summary>
        /// <param name="output_text">The text lines of the output</param>
        /// <param name="tests">List of tests from the discoverer</param>
        /// <param name="frameworkHandle"></param>
        protected virtual void ComposeResults(IList<string> output_text, IList<TestCase> tests, IFrameworkHandle frameworkHandle)
        {
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
                    }
                    else
                    {
                        // Mark failure.
                        testResult.Outcome = TestOutcome.Failed;

                        // Parse the failure to a flat result.
                        List<FlatResult> failures = GetFlatFailure(xmlResult);
                        testResult.ErrorMessage = $"{Environment.NewLine}";
                        for (int i = 1; i <= failures.Count; ++i)
                        {
                            var failure = failures[i - 1];
                            if (failure.Expression.Contains("WARN:") || failure.Expression.Contains("INFO:"))
                            {
                                testResult.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, failure.Expression));
                                continue;
                            }
                            // Populate the error message.
                            var newline = failure.SectionPath.IndexOf("\n");
                            if (newline != -1)
                            {
                                // Remove first line of the SectionPath, which is the test case name.
                                failure.SectionPath = failure.SectionPath.Substring(failure.SectionPath.IndexOf("\n") + 1);
                                testResult.ErrorMessage += $"#{i} - {failure.SectionPath}{Environment.NewLine}{failure.Expression}{Environment.NewLine}";
                            }
                            else
                            {
                                testResult.ErrorMessage += $"#{i} - {failure.Expression}{Environment.NewLine}";
                            }
                            // And the error stack.
                            testResult.ErrorStackTrace += $"at #{i} - {test.DisplayName}() in {failure.FilePath}:line {failure.LineNumber}{Environment.NewLine}";
                        }
                    }

                    // Finally record the result.
                    frameworkHandle.RecordResult(testResult);

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