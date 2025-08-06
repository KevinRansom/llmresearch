namespace OllamaMux
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    class Program
    {
        static async Task Main(string[] args)
        {
            var handler = new OllamaCommandHandler();
            if (handler.Execute(args) == OllamaCommandHandler.CommandOutcome.HelpDisplayed)
            {
                //Nothing to do here, help was displayed
            }
            else if (handler.Execute(args) == OllamaCommandHandler.CommandOutcome.Executed)
            {
                var proxy = new OllamaProxy();
                if (await proxy.IsProxyAlreadyRunningAsync())
                {
                    Console.WriteLine("[?] Detected sibling Ollama proxy running..");
                }

                await proxy.StartAsync();

                // Here we go to ollama
                await OllamaProcess.RunAsync(args);
            }
            else
            {
                // An error has been reported do nothing 
            }
        }
    }
}
