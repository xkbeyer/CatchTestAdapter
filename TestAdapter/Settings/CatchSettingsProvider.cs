using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using System.Xml.Serialization;

namespace TestAdapter.Settings
{
    /// <summary>
    /// SettingsProvider used to register our interest in the "Catch" node.
    /// </summary>
    [Export( typeof( ISettingsProvider ) )]
    [SettingsName( CatchAdapterSettings.XmlRoot )]
    public class CatchSettingsProvider : ISettingsProvider
    {
        /// <summary>
        /// Stored the last loaded settings, if any.
        /// </summary>
        public CatchAdapterSettings Settings { get; set; }

        [ImportingConstructor]
        public CatchSettingsProvider() { }

        /// <summary>
        /// Load the settings from xml.
        /// </summary>
        /// <param name="reader">Reader pointed at the root node of the adapter settings node.</param>
        public void Load( XmlReader reader )
        {
            // Check that the node is correct.
            if(reader.Read() && reader.Name == CatchAdapterSettings.XmlRoot )
            {
                // Deserialize the settings.
                var deserializer = new XmlSerializer( typeof(CatchAdapterSettings) );
                this.Settings = deserializer.Deserialize( reader ) as CatchAdapterSettings;
            }
        }

        /// <summary>
        /// Loads settings from the runsettings in the discovery context.
        /// Returns default settings otherwise.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static CatchAdapterSettings LoadSettings(IRunSettings runSettings )
        {
            CatchAdapterSettings settings = new CatchAdapterSettings();

            if( runSettings != null )
            {
                var provider = runSettings.GetSettings( CatchAdapterSettings.XmlRoot ) as CatchSettingsProvider;
                if( provider != null && provider.Settings != null )
                {
                    settings = provider.Settings;
                }
            }

            return settings;
        }
    }
}
