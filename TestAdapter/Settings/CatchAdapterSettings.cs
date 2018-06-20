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
        public const string XmlRoot = "CatchAdapter";

        public CatchAdapterSettings(): base( XmlRoot ) { }

        #region Settings

        /// <summary>
        /// Regex used to find test executables.
        /// </summary>
        [XmlArray( "TestExeInclude" )]
        [XmlArrayItem( "Regex" )]
        public List<string> TestExeInclude { get; set; } = new List<string>();

        /// <summary>
        /// Regex used to exclude test executables.
        /// </summary>
        [XmlArray( "TestExeExclude" )]
        [XmlArrayItem( "Regex" )]
        public List<string> TestExeExclude { get; set; } = new List<string>();

        #endregion

        #region Interpretations

        /// <summary>
        /// Returns true if the given executable should be treated as a source of tests.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IncludeTestExe( string name )
        {
            // If there are include patterns, the name must match one of them.
            if( this.TestExeInclude.Count > 0 )
            {
                if ( !TestExeInclude.Any( regex => Regex.IsMatch( name, regex ) ) )
                    return false;
            }

            // The name must not match any exclude pattern.
            return !this.TestExeExclude.Any( regex => Regex.IsMatch( name, regex ) );
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

        /// <summary>
        /// Combine the settings in an other instance with the settings in this one.
        /// The settings in other overwrite settings in this.
        /// </summary>
        /// <param name="other"></param>
        public void MergeFrom( CatchAdapterSettings other )
        {
            // Combine the xclusion lists.
            this.TestExeInclude.AddRange( other.TestExeInclude );
            this.TestExeExclude.AddRange( other.TestExeExclude );
        }
    }
}
