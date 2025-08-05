using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

class DirectMcp
{
    static readonly HttpClient client = new();

    static async Task Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        // Start tool handler endpoint server on a background thread
        _ = Task.Run(() => ToolEndpointServer.StartServer(9876));
        _ = Task.Run(() => OllamaProxyServer.StartOllamaProxy());

        // Internal registration payload—merged from RegisterWithGemma
        var registration = new JsonRpcRequest
        {
            JsonRpcVersion = "2.0",
            Method = "registerTool",
            Params = null,
            Id = Guid.NewGuid().ToString()
        };

        // Handle registration payload directly without inter-process
        var response = await HandleRequest(registration);
        Console.WriteLine(JsonConvert.SerializeObject(response));

        // Enter console loop to handle additional JSON-RPC requests
        while (true)
        {
            var line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var request = JsonConvert.DeserializeObject<JsonRpcRequest>(line);

                if (request == null)
                {
                    var error = new JsonRpcResponse
                    {
                        JsonRpcVersion = "2.0",
                        Id = null,
                        Error = new JsonRpcError
                        {
                            Code = -32700,
                            Message = "Deserializer returned null — unable to parse JSON-RPC request.",
                            Data = new { input = line }
                        }
                    };

                    Console.WriteLine(JsonConvert.SerializeObject(error));
                    continue;
                }

                var liveResponse = await HandleRequest(request);
                Console.WriteLine(JsonConvert.SerializeObject(liveResponse));
            }
            catch (JsonException ex)
            {
                var error = new JsonRpcResponse
                {
                    JsonRpcVersion = "2.0",
                    Id = null,
                    Error = new JsonRpcError
                    {
                        Code = -32700,
                        Message = "JSON parsing error during deserialization.",
                        Data = new { input = line, exception = ex.Message }
                    }
                };

                Console.WriteLine(JsonConvert.SerializeObject(error));
            }
        }
    }

    static async Task<JsonRpcResponse> HandleRequest(JsonRpcRequest request)
    {
        if (request.Method == "registerTool")
        {
            var payload = new
            {
                model = "gemma3:4B",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = "Tell me something fascinating about the history of Istanbul." }
                },
                tools = new[]
                {
                    new
                    {
                        tool_name = "toolName",
                        handler = "http://localhost:9876/tools/toolName",
                        description = "A tool that retrieves information based on paramA.",
                        parameters = new
                        {
                            type = "object",
                            properties = new
                            {
                                paramA = new
                                {
                                    type = "string",
                                    description = "Location or keyword"
                                }
                            },
                            required = new[] { "paramA" }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:11434/api/chat", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            dynamic? result = JsonConvert.DeserializeObject<dynamic>(responseContent);

            if (result?.tool_calls != null)
            {
                foreach (var call in result.tool_calls)
                {
                    var simulatedResponse = new
                    {
                        tool_responses = new[]
                        {
                            new
                            {
                                tool_name = (string)call.function_name,
                                response = new { result = $"Simulated result for {call.parameters.paramA}" }
                            }
                        }
                    };
                    // You can emit this to console or log if needed
                }
            }

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = $"Echo: {request.Method} handled as inline tool definition"
            };
        }

        return new JsonRpcResponse
        {
            Id = request.Id,
            Error = new JsonRpcError
            {
                Code = -32601,
                Message = $"Method '{request.Method}' not found"
            }
        };
    }
}

// JSON-RPC support types

public class JsonRpcRequest
{
    [JsonProperty("jsonrpc")]
    public string? JsonRpcVersion { get; set; }

    [JsonProperty("method")]
    public string? Method { get; set; }

    [JsonProperty("params")]
    public object? Params { get; set; }

    [JsonProperty("id")]
    public object? Id { get; set; }
}

public class JsonRpcResponse
{
    [JsonProperty("jsonrpc")]
    public string JsonRpcVersion { get; set; } = "2.0";

    [JsonProperty("result")]
    public object? Result { get; set; }

    [JsonProperty("error")]
    public JsonRpcError? Error { get; set; }

    [JsonProperty("id")]
    public object? Id { get; set; }
}

public class JsonRpcError
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("data")]
    public object? Data { get; set; }
}
