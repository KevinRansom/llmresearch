using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SampleMcpServer;
using System.Reflection;
using System.Text.Json;


var stdoutputLog = new StreamWriter("mcp_stdoutput.log") { AutoFlush = true };
var stderrorLog = new StreamWriter("mcp_stderror.log") { AutoFlush = true };
Console.SetOut(new TeeStream(Console.Out, stdoutputLog));
Console.SetError(new TeeStream(Console.Error, stderrorLog));

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
//    .WithStdioServerTransport()
    .WithCustomServerTransport(new StreamServerTransport(
        new CancellableStdinStream(Console.OpenStandardInput()),
        new BufferedStream(tee),
        "SampleMcpServer",
        loggerFactory));
    .WithTools<RandomNumberTools>();

McpToolManifestLogger.DumpRegisteredTools();

await builder.Build().RunAsync();
public static class McpToolManifestLogger
{
    public static void DumpRegisteredTools(string logPath = "registeredTools.log", string jsonPath = "toolManifest.json")
    {
        void Log(string message)
        {
            File.AppendAllLines(logPath, new[] { $"{DateTime.UtcNow:u} - {message}" });
        }

        var manifest = new List<object>();

        try
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var asmName = asm.GetName().Name;
                Log($"🔍 Scanning assembly: {asmName}");

                if (!asmName.Equals("SampleMcpServer", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var type in asm.GetTypes().Where(t => t.Name.Contains("Tool")))
                {
                    Log($"📦 Found tool type: {type.FullName}");
                    var methodRecords = new List<object>();

                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                           BindingFlags.Static | BindingFlags.Instance))
                    {
                        var hasToolAttr = method.GetCustomAttributes(false)
                                                .Any(attr => attr.GetType().FullName == "ModelContextProtocol.Server.McpServerToolAttribute");

                        var parameters = method.GetParameters()
                                               .Select(p => new { Name = p.Name, Type = p.ParameterType.FullName })
                                               .ToList();

                        var visibility = method.IsPublic ? "public"
                                       : method.IsPrivate ? "private"
                                       : method.IsFamily ? "protected"
                                       : "internal";

                        var entry = new
                        {
                            Method = method.Name,
                            ReturnType = method.ReturnType.FullName,
                            Parameters = parameters,
                            Accessibility = visibility,
                            HasMcpServerToolAttribute = hasToolAttr
                        };

                        methodRecords.Add(entry);

                        if (hasToolAttr)
                        {
                            var argList = string.Join(", ", parameters.Select(p => $"{p.Name}: {p.Type}"));
                            Log($"✔️ Registered tool method: {type.FullName}.{method.Name}({argList})");
                        }
                    }

                    manifest.Add(new
                    {
                        Type = type.FullName,
                        Methods = methodRecords
                    });
                }
            }

            File.WriteAllText(jsonPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions
            {
                WriteIndented = true
            }));

            Log($"✅ Tool manifest written to: {jsonPath}");
        }
        catch (Exception ex)
        {
            Log($"❌ Error: {ex.Message}");
        }
    }
}
