using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

class RegisterWithGemma
{
    static void Main()
    {
        var registration = new
        {
            jsonrpc = "2.0",
            method = "registerTool",
            @params = new
            {
                tools = new[]
                {
                    new
                    {
                        name = "toolName",
                        description = "Describe what it does",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                paramA = new
                                {
                                    type = "string",
                                    description = "Example parameter"
                                }
                            },
                            required = new[] { "paramA" }
                        }
                    }
                }
            }
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
