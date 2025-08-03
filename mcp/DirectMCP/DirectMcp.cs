using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

class Program
{
    static readonly HttpClient client = new();

    static void Main()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var request = JsonSerializer.Deserialize<JsonRpcRequest>(line, options);

                if (request == null)
                {
                    var error = new
                    {
                        jsonrpc = "2.0",
                        id = (string?)null,
                        error = new
                        {
                            code = -32700,
                            message = "Deserializer returned null — unable to parse JSON-RPC request.",
                            data = new
                            {
                                input = line,
                                note = "Payload was structurally valid but deserialized to null."
                            }
                        }
                    };

                    Console.WriteLine(JsonSerializer.Serialize(error, options));
                    continue;
                }

                var response = HandleRequest(request);
                Console.WriteLine(JsonSerializer.Serialize(response, options));
            }
            catch (JsonException ex)
            {
                var error = new
                {
                    jsonrpc = "2.0",
                    id = (string?)null,
                    error = new
                    {
                        code = -32700,
                        message = "JSON parsing error during deserialization.",
                        data = new
                        {
                            input = line,
                            exception = ex.Message
                        }
                    }
                };

                Console.WriteLine(JsonSerializer.Serialize(error, options));
            }
        }
    }

    static JsonRpcResponse HandleRequest(JsonRpcRequest request)
    {
        if (request.method == "registerTool")
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            // POST to Gemma’s model server
            var response = client.PostAsync("http://127.0.0.1:62097/api/tool", content).Result;

            // Optional: log success/failure to stdout
            Console.WriteLine($"Registration sent, status: {response.StatusCode}");
        }

        return new JsonRpcResponse
        {
            id = request.id,
            result = $"Echo: {request.method} received"
        };
    }

}

public class JsonRpcRequest
{
    public string? jsonrpc { get; set; }
    public string? method { get; set; }
    public object? @params { get; set; }
    public object? id { get; set; }
}

public class JsonRpcResponse
{
    public string jsonrpc { get; set; } = "2.0";
    public object? result { get; set; }
    public object? id { get; set; }
}
