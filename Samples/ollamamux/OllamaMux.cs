namespace OllamaMux
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static async Task Main(string[] args)
        {
            var proxyMain = new OllamaProxy();
            var (muxHostMain, execHostMain) = OllamaProxy.GetHosts();
            var detached = !args.Contains("serve");
            if (!await proxyMain.IsProxyAlreadyRunningAsync())
            {
                using var muxLock = OllamaProxy.TryAcquireMuxLock();
                if (muxLock != null)
                {
                    proxyMain.StartProxy(detached);
                }

                await OllamaProxy.WaitForHealthyAsync(proxyMain, TimeSpan.FromSeconds(3));
            }

            var handler = new OllamaCommandHandler();
            var commandOutcome = await handler.ExecuteAsync(args);
        }
    }
}
