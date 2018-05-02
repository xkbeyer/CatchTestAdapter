using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestAdapterTest.Mocks
{
    class MockRunSettings : IRunSettings
    {
        public string SettingsXml => throw new NotImplementedException();

        public ISettingsProvider GetSettings( string settingsName )
        {
            return this.Provider;
        }

        public ISettingsProvider Provider { get; set; }
    }
}
