using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;

static class ToolEndpointServer
{
    static public void StartServer(uint port)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/tools/");
        listener.Start();
        Console.WriteLine($"Listening on http://localhost:{port}/tools/ ...");

        while (true)
        {
            var context = listener.GetContext();
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST" && request?.Url?.AbsolutePath == "/tools/toolName")
            {
                using var reader = new StreamReader(request.InputStream);
                var body = reader.ReadToEnd();

                // Parse paramA from JSON manually or using Newtonsoft.Json
                dynamic? data = JsonConvert.DeserializeObject(body);
                string paramA = data?.paramA ?? "unknown";
                string reply = $"Tool invoked with paramA = {paramA}";

                var buffer = Encoding.UTF8.GetBytes("{ \"content\": \"" + reply + "\" }");
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.Close();
            }
            else
            {
                response.StatusCode = 404;
                response.Close();
            }
        }
    }
}
