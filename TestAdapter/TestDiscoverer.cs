using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

using System.Text.RegularExpressions;
using System.Linq;
using TestAdapter.Settings;
using System.Xml.XPath;
using System.IO;

namespace Catch.TestAdapter
{
    [DefaultExecutorUri(TestExecutor.ExecutorUriString)]
    [FileExtension(".exe")]
    public class TestDiscoverer : ITestDiscoverer
    {
        public void DiscoverTests(IEnumerable<string> sources, IDiscoveryContext discoveryContext, IMessageLogger logger, ITestCaseDiscoverySink discoverySink)
        {
            logger.SendMessage(TestMessageLevel.Informational, "Catch Discover in process ...");

            // Load settings from the discovery context.
            CatchAdapterSettings settings = CatchSettingsProvider.LoadSettings( discoveryContext.RunSettings );
            logger.SendMessage( TestMessageLevel.Informational, "Inclusion patterns: " + String.Join( ",", settings.TestExeInclude ) );

            try
            {
                var slnRoot = GetSolutionDirectory(discoveryContext.RunSettings);
                foreach (var src in sources.Where(src => settings.IncludeTestExe(src)))
                {
                    logger.SendMessage(TestMessageLevel.Informational, $"Processing catch test source: '{src}'...");

                    var testCases = CreateTestCases(src, slnRoot);
                    foreach (var t in testCases)
                    {
                        discoverySink.SendTestCase(t);
                        logger.SendMessage(TestMessageLevel.Informational, $"Found TestCase Name={t.DisplayName}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Just log the error message.
                logger.SendMessage(TestMessageLevel.Error, ex.Message);
            }
        }

        public static string GetSolutionDirectory(IRunSettings settings)
        {
            XPathDocument document = new XPathDocument(new StringReader(settings.SettingsXml));
            XPathNavigator navigator = document.CreateNavigator();

            if (navigator.MoveToFollowing("SolutionDirectory", ""))
                return navigator.Value;
            return "";
        }

        public static string ResolvePath(string path, string root)
        {
            if (!System.IO.Path.IsPathRooted(path))
            {
                string maybePath = System.IO.Path.Combine(root, path);
                if (System.IO.File.Exists(maybePath))
                {
                    path = maybePath;
                }
            }
            return path;
        }

        public static IList<TestCase> CreateTestCases( string exeName, string slnRoot )
        {
            var testCases = new List<TestCase>();

            // Use the directory of the executable as the working directory.
            string workingDirectory = System.IO.Path.GetDirectoryName( exeName );

            var output = ProcessRunner.RunDiscoverProcess(exeName, "--list-tests --verbosity high", workingDirectory);
            foreach (var test in ParseListing( exeName, output ) )
            {
                test.CodeFilePath = ResolvePath(test.CodeFilePath, slnRoot);
                testCases.Add( test );
            }

            return testCases;
        }

        // Regular expression for separating file path and line number from catch output.
        static Regex lineInfoPattern = new Regex( @"(?<path>.*)\((?<line>\d+)\)$" );

        private static TestCase LineGroupToTestCase( string exeName, IList<string> lineGroup )
        {
            // The group needs to have at least three lines.
            if ( lineGroup.Count < 3 )
            {
                string lines = lineGroup.Aggregate( "", ( add, acc ) => add + "\n" + acc );
                throw new Exception( string.Format( "Unexpectedly few lines in catch output group: '{0}'", lines ) );
            }

            // Get the name.
            string name = lineGroup[ 0 ];

            // If the line info contains a long full path,
            // catch may have wrapped it to multiple lines.
            // We have to combine them into one.
            string lineInfoString = lineGroup[ 1 ];
            int lastLineInfoLine = 2;
            for ( ; lastLineInfoLine < lineGroup.Count; lastLineInfoLine++ )
            {
                // When we have something that looks like a source line information,
                // be satisfied with it. Otherwise append the next line to it.
                if ( lineInfoPattern.IsMatch( lineInfoString ) )
                    break;
                else
                    lineInfoString += lineGroup[ lastLineInfoLine ];
            }

            // Parse line info with regex.
            var lineInfoMatch = lineInfoPattern.Match( lineInfoString );
            if ( !lineInfoMatch.Success )
                throw new Exception(String.Format("Could not parse line info from '{0}'.", lineInfoString ) );

            string path = lineInfoMatch.Groups[ "path" ].Value;
            int lineNumber = Int32.Parse( lineInfoMatch.Groups[ "line" ].Value );

            // Construct the test.
            TestCase test = new TestCase(name, new Uri(TestExecutor.ExecutorUriString), exeName)
            {
                CodeFilePath = path,
                LineNumber = lineNumber,
                DisplayName = name
            };

            // Turn tags to traits.

            // Catch tags are enclosed in square brackets.
            Regex tagPattern = new Regex( @"\[([^]]+)\]" );

            // Look at all lines after the line info.
            for( int i = lastLineInfoLine + 1; i < lineGroup.Count; ++i )
            {
                // Find all things that look like catch tags.
                foreach( Match match in tagPattern.Matches( lineGroup[ i ] ) )
                {
                    // Create the tags as traits with empty values.
                    string tag = match.Groups[ 1 ].Value;
                    test.Traits.Add( new Trait( tag, "" ) );
                }
            }

            return test;
        }

        private static IEnumerable<TestCase> ParseListing( string exeName, IList<string> lines )
        {
            // We manually enumerate through the lines of the output.
            IEnumerator<string> line = lines.GetEnumerator();

            // The first line should be fixed.
            if( !line.MoveNext() || line.Current != "All available test cases:" )
            {
                yield break;
                //throw new Exception( "Unexpected line in catch output: " + line.Current );
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

                // Add the line to the current group.
                currentGroup.Add( line.Current.Trim() );

                // Remember indent.
                lastIndent = indent;
            }

            // Yield the final group.
            yield return LineGroupToTestCase( exeName, currentGroup );
        }
    }
}