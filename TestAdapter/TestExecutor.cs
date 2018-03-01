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
                        }
                    }
                }
                frameworkHandle.RecordResult(testResult);
            }
        }
    }
}