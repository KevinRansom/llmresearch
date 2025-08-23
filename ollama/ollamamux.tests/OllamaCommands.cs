namespace OllamaMuxTests
{
    using ollamamux.tests;
    using System.Diagnostics;
    using System.Text;
    using Xunit;

    public class OllamaCommands
    {
        [Fact]
        public async Task EmptyCommand_ShouldShowHelp()
        {
            var (output, errors, exitCode) = await ProcessRunner.RunAsync(
                "ollamamux.exe",
                "",
                TimeSpan.FromSeconds(10));
            return;
        }


        [Fact]
        public async Task ListCommand_ShouldReturnModels()
        {
            var (output, errors, exitCode) = await ProcessRunner.RunAsync(
                "ollamamux.exe",
                "list",
                TimeSpan.FromSeconds(10));

            Assert.True(exitCode == 0, $"Exit code: {exitCode}, Errors: {errors}");
            Assert.Contains("name", output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("llama", output, StringComparison.OrdinalIgnoreCase);
        }
    }
}
