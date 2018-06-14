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
using Microsoft.VisualStudio.TestWindow.Extensibility;
using System.Xml.XPath;
using System.IO;

namespace TestAdapter.Settings
{
    /// <summary>
    /// SettingsProvider used to register our interest in the "Catch" node.
    /// </summary>
    [Export( typeof( ISettingsProvider ) )]
    [Export( typeof( IRunSettingsService ) )]
    [SettingsName( CatchAdapterSettings.XmlRoot )]
    public class CatchSettingsProvider : ISettingsProvider, IRunSettingsService
    {
        /// <summary>
        /// Stored the last loaded settings, if any.
        /// </summary>
        public CatchAdapterSettings Settings { get; set; }

        public string Name => CatchAdapterSettings.XmlRoot;

        public CatchSettingsProvider()
        {
            // System.Diagnostics.Debugger.Launch();
        }

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
        public static CatchAdapterSettings LoadSettings(
            Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter.IRunSettings runSettings )
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
            System.Diagnostics.Debugger.Launch();

            // Try to find an existing catch configuration node.
            XPathNavigator navigator = inputRunSettingDocument.CreateNavigator();
            navigator.MoveToFirstChild();
            if( !navigator.MoveToChild( CatchAdapterSettings.XmlRoot, "" ) )
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

            // If there are no filters, add a default.
            if( settings.TestExeInclude.Count < 1 && settings.TestExeExclude.Count < 1 )
            {
                settings.TestExeInclude.Add( @"*\.Test\.exe" );
            }

            // Write the resolved settings to the xml.
            XPathNavigator settingsAsXml = settings.ToXml().CreateNavigator();
            settingsAsXml.MoveToFirstChild();

            navigator.MoveToRoot();
            navigator.MoveToFirstChild();
            navigator.AppendChild( settingsAsXml );

            // Clean up the navigator.
            navigator.MoveToRoot();
            return navigator;
        }
    }
}
