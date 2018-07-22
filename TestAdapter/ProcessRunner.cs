using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace CatchTestAdapter
{
    /// <summary>
    /// Runs external processes.
    /// </summary>
    public class ProcessRunner
    {
        /// <summary>
        /// Execute a plain external process.
        /// </summary>
        /// <param name="cmd">Path to executable.</param>
        /// <param name="args">Command line arguments.</param>
        /// <returns></returns>
        public static IList<string> RunProcess(string cmd, string args, string workingDirectory )
        {
            // Start a new process.
            var processStartInfo = new ProcessStartInfo( cmd, args )
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
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
        public static IList<string> RunDebugProcess( IFrameworkHandle frameworkHandle, string cmd, string args, string workingDirectory )
        {
            // We cannot reliably capture the output of a process launched by the framework.
            // We store the output in a temp file instead.
            string exeName = System.IO.Path.GetFileName( cmd );
            string outputFile = exeName + ".catchout.xml";
            string argsWithOutFile = args + $" --out \"{outputFile}\"";

            // Tell the framework to run the process in a debugger.
            int pid = frameworkHandle.LaunchProcessWithDebuggerAttached(
                cmd, workingDirectory,
                argsWithOutFile, new Dictionary<string, string>() );

            // Wait for exit.
            using ( Process process = Process.GetProcessById( pid ) )
            {
                process.WaitForExit();
            }

            // Get the output.
            var outputLines = new List<string>( System.IO.File.ReadAllLines( workingDirectory +
                System.IO.Path.DirectorySeparatorChar + outputFile ) );
            System.IO.File.Delete( outputFile );

            return outputLines;
        }

        private static IList<string> GetProcessOutput( Process process )
        {
            // Get output from the process.
            var outputLines = new List<string>();
            process.OutputDataReceived += ( object sender, DataReceivedEventArgs e ) =>
            {
                if( e.Data != null )
                    outputLines.Add( e.Data );
            };

            var errorString = new StringBuilder();
            process.ErrorDataReceived += ( object sender, DataReceivedEventArgs e ) =>
            {
                if( e.Data != null )
                    errorString.AppendLine( e.Data );
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            string processName = process.ProcessName;
            string processArguments = process.StartInfo.Arguments;

            process.WaitForExit();

            // Catch returns the number of failed or found tests as the exit code,
            // so we cannot use a simple compare to zero. It uses 255 for errors instead.
            if ( process.ExitCode == 255 )
            {
                throw new System.Exception( $"Failed executing '{processName} {processArguments}'. Exit code {process.ExitCode}. StdErr: '{errorString}'" );
            }

            return outputLines;
        }
    }
}
