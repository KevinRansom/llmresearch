namespace OllamaMux
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    static class OllamaProcess
    {
        public static async Task<int> RunAsync(string[] args, string ollamaExecutionHost)
        {
            var binaryName = OperatingSystem.IsWindows() ? "ollama.exe" : "ollama";
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = binaryName,
                    Arguments = args.Aggregate((acc, arg) => acc + " " + arg),
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                startInfo.EnvironmentVariables["OLLAMA_HOST"] = ollamaExecutionHost;

                Console.WriteLine($"[✓] Set OLLAMA_HOST={ollamaExecutionHost}");

                using var process = new Process { StartInfo = startInfo };
                process.OutputDataReceived += (sender, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };

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
    }
}
