namespace OllamaMux
{
    using System;
    using System.Threading.Tasks;

    public static class OllamaMux
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                var proxy = new OllamaProxy();

                // Start both mux and backend in the same process/window
                if (args.Length > 0 && string.Equals(args[0], "serve", StringComparison.OrdinalIgnoreCase))
                {
                    // Proxy first
                    proxy.StartProxy(detached: false);

                    // Foreground backend serve on 11435 so logs stream here
                    OllamaProcess.RunForeground(new[] { "serve" }, "http://127.0.0.1:11435");

                    // When backend exits, end process
                    return 0;
                }

                // For any other command, make sure backend is up
                if (!await OllamaProxy.IsExecutionAlreadyRunningAsync(TimeSpan.FromMilliseconds(500)))
                {
                    Console.Error.WriteLine("Execution backend not responding on port 11435.");
                    return 1;
                }

                var handler = new OllamaCommandHandler();
                return (int)await handler.ExecuteAsync(args);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unhandled error: {ex}");
                return 1;
            }
        }
    }
}
