using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

public class Program
{
    public static async void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
        builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

        // Add the MCP services: the transport to use (stdio) and the tools to register.
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<RandomNumberTools>();

        //McpToolEnumerator.DumpRegisteredTools();

        await builder.Build().RunAsync();
    }
}

public static class McpToolEnumerator
{
    public static void DumpRegisteredTools()
    {
        static void WriteLogMessage(string message)
        {
            File.AppendAllLines(@"c:\Temp\logfile.fil.txt", [message]);
        }

        var toolMap = new Dictionary<string, string>();
        WriteLogMessage("=== MCP Tool Enumeration ===");
        WriteLogMessage($"Assemblies: {AppDomain.CurrentDomain.GetAssemblies().Length}");

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            WriteLogMessage($"Assembly: {assembly.Location}");
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }

            foreach (var type in types)
            {
                WriteLogMessage($"Type: {type.AssemblyQualifiedName}");
                if (!type.IsPublic || !type.IsClass) continue;

                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    //                    WriteLogMessage($"Method: {method.Name}");
                    var hasToolAttribute = method.GetCustomAttributes()
                        .Any(attr => attr.GetType().Name == "McpServerToolAttribute");

                    if (!hasToolAttribute) continue;

                    var signature = $"{type.FullName}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";
                    toolMap[method.Name] = signature;
                }
            }
        }

        WriteLogMessage("=== MCP Registered Tools ===");
        WriteLogMessage($"toolMap: {toolMap.Count}");
        foreach (var kvp in toolMap.OrderBy(kvp => kvp.Key))
        {
            WriteLogMessage($"{kvp.Key}: {kvp.Value}");
        }
    }
}
