using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAdapterTest.Mocks
{
    public class MockTestExecutor : CatchTestAdapter.TestExecutor
    {
        public void MockComposeResults(IList<string> output_text, IList<TestCase> tests, IFrameworkHandle frameworkHandle)
        {
            base.ComposeResults(output_text, tests, frameworkHandle);
        }
    }
}
