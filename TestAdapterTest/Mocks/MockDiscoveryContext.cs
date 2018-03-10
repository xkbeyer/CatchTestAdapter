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
                throw new NotImplementedException();
            }
        }
    }
}
