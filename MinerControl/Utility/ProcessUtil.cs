using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace MinerControl.Utility
{
    public static class ProcessUtil
    {
        /// <summary>
        /// Kill a process, and all of its children, grandchildren, etc.
        /// </summary>
        /// <param name="pid">Process ID.</param>
        public static void KillProcessAndChildren(int pid)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }

        public static void KillProcess(Process process)
        {
            try
            {
                if (process == null || process.HasExited == true)
                    return;

                process.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }

        //http://stackoverflow.com/questions/2647820/toggle-process-startinfo-windowstyle-processwindowstyle-hidden-at-runtime
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void HideWindow(Process process)
        {
            if (process == null || process.HasExited) return;
            ShowWindow(process.MainWindowHandle, 0); // Hidden
        }

        public static void MinimizeWindow(Process process)
        {
            if (process == null || process.HasExited) return;
            ShowWindow(process.MainWindowHandle, 2); // Minimized
        }

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);

        public static void SetWindowTitle(Process process, string title)
        {
            SetWindowText(process.MainWindowHandle, title);
        }

        /// <summary>
        /// Safely check if process exists and has not exited.
        /// </summary>
        public static bool IsRunning(this Process process)
        {
            try
            {
                if (process == null || process.HasExited) return false;
            }
            catch (InvalidOperationException) // Happens if the process did not launch but object still exists
            {
                return false;
            }

            return true;
        }
    }
}
