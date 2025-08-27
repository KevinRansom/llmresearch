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
                // If the arguments are not recognised fall through to ollama.exe for error handling
                if (!OllamaCommandHandler.IsInValidArguments(args))
                {
                    var proxy = new OllamaProxy();

                    if (OllamaCommandHandler.IsForegroundRequired(args))
                    {
                        // Only start proxy if it's not already bound
                        if (!await OllamaProxy.IsExecutionAlreadyRunningAsync(TimeSpan.FromMilliseconds(500)))
                        {
                            proxy.StartProxy(detached: OllamaCommandHandler.IsDetachedRequired(args));
                        }
                        else
                        {
                            Console.Error.WriteLine("Reusing existing proxy on port 11434.");
                        }

                        // Foreground backend so logs stream here
                        await OllamaProcess.RunForeground(args, "http://127.0.0.1:11435");
                        return 0;
                    }
                    else
                    {
                        var fallbackHandler = new OllamaCommandHandler();
                        return (int)await fallbackHandler.ExecuteAsync(args);
                    }
                }
                else
                {
                    var fallbackHandler = new OllamaCommandHandler();
                    return (int)await fallbackHandler.ExecuteAsync(args);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unhandled error: {ex}");
                return 1;
            }
        }
    }
}
