using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace TestAdapterTest.Mocks
{
    class MockDiscoveryContext : IDiscoveryContext
    {
        public IRunSettings RunSettings
        {
            get
            {
                return this.MockSettings;
            }
        }

        public MockRunSettings MockSettings { get; set; } = new MockRunSettings();
    }
}
