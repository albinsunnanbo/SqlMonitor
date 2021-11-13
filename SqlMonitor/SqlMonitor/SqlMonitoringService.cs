using Microsoft.SqlServer.XEvent.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SqlMonitor
{
    public partial class SqlMonitoringService : ServiceBase
    {
        private Thread session;
        public SqlMonitoringService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StartServer();
        }

        public void StartServer()
        {
            try
            {
                Helpers.LogHelper.LogInformation(nameof(RunEventSession), "Starting SQL Monitoring Service");
                session = new Thread(RunEventSession);
                session.IsBackground = true;
                session.Start();
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.LogError(nameof(StartServer), ex, "Failed SQL Monitor startup");
                throw;
            }
        }

        private static void RunEventSession()
        {
            int noOrganizationLifecycle = 0;
            while (true)
            {
                try
                {
                    if (!Helpers.ExtendedEvents.EventSessionExists)
                    {
                        Helpers.LogHelper.LogInformation(nameof(RunEventSession), "Creating event session");
                        Helpers.ExtendedEvents.CreateEventSession();
                    }
                    if (!Helpers.ExtendedEvents.EventSessionActive)
                    {
                        Helpers.LogHelper.LogInformation(nameof(RunEventSession), "Starting SQL event session");
                        Helpers.ExtendedEvents.StartEventSession();
                    }

                    SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(Helpers.SqlRunner.connectionString);
                    csb.InitialCatalog = "master"; // Extended events runs on the database level

                    using (QueryableXEventData xEvents =
                        new QueryableXEventData(
                            csb.ConnectionString,
                            Properties.Resources.SessionName,
                            EventStreamSourceOptions.EventStream,
                            EventStreamCacheOptions.DoNotCache))
                    {
                        foreach (PublishedEvent evt in xEvents)
                        {
                            bool skip = false;
                            var logRecord = Helpers.LogHelper.BuildLogRecord(evt.Name, "SQL Server extended event");
                            var isError = true;

                            if (evt.Name.Contains("deadlock_report"))
                            {
                                logRecord.Message = "Deadlock";
                            }

                            foreach (PublishedEventField fld in evt.Fields)
                            {
                                switch (fld.Name)
                                {
                                    case "xml_report":
                                        var xmlData = fld.Value as Microsoft.SqlServer.XEvent.XMLData;
                                        if (xmlData != null)
                                        {
                                            var fileName = System.IO.Path.Combine(Helpers.LogHelper.DeadlockFolder, Guid.NewGuid() + ".xdl");
                                            xmlData.XML.Save(fileName);
                                            logRecord.Data["DeadlockGraph"] = Helpers.LogHelper.ToNetworkPath(fileName);
                                        }
                                        break;
                                    case "duration":
                                        logRecord.Data["Fields." + fld.Name] = fld.Value;
                                        int ns;
                                        if (int.TryParse(fld.Value.ToString(), out ns))
                                        {
                                            logRecord.ElapsedMilliseconds = ns / 1000;
                                        }
                                        break;
                                    case "result":
                                        if (evt.Name == "rpc_completed")
                                        {
                                            var mapValue = fld.Value as Microsoft.SqlServer.XEvent.MapValue;
                                            if (mapValue != null && mapValue.Value == "Abort")
                                            {
                                                logRecord.Message = "Client disconnect / timeout";
                                            }
                                            else
                                            {
                                                logRecord.Message = "Long running query";
                                                isError = false;
                                            }
                                        }
                                        logRecord.Data["Fields." + fld.Name] = fld.Value;
                                        break;
                                    case "message":
                                        logRecord.Data["Fields." + fld.Name] = fld.Value;
                                        if (fld.Value as string == "Invalid object name 'OrganizationLifecycle'.")
                                        {
                                            if (noOrganizationLifecycle > 0) // Just log 1/100
                                            {
                                                skip = true;
                                            }
                                            logRecord.Data["Fields." + fld.Name] = $"{fld.Value} (suppress next 100 logs)";
                                            noOrganizationLifecycle = (noOrganizationLifecycle + 1) % 100;
                                        }
                                        break;
                                    default:
                                        logRecord.Data["Fields." + fld.Name] = fld.Value;
                                        break;
                                }
                            }

                            foreach (PublishedAction act in evt.Actions)
                            {
                                logRecord.Data["Action." + act.Name] = act.Value;
                            }

                            // Log to Kibana
                            if (!skip)
                            {
                                if (isError)
                                {
                                    // TODO Log to elastic
                                    Console.Error.WriteLine($"E: {logRecord}");
                                }
                                else
                                {
                                    // TODO Log to elastic
                                    Console.Error.WriteLine($"I: {logRecord}");
                                }
                            }

                            //// Log to console
                            //Console.ForegroundColor = ConsoleColor.Green;
                            //Console.WriteLine(evt.Name);
                            //Console.ForegroundColor = ConsoleColor.Yellow;
                            //Console.WriteLine();   //Whitespace
                        }
                    }
                }
                catch (ThreadAbortException ex)
                {
                    Helpers.LogHelper.LogException(ex, nameof(RunEventSession), "SQL Event session ThreadAbortException, terminating");
                    Thread.Sleep(100); // Time to log
                    // Break;
                    return;
                }
                catch (Exception ex)
                {
                    // Log

                    Console.WriteLine(Environment.NewLine);
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine(ex.ToString());
                    Console.WriteLine(Environment.NewLine);

                    Helpers.LogHelper.LogException(ex, nameof(RunEventSession), "SQL Event session failed, restarting");
                    Thread.Sleep(TimeSpan.FromMinutes(1)); // Sleep to avoid excessive spinning on errors
                }
            }
        }

        protected override void OnStop()
        {
            StopServer();
        }

        public void StopServer()
        {
            Helpers.LogHelper.LogInformation(nameof(StopServer), "Stopping SQL Monitoring Service");
            session?.Abort();
            try
            {
                if (Helpers.ExtendedEvents.EventSessionActive)
                {
                    Helpers.LogHelper.LogInformation(nameof(StopServer), "Stopping SQL event session");
                    Helpers.ExtendedEvents.StopEventSession();
                }
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.LogError(nameof(StopServer), ex, "Stopping SQL Monitoring Service");
            }
            Thread.Sleep(100); // Time to log
        }
    }
}
