using System;
using System.Security.Permissions;
using System.Threading;
using System.Windows.Forms;
using MinerControl.Utility;

namespace MinerControl
{
    static class Program
    {
        public static bool HasAutoStart { get; set; }
        public static bool MinimizeToTray { get; set; }
        public static bool MinimizeOnStart { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        static void Main(string[] args)
        {
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-a":
                    case "--auto-start":
                        HasAutoStart = true;
                        break;
                    case "-t":
                    case "--minimize-to-tray":
                        MinimizeToTray = true;
                        break;
                    case "-m":
                    case "--minimize":
                        MinimizeOnStart = true;
                        break;
                }
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(LastResortThreadExceptionHandler);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LastResortDomainExceptionHandler);

            Application.Run(new MainWindow());
        }

        private static void LastResortThreadExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            ErrorLogger.Log(e.Exception);
        }

        private static void LastResortDomainExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = (Exception)e.ExceptionObject;
            ErrorLogger.Log(ex);
        }
    }
}
