using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using TestAdapter.Settings;

namespace CatchTestAdapter.Settings
{
    [Export( typeof( IRunSettingsService ) )]
    [SettingsName( CatchAdapterSettings.XmlRoot )]
    class CatchSettingsService : IRunSettingsService
    {
        public string Name => CatchAdapterSettings.XmlRoot;

        public IXPathNavigable AddRunSettings( IXPathNavigable inputRunSettingDocument, IRunSettingsConfigurationInfo configurationInfo, ILogger log )
        {
            throw new NotImplementedException();
        }
    }
}
