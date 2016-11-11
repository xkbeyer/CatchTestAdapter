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
                var p = new ProcessRunner(test.Source, "-r compact \"" + test.DisplayName + "\"");
                var testResult = new TestResult(test);
                testResult.Outcome = TestOutcome.Passed;
                foreach (var s in p.Output)
                {
                    if (s.Length != 0)
                    {
                        var msg = new TestResultMessage(TestResultMessage.StandardOutCategory, s + "\n");
                        testResult.Messages.Add(msg);
                        if( s.Contains("failed:"))
                        {
                            string srcline = s.Remove(s.LastIndexOf(": failed:"));
                            string pattern = @"\(\d+\)";
                            Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
                            MatchCollection matches = rgx.Matches(srcline);
                            string lineno = "";
                            if( matches.Count != 0)
                            {
                                lineno = matches[0].Value.Trim('(',')');
                            }

                            string errtxt = s.Remove(0, s.LastIndexOf("failed:"));
                            var errmsg = new TestResultMessage(TestResultMessage.StandardErrorCategory, s + "\n");
                            testResult.Messages.Add(errmsg);
                            testResult.ErrorMessage += errtxt + "\n";
                            testResult.ErrorStackTrace += "at " + test.DisplayName + " in " +srcline.Remove(s.IndexOf("(")) + ": Line "+ lineno + "\n";
                            testResult.Outcome = TestOutcome.Failed;
                        }
                        frameworkHandle.SendMessage(TestMessageLevel.Informational, s);
                    }
                }
                frameworkHandle.RecordResult(testResult);
            }
        }
    }
}