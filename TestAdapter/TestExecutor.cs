using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestWindow;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CatchTestAdapter
{
    [ExtensionUri(ExecutorUriString)]
    public class TestExecutor : ITestExecutor
    {
        public const string ExecutorUriString = "executor://CatchTestRunner/v1";
        public static readonly Uri ExecutorUri = new Uri(ExecutorUriString);
        private string exe;

        public void Cancel()
        {
            throw new NotImplementedException();
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Informational, "RunTest with source " + sources.First());
            foreach(var exeName in sources)
            {
                var  tests = TestDiscoverer.CreateTestCases(exeName);
                RunTests(tests, runContext, frameworkHandle);
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
            if( element.Name == "Section" || element.Name == "TestCase" )
            {
                string name = element.Attribute( "name" ).Value;

                // Try to find the failure from this element.
                foreach( var expression in element.Elements("Expression") )
                {
                    if ( expression.Attribute( "success" ).Value == "false" )
                    {
                        string expanded = expression.Element( "Expanded" ).Value;
                        result = new FlatResult() {
                            SectionPath = name,
                            Expression = expanded,
                            LineNumber = Int32.Parse( expression.Attribute("line").Value ),
                            FilePath = expression.Attribute("filename").Value
                        };
                        return true;
                    }
                }

                // Try to find the failure from a subsection of this element.
                foreach( var section in element.Elements("Section") )
                {
                    if( TryGetFailure( section, out result ) )
                    {
                        result.SectionPath = name + "\n" + result.SectionPath;
                        return true;
                    }
                }
            }

            // Return dummy result if not found.
            result = new FlatResult() {
                SectionPath = "[Not found]",
                Expression = "N/A",
                LineNumber = -1,
                FilePath = "" };
            return false;
        }

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
            frameworkHandle.SendMessage(TestMessageLevel.Informational, "RunTest with test cases " + tests);
            foreach (var test in tests)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, test.DisplayName);
                var p = new ProcessRunner(test.Source, "-r xml \"" + test.DisplayName + "\"");

                // Output as a single string.
                string output = p.Output.Aggregate( "", ( acc, add ) => acc + add );

                // Output as an XML document.
                XDocument doc = XDocument.Parse( output );

                // Process the output.
                var testResult = new TestResult( test );
                foreach ( var group in doc.Element("Catch").Elements("Group") )
                {
                    foreach( var testCase in group.Elements( "TestCase" ) )
                    {
                        XElement result = testCase.Element( "OverallResult" );
                        if( result.Attribute("success" ).Value.ToLowerInvariant() == "true" )
                        {
                            testResult.Outcome = TestOutcome.Passed;
                        }
                        else
                        {
                            testResult.Outcome = TestOutcome.Failed;
                            FlatResult failure = GetFlatFailure( testCase );
                            testResult.ErrorMessage = failure.SectionPath + "\n" + failure.Expression;

                            testResult.ErrorStackTrace = String.Format( "at {0}() in {1}:line {2}\n",
                                test.DisplayName,
                                failure.FilePath,
                                failure.LineNumber );
                        }
                    }
                }
                frameworkHandle.RecordResult(testResult);
            }
        }
    }
}