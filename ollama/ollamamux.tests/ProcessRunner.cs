using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ollamamux.tests
{
    public static class ProcessRunner
    {
        public static async Task<(string stdout, string stderr, int exitCode)> RunAsync(
            string fileName,
            string arguments,
            TimeSpan timeout)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            var stdoutClosed = new TaskCompletionSource();
            var stderrClosed = new TaskCompletionSource();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data == null)
                {
                    stdoutClosed.TrySetResult();
                }
                else if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null)
                {
                    stderrClosed.TrySetResult();
                }
                else if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exitTask = Task.Run(() =>
            {
                process.WaitForExit();
                return process.ExitCode;
            });

            var allTasks = Task.WhenAll(stdoutClosed.Task, stderrClosed.Task, exitTask);

            if (await Task.WhenAny(allTasks, Task.Delay(timeout)) != allTasks)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
                throw new TimeoutException("Process did not complete in time.");
            }

            return (
                stdout: outputBuilder.ToString(),
                stderr: errorBuilder.ToString(),
                exitCode: exitTask.Result
            );
        }
    }
}
