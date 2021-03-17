using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Server
{
    public class CLIHelper
    {
        private string exePath;
        public CLIHelper(string exePath)
        {
            this.exePath = exePath;
        }

        /// <summary>
        /// Run <see cref="exePath"/> with <param name="args"/>.
        /// Waiting process to exit for <param name="timeout"/>, if <param name="timeout"/> is null, then waiting forever until process exits.
        /// </summary>
        public (string output, string error, int exitCode) Run(string args = null, TimeSpan? timeout = null)
        {
            ProcessStartInfo processInfo = new ProcessStartInfo(exePath)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Arguments = args ?? string.Empty
            };
            using (Process process = new Process())
            {
                process.StartInfo = processInfo;

                StringBuilder output = new StringBuilder();
                StringBuilder error = new StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        output.AppendLine(e.Data);
                    }
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        error.AppendLine(e.Data);
                    }
                };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (timeout != null)
                {
                    if (!process.WaitForExit((int)timeout.Value.TotalMilliseconds))
                    {
                        process.Kill();
                    }
                }
                else
                {
                    process.WaitForExit();
                }

                return (output.Length > 0 ? output.ToString() : null, error.Length > 0 ? error.ToString() : null, process.ExitCode);
            }
        }
    }
}
