using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaMux
{
    static class LogEvent
    {
        public static readonly EventId ProxyStart = new(1001, nameof(ProxyStart));
        public static readonly EventId RequestReceived = new(1002, nameof(RequestReceived));
        public static readonly EventId UpstreamResponse = new(1003, nameof(UpstreamResponse));
        public static readonly EventId HeaderSkipped = new(1004, nameof(HeaderSkipped));
        public static readonly EventId CORSDecision = new(1005, nameof(CORSDecision));
        public static readonly EventId UpstreamError = new(1006, nameof(UpstreamError));
        public static readonly EventId HealthCheckFailed = new(1007, nameof(HealthCheckFailed));
        public static readonly EventId RequestCompleted = new(1008, nameof(RequestCompleted));
        // Add more as needed
    }

    static class Log
    {
        public static ILoggerFactory CreateFactory()
        {
            var min = ParseLevel(Environment.GetEnvironmentVariable("OLLAMAMUX_LOG_LEVEL")) ?? LogLevel.Information;
            var json = IsEnabled(Environment.GetEnvironmentVariable("OLLAMAMUX_LOG_JSON"));

            return LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(min);
                if (json)
                {
                    builder.AddJsonConsole(o =>
                    {
                        o.IncludeScopes = true;
                        o.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                        o.UseUtcTimestamp = true;
                    });
                }
                else
                {
                    builder.AddSimpleConsole(o =>
                    {
                        o.IncludeScopes = true;
                        o.SingleLine = true;
                        o.TimestampFormat = "HH:mm:ss.fff ";
                    });
                }
            });
        }

        private static LogLevel? ParseLevel(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return Enum.TryParse<LogLevel>(s, true, out var lvl) ? lvl : null;
        }

        private static bool IsEnabled(string? s)
            => !string.IsNullOrWhiteSpace(s) && s.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
