
namespace OllamaMux
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    static class OllamaProcess
    {
        public static bool TryLaunchRunProgramDetachedDetached(string path, string arguments)
        {
            int? exitCode;
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = arguments,
                UseShellExecute = false, // allows monitoring
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            bool? exitedEarly = proc?.WaitForExit(3000); // Wait briefly for crash

            if (exitedEarly.GetValueOrDefault())
            {
                exitCode = proc?.ExitCode;
                Console.WriteLine($"Process exited early with code {exitCode}");
                return false; // Indicates failure
            }

            exitCode = null;
            Console.WriteLine("Process launched successfully and is still running.");
            return true; // Indicates success
        }

        public static async Task<int?> RunProcessAsync(
            string[] args,
            string? ollamaExecutionHost,
            bool captureOutput,
            bool asyncMode)
        {
            var binaryName = OperatingSystem.IsWindows() ? "ollama.exe" : "ollama";

            var psi = new ProcessStartInfo
            {
                FileName = binaryName,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = captureOutput,
                RedirectStandardError = captureOutput,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = captureOutput
            };

            if (captureOutput)
            {
                psi.StandardOutputEncoding = Encoding.UTF8;
                psi.StandardErrorEncoding = Encoding.UTF8;
            }

            if (ollamaExecutionHost != null)
            {
                psi.Environment["OLLAMA_HOST"] = ollamaExecutionHost;
            }

            try
            {
                using var process = new Process { StartInfo = psi };

                if (captureOutput)
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (e.Data != null) Console.WriteLine(FilterOutput(e.Data));
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (e.Data != null) Console.Error.WriteLine(FilterOutput(e.Data));
                    };
                }

                process.Start();

                if (captureOutput)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                if (asyncMode)
                {
                    await process.WaitForExitAsync();
                    return process.ExitCode;
                }
                else
                {
                    process.WaitForExit();
                    return null; // matches RunForeground’s “no exit code” signature
                }
            }
            catch (Win32Exception)
            {
                Console.Error.WriteLine(
                    $"'{binaryName}' not found. Please ensure Ollama is installed and in your PATH.");
                return asyncMode ? -1 : null;
            }
        }

        public static void RunForeground(string[] args, string ollamaExecutionHost)
        {
            RunProcessAsync(args, ollamaExecutionHost, captureOutput: false, asyncMode: false)
                .GetAwaiter()
                .GetResult();
        }

        public static Task<int> RunAsync(string[] args, string? ollamaExecutionHost)
        {
            return RunProcessAsync(args, ollamaExecutionHost, captureOutput: true, asyncMode: true)
                .ContinueWith(t => t.Result ?? 0);
        }

        private static string FilterOutput(string line)
        {
            bool hasExclusion =
                line.Contains("OLLAMA_") ||
                line.Contains("ollama/") ||
                line.Contains("http") ||
                line.Contains(".com") ||
                line.Contains("docker") ||
                line.Contains("/usr");

            if (hasExclusion)
            {
                // Nothing to rewrite – just return as-is.
                return line;
            }

            // Replace CLI usage and prefix references
            line = Regex.Replace(line, @"(?<!mux)\bUsage: ollama\b", "Usage: ollamamux");
            line = Regex.Replace(line, @"(?<!mux)\bollama:", "ollamamux:");
            line = Regex.Replace(line, @"(?<!mux)\bollama\b", "ollamamux");

            return line;
        }
    }
}
