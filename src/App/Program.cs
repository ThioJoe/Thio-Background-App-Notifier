using System;
using System.Linq;
using System.Windows.Forms;
using ThioWinUtils;

namespace Thio_Background_App_Notifier
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
                ? "1 new startup item detected"
                : count + " new startup items detected";

            // List a few of the names so the user knows what turned up.
            const int maxNames = 5;
            var names = result.UnalertedItems.Take(maxNames).Select(UiHelpers.GetDisplayName).ToList();
            string body = string.Join("\r\n", names);
            if (count > maxNames)
                body += "\r\n… and " + (count - maxNames) + " more";
            body += $"\r\n\r\nOpen {AppName} to review?";

            try
            {
                ModernTaskDialog.Template.VerificationResult vr =
                    ModernTaskDialog.Template.ShowYesNoWithVerification(
                        title: AppName,
                        mainInstruction: headline,
                        content: body,
                        verificationText: "Remind me again at next startup",
                        icon: ModernTaskDialog.TaskDialogIcon.Information
                    );

                bool open = vr.ButtonId == ModernTaskDialog.Template.ButtonIds.Yes;
                return (open, vr.VerificationChecked);
            }
            catch (Exception ex)
            {
                #if DEBUG
                MessageBox.Show($"Exception trying to show modern task dialog: {ex.Message}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                #endif
                // Fall back to a standard message box if the task dialog can't be shown
                // (e.g. missing common-controls v6 manifest). No verification checkbox here.
                bool open = MessageBox.Show(headline + "\r\n\r\n" + body, AppName,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes;
                return (open, false);
                
            }
        }
    }
}
