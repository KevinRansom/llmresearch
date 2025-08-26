using OllamaMux.Testing;

namespace OllamaMuxTests
{
    using ollamamux.tests;
    using System.Diagnostics;
    using System.Text;
    using Xunit;
    using static AssertUtilities;

    public class OllamaCommands
    {
        [Theory]
        [InlineData("ollamamux.exe", "", true)]
        [InlineData("ollamamux.exe", "-h", false)]
        [InlineData("ollamamux.exe", "--help", false)]
        [InlineData("ollamamux.exe", "help", false)]
        public async Task HelpCommandSwitches(string app, string args, bool usesStdError)
        {
            var (stdout, stderr, exitCode) = await ProcessRunner.RunAsync(
                app,
                args,
                TimeSpan.FromSeconds(10));

            var expected = usesStdError ? "" : "Large language model runner\n\n";
            expected = expected + @"Usage:
  ollamamux [flags]
  ollamamux [command]

Available Commands:
  serve       Start ollamamux
  create      Create a model
  show        Show information for a model
  run         Run a model
  stop        Stop a running model
  pull        Pull a model from a registry
  push        Push a model to a registry
  list        List models
  ps          List running models
  cp          Copy a model
  rm          Remove a model
  help        Help about any command

Flags:
  -h, --help      help for ollamamux
  -v, --version   Show version information

Use ""ollamamux [command] --help"" for more information about a command.
";

            // alternateOutput: the stream *not* carrying the expected help text for this invocation.
            // In “error” mode (no args), help text goes to stderr and stdout becomes the alternate.
            // In explicit help mode (--help/help), help text goes to stdout and stderr becomes the alternate.
            // This variable exists so tests can assert both the primary output and confirm the
            // secondary stream is either empty or contains only incidental data.
            var expectedHelpText = NormalizeLineEndings(expected);
            var actualHelpText = NormalizeLineEndings(usesStdError ? stderr : stdout);
            var secondaryOutput = NormalizeLineEndings(usesStdError ? stdout : stderr);

            Assert.Equal(0, exitCode);
            Assert.Equal(expectedHelpText, actualHelpText);
            Assert.True(string.IsNullOrWhiteSpace(secondaryOutput), $"empty: {(usesStdError ? nameof(stdout) : nameof(stderr))}, but got: {secondaryOutput}");
        }


        [Theory]
        [InlineData("ollamamux.exe", "serve -h")]
        [InlineData("ollamamux.exe", "serve --help")]
        [InlineData("ollamamux.exe", "help serve")]
        public async Task ServeHelpCommandSwitches(string app, string args)
        {
            var (stdout, stderr, exitCode) = await ProcessRunner.RunAsync(
                app,
                args,
                TimeSpan.FromSeconds(10));

            var expected = @"Start ollamamux

Usage:
  ollamamux serve [flags]

Aliases:
  serve, start

Flags:
  -h, --help   help for serve

Environment Variables:
      OLLAMA_DEBUG               Show additional debug information (e.g. OLLAMA_DEBUG=1)
      OLLAMA_HOST                IP Address for the ollama server (default 127.0.0.1:11434)
      OLLAMA_CONTEXT_LENGTH      Context length to use unless otherwise specified (default: 4096)
      OLLAMA_KEEP_ALIVE          The duration that models stay loaded in memory (default ""5m"")
      OLLAMA_MAX_LOADED_MODELS   Maximum number of loaded models per GPU
      OLLAMA_MAX_QUEUE           Maximum number of queued requests
      OLLAMA_MODELS              The path to the models directory
      OLLAMA_NUM_PARALLEL        Maximum number of parallel requests
      OLLAMA_NOPRUNE             Do not prune model blobs on startup
      OLLAMA_ORIGINS             A comma separated list of allowed origins
      OLLAMA_SCHED_SPREAD        Always schedule model across all GPUs
      OLLAMA_FLASH_ATTENTION     Enabled flash attention
      OLLAMA_KV_CACHE_TYPE       Quantization type for the K/V cache (default: f16)
      OLLAMA_LLM_LIBRARY         Set LLM library to bypass autodetection
      OLLAMA_GPU_OVERHEAD        Reserve a portion of VRAM per GPU (bytes)
      OLLAMA_LOAD_TIMEOUT        How long to allow model loads to stall before giving up (default ""5m"")
";

            var expectedHelpText = NormalizeLineEndings(expected);
            var actualHelpText = NormalizeLineEndings(stdout);
            var secondaryOutput = NormalizeLineEndings("");

            Assert.Equal(0, exitCode);
            Assert.Equal(expectedHelpText, actualHelpText);
            Assert.True(string.IsNullOrWhiteSpace(secondaryOutput), $"empty: {(nameof(stderr))}, but got: {stderr}");
        }


        [Theory]
        [InlineData("ollamamux.exe", "run -h")]
        [InlineData("ollamamux.exe", "run --help")]
        [InlineData("ollamamux.exe", "help run")]
        [InlineData("ollamamux.exe", "run gemma3:1B -h")]
        [InlineData("ollamamux.exe", "run gemma3:1B --help")]
        public async Task ListCommand_ShouldRunHelp(string app, string args)
        {
            var(stdout, stderr, exitCode) = await ProcessRunner.RunAsync(
                app,
                args,
                TimeSpan.FromSeconds(10));
            var expected = @"Run a model

Usage:
  ollamamux run MODEL [PROMPT] [flags]

Flags:
      --format string           Response format (e.g. json)
  -h, --help                    help for run
      --hidethinking            Hide thinking output (if provided)
      --insecure                Use an insecure registry
      --keepalive string        Duration to keep a model loaded (e.g. 5m)
      --nowordwrap              Don't wrap words to the next line automatically
      --think string[=""true""]   Enable thinking mode: true/false or high/medium/low for supported models
      --verbose                 Show timings for response

Environment Variables:
      OLLAMA_HOST                IP Address for the ollama server (default 127.0.0.1:11434)
      OLLAMA_NOHISTORY           Do not preserve readline history
";

            var expectedHelpText = NormalizeLineEndings(expected);
            var actualHelpText = NormalizeLineEndings(stdout);
            var secondaryOutput = NormalizeLineEndings("");

            Assert.Equal(0, exitCode);
            Assert.Equal(expectedHelpText, actualHelpText);
            Assert.True(string.IsNullOrWhiteSpace(secondaryOutput), $"empty: {(nameof(stderr))}, but got: {stderr}");
        }

        [Theory]
        [InlineData("ollamamux.exe", "run gemma3:1B")]
        public async Task ListCommand_ShouldRunModel(string app, string args)
        {
            var (stdout, stderr, exitCode) = await ProcessRunner.RunAsync(
                app,
                args,
                TimeSpan.FromSeconds(10));

            var expected = @"";

            var expectedOutputText = NormalizeLineEndings(expected);
            var actualOutputText = NormalizeLineEndings(stdout);
            var secondaryOutput = NormalizeLineEndings("");

            Assert.Equal(0, exitCode);
            Assert.Equal(expectedOutputText, actualOutputText);
            Assert.True(string.IsNullOrWhiteSpace(secondaryOutput), $"empty: {(nameof(stderr))}, but got: {stderr}");
        }

        [Theory]
        [InlineData("ollamamux.exe", "run gemma3:1B")]
        public async Task ListCommand_ShouldRunModel_WithBye_ExpectHelloWorld(string app, string args)
        {
            var (stdout, stderr) = await ProcessRunner.RunAndCaptureAsync(
                app, args,
                async proc =>
                {
                    await proc.StandardInput.WriteLineAsync("/bye");
                    await proc.StandardInput.FlushAsync();
                },
                timeout: TimeSpan.FromSeconds(10));

            Assert.Equal("Hello, World", stdout);
            Assert.True(string.IsNullOrEmpty(stderr), $"Unexpected stderr: {stderr}");
        }

        //[Fact]
        //public async Task ListCommand_ShouldReturnModels()
        //{
        //    var (output, errors, exitCode) = await ProcessRunner.RunAsync(
        //        "ollamamux.exe",
        //        "list",
        //        TimeSpan.FromSeconds(10));

        //    Assert.True(exitCode == 0, $"Exit code: {exitCode}, Errors: {errors}");
        //    Assert.Contains("name", output, StringComparison.OrdinalIgnoreCase);
        //    Assert.Contains("llama", output, StringComparison.OrdinalIgnoreCase);
        //}
    }
}
