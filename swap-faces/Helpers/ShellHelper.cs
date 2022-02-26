using SwapFaces.Dto;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SwapFaces.Helpers
{
    public class ShellHelper : IShellHelper
    {
        public async Task<ExecuteResult> ExecuteWithTimeout(string[] commands, string? workingDirectory = null, int timeoutMinutes = 15, 
            Action<string> stdErrDataReceivedCallback = null, Action<string> stdOutDataReceivedCallback = null)
        {
            LogHelper.EphemeralLog("Will execute commands: " + string.Join(Environment.NewLine, commands));
            var stdOutputBuilder = new StringBuilder();
            var stdErrorBuilder = new StringBuilder();

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"cmd.exe",
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                },
                EnableRaisingEvents = true
            };
            process.ErrorDataReceived += new DataReceivedEventHandler(delegate (object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stdErrDataReceivedCallback?.Invoke(e.Data);
                    stdErrorBuilder.AppendLine(e.Data);
                }
            });
            process.OutputDataReceived += new DataReceivedEventHandler(delegate (object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    stdOutDataReceivedCallback?.Invoke(e.Data);
                    stdOutputBuilder.AppendLine(e.Data);
                }
            });
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (var sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    foreach (var command in commands)
                    {
                        await sw.WriteLineAsync(command);
                    }
                }
            }

            await WaitOrKill(process, timeoutMinutes);

            return new ExecuteResult()
            {
                ExitCode = process.ExitCode,
                StdError = stdErrorBuilder.ToString(),
                StdOutput = stdOutputBuilder.ToString()
            };
        }

        public async Task<ExecuteResult> Execute(string cmd, Action<string> stdErrDataReceivedCallback = null, Action<string> stdOutDataReceivedCallback = null)
        {
            LogHelper.EphemeralLog("Will execute: " + cmd);
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var escapedArgs = isWindows ? cmd : cmd.Replace("\"", "\\\"");
            var stdOutputBuilder = new StringBuilder();
            var stdErrorBuilder = new StringBuilder();

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = isWindows ? "cmd.exe" : "/bin/bash",
                    Arguments = isWindows ? $"/C \"{escapedArgs}\"" : $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true
            };
            process.ErrorDataReceived += new DataReceivedEventHandler
            (
                delegate (object sender, DataReceivedEventArgs e)
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        stdErrDataReceivedCallback?.Invoke(e.Data);
                        stdErrorBuilder.AppendLine(e.Data);
                    }
                }
            );
            process.OutputDataReceived += new DataReceivedEventHandler
            (
                delegate (object sender, DataReceivedEventArgs e)
                {
                    if (!string.IsNullOrWhiteSpace(e.Data))
                    {
                        stdOutDataReceivedCallback?.Invoke(e.Data);
                        stdOutputBuilder.AppendLine(e.Data);
                    }
                }
            );

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            await process.WaitForExitAsync();
            process.CancelOutputRead();
            process.CancelErrorRead();

            return new ExecuteResult()
            {
                ExitCode = process.ExitCode,
                StdError = stdErrorBuilder.ToString(),
                StdOutput = stdOutputBuilder.ToString()
            };
        }

        public string SanitizeFilename(string filename)
        {
            filename = filename.Replace("/", "_").Replace("\\", "_").Replace("\"", "_").Replace("__", "_");
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        private static async Task<bool> WaitOrKill(Process process, int timeoutMinutes)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(timeoutMinutes * 60 * 1000);
            await process.WaitForExitAsync(cts.Token);
            bool cancelled = false;
            if (cts.IsCancellationRequested)
            {
                cancelled = true;
                LogHelper.EphemeralLog($"---------------> PROCESS EXITED AFTER TIMEOUT. Killing process.", true);
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    try { process.Kill(); } finally { }
                }
            }
            process.CancelOutputRead();
            process.CancelErrorRead();
            return cancelled; 
        }
    }
}
