using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestWindow;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using TestAdapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

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
                var found = CreateTestCases(src);
                foreach (var t in found)
                {
                    discoverySink.SendTestCase(t);
                    logger.SendMessage(TestMessageLevel.Informational, t.DisplayName);
                }
            }
        }

        public IList<TestCase> CreateTestCases(string exeName)
        {
            var testCases = new List<TestCase>();
            var p = new ProcessRunner(exeName, "--list-test-names-only");
            foreach(var line in p.Output)
            {
                testCases.Add(new TestCase(line, TestExecutor.ExecutorUri, exeName));
            }
            return testCases;
        }
    }
}