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
using EnvDTE;

namespace TestAdapter.Settings
{
    /// <summary>
    /// Provides an IRunSettingsService to provide Catch settings
    /// even when a runsettings file is not manually specified.
    /// </summary>
    [Export(typeof(IRunSettingsService))]
    [SettingsName(CatchAdapterSettings.XmlRoot)]
    public class CatchSettingsService : IRunSettingsService
    {

        /// <summary>
        /// IRunSettingsService name.
        /// </summary>
        public string Name => CatchAdapterSettings.XmlRoot;

        private DTE dte;

        /// <summary>
        /// Must have explicit constructor with the ImportingConstructorAttribute
        /// for Visual Studio to successfully initialize the RunSettingsService.
        /// </summary>
        [ImportingConstructor]
        public CatchSettingsService(Microsoft.VisualStudio.Shell.SVsServiceProvider serviceProvider)
        {
            this.dte = (DTE)serviceProvider.GetService(typeof(DTE));
        }

        /// <summary>
        /// Visual studio calls this method on the IRunSettingsService to collect
        /// run settings for tests adapters.
        /// 
        /// The settings are serialized as XML, because they need to be passed to the
        /// test host service across a process boundary.
        /// </summary>
        /// <param name="inputRunSettingDocument">Pre-existing run settings. Defaults or from a manually specified runsettings file.</param>
        /// <param name="configurationInfo">Contextual information on the test run.</param>
        /// <param name="log">Logger.</param>
        /// <returns>The entire settings document, as it should be after our modifications.</returns>
        public IXPathNavigable AddRunSettings(
            IXPathNavigable inputRunSettingDocument,
            IRunSettingsConfigurationInfo configurationInfo,
            ILogger log )
        {
            // This shall contain the merged settings.
            CatchAdapterSettings settings = new CatchAdapterSettings();

            // Try to find an existing catch configuration node.
            var settingsFromContext = MaybeReadSettingsFromXml(inputRunSettingDocument);
            XPathNavigator navigator = inputRunSettingDocument.CreateNavigator();
            if (settingsFromContext == null)
            {
                // Note that explicit runsettings for catch were not provided.
                log.Log(MessageLevel.Informational,
                    $"No '{CatchAdapterSettings.XmlRoot}' node in explicit runsettings (or no explicit runsettings at all). " +
                    "Searching for runsettings in solution directory and above.");

                // Read settings from files.
                foreach (var file in FindSettingsInFoldersAbove( Path.GetDirectoryName( dte.Solution.FullName ), log))
                {
                    try
                    {
                        // Try to find settings from the file.
                        var settingsFromFile = MaybeReadSettingsFromFile(file);
                        if (settingsFromFile != null)
                        {
                            log.Log(MessageLevel.Informational, $"Reading test run settings from {file}.");
                            settings.MergeFrom(settingsFromFile);
                        }
                    }
                    catch (IOException ex)
                    {
                        log.Log(MessageLevel.Warning,
                            $"Failed to read test run settings from file '{file}'. Exception: {ex.ToString()}");
                    }
                }
            }
            else
            {
                // Merge the settings from the context.
                settings.MergeFrom(settingsFromContext);

                // Erase the original.
                if (navigator.MoveToFollowing(CatchAdapterSettings.XmlRoot, ""))
                    navigator.DeleteSelf();
            }

            // Write the resolved settings to the xml.
            XPathNavigator settingsAsXml = settings.ToXml().CreateNavigator();
            navigator.MoveToRoot();
            navigator.MoveToFirstChild();
            navigator.AppendChild(settingsAsXml);

            // Clean up the navigator.
            navigator.MoveToRoot();
            return navigator;
        }

        /// <summary>
        /// Read catch settings from a runsettings file. Return null if there are none.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private CatchAdapterSettings MaybeReadSettingsFromFile(string filename)
        {
            XPathDocument doc = new XPathDocument(filename);
            return MaybeReadSettingsFromXml(doc);
        }

        /// <summary>
        /// Read catch settings from XML. Return null if there are none.
        /// </summary>
        /// <param name="settingSource">An XML navigable that may contain catch adapter settings.</param>
        /// <returns></returns>
        private CatchAdapterSettings MaybeReadSettingsFromXml(IXPathNavigable settingSource)
        {
            XPathNavigator navigator = settingSource.CreateNavigator();
            if (navigator.MoveToFollowing(CatchAdapterSettings.XmlRoot, ""))
            {

                // Catch adapter settings found. Try to read them.
                XmlReader reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(navigator.OuterXml)));
                XmlSerializer serializer = new XmlSerializer(typeof(CatchAdapterSettings));
                return serializer.Deserialize(reader) as CatchAdapterSettings;
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
        /// <param name="initialPath"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        IEnumerable<string> FindSettingsInFoldersAbove(string initialPath,
            ILogger log)
        {
            // Get the full path.
            string fullPath = Path.GetFullPath(initialPath);

            // Split the path to components.
            var pathComponents = fullPath.Split(new char[] { Path.DirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);

            // Append the path components to each other to process each intermediate folder.
            var currentPath = "";
            foreach (string component in pathComponents)
            {
                currentPath = currentPath + component + Path.DirectorySeparatorChar;
                IEnumerable<string> files = new string[0];
                try
                {
                    // Find matching files.
                    // Force evaluation to ensure errors occur inside the try.
                    files = Directory.EnumerateFiles(currentPath, "*.runsettings").ToArray();
                }
                catch (IOException ex)
                {
                    // We may not have permission or something. Ignore silently.
                    log.Log(MessageLevel.Informational,
                        $"Error looking for settings at path {currentPath}: {ex.ToString()}.");
                }

                // Yield the found files.
                foreach (string file in files)
                {
                    yield return file;
                }
            }
        }
    }
}
