using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestWindow;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Text.RegularExpressions;

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


        public static IList<TestCase> CreateTestCases(string exeName)
        {
            var testCases = new List<TestCase>();
            var p = new ProcessRunner(exeName, "--list-test-names-only");

            foreach (var line in p.Output)
            {
                testCases.Add(Generate(exeName, line));
            }

            return testCases;
        }

        enum scan_state { init, deli, source };

        private static TestCase Generate(string exeName, string testName)
        {
            var p = new ProcessRunner(exeName, "-s \"" + testName + "\"");
            scan_state state = scan_state.init;
            foreach (var line in p.Output)
            {
                switch (state)
                {
                    case scan_state.init:
                        if (line == testName)
                        {
                            state = scan_state.deli;
                        }
                        break;
                    case scan_state.deli:
                        if (line.StartsWith("----"))
                        {
                            state = scan_state.source;
                        }
                        break;
                    case scan_state.source:
                        string pattern = @"\(\d+\)";
                        Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                        MatchCollection matches = rgx.Matches(line);
                        string lineno = "";
                        if (matches.Count != 0)
                        {
                            lineno = matches[0].Value.Trim('(', ')');
                            string source = line.Remove(line.IndexOf('('));
                            var test = new TestCase(testName, TestExecutor.ExecutorUri, exeName);
                            test.CodeFilePath = source;
                            test.LineNumber = Int32.Parse(lineno);
                            return test;
                        }
                        break;
                }
            }
            return null;
        }
    }
}