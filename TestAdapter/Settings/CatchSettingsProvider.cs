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

        [ImportingConstructor]
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
            // System.Diagnostics.Debugger.Launch();

            // First read settings from files.
            foreach(var file in FindSettingsInFoldersAbove( configurationInfo.SolutionDirectory, log ) )
            {
                try
                {
                    // Try to find settings from the file.
                    var settingsFromFile = MaybeReadSettingsFromFile( file );
                    if( settingsFromFile != null )
                    {
                        log.Log( MessageLevel.Informational, $"Reading test run settings from {file}." );
                        settings.MergeFrom( settingsFromFile );
                    }
                }
                catch(IOException ex )
                {
                    log.Log( MessageLevel.Warning,
                        $"Failed to read test run settings from file '{file}'. Exception: {ex.ToString()}" );
                }
            }

            // Try to find an existing catch configuration node.
            var settingsFromContext = MaybeReadSettingsFromXml( inputRunSettingDocument );
            XPathNavigator navigator = inputRunSettingDocument.CreateNavigator();
            if( settingsFromContext == null )
            {
                log.Log( MessageLevel.Informational, $"No '{CatchAdapterSettings.XmlRoot}' node in runsettings." );
            }
            else
            {
                // Merge the settings from the context.
                settings.MergeFrom( settingsFromContext );

                // Erase the original.
                if( navigator.MoveToFollowing( CatchAdapterSettings.XmlRoot, "" ) )
                    navigator.DeleteSelf();
            }

            // If there are no filters, add a default.
            if( settings.TestExeInclude.Count < 1 && settings.TestExeExclude.Count < 1 )
            {
                settings.TestExeInclude.Add( @"\.Test\.exe" );
            }

            // Write the resolved settings to the xml.
            XPathNavigator settingsAsXml = settings.ToXml().CreateNavigator();

            navigator.MoveToRoot();
            navigator.MoveToFirstChild();
            navigator.AppendChild( settingsAsXml );

            // Clean up the navigator.
            navigator.MoveToRoot();
            return navigator;
        }

        private CatchAdapterSettings MaybeReadSettingsFromFile( string filename )
        {
            XPathDocument doc = new XPathDocument( filename );
            return MaybeReadSettingsFromXml( doc );
        }

        private CatchAdapterSettings MaybeReadSettingsFromXml( IXPathNavigable settingSource )
        {
            XPathNavigator navigator = settingSource.CreateNavigator();

            if( navigator.MoveToFollowing( CatchAdapterSettings.XmlRoot, "" ) )
            {

                // Catch adapter settings found. Try to read them.
                XmlReader reader = XmlReader.Create( new MemoryStream( Encoding.UTF8.GetBytes( navigator.OuterXml ) ) );
                XmlSerializer serializer = new XmlSerializer( typeof( CatchAdapterSettings ) );
                return serializer.Deserialize( reader ) as CatchAdapterSettings;
            }
            else
            {
                // No settings found.
                return null;
            }
        }

        /// <summary>
        /// Finds all runsettings files in folders above the provided one.
        /// The files closest to root are returned first.
        /// </summary>
        /// <param name="initialFolder"></param>
        /// <returns></returns>
        IEnumerable<string> FindSettingsInFoldersAbove( string initialFolder,
            ILogger log )
        {
            // Get the full path.
            string fullPath = Path.GetFullPath( initialFolder );

            // Split the path to components.
            var pathComponents = fullPath.Split( new char[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries );

            // Append the path components to each other to process each intermediate folder.
            var currentPath = "";
            foreach(string component in pathComponents )
            {
                currentPath = currentPath + component + Path.DirectorySeparatorChar;
                IEnumerable<string> files = new string[0];
                try
                {
                    files = Directory.EnumerateFiles( currentPath, "*.runsettings" ).ToArray();
                }
                catch(IOException ex)
                {
                    // We may not have permission or something. Ignore silently.
                    log.Log( MessageLevel.Informational,
                        $"Error looking for settings at path {currentPath}: {ex.ToString()}." );
                }

                // Yield the found files.
                foreach( string file in files )
                {
                    yield return file;
                }
            }
        }
    }
}
