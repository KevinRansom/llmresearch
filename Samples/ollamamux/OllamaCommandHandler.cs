using System;
using System.Collections.Generic;
using System.Linq;

namespace Ollamamux
{
    public class OllamaCommandHandler
    {
        private class CommandInfo
        {
            public required string Description { get; set; }
            public required Func<List<string>, CommandOutcome> Execute { get; set; }
            public required Action ShowHelp { get; set; }
            public int ExpectedArgs { get; set; } = 0;
        }

        public enum CommandOutcome
        {
            Executed,
            HelpDisplayed,
            Error
        }

        private readonly Dictionary<string, CommandInfo> _commands;
        private readonly string[] empty = Array.Empty<string>();

        public OllamaCommandHandler()
        {
            _commands = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["serve"] = new CommandInfo
                {
                    Description = "Start ollama",
                    Execute = args =>
                    {
                        Console.WriteLine("Starting server...");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowServeHelp,
                    ExpectedArgs = 0
                },

                ["create"] = new CommandInfo
                {
                    Description = "Create a model",
                    Execute = args =>
                    {
                        Console.WriteLine("Creating model...");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowCreateHelp,
                    ExpectedArgs = 1
                },

                ["show"] = new CommandInfo
                {
                    Description = "Show information for a model",
                    Execute = args =>
                    {
                        Console.WriteLine($"Showing model: {string.Join(" ", args)}");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowShowHelp,
                    ExpectedArgs = 1
                },

                ["run"] = new CommandInfo
                {
                    Description = "Run a model",
                    Execute = args =>
                    {
                        Console.WriteLine($"Running model: {string.Join(" ", args)}");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowRunHelp,
                    ExpectedArgs = 2
                },

                ["stop"] = new CommandInfo
                {
                    Description = "Stop a running model",
                    Execute = args =>
                    {
                        Console.WriteLine($"Stopping model: {string.Join(" ", args)}");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowStopHelp,
                    ExpectedArgs = 1
                },

                ["pull"] = new CommandInfo
                {
                    Description = "Pull a model from a registry",
                    Execute = args =>
                    {
                        Console.WriteLine($"Pulling model: {string.Join(" ", args)}");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowPullHelp,
                    ExpectedArgs = 1
                },

                ["push"] = new CommandInfo
                {
                    Description = "Push a model to a registry",
                    Execute = args =>
                    {
                        Console.WriteLine($"Pushing model: {string.Join(" ", args)}");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowPushHelp,
                    ExpectedArgs = 1
                },

                ["list"] = new CommandInfo
                {
                    Description = "List models",
                    Execute = args =>
                    {
                        Console.WriteLine("Listing models...");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowListHelp,
                    ExpectedArgs = 0
                },

                ["ps"] = new CommandInfo
                {
                    Description = "List running models",
                    Execute = args =>
                    {
                        Console.WriteLine("Listing running models...");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowPsHelp,
                    ExpectedArgs = 0
                },

                ["cp"] = new CommandInfo
                {
                    Description = "Copy a model",
                    Execute = args =>
                    {
                        Console.WriteLine($"Copying model: {string.Join(" ", args)}");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowCpHelp,
                    ExpectedArgs = 2
                },

                ["rm"] = new CommandInfo
                {
                    Description = "Remove a model",
                    Execute = args =>
                    {
                        Console.WriteLine($"Removing model(s): {string.Join(" ", args)}");
                        return CommandOutcome.Executed;
                    },
                    ShowHelp = ShowRmHelp,
                    ExpectedArgs = 1
                },

                ["help"] = new CommandInfo
                {
                    Description = "Help about any command",
                    Execute = args => DisplayHelp(),
                    ShowHelp = ShowHelpHelp,
                    ExpectedArgs = 0
                }
            };
        }

        public CommandOutcome Execute(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
                return DisplayHelp();

            if (args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                if (args.Length > 1 && _commands.TryGetValue(args[1], out var cmd))
                    return DisplayHelp(cmd.ShowHelp);
                else
                    return DisplayHelp();
            }

            var command = args[0];
            var tail = args.Skip(1).ToList();

            if (_commands.TryGetValue(command, out var info))
            {
                if (tail.Count > 0 && (tail[0] == "--help" || tail[0] == "-h"))
                    return DisplayHelp(info.ShowHelp);

                if (tail.Count != info.ExpectedArgs)
                {
                    Console.WriteLine($"Error: accepts {info.ExpectedArgs} arg(s), received {tail.Count}");
                    return CommandOutcome.Error;
                }

                return info.Execute.Invoke(tail);
            }

            Console.WriteLine($"Unknown command: {command}\n");
            return DisplayHelp();
        }

        private CommandOutcome DisplayHelp(Action showHelp)
        {
            showHelp?.Invoke();
            return CommandOutcome.HelpDisplayed;
        }
        private CommandOutcome DisplayHelp()
        {
            Console.WriteLine("Large language model runner\n");
            Console.WriteLine("Usage:\n  ollama [flags]\n  ollama [command]\n");
            Console.WriteLine("Available Commands:");
            foreach (var kvp in _commands)
                Console.WriteLine($"  {kvp.Key,-10} {kvp.Value.Description}");
            Console.WriteLine("\nFlags:\n  -h, --help      help for ollama\n  -v, --version   Show version information");
            Console.WriteLine("\nUse \"ollama [command] --help\" or \"ollama help [command]\" for more information about a command.");
            return CommandOutcome.HelpDisplayed;
        }

        private void PrintHelp(string title, string usage, string[] aliases, string[] flags, string[] envVars)
        {
            Console.WriteLine($"{title}\n");
            Console.WriteLine($"Usage:\n  {usage}\n");

            if (aliases.Length > 0)
            {
                Console.WriteLine("Aliases:");
                foreach (var alias in aliases)
                    Console.WriteLine($"  {alias}");
                Console.WriteLine();
            }

            if (flags.Length > 0)
            {
                Console.WriteLine("Flags:");
                foreach (var flag in flags)
                    Console.WriteLine($"  {flag}");
                Console.WriteLine();
            }

            if (envVars.Length > 0)
            {
                Console.WriteLine("Environment Variables:");
                foreach (var env in envVars)
                    Console.WriteLine($"  {env}");
            }
        }
        private void ShowServeHelp() => PrintHelp("Start ollama", "ollama serve [flags]", new[] { "serve", "start" }, new[] { "-h, --help   help for serve" }, new[]
        {
            "OLLAMA_DEBUG               Show additional debug information (e.g. OLLAMA_DEBUG=1)",
            "OLLAMA_HOST                IP Address for the ollama server (default 127.0.0.1:11434)",
            "OLLAMA_KEEP_ALIVE          The duration that models stay loaded in memory (default \"5m\")",
            "OLLAMA_MAX_LOADED_MODELS   Maximum number of loaded models per GPU",
            "OLLAMA_MAX_QUEUE           Maximum number of queued requests",
            "OLLAMA_MODELS              The path to the models directory",
            "OLLAMA_NUM_PARALLEL        Maximum number of parallel requests",
            "OLLAMA_NOPRUNE             Do not prune model blobs on startup",
            "OLLAMA_ORIGINS             A comma separated list of allowed origins",
            "OLLAMA_SCHED_SPREAD        Always schedule model across all GPUs",
            "OLLAMA_FLASH_ATTENTION     Enabled flash attention",
            "OLLAMA_KV_CACHE_TYPE       Quantization type for the K/V cache (default: f16)",
            "OLLAMA_LLM_LIBRARY         Set LLM library to bypass autodetection",
            "OLLAMA_GPU_OVERHEAD        Reserve a portion of VRAM per GPU (bytes)",
            "OLLAMA_LOAD_TIMEOUT        How long to allow model loads to stall before giving up (default \"5m\")"
        });

        private void ShowCreateHelp() => PrintHelp("Create a model", "ollama create MODEL [flags]", empty, new[] { "-h, --help   help for create" }, new[] { "OLLAMA_HOST                IP Address for the ollama server (default 127.0.0.1:11434)" });

        private void ShowShowHelp() => PrintHelp("Show information for a model", "ollama show MODEL [flags]", empty, new[] { "-h, --help", "--license", "--modelfile", "--parameters", "--system", "--template", "-v, --verbose" }, new[] { "OLLAMA_HOST                IP Address for the ollama server (default 127.0.0.1:11434)" });

        private void ShowRunHelp() => PrintHelp("Run a model", "ollama run MODEL PROMPT [flags]", empty, new[] { "--format string", "-h, --help", "--hidethinking", "--insecure", "--keepalive string", "--nowordwrap", "--think string", "--verbose" }, new[] { "OLLAMA_HOST", "OLLAMA_NOHISTORY" });

        private void ShowStopHelp() => PrintHelp("Stop a running model", "ollama stop MODEL [flags]", empty, new[] { "-h, --help" }, new[] { "OLLAMA_HOST" });

        private void ShowPullHelp() => PrintHelp("Pull a model from a registry", "ollama pull MODEL [flags]", empty, new[] { "-h, --help", "--insecure" }, new[] { "OLLAMA_HOST" });

        private void ShowPushHelp() => PrintHelp("Push a model to a registry", "ollama push MODEL [flags]", empty, new[] { "-h, --help", "--insecure" }, new[] { "OLLAMA_HOST" });

        private void ShowListHelp() => PrintHelp("List models", "ollama list [flags]", new[] { "list", "ls" }, new[] { "-h, --help" }, new[] { "OLLAMA_HOST" });

        private void ShowPsHelp() => PrintHelp("List running models", "ollama ps [flags]", empty, new[] { "-h, --help" }, new[] { "OLLAMA_HOST" });

        private void ShowCpHelp() => PrintHelp("Copy a model", "ollama cp SOURCE DESTINATION [flags]", empty, new[] { "-h, --help" }, new[] { "OLLAMA_HOST" });

        private void ShowRmHelp() => PrintHelp("Remove a model", "ollama rm MODEL [flags]", empty, new[] { "-h, --help" }, new[] { "OLLAMA_HOST" });

        private void ShowHelpHelp() => PrintHelp("Help about any command", "ollama help [command]", empty, new[] { "-h, --help" }, empty);
    }
}
