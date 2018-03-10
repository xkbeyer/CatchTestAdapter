using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestAdapterTest.Mocks
{
    class MockTestCaseDiscoverySink : ITestCaseDiscoverySink
    {
        public IList<TestCase> Tests { get; } = new List<TestCase>();

        public void SendTestCase( TestCase discoveredTest )
        {
            this.Tests.Add( discoveredTest );
        }
    }
}
