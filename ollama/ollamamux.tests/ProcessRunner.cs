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
        public sealed class LiveProcess : IAsyncDisposable
        {
            private readonly Process _process;
            internal LiveProcess(Process process) => _process = process;

            public StreamWriter StandardInput => _process.StandardInput;
            public int ExitCode => _process.HasExited ? _process.ExitCode : -1;

            public event DataReceivedEventHandler? OutputDataReceived
            {
                add => _process.OutputDataReceived += value;
                remove => _process.OutputDataReceived -= value;
            }
            public event DataReceivedEventHandler? ErrorDataReceived
            {
                add => _process.ErrorDataReceived += value;
                remove => _process.ErrorDataReceived -= value;
            }

            public Task WaitForExitAsync() => _process.WaitForExitAsync();
            public void Kill(bool entireTree = true) =>
                _process.Kill(entireProcessTree: entireTree);

            public ValueTask DisposeAsync()
            {
                _process.Dispose();
                return ValueTask.CompletedTask;
            }

            public async Task WaitForExitAsync(CancellationToken cancellationToken)
            {
                await _process.WaitForExitAsync(cancellationToken);
            }
        }

        public static Task<LiveProcess> StartLiveAsync(string fileName, string arguments)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return Task.FromResult(new LiveProcess(process));
        }

        public static async Task<(string stdout, string stderr, int exitCode)>
            RunAsync(string fileName, string arguments, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);

            await using var proc = await StartLiveAsync(fileName, arguments);

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            proc.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            try
            {
                await proc.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                proc.Kill(entireTree: true);
                throw;
            }

            return (outputBuilder.ToString(),
                    errorBuilder.ToString(),
                    proc.ExitCode);
        }

        public static async Task<(string stdout, string stderr)> RunAndCaptureAsync(
            string fileName,
            string arguments,
            Func<LiveProcess, Task>? interact = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout.HasValue)
                cts.CancelAfter(timeout.Value);

            await using var proc = await ProcessRunner.StartLiveAsync(fileName, arguments);

            var outBuf = new StringBuilder();
            var errBuf = new StringBuilder();

            proc.OutputDataReceived += (_, e) => { if (e.Data != null) outBuf.AppendLine(e.Data); };
            proc.ErrorDataReceived += (_, e) => { if (e.Data != null) errBuf.AppendLine(e.Data); };

            if (interact != null)
                await interact(proc);

            await proc.WaitForExitAsync(cts.Token);

            return (outBuf.ToString().Trim(), errBuf.ToString().Trim());
        }

    }
}
