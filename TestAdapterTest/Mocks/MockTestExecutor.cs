using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Collections.Generic;

namespace TestAdapterTest.Mocks
{
    public class MockTestExecutor : Catch.TestAdapter.TestExecutor
    {
        public void MockComposeResults(IList<string> output_text, IList<TestCase> tests, IFrameworkHandle frameworkHandle)
        {
            base.ComposeResults(output_text, tests, frameworkHandle);
        }
    }
}
