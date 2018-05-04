using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace CatchTestAdapter
{
    /// <summary>
    /// Runs external processes.
    /// </summary>
    class ProcessRunner
    {
        /// <summary>
        /// Execute a plain external process.
        /// </summary>
        /// <param name="cmd">Path to executable.</param>
        /// <param name="args">Command line arguments.</param>
        /// <returns></returns>
        public static IList<string> RunProcess(string cmd, string args )
        {
            // Start a new process.
            var processStartInfo = new ProcessStartInfo( cmd, args )
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = @"."
            };

            using ( Process process = Process.Start( processStartInfo ) )
            {
                return GetProcessOutput( process );
            }
        }

        /// <summary>
        /// Executes an external process attached to the debugger.
        /// </summary>
        /// <param name="frameworkHandle">The testing framework that provides the debugger to attach to.</param>
        /// <param name="cmd">The executable.</param>
        /// <param name="args">Command-line parameters.</param>
        /// <returns></returns>
        public static IList<string> RunDebugProcess( IFrameworkHandle frameworkHandle, string cmd, string args )
        {
            // We cannot reliably capture the output of a process launched by the framework.
            // We store the output in a temp file instead.
            string exeName = System.IO.Path.GetFileName( cmd );
            string outputFile = exeName + ".catchout.xml";
            string argsWithOutFile = args + $" --out \"{outputFile}\"";

            // Tell the framework to run the process in a debugger.
            int pid = frameworkHandle.LaunchProcessWithDebuggerAttached(
                cmd, System.Environment.CurrentDirectory,
                argsWithOutFile, new Dictionary<string, string>() );

            // Wait for exit.
            using ( Process process = Process.GetProcessById( pid ) )
            {
                process.WaitForExit();
            }

            // Get the output.
            var outputLines = new List<string>( System.IO.File.ReadAllLines( outputFile ) );
            System.IO.File.Delete( outputFile );

            return outputLines;
        }

        private static IList<string> GetProcessOutput( Process process )
        {
            // Get output from the process.
            var outputLines = new List<string>();
            while ( !process.StandardOutput.EndOfStream )
            {
                outputLines.Add( process.StandardOutput.ReadLine() );
            }

            return outputLines;
        }
    }
}
