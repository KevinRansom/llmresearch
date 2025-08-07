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
            var commandOutcome = handler.Execute(args);
            switch(commandOutcome)
            {
                case OllamaCommandHandler.CommandOutcome.HelpDisplayed:
                    //Nothing to do here, help was displayed
                    break;

                case OllamaCommandHandler.CommandOutcome.Executed:

                    var proxy = new OllamaProxy();
                    if (await proxy.IsProxyAlreadyRunningAsync())
                    {
                        Console.WriteLine("[?] Detected sibling Ollama proxy running..");
                    }
                    var ollamaExecutionHost = proxy.StartProxy();

                    // Here we go to ollama
                    await OllamaProcess.RunAsync(args, ollamaExecutionHost);
                    break;

                case OllamaCommandHandler.CommandOutcome.Error:
                    // An error has been reported do nothing 
                    break;
            }
        }
    }
}
