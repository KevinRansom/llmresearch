using Microsoft.Extensions.Logging;
using System;

namespace OllamaMux
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class OllamaProxy
    {
        private const int OllamaDefaultPort = 11434;
        private const int OllamaExecutionPort = 11435;
        private const string AckGuid = "2FE6FFE6-C2AC-4DC0-BFAD-2371B47AAE71";

        private readonly ILogger _log;

        public OllamaProxy() : this(Log.CreateFactory().CreateLogger<OllamaProxy>()) { }

        public OllamaProxy(ILogger logger)
        {
            _log = logger;
        }

        private static readonly SocketsHttpHandler HttpHandler = new()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 128,
            AllowAutoRedirect = false
        };

        private static readonly HttpClient Upstream = new(HttpHandler)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private static readonly HashSet<string> HopByHop = new(StringComparer.OrdinalIgnoreCase)
        {
            "Connection","Keep-Alive","Proxy-Authenticate","Proxy-Authorization","TE",
            "Trailer","Transfer-Encoding","Upgrade","Proxy-Connection"
        };

        private static readonly HashSet<string> SkipRequestHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Host", "Accept-Encoding"
        };

        private static readonly HashSet<string> SkipResponseHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Content-Length","Transfer-Encoding","Connection","Keep-Alive","Server","Date","Proxy-Connection"
        };

        // Checks the mux on 11434 via /__mux/health
        public async Task<bool> IsProxyAlreadyRunningAsync()
        {
            var (ollamaMuxHost, _) = GetHosts();
            var baseUri = NormalizeBaseUri(ollamaMuxHost);
            var probeUri = new Uri(baseUri, "__mux/health");

            try
            {
                using var resp = await Upstream.GetAsync(probeUri);
                var body = await resp.Content.ReadAsStringAsync();
                return resp.IsSuccessStatusCode &&
                       body.Contains("\"acknowledged\":true", StringComparison.OrdinalIgnoreCase) &&
                       body.Contains(AckGuid, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception _ex)
            {
                //@@@@_log.LogDebug(LogEvent.HealthCheckFailed, ex, "Health check failed for {ProbeUri}", probeUri);
                return false;
            }
        }

        // Checks whether the backend (ollama) is already listening on 11435
        public static async Task<bool> IsExecutionAlreadyRunningAsync(TimeSpan timeout)
        {
            var (_, execHost) = GetHosts();
            var uri = new Uri(execHost, UriKind.Absolute);

            var host = NormalizeProbeHost(uri.Host);
            var port = uri.Port;

            return await CanConnectAsync(host, port, timeout);
        }

        private Task StartAsync(string ollamaMuxHost, string ollamaExecutionHost)
        {
            var muxBase = NormalizeBaseUri(ollamaMuxHost);
            var execBase = NormalizeBaseUri(ollamaExecutionHost);

            if (UriEquals(muxBase, execBase))
                throw new InvalidOperationException("Mux host and execution host are identical — would loop.");

            var listener = new HttpListener();
            listener.Prefixes.Add(muxBase.ToString());
            listener.Start();

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    HttpListenerContext? ctx = null;
                    try
                    {
                        ctx = await listener.GetContextAsync();
                        _ = Task.Run(() => HandleRequestAsync(ctx, execBase));
                    }
                    catch (HttpListenerException hlex) when (hlex.ErrorCode == 995 || hlex.ErrorCode == 500)
                    {
                        if (!listener.IsListening) break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (Exception _ex)
                    {
                        //@@@@ _log.LogError(ex, "Accept loop error");
                        if (ctx != null)
                        {
                            try { ctx.Response.StatusCode = 500; ctx.Response.OutputStream.Close(); } catch { }
                        }
                    }
                }
            });

            //@@@@_log.LogInformation(LogEvent.ProxyStart, "Proxy listening on {MuxBase}", muxBase);
            //@@@@_log.LogInformation("Forwarding to Ollama at {ExecBase}", execBase);
            return Task.CompletedTask;
        }

        public string StartProxy(bool detached)
        {
            var (ollamaMuxHost, ollamaExecutionHost) = GetHosts();
            if (detached)
            {
                // Spawn a detached `serve` instance of *this* executable
                if (!OllamaProcess.TryLaunchRunProgramDetachedDetached(Path.ChangeExtension(Assembly.GetExecutingAssembly().Location, "exe"), "serve"))
                    throw new InvalidOperationException("Failed to launch ollamamux in detached mode.");
            }
            else
            {
                _ = StartAsync(ollamaMuxHost, ollamaExecutionHost);
            }
            return ollamaExecutionHost;
        }

        private async Task HandleRequestAsync(HttpListenerContext context, Uri targetBase)
        {
            var rid = Guid.NewGuid().ToString("n")[..8];
            using var _ = _log.BeginScope(new Dictionary<string, object> { ["rid"] = rid });

            var swTotal = Stopwatch.StartNew();
            var request = context.Request;
            var response = context.Response;

            try
            {
                await Task.Yield();

                //@@@@_log.LogInformation(LogEvent.RequestReceived,
                //@@@@    "→ {Method} {PathQuery} (from {Remote}) Headers: {Headers} [rid:{Rid}]",
                //@@@@    request.HttpMethod, request.Url?.PathAndQuery,
                //@@@@    request.RemoteEndPoint, FormatHeaders(request.Headers), rid);

                // Handle CORS preflight
                if (string.Equals(request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    WriteCors(request, response);
                    response.StatusCode = (int)HttpStatusCode.NoContent;
                    return;
                }

                // Prepare outbound request
                var targetUri = new Uri(targetBase, request.Url?.PathAndQuery ?? string.Empty);

                using var outgoing = new HttpRequestMessage(new HttpMethod(request.HttpMethod), targetUri);
                if (request.HasEntityBody)
                {
                    // Stream directly from inbound InputStream to upstream
                    outgoing.Content = new StreamContent(request.InputStream);
                
                    // ✅ First copy over all *general* headers (Host, Accept-Encoding still skipped)
                    CopyRequestHeaders(request, outgoing);
                
                    // ✅ Now try to preserve inbound content headers explicitly (in case the above skipped any)
                    foreach (string headerName in request.Headers)
                    {
                        if (!SkipRequestHeaders.Contains(headerName) && 
                            headerName.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                        {
                            outgoing.Content.Headers.TryAddWithoutValidation(headerName, request.Headers[headerName]);
                        }
                    }
                }
                else
                {
                    CopyRequestHeaders(request, outgoing);
                }

                // Send to upstream
                var swUpstream = Stopwatch.StartNew();
                using var upstreamResponse = await Upstream.SendAsync(outgoing, HttpCompletionOption.ResponseHeadersRead);
                swUpstream.Stop();

                //@@@@_log.LogInformation(LogEvent.UpstreamResponse,
                //@@@@    "← {Status} {Reason} in {Elapsed} ms [rid:{Rid}]",
                //@@@@    (int)upstreamResponse.StatusCode, upstreamResponse.ReasonPhrase,
                //@@@@    swUpstream.ElapsedMilliseconds, rid);

                // Write response
                response.StatusCode = (int)upstreamResponse.StatusCode;
                response.StatusDescription = upstreamResponse.ReasonPhrase ?? string.Empty;
                CopyResponseHeaders(upstreamResponse, response);
                WriteCors(request, response);

                await using (var respStream = await upstreamResponse.Content.ReadAsStreamAsync())
                {
                    await respStream.CopyToAsync(response.OutputStream);
                }
            }
            catch (Exception _ex)
            {
                //@@@@_log.LogError(ex, "Error handling request [rid:{Rid}]", rid);
                try
                {
                    response.StatusCode = 502;
            
                    // ✅ Always advertise a sensible type for the error payload
                    if (string.IsNullOrEmpty(response.ContentType))
                        response.ContentType = "text/plain; charset=utf-8";
            
                    using var writer = new StreamWriter(response.OutputStream);
                    await writer.WriteAsync("Proxy error");
                }
                catch { /* ignore secondary failures */ }
            }
            finally
            {
                swTotal.Stop();
                //@@@@_log.LogInformation(LogEvent.RequestCompleted,
                //@@@@    "✓ {Method} {PathQuery} {Status} in {Elapsed} ms total [rid:{Rid}]",
                //@@@@    request.HttpMethod, request.Url?.PathAndQuery,
                //@@@@    response.StatusCode, swTotal.ElapsedMilliseconds, rid);

                try { response.OutputStream.Close(); } catch { }
            }
        }

        private void CopyRequestHeaders(HttpListenerRequest src, HttpRequestMessage dst)
        {
            foreach (string? name in src.Headers.AllKeys)
            {
                if (string.IsNullOrEmpty(name)) continue;
                if (HopByHop.Contains(name) || SkipRequestHeaders.Contains(name))
                {
                    //@@@@_log.LogTrace(LogEvent.HeaderSkipped, "Skipping header {Header} during request", name);
                    continue;
                }

                var value = src.Headers[name];
                if (string.IsNullOrEmpty(value)) continue;

                if (dst.Content != null && name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                    dst.Content.Headers.TryAddWithoutValidation(name, value);
                else
                    dst.Headers.TryAddWithoutValidation(name, value);
            }
        }

        private void CopyResponseHeaders(HttpResponseMessage src, HttpListenerResponse dst)
        {
            foreach (var header in src.Headers)
            {
                if (SkipResponseHeaders.Contains(header.Key) || HopByHop.Contains(header.Key))
                {
                    //@@@@_log.LogTrace(LogEvent.HeaderSkipped, "Skipping header {Header} during response", header.Key);
                    continue;
                }
                TrySetHeader(dst, header.Key, header.Value);
            }

            foreach (var header in src.Content.Headers)
            {
                if (SkipResponseHeaders.Contains(header.Key) || HopByHop.Contains(header.Key))
                {
                    //@@@@_log.LogTrace(LogEvent.HeaderSkipped, "Skipping header {Header} during response", header.Key);
                    continue;
                }

                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    if (src.Content.Headers.ContentType != null)
                        dst.ContentType = src.Content.Headers.ContentType.ToString();
                }
                else
                {
                    TrySetHeader(dst, header.Key, header.Value);
                }
            }

            //@@@@_log.LogDebug("⇠ Response headers: {Headers}", string.Join(", ", src.Headers.Select(h => $"{h.Key}: {string.Join(";", h.Value)}")));
        }

        private static void TrySetHeader(HttpListenerResponse res, string key, IEnumerable<string> values)
        {
            try { res.Headers[key] = string.Join(", ", values); }
            catch { /* restricted by HttpListener; ignore */ }
        }

        private void WriteCors(HttpListenerRequest req, HttpListenerResponse res)
        {
            var origin = req.Headers["Origin"];
            if (string.IsNullOrEmpty(origin)) return;

            var reqHeaders = req.Headers["Access-Control-Request-Headers"];

            res.Headers["Access-Control-Allow-Origin"] = origin;
            res.Headers["Vary"] = "Origin";
            res.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            res.Headers["Access-Control-Allow-Credentials"] = "true";
            res.Headers["Access-Control-Max-Age"] = "86400";

            if (!string.IsNullOrEmpty(reqHeaders))
                res.Headers["Access-Control-Allow-Headers"] = reqHeaders;

            //@@@@_log.LogDebug(LogEvent.CORSDecision, "CORS applied for origin {Origin}", origin);
        }

        public static (string ollamaMuxHost, string ollamaExecutionHost) GetHosts()
        {
            var muxEnv = Environment.GetEnvironmentVariable("OLLAMA_HOST");
            var execEnv = Environment.GetEnvironmentVariable("OLLAMA_EXEC_HOST");

            var defaultMux = $"http://127.0.0.1:{OllamaDefaultPort}/";
            var defaultExec = $"http://127.0.0.1:{OllamaExecutionPort}/";

            string mux = defaultMux;
            string exec = defaultExec;

            if (!string.IsNullOrWhiteSpace(muxEnv))
            {
                try
                {
                    var uri = ParseToAbsoluteUri(muxEnv);
                    if (!IsLoopbackHost(uri.Host)) { /* warn if needed */ }
                    mux = EnsureSlash(uri.ToString());
                }
                catch { /* fallback to default */ }
            }

            if (!string.IsNullOrWhiteSpace(execEnv))
            {
                try
                {
                    var uri = ParseToAbsoluteUri(execEnv);
                    exec = EnsureSlash(uri.ToString());
                }
                catch { /* fallback to default */ }
            }

            return (mux, exec);
        }

        private static Uri NormalizeBaseUri(string uri)
        {
            var abs = ParseToAbsoluteUri(uri);
            return new Uri(EnsureSlash(abs.ToString()), UriKind.Absolute);
        }

        private static Uri ParseToAbsoluteUri(string value)
        {
            var uri = new Uri(value, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri)
                uri = new Uri($"http://{value}", UriKind.Absolute);
            return uri;
        }

        private static string EnsureSlash(string s) => s.EndsWith("/") ? s : s + "/";

        private static bool UriEquals(Uri a, Uri b)
        {
            return string.Equals(a.Scheme, b.Scheme, StringComparison.OrdinalIgnoreCase)
                && string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)
                && a.Port == b.Port
                && string.Equals(a.AbsolutePath.TrimEnd('/'), b.AbsolutePath.TrimEnd('/'), StringComparison.Ordinal);
        }

        private static bool IsLoopbackHost(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            if (IPAddress.TryParse(host, out var ip))
                return IPAddress.IsLoopback(ip);

            return false;
        }

        private static string FormatHeaders(System.Collections.Specialized.NameValueCollection headers)
        {
            var pairs = headers.AllKeys
                .Where(k => !string.IsNullOrEmpty(k) && !string.Equals(k, "Authorization", StringComparison.OrdinalIgnoreCase))
                .Select(k => $"{k}: {headers[k] ?? ""}");

            return string.Join(", ", pairs);
        }

        // Helpers for backend probing
        private static async Task<bool> CanConnectAsync(string host, int port, TimeSpan timeout)
        {
            using var client = new TcpClient();
            try
            {
                var connectTask = client.ConnectAsync(host, port);
                var completed = await Task.WhenAny(connectTask, Task.Delay(timeout));
                return completed == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        private static string NormalizeProbeHost(string host)
        {
            // 0.0.0.0 or '+' aren’t connectable probe targets; map to loopback for checks.
            if (string.Equals(host, "0.0.0.0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(host, "+", StringComparison.OrdinalIgnoreCase))
                return "127.0.0.1";
            return host;
        }
    }
}
