
namespace OllamaMux
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
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

        private static Task<Process?> RunOllamaProcessAsync(
            string[] args,
            string? ollamaExecutionHost)
        {
            var binaryName = OperatingSystem.IsWindows() ? "ollama.exe" : "ollama";

            var psi = new ProcessStartInfo
            {
                FileName = binaryName,
                Arguments = string.Join(" ", args),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (ollamaExecutionHost != null)
            {
                psi.Environment["OLLAMA_HOST"] = ollamaExecutionHost;
            }

            try
            {
                var process = new Process { StartInfo = psi };

                process.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null) Console.WriteLine(FilterOutput(e.Data));
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null) Console.Error.WriteLine(FilterOutput(e.Data));
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Optional: Attach to a kill‑on‑close job object
                if (OperatingSystem.IsWindows())
                {
                    var job = JobObjectHelper.CreateKillOnCloseJob();
                    JobObjectHelper.AssignProcess(job, process);
                }

                return Task.FromResult<Process?>(process);
            }
            catch (Win32Exception)
            {
                Console.Error.WriteLine(
                    $"'{binaryName}' not found. Please ensure Ollama is installed and in your PATH.");
                return Task.FromResult<Process?>(null);
            }
        }

        public static async Task RunForeground(string[] args, string ollamaExecutionHost)
        {
            var process = await RunOllamaProcessAsync(args, ollamaExecutionHost);

            if (process != null)
            {
                var job = JobObjectHelper.CreateKillOnCloseJob();
                JobObjectHelper.AssignProcess(job, process);
                await process.WaitForExitAsync();
            }
        }

        public static async Task<int> RunOllamaAsync(string[] args, string? host)
        {
            var proc = await RunOllamaProcessAsync(args, host);
            if (proc == null) return -1;
            await proc.WaitForExitAsync();
            return proc.ExitCode;
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

        static class JobObjectHelper
        {
            private const int JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;
            private const int JobObjectExtendedLimitInformation = 9;

            [StructLayout(LayoutKind.Sequential)]
            struct JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                public long PerProcessUserTimeLimit;
                public long PerJobUserTimeLimit;
                public int LimitFlags;
                public UIntPtr MinimumWorkingSetSize;
                public UIntPtr MaximumWorkingSetSize;
                public int ActiveProcessLimit;
                public long Affinity;
                public int PriorityClass;
                public int SchedulingClass;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct IO_COUNTERS
            {
                public ulong ReadOperationCount;
                public ulong WriteOperationCount;
                public ulong OtherOperationCount;
                public ulong ReadTransferCount;
                public ulong WriteTransferCount;
                public ulong OtherTransferCount;
            }

            [StructLayout(LayoutKind.Sequential)]
            struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
            {
                public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
                public IO_COUNTERS IoInfo;
                public UIntPtr ProcessMemoryLimit;
                public UIntPtr JobMemoryLimit;
                public UIntPtr PeakProcessMemoryUsed;
                public UIntPtr PeakJobMemoryUsed;
            }

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
            static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool SetInformationJobObject(IntPtr hJob, int JobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

            public static IntPtr CreateKillOnCloseJob()
            {
                var hJob = CreateJobObject(IntPtr.Zero, null);

                var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
                info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;

                int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
                IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
                try
                {
                    Marshal.StructureToPtr(info, extendedInfoPtr, false);
                    if (!SetInformationJobObject(hJob, JobObjectExtendedLimitInformation, extendedInfoPtr, (uint)length))
                    {
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(extendedInfoPtr);
                }

                return hJob;
            }

            public static void AssignProcess(IntPtr job, Process process)
            {
                if (!AssignProcessToJobObject(job, process.Handle))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }
    }
}
