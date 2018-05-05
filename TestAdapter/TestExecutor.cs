﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Xml.Linq;
using System.Globalization;
using System.Diagnostics;

using TestAdapter.Settings;

namespace CatchTestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://CatchTestRunner/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // Load settings from the context.
            var settings = CatchSettingsProvider.LoadSettings( runContext.RunSettings );

            frameworkHandle.SendMessage(TestMessageLevel.Informational, "CatchAdapter::RunTests... " );

            // Run tests in all included executables.
            foreach ( var exeName in sources.Where( name => settings.IncludeTestExe( name ) ) )
            {
                // Wrap execution in try to stop one executable's exceptions from stopping the others from being run.
                try
                {
                    frameworkHandle.SendMessage( TestMessageLevel.Informational, "RunTest with source " + exeName );
                    var tests = TestDiscoverer.CreateTestCases( exeName );
                    RunTests( tests, runContext, frameworkHandle );
                }
                catch ( Exception ex )
                {
                    frameworkHandle.SendMessage( TestMessageLevel.Error, "Exception running tests: " + ex.Message );
                    frameworkHandle.SendMessage( TestMessageLevel.Error, "Exception stack: " + ex.StackTrace );
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

        /// <summary>
        /// Tries to find a failure in the section tree of a test case.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        bool TryGetFailure( XElement element, out FlatResult result )
        {
            // Parse test cases and their sections.
            if( element.Name == "Section" || element.Name == "TestCase" )
            {
                // Get current level's name.
                string name = element.Attribute( "name" ).Value;

                // Try to find the failure from this element.
                foreach( var expression in element.Elements("Expression") )
                {
                    // Map the failure to a flat result.
                    if ( expression.Attribute( "success" ).Value == "false" )
                    {
                        string expanded = expression.Element( "Expanded" ).Value;
                        string original = expression.Element( "Original" ).Value;
                        string type = expression.Attribute( "type" ).Value;
                        result = new FlatResult() {
                            // The path will be expanded by preceding stack frames.
                            SectionPath = name,
                            Expression = String.Format( CultureInfo.InvariantCulture, "{0} {1} => {2}", type, original, expanded),
                            LineNumber = Int32.Parse( expression.Attribute("line").Value ),
                            FilePath = expression.Attribute("filename").Value
                        };
                        return true;
                    }
                }

                // Try to find the failure from a subsection of this element.
                foreach( var section in element.Elements("Section") )
                {
                    // Try to find a failure in this section.
                    if( TryGetFailure( section, out result ) )
                    {
                        // If found, add the current section to the path and return it.
                        result.SectionPath = name + "\n" + result.SectionPath;
                        return true;
                    }
                }

                // Check if this element is a failure generated by FAIL().
                var fail = element.Element("Failure");
                if(fail != null && !fail.IsEmpty)
                {
                    result = new FlatResult()
                    {
                        SectionPath = name,
                        Expression = fail.Value,
                        LineNumber = Int32.Parse(fail.Attribute("line").Value),
                        FilePath = fail.Attribute("filename").Value
                    };
                    return true;
                }
            }

            // Return a dummy result if no failure found.
            result = new FlatResult() {
                SectionPath = "[Not found]",
                Expression = "N/A",
                LineNumber = -1,
                FilePath = "" };
            return false;
        }

        /// <summary>
        /// Finds a failure in a test case and flattens the section path that leads to it.
        /// </summary>
        /// <param name="testCase"></param>
        /// <returns></returns>
        FlatResult GetFlatFailure( XElement testCase )
        {
            FlatResult result;
            if ( TryGetFailure( testCase, out result ) )
                return result;
            else
                throw new Exception( "Could not find failure " + testCase.ToString() );
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            var CatchExe = tests.First().Source;
            var timer = Stopwatch.StartNew();
            
            // Get a list of all test case names
            var listOfTestCases = tests.Aggregate("", (acc, test) => acc + test.DisplayName + "\n");
            
            // Write them to the input file for Catch runner
            System.IO.File.WriteAllText(CatchExe + ".testcases", listOfTestCases);

            // Execute the tests
            IList<string> output_text;

            string arguments = "-r xml --durations yes --input-file " + CatchExe + ".testcases";
            if ( runContext.IsBeingDebugged )
            {
                output_text = ProcessRunner.RunDebugProcess( frameworkHandle, CatchExe, arguments );
            }
            else
            {
                output_text = ProcessRunner.RunProcess( CatchExe, arguments );
            }

            timer.Stop();
            frameworkHandle.SendMessage(TestMessageLevel.Informational, "Overall time " + timer.Elapsed.ToString());

            // Output as a single string.
            string output = output_text.Aggregate("", (acc, add) => acc + add);

            // Output as an XML document.
            XDocument doc = XDocument.Parse(output);

            // Process the output.
            foreach(var group in doc.Element("Catch").Elements("Group"))
            {
                foreach(var testCase in group.Elements("TestCase"))
                {
                    // Find the matching test case
                    var test = tests.Where((test_case) => test_case.DisplayName == testCase.Attribute("name").Value).ElementAt(0);

                    var testResult = new TestResult(test);
                    XElement result = testCase.Element("OverallResult");
                    if(result.Attribute("success").Value.ToLowerInvariant() == "true")
                    {
                        testResult.Outcome = TestOutcome.Passed;
                    }
                    else
                    {
                        // Mark failure.
                        testResult.Outcome = TestOutcome.Failed;

                        // Parse the failure to a flat result.
                        FlatResult failure = GetFlatFailure(testCase);

                        // Populate the test result.
                        testResult.ErrorMessage = failure.SectionPath + "\n" + failure.Expression;
                        testResult.ErrorStackTrace = String.Format(CultureInfo.InvariantCulture, "at {0}() in {1}:line {2}\n",
                            test.DisplayName,
                            failure.FilePath,
                            failure.LineNumber);
                    }

                    // Add the test execution time provided by Catch to the result.
                    var testTime = result.Attribute("durationInSeconds").Value;
                    testResult.Duration = TimeSpan.FromSeconds(Double.Parse(testTime, CultureInfo.InvariantCulture));
                    
                    // Finally record the result.
                    frameworkHandle.RecordResult(testResult);
                }
            }
            // Remove the temporary input file.
            System.IO.File.Delete(CatchExe + ".testcases");
        }
    }
}