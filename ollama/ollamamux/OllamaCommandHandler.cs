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

        private static readonly HashSet<string> ForegroundRequiredCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "serve", "run" // stream backend output inline for both
        };

        private static readonly HashSet<string> DetachedRequiredCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "run", "list", "ps", "show", "pull", "push", "cp", "rm", "help", "--help", "-h", "--version", "stop", "create"
        };

        private static readonly HashSet<string> NoProxyRequiredCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "list", "ps", "show", "pull", "push", "cp", "rm", "help", "--help", "-h", "--version", "stop", "create"
        };

        public static string GetCommand(string[] args)
        {
            args = args is { Length: > 0 } ? args : Array.Empty<string>();
            return (args != Array.Empty<string>() && args.Length > 1) ? args[0] : "";
        }

        public static bool IsDetachedRequired(string[] args) => DetachedRequiredCommands.Contains(GetCommand(args));

        public static bool IsForegroundRequired(string[] args) => ForegroundRequiredCommands.Contains(GetCommand(args));

        public static bool IsNoProxyRequired(string[] args) => NoProxyRequiredCommands.Contains(GetCommand(args));

        public static bool IsInValidArguments(string[] args)
        {
            return !IsForegroundRequired(args) && !IsNoProxyRequired(args);
        }
        public async Task<CommandOutcome> ExecuteAsync(string[] args)
        {
            var (_, execHost) = OllamaProxy.GetHosts();

            // Always run the underlying ollama command against the execution host (11435).
            // For 'serve', this blocks until ollama exits. For other commands, it returns when done.
            var exitCode = await OllamaProcess.RunOllamaAsync(args, execHost);
            Environment.Exit(exitCode);
            return CommandOutcome.Executed;
        }
    }
}
