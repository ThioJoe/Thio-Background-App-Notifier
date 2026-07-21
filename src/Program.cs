using System;
using System.Windows.Forms;

namespace New_Startup_App_Notifier
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppOptions options = AppOptions.Parse(args);

            // Do the scan up front (it needs no UI). This updates the on-disk detection log and tells
            // us whether anything is new since the last run.
            ScanResult result = DetectionStore.PerformScan();

            // Quiet mode is meant for the Startup-folder shortcut: stay out of the way unless there's
            // actually something new to report. Otherwise exit silently without ever showing a window.
            if (options.QuietMode && !result.HasNewItems)
                return;

            Application.Run(new MainForm(options, result));
        }
    }
}
