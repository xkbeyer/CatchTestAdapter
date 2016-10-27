using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Microsoft.VisualStudio.VCProjectEngine;
using EnvDTE;

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CatchTestAdapter
{
    [Export(typeof(ITestContainerDiscoverer))]
    public class CatchTestContainerDiscoverer : ITestContainerDiscoverer
    {
        public const string ExecutorUriString = TestExecutor.ExecutorUriString;
   
        public event EventHandler TestContainersUpdated;
        public Uri ExecutorUri { get { return new System.Uri(ExecutorUriString); } }
        public IEnumerable<ITestContainer> TestContainers { get { return GetTestContainers(); } }

        private IServiceProvider serviceProvider;
        private ILogger logger;
        private bool initialContainerSearch = true;
        private readonly List<ITestContainer> cachedContainers;
        private EnvDTE.DTE dte;

        [ImportingConstructor]
        public CatchTestContainerDiscoverer(
            [Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider,
            // IServiceProvider serviceProvider,
            ILogger logger
            )
        {
            logger.Log(MessageLevel.Informational, "Invoking CatchTestContainerDiscoverer ...");
            initialContainerSearch = true;
            cachedContainers = new List<ITestContainer>();
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            dte = (EnvDTE.DTE)serviceProvider.GetService(typeof(EnvDTE.DTE));
            //TestContainersUpdated(this, new EventArgs());
            //throw new NotImplementedException();
        }


        private IEnumerable<ITestContainer> GetTestContainers()
        {
            if (initialContainerSearch)
            {
                cachedContainers.Clear();
                initialContainerSearch = false;
            }
            logger.Log(MessageLevel.Informational, "Invoking CatchTestContainerDiscoverer ::GetTestContainers() ...");

            return cachedContainers;
        }

        public void Dispose()
        {
            Dispose(true);
            // Use SupressFinalize in case a subclass
            // of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }


    }
}
