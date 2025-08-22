namespace OllamaMux
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    static class OllamaProcess
    {
        static public bool TryLaunchRunProgramDetachedDetached(string path, string arguments)
        {
            int? exitCode;
            var psi = new ProcessStartInfo
            {
                FileName = path,
                Arguments = arguments,
                UseShellExecute = false,                // allows monitoring
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var proc = Process.Start(psi);
            bool? exitedEarly = proc?.WaitForExit(3000);  // Wait briefly for crash

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

        public static void RunForeground(string[] args, string ollamaHost)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "ollama",
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                UseShellExecute = false
            };

            foreach (var arg in args)
            {
                psi.ArgumentList.Add(arg);
            }
            psi.Environment["OLLAMA_HOST"] = ollamaHost;

            using var proc = Process.Start(psi);
            proc?.WaitForExit();
        }

    public static async Task<int> RunAsync(string[] args, string? ollamaExecutionHost)
        {
            var binaryName = OperatingSystem.IsWindows() ? "ollama.exe" : "ollama";
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = binaryName,
                    Arguments = string.Join(" ", args),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                if (ollamaExecutionHost != null)
                {
                    startInfo.EnvironmentVariables["OLLAMA_HOST"] = ollamaExecutionHost;
                    Console.WriteLine($"[✓] Set OLLAMA_HOST={ollamaExecutionHost}");
                }

                using var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        Console.WriteLine(FilterOutput(e.Data));
                };
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                        Console.Error.WriteLine(FilterOutput(e.Data));
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();
                return process.ExitCode;
            }
            catch (Win32Exception)
            {
                Console.Error.WriteLine($"'{binaryName}' not found. Please ensure Ollama is installed and available in your system PATH.");
                return -1;
            }
        }

        private static string FilterOutput(string line)
        {
            // Skip filtering if line contains known exclusions
            if (line.Contains("OLLAMA_") ||
                line.Contains("ollama/") ||
                line.Contains("/") ||
                line.Contains("http") ||
                line.Contains(".com") ||
                line.Contains("docker") ||
                line.Contains("/usr"))
            {
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
