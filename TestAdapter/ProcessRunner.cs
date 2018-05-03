using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace CatchTestAdapter
{
    class ProcessRunner
    {
        public static IList<string> RunProcess(string cmd, string args )
        {
            List<string> outputLines = new List<string>();

            var processStartInfo = new ProcessStartInfo( cmd, args )
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = @"."
            };

            Process process = Process.Start( processStartInfo );
            process?.WaitForExit( 500 );
            while ( !process.StandardOutput.EndOfStream )
            {
                string line = process.StandardOutput.ReadLine();
                outputLines.Add( line );
            }

            process?.Dispose();

            return outputLines;
        }

        public static IList<string> RunDebugProcess( IFrameworkHandle frameworkHandle, string cmd, string args )
        {
            List<string> outputLines = new List<string>();
			var env = new Dictionary<string, string>();
            var ourEnv = System.Environment.GetEnvironmentVariables();

			foreach( string key in ourEnv.Keys )
			{
				env.Add( key, (string)ourEnv[ key ] );
			}
            int pid = frameworkHandle.LaunchProcessWithDebuggerAttached( cmd, System.Environment.CurrentDirectory, args, env );
            using ( Process process = Process.GetProcessById( pid ) )
            {
                while ( process.StandardOutput.Peek() > 0 )
                {
                    outputLines.Add( process.StandardOutput.ReadLine() );
                }
            }

            return outputLines;
        }
    }
}
