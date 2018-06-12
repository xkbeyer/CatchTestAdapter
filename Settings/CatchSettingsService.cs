using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.XPath;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using TestAdapter.Settings;

namespace CatchTestAdapter.Settings
{
    /// <summary>
    /// Implement and export IRunSettingsService to provide RunSettings
    /// for test adapters.
    /// </summary>
    [Export( typeof( IRunSettingsService ) )]
    [SettingsName( CatchAdapterSettings.XmlRoot )]
    class CatchSettingsService : IRunSettingsService
    {
        public string Name => CatchAdapterSettings.XmlRoot;

        /// <summary>
        /// I believe this will be called to augment the run settings.
        /// </summary>
        /// <param name="inputRunSettingDocument"></param>
        /// <param name="configurationInfo"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        public IXPathNavigable AddRunSettings(
            IXPathNavigable inputRunSettingDocument,
            IRunSettingsConfigurationInfo configurationInfo,
            ILogger log )
        {
            // This shall contain the merged settings.
            CatchAdapterSettings settings = new CatchAdapterSettings();

            // Try to find an existing catch configuration node.
            XPathNavigator navigator = inputRunSettingDocument.CreateNavigator();
            if( ! navigator.MoveToChild( CatchAdapterSettings.XmlRoot, "") )
            {
                log.Log( MessageLevel.Informational, $"No '{CatchAdapterSettings.XmlRoot}' node in runsettings." );
            }
            else
            {
                // Catch adapter settings found. Try to read them.
                XmlReader reader = XmlReader.Create( new MemoryStream( Encoding.UTF8.GetBytes( navigator.OuterXml ) ) );
                XmlSerializer serializer = new XmlSerializer( typeof( CatchAdapterSettings ) );
                settings = serializer.Deserialize( reader ) as CatchAdapterSettings ?? settings;

                // Erase the original.
                navigator.DeleteSelf();
            }

            // Write the resolved settings to the xml.
            XPathNavigator settingsAsXml = settings.ToXml().CreateNavigator();
            navigator.AppendChild( settingsAsXml );

            // Clean up the navigator.
            navigator.MoveToRoot();
            return navigator;
        }
    }
}
