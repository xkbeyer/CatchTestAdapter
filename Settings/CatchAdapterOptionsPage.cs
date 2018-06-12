using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace CatchTestAdapter
{
	/// <summary>
    /// The visual studio options page for this adapter.
    /// </summary>
    class CatchAdapterOptionsPage : DialogPage
    {
        [Category( "Catch Adapter" )]
        [DisplayName( "Default exe filter" )]
        [Description( "The filter to use when finding test executables, if none is set in the runsettings." )]
        public string DefaultExeFilter { get; set; }
    }
}
