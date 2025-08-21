namespace OllamaMux
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class OllamaCommandHandler
    {
        public enum CommandOutcome
        {
            StartedProxy,
            Executed,
            Error
        }

        private static readonly HashSet<string> ProxyRequiredCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "serve", "run", "create"
        };

        private static readonly HashSet<string> NoProxyRequiredCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "list", "ps", "show", "pull", "push", "cp", "rm", "help", "--version", "stop"
        };

        public async Task<CommandOutcome> ExecuteAsync(string[] args)
        {
            if (args.Length == 0)
                return CommandOutcome.Executed;

            var command = args[0];
            var proxy = new OllamaProxy();
            var (_, execHost) = OllamaProxy.GetHosts();

            if (ProxyRequiredCommands.Contains(command))
            {
                Console.WriteLine($"[mux] Starting proxy for '{command}'...");
                proxy.StartProxy(false); // in-process listener
                await Task.Delay(Timeout.Infinite);
                return CommandOutcome.StartedProxy;
            }

            if (NoProxyRequiredCommands.Contains(command))
            {
                var exitCode = await OllamaProcess.RunAsync(args, execHost);
                Environment.Exit(exitCode);
                return CommandOutcome.Executed;
            }

            Console.Error.WriteLine($"[mux] Unknown command: '{command}'");
            return CommandOutcome.Error;
        }
    }
}
