using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlMonitor.Helpers
{
    public static class LogHelper
    {
        public static void LogException(Exception ex, string method, string message)
        {
            SqlMonitorLogDetail logRecord = BuildLogRecord(method, message);
            logRecord.Exception = ex;
            // TODO Log to elastic
            Console.Error.WriteLine($"EX: {logRecord}");
        }
        public static void LogInformation(string method, string message)
        {
            SqlMonitorLogDetail logRecord = BuildLogRecord(method, message);
            // TODO Log to elastic
            Console.Error.WriteLine($"I: {logRecord}");
        }

        public static void LogError(string method, Exception exception, string message)
        {
            SqlMonitorLogDetail logRecord = BuildLogRecord(method, message);
            logRecord.Exception = exception;
            // TODO Log to elastic
            Console.Error.WriteLine($"E: {logRecord}");

        }

        public static SqlMonitorLogDetail BuildLogRecord(string method, string message)
        {
            return new SqlMonitorLogDetail
            {
                Hostname = Environment.MachineName,
                Product = "SQL",
                Service = "Monitoring",
                Method = method,
                Message = message,
            };
        }

        public static string DeadlockFolder
        {
            get
            {
                return ConfigurationManager.AppSettings["SqlMonitor.Monitoring.DeadlockFiles"];
            }
        }

        public static void EnsureDeadlockFolder()
        {
            if (!Directory.Exists(DeadlockFolder))
            {
                Directory.CreateDirectory(DeadlockFolder);
            }
        }

        public static string ToNetworkPath(string path)
        {
            var disk = path.Substring(0, 1);
            return $"\\\\{Environment.MachineName}\\{disk}${path.Substring(2)}";
        }
    }
}
