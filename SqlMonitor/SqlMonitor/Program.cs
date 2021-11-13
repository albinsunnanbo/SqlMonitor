using SqlMonitor.Properties;
using System;
using System.Collections.Generic;
using System.Configuration.Install;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace SqlMonitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            // TODO: Setup Serilog here

            Helpers.LogHelper.EnsureDeadlockFolder();

            try
            {
                // Måste köra .exe med argumentet /i /u för att köra installation/avinstallation
                if (args.Length > 0 && args[0] == "/i")
                {
                    if (args.Length != 3)
                        throw new ArgumentException("Saknar argument för användare och lösenord. Fick: " + args.Length);

                    var user = args[1].Split('=')[1];
                    var password = args[2].Split('=')[1];

                    InstallService(user, password);

                    try
                    {
                        if (Helpers.ExtendedEvents.EventSessionExists)
                        {
                            Helpers.LogHelper.LogInformation("Install", "Dropping previous event session");
                            Helpers.ExtendedEvents.DropEventSession();
                        }
                        Helpers.LogHelper.LogInformation("Install", "Creating event session");
                        Helpers.ExtendedEvents.CreateEventSession();
                    }
                    catch (Exception ex)
                    {
                        Helpers.LogHelper.LogException(ex, nameof(Main), "Install: Init SqlMonitor.Monitoring.SQL failed");
                    }
                }
                else if (args.Length > 0 && args[0] == "/u")
                {
                    UninstallService();
                    try
                    {
                        if (Helpers.ExtendedEvents.EventSessionExists)
                        {
                            Helpers.LogHelper.LogInformation("Uninstall", "Dropping event session");
                            Helpers.ExtendedEvents.DropEventSession();
                        }
                    }
                    catch (Exception ex)
                    {
                        Helpers.LogHelper.LogException(ex, nameof(Main), "Uninstall: Deactivate SqlMonitor.Monitoring.SQL failed");
                    }
                }
                else if (args.Length > 0 && args[0] == "/c") // Create event session
                {
                    UninstallService();
                }
                else if (Environment.UserInteractive)
                {
                    RunAsConsoleApp();
                }
                else
                {
                    RunAsService();
                }
            }
            catch (Exception ex)
            {
                Helpers.LogHelper.LogException(ex, nameof(Main), "Init SqlMonitor.Monitoring.SQL failed");
                throw;
            }
        }

        private static void RunAsConsoleApp()
        {
            Console.Title = "SQL Monitor";
            var service = new SqlMonitoringService();
            service.StartServer();

            Console.WriteLine("Press enter to stop service");
            Console.ReadLine();
            service.StopServer();
        }

        private static void RunAsService()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SqlMonitoringService()
            };
            ServiceBase.Run(ServicesToRun);
        }

        private static void InstallService(string user, string password)
        {
            string service = GetServiceExePath();

            if (IsServiceInstalled())
            {
                try
                {
                    ManagedInstallerClass.InstallHelper(new string[] { "/u", service });
                }
                catch
                {
                    throw new InstallException("The service could not be uninstalled");
                }
            }

            try
            {
                ManagedInstallerClass.InstallHelper(new string[] {
                    "/i",
                    "/user=" + user,
                    "/password=" + password,
                    service
                });
            }
            catch (Exception ex)
            {
                throw new InstallException("The service could not be installed", ex);
            }
        }

        private static void UninstallService()
        {
            string service = GetServiceExePath();

            if (IsServiceInstalled())
            {
                try
                {
                    ManagedInstallerClass.InstallHelper(new string[] { "/u", service });
                }
                catch (Exception ex)
                {
                    throw new InstallException("The service could not be uninstalled", ex);
                }
            }
        }

        private static string GetServiceExePath()
        {
            var location = Assembly.GetExecutingAssembly().Location;
            string fileName = Path.GetFileName(location);
            string installDir = Path.GetDirectoryName(location);
            string serviceExePath = Path.Combine(installDir, fileName);

            return serviceExePath;
        }

        private static bool IsServiceInstalled()
        {
            foreach (ServiceController service in ServiceController.GetServices())
            {
                if (service.ServiceName == Resources.ServiceName)
                    return true;
            }

            return false;
        }
    }
}
