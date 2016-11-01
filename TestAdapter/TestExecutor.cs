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
            frameworkHandle.SendMessage(TestMessageLevel.Informational, "RunTest with source " + sources.First());
            foreach(var exeName in sources)
            {
                var tests = TestDiscoverer.CreateTestCases(exeName);
                foreach (var test in tests)
                {
                    frameworkHandle.SendMessage(TestMessageLevel.Informational, test.DisplayName);
                }
            }
            throw new NotImplementedException("RunTests with sources.");
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            frameworkHandle.SendMessage(TestMessageLevel.Informational, "RunTest with test cases " + tests);
            foreach (var test in tests)
            {
                frameworkHandle.SendMessage(TestMessageLevel.Informational, test.DisplayName);
            }
            throw new NotImplementedException("RunTests with TestCases.");
        }
    }
}