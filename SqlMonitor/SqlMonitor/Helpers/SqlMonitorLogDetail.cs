using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlMonitor.Helpers
{
    public class SqlMonitorLogDetail
    {
        public string Hostname { get; internal set; }
        public string Product { get; internal set; }
        public string Service { get; internal set; }
        public string Method { get; internal set; }
        public string Message { get; internal set; }
        public long? ElapsedMilliseconds { get; internal set; }
        public Dictionary<string, object> Data { get; internal set; } = new Dictionary<string, object>();
        public Exception Exception { get; internal set; }

        public override string ToString()
        {
            return $"{Hostname}/{Product}/{Service}/{Method}/{Message}/{Exception?.Message}/{string.Join("", Data.Select(d => $"{Environment.NewLine}    {d.Key}: {d.Value?.ToString()}"))}";
        }
    }
}