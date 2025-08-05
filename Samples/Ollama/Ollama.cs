using System;
using System.Threading.Tasks;

class Ollama
{
    static async Task Main()
    {
        var proxy = new OllamaProxy();
        if (await proxy.IsProxyAlreadyRunningAsync())
        {
            Console.WriteLine("[?] Detected sibling Ollama proxy running. Exiting quietly.");
            return;
        }

        await proxy.StartAsync();
    }
}

