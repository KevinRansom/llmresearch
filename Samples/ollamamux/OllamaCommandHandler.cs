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
            StartProxy,
            Executed,
            Error
        }

        public delegate Task CommandHook(string[] args);

        private static CommandHook CreateProxyHandler(string commandName)
        {
            return async args =>
            {
                Console.WriteLine($"[mux] Intercepting '{commandName}' command...");
                var proxy = new OllamaProxy();
                var (muxHost, execHost) = OllamaProxy.GetHosts();
                proxy.StartProxy(); // in-process listener
                await Task.Delay(Timeout.Infinite);
                await Task.CompletedTask;
            };
        }

        private static readonly HashSet<string> ProxyAwareCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "serve", "create", "show", "run", "stop", "pull", "push", "list", "ps", "cp", "rm"
        };

        private static readonly Dictionary<string, CommandHook> Hooks =
            ProxyAwareCommands.ToDictionary(
                command => command,
                command => CreateProxyHandler(command),
                StringComparer.OrdinalIgnoreCase
            );

        public async Task<CommandOutcome> ExecuteAsync(string[] args)
        {
            if (args.Length == 0)
            {
                return CommandOutcome.Executed;
            }

            var command = args[0];

            if (Hooks.TryGetValue(command, out var hook))
            {
                await hook(args);
            }

            return CommandOutcome.Executed;
        }

        public bool ShouldStartProxy(string command) => ProxyAwareCommands.Contains(command);
    }
}
