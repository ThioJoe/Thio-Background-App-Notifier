using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Windows.Forms;
using ThioWinUtils;
using static ThioWinUtils.ModernTaskDialog;
using static ThioWinUtils.ModernTaskDialog.Template;

namespace Thio_Background_App_Notifier
{
    internal static class Program
    {
        // Import the Win32 API to set OS-level DPI awareness. This way we don't need separate app.config to remain next to the EXE.
        // See: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setprocessdpiawarenesscontext
        //  The docs recommend doing this via app.config instead of API call but have a file is ugly.
        [DllImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiFlag);

        /// <summary> The main entry point for the application. </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try 
            {
                // Tell Windows this app handles its own DPI scaling (replaces the manifest setting)
                // Enum values at: https://learn.microsoft.com/en-us/windows/win32/hidpi/dpi-awareness-context
                SetProcessDpiAwarenessContext(new IntPtr(-4));  // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = -4
                // Tell WinForms to actively resize controls (replaces the app.config setting)
                AppContext.SetSwitch("Switch.System.Windows.Forms.EnableWindowsFormsHighDpiAutoResizing", true);
            }
            catch (EntryPointNotFoundException) // Fails gracefully on Windows versions older than Windows 10 Anniversary Update
            { Debug.WriteLine("SetProcessDpiAwarenessContext not available on this Windows version"); }
            
            // Continue on to normally included setup
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppOptions options = AppOptions.Parse(args);

            // Elevated helper mode: we were relaunched (via UAC) solely to delete the all-users startup
            // shortcut. Remove it and exit before any scanning or UI.
            if (options.RemoveMachineStartupShortcut)
            {
                StartupShortcut.RemoveMachineShortcut(out _);
                return;
            }

            // Do the scan up front (it needs no UI). This compares against the on-disk log but does
            // NOT persist yet — the log is only committed once the results are actually surfaced.
            ScanResult result = DetectionStore.PerformScan();

            if (options.QuietMode)
            {
                // The quiet popup only fires for items we haven't already alerted about.
                if (!result.HasUnalertedItems)
                {
                    // Nothing new to surface, so nothing would be wrongly marked. Just record
                    // baseline / run bookkeeping.
                    result.CommitBookkeeping();
                    return;
                }

                (bool open, bool remindLater) = AskWhetherToOpen(result);
                if (!open)
                {
                    // Declined. Unless they ticked "remind me later", mark these as alerted so the
                    // popup won't nag about them again. Either way they still appear in the main
                    // window the next time it's opened (that's what marks them "seen").
                    if (!remindLater)
                        result.CommitAlerted();
                    return;
                }

                // Opening the window: it commits (seen + alerted) on load.
            }

            Application.Run(new MainForm(options, result));
        }

        /// <summary>
        /// Shows a modern task dialog listing what was newly found and asking whether to open the
        /// main window. Returns whether to open the app, and whether the user ticked the
        /// (default-off) "remind me later" checkbox.
        /// </summary>
        private static (bool Open, bool RemindLater) AskWhetherToOpen(ScanResult result)
        {
            int count = result.UnalertedItems.Count;
            string headline = count == 1
                ? "1 new autorun item detected:"
                : count + " new autorun items detected:";

            // List a few of the names so the user knows what turned up.
            const int maxNames = 5;
            List<string> names = result.UnalertedItems.Take(maxNames).Select(UiHelpers.GetDisplayName).ToList();

            string body = "• " + string.Join("\n• ", names);

            if (count > maxNames)
                body += "\n… and " + (count - maxNames) + " more";
            body += $"\n\n———————\nSee details now?";

            try
            {
                var dialog = new ModernTaskDialog
                {
                    Title = AppName,
                    MainInstruction = headline,
                    Content = body,
                    VerificationText = "Remind me again at next startup",
                    MainIcon = ModernTaskDialog.TaskDialogIcon.Information,

                    CommonButtons = TaskDialogCommonButtonFlags.TDCBF_YES_BUTTON |
                                    TaskDialogCommonButtonFlags.TDCBF_NO_BUTTON,
                    Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                            TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                            TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                    ParentWindowHandle = default,
                    Coloredbar = TaskDialogBarColor.Yellow,
                    
                };

                int buttonId = dialog.Show();
                bool open = buttonId == ModernTaskDialog.Template.ButtonIds.Yes;

                return (open, dialog.VerificationChecked);
            }
            catch (Exception ex)
            {
                #if DEBUG
                    MessageBox.Show($"Exception trying to show modern task dialog: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                #endif
                // Fall back to a standard message box if the task dialog can't be shown
                // (e.g. missing common-controls v6 manifest). No verification checkbox here.
                bool open = MessageBox.Show(
                    text: headline + "\r\n\r\n" + body,
                    caption: AppName,
                    buttons: MessageBoxButtons.YesNo,
                    icon: MessageBoxIcon.Information
                ) == DialogResult.Yes;

                return (open, false);
                
            }
        }
    }
}
