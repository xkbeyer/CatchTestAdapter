using System.Collections.Generic;
using System.Diagnostics;

namespace CatchTestAdapter
{
    class ProcessRunner
    {
        private IList<string> outputLines = new List<string>();
        public ProcessRunner(string cmd, string args)
        {
            var processStartInfo = new ProcessStartInfo(cmd, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = @"."
            };

            Process process = Process.Start(processStartInfo);
            process?.WaitForExit(500);
            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                outputLines.Add(line);
            }

            process?.Dispose();

        }

        public IList<string> Output { get { return outputLines; } }
    }
}
