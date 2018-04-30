using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace TestAdapter.Settings
{
    /// <summary>
    /// Settings for the catch test adapter.
    /// Found as the element "Catch" in a Visual Studio runsettings xml file.
    /// </summary>
    [XmlRoot(XmlRoot)]
    public class CatchAdapterSettings : TestRunSettings
    {
        /// <summary>
        /// The name of the XML that holds these settings.
        /// </summary>
        public const string XmlRoot = "Catch";

        public CatchAdapterSettings(): base( XmlRoot ) { }

        #region Settings

        /// <summary>
        /// Regex used to find test executables.
        /// </summary>
        [DefaultValue( @".*\.exe" )]
        public string TestExeFilter { get; set; } = @".*\.exe";

        #endregion

        #region Interpretations

        /// <summary>
        /// Returns true if the given executable should be treated as a source of tests.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IncludeTestExe( string name )
        {
            return Regex.Match( name, this.TestExeFilter ).Success;
        }

        #endregion Interpretations

        public override XmlElement ToXml()
        {
            // Create a temporary Xml document.
            XmlDocument doc = new XmlDocument();

            // Write the settings to it.
            using ( var writer = doc.CreateNavigator().AppendChild() )
            {
                var serializer = new XmlSerializer( typeof( CatchAdapterSettings ) );
                serializer.Serialize( writer, this );
            }

            // Because the element is a root in the temporary document,
            // some root specific elements were automatically added to it,
            // but it won't actually be a root where we want it, so we remove them.
            doc.DocumentElement.RemoveAllAttributes();

            return doc.DocumentElement;
        }
    }
}
