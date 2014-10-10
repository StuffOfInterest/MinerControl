using System;
using System.IO;
using System.Text;

namespace MinerControl.Utility
{
    public static class ErrorLogger
    {
        const string logfile = "error.log";

        public static bool LogExceptions { get; set; }

        public static void Log(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine(DateTime.Now.ToString());
            sb.AppendLine("----------------------------------------------");
            sb.AppendLine(string.Format("Type: {0}", ex.GetType().Name));
            sb.AppendLine(string.Format("Message: {0}", ex.Message));
            sb.AppendLine(string.Format("Stack trace: {0}", ex.StackTrace));

            using (var w = File.Exists(logfile) ? File.AppendText(logfile) : File.CreateText(logfile))
            {
                w.Write(sb.ToString());
            }

            if (ex.InnerException != null)
                Log(ex.InnerException);
        }
    }
}
