using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAdapterTest.Mocks
{
    class MockRunContext : IRunContext
    {
        public bool KeepAlive => true;

        public bool InIsolation => false;

        public bool IsDataCollectionEnabled => false;

        public bool IsBeingDebugged => false; //System.Diagnostics.Debugger.IsAttached;

        public string TestRunDirectory => throw new NotImplementedException();

        public string SolutionDirectory { get; } = "./";

        public IRunSettings RunSettings => this.MockSettings;

        public ITestCaseFilterExpression GetTestCaseFilter( IEnumerable<string> supportedProperties, Func<string, TestProperty> propertyProvider )
        {
            throw new NotImplementedException();
        }

        public MockRunSettings MockSettings { get; set; } = new MockRunSettings();
    }
}
