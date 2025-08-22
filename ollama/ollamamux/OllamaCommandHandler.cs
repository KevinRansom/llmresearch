namespace OllamaMux
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public class OllamaCommandHandler
    {
        public enum CommandOutcome
        {
            StartedProxy,
            Executed,
            Error
        }

        // Keep the sets if you’ll use them later, but we won’t branch on them here.
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

            var (_, execHost) = OllamaProxy.GetHosts();

            // Always run the underlying ollama command against the execution host (11435).
            // For 'serve', this blocks until ollama exits. For other commands, it returns when done.
            var exitCode = await OllamaProcess.RunAsync(args, execHost);
            Environment.Exit(exitCode);
            return CommandOutcome.Executed;
        }
    }
}
