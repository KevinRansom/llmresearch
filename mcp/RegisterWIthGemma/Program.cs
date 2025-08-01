using System.Diagnostics;
using System.Text;
using System.Text.Json;

class MCPClient
{
    static async Task Main()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "SampleMcpServer.dll",
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();

        var stdin = process.StandardInput;
        var stdout = process.StandardOutput;

		var request = new Dictionary<string, object>
		{
		    { "jsonrpc", "2.0" },
		    { "id", "1" },
		    { "method", "GetRandomNumber" },
		    { "params", new Dictionary<string, object>
		        {
		            { "min", 10 },
		            { "max", 50 }
		        }
		    }
		};

        string jsonRequest = JsonSerializer.Serialize(request);
        await stdin.WriteLineAsync(jsonRequest);

        string responseLine = await stdout.ReadLineAsync();
        Console.WriteLine($"Response: {responseLine}");
    }
}
