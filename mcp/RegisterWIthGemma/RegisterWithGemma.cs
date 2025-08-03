using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

class GemmaToolRegistration
{
    static void Main()
    {
        // Prepare registration payload
        var registration = new
        {
            jsonrpc = "2.0",
            method = "registerTool",
            @params = new
            {
                name = "EchoTool",
                description = "Echoes incoming JSON-RPC method names.",
                methods = new[]
                {
                    new
                    {
                        name = "diagnose",
                        description = "Returns the method name and params for inspection.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                input = new { type = "string" }
                            },
                            required = new[] { "input" }
                        },
                        returns = new
                        {
                            type = "object",
                            properties = new
                            {
                                report = new { type = "string" }
                            }
                        }
                    }
                }
            },
            id = 1
        };

        string requestJson = JsonSerializer.Serialize(registration);

        // Launch RegisterWithGemma.exe
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "DirectMCP.exe",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        // Send registration JSON
        process.StandardInput.WriteLine(requestJson);
        process.StandardInput.Flush();

        // Read and display response from Gemma
        string? responseLine;
        while ((responseLine = process.StandardOutput.ReadLine()) != null)
        {
            Console.WriteLine($"Gemma: {responseLine}");
        }

        process.WaitForExit();
    }
}
