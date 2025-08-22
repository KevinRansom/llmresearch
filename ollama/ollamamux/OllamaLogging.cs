using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OllamaMux
{
    public static class LogEvent
    {
        public static readonly EventId ProxyStart = new(1001, nameof(ProxyStart));
        public static readonly EventId RequestReceived = new(1002, nameof(RequestReceived));
        public static readonly EventId UpstreamResponded = new(1003, nameof(UpstreamResponded));
        public static readonly EventId RequestCompleted = new(1004, nameof(RequestCompleted));
        public static readonly EventId HeaderSkipped = new(1005, nameof(HeaderSkipped));
        public static readonly EventId CORSDecision = new(1006, nameof(CORSDecision));
        public static readonly EventId UpstreamError = new(1007, nameof(UpstreamError));
        public static readonly EventId HealthCheckFailed = new(1008, nameof(HealthCheckFailed));
    }        // Add more as needed
}
