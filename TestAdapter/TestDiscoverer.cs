using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestWindow;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Text.RegularExpressions;
using System.Linq;

namespace CatchTestAdapter
{
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    [FileExtension(".exe")]
    public class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger.SendMessage(TestMessageLevel.Informational, "Catch Discover in process ...");
            //System.Diagnostics.Debugger.Launch();
            foreach (var src in sources)
            {
                var testCases = CreateTestCases(src);
                foreach (var t in testCases)
                {
                    discoverySink.SendTestCase(t);
                    logger.SendMessage(TestMessageLevel.Informational, t.DisplayName);
                }
            }
        }


        public static IList<TestCase> CreateTestCases( string exeName )
        {
            var testCases = new List<TestCase>();
            var p = new ProcessRunner(exeName, "--list-tests --verbosity high");

            foreach (var test in ParseListing( exeName, p.Output ) )
            {
                testCases.Add( test );
            }

            return testCases;
        }

        // Regular expression for separating file path and line number from catch output.
        static Regex lineInfoPattern = new Regex( @"(?<path>.*)\((?<line>\d+)\)" );

        private static TestCase LineGroupToTestCase( string exeName, IList<string> lineGroup )
        {
            // The group needs to have at least three lines.
            if ( lineGroup.Count < 3 )
            {
                string lines = lineGroup.Aggregate( "", ( add, acc ) => add + "\n" + acc );
                throw new Exception( string.Format( "Unexpectedly few lines in catch output group: '{0}'", lines ) );
            }

            // Get the name.
            string name = lineGroup[ 0 ].Trim();

            // Parse line info with regex.
            string lineInfoString = lineGroup[ 1 ].Trim();
            var match = lineInfoPattern.Match( lineInfoString );
            if ( !match.Success )
                throw new Exception(String.Format("Could not parse line info from '{0}'.", lineInfoString ) );

            string path = match.Groups[ "path" ].Value;
            int lineNumber = Int32.Parse( match.Groups[ "line" ].Value );

            // Construct the test.
            TestCase test = new TestCase( name, new Uri( TestExecutor.ExecutorUriString ), exeName );
            test.CodeFilePath = path;
            test.LineNumber = lineNumber;
            return test;
        }

        private static IEnumerable<TestCase> ParseListing( string exeName, IList<string> lines )
        {
            // We manually enumerate through the lines of the output.
            IEnumerator<string> line = lines.GetEnumerator();

            // The first line should be fixed.
            if( !line.MoveNext() || line.Current != "All available test cases:" )
            {
                throw new Exception( "Unexpected line in catch output: " + line.Current );
            }

            // Split output to groups of lines related to the same test case.
            // We detect groups by indentation.
            var currentGroup = new List<string>();
            int lastIndent = 0;
            while( line.MoveNext() )
            {
                // A new group begins when indent drops.
                // If there is no indent, we are at the end.
                int indent = line.Current.Length - line.Current.TrimStart().Length;
                if ( indent > 0 && indent < lastIndent )
                {
                    // Yield the finished group.
                    yield return LineGroupToTestCase( exeName, currentGroup );

                    // Begin a new group.
                    currentGroup = new List<string>();
                }
                lastIndent = indent;

                // Add the line to the current group.
                currentGroup.Add( line.Current );
            }

            // Yield the final group.
            yield return LineGroupToTestCase( exeName, currentGroup );
        }
    }
}