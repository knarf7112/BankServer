using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
//
using System.Configuration.Install;
using System.Reflection;
using Common.Logging;


namespace BankAgentService
{
    static class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            AppDomain.CurrentDomain.ResourceResolve += CurrentDomain_ResourceResolve;

            if (System.Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        break;
                    case "--uninstall":
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        break;
                }
            }
            else
            {
                ServiceBase[] ServicesRun;
                ServicesRun = new ServiceBase[]
                {
                    new BankAgent()
                };
                ServiceBase.Run(ServicesRun);
            }
        }
        static Assembly CurrentDomain_ResourceResolve(object sender, ResolveEventArgs args)
        {
            if (args != null && args.RequestingAssembly != null)
            {
                log.Error("Resolve Error: " + args.Name + " Full Name:" + args.RequestingAssembly.FullName);
                return args.RequestingAssembly;
            }
            else
            {
                return null;
            }
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args != null && args.RequestingAssembly != null)
            {
                log.Error("Resolve Error: " + args.Name + " Full Name:" + args.RequestingAssembly.FullName);
                return args.RequestingAssembly;
            }
            else
            {
                return null;
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e != null && e.ExceptionObject != null)
            {
                log.Error("Error: " + e.ExceptionObject);
            }
        }
    }
}
