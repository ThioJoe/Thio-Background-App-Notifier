using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ThioWinUtils;

#nullable enable

namespace Thio_Background_App_Notifier
{
    public partial class MainForm : Form
    {
        private ScanResult _result;

        // Guards the "run at startup" checkbox so setting it in code doesn't trigger the toggle logic.
        private bool _updatingStartupCheckbox;

        private DevViewForm? _devViewForm = null;

        public MainForm(AppOptions options, ScanResult result)
        {
            _result = result;

            InitializeComponent();

            // ---- System tray (left as-is from the existing implementation for now) ----
            TrayContextMenu trayContextMenu = new TrayContextMenu(
                exitAppMenuOption: true,
                updateURL: "https://github.com/ThioJoe/New-Startup-App-Notifier/releases"
            );
            trayContextMenu.AddCustomMenuItem( text: "Open Log Folder", action: OpenLogFolder );

            SystemTray sysTray = new SystemTray(
                trayContextMenu,
                iconHandle: null,
                useExeIcon: true,
                tooltipText: AppName,
                restoreAction: null,
                hwndInput: this.Handle
            );

            #if DEBUG
                _devViewForm = new DevViewForm();
                buttonDevView.Visible = true;
                buttonDevView.Enabled = true;
            #endif
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UiHelpers.AttachCopyContextMenu(listViewItems);
            RefreshStartupCheckbox();
            RefreshWindowsNotificationState();
            DisplayResult(_result);

            // The window is now shown, so persist the scan (and mark the listed items alerted so the
            // quiet popup won't re-nag about anything already shown here).
            _result.CommitShown();

        }

        // ---------------------------------------------------------------------
        // Display
        // ---------------------------------------------------------------------

        private void DisplayResult(ScanResult result)
        {
            _result = result;
            UpdateStatusLabels(result);
            PopulateList(result);
        }

        private void UpdateStatusLabels(ScanResult result)
        {
            // The tracked-item counter is its own dedicated label (the boxed number in the status
            // card), independent of whatever the status message happens to say.
            labelTrackedCount.Text = result.AllItems.Count.ToString();

            string lastChecked = result.PreviousRunTimeLocal.HasValue
                ? "Last checked " + result.PreviousRunTimeLocal.Value.ToString("g") + "."
                : "This is the first time it has run.";

            if (result.IsFirstRun)
            {
                labelStatusValue.Text = $"First run — saved a baseline of startup items.";
                labelStatusValue.ForeColor = SystemColors.ControlText;
                labelStatusDetail.Text = "From now on you'll be told which startup items are new.";
            }
            else if (result.HasNewItems)
            {
                labelStatusValue.Text = $"{result.NewItems.Count} new startup item(s) since you last checked.";
                labelStatusValue.ForeColor = Color.FromArgb(176, 0, 0);
                labelStatusDetail.Text = "New items are highlighted below.   " + lastChecked;
            }
            else
            {
                labelStatusValue.Text = "No new startup items since you last checked.";
                labelStatusValue.ForeColor = Color.FromArgb(0, 120, 0);
                labelStatusDetail.Text = lastChecked;
            }
        }

        private void PopulateList(ScanResult result)
        {
            // The main list is a running history of everything that has appeared since the baseline
            // (so items stay listed across rescans, not just until viewed). The baseline itself is
            // excluded so a fresh install doesn't look like a wall of things needing attention, and
            // the items detected on this scan are highlighted below.
            var items = result.ItemsSinceBaseline;

            listViewItems.BeginUpdate();
            try
            {
                listViewItems.Items.Clear();
                listViewItems.Groups.Clear();

                // Group rows by the day they were first detected so different days are separated by a
                // header divider. Groups appear in the order they're added (newest day first here).
                listViewItems.ShowGroups = true;
                var groupsByDate = new Dictionary<DateTime, ListViewGroup>();

                // Most-recently-detected first, so anything new floats to the top.
                var ordered = items
                    .OrderByDescending(i => i.FirstDetectionTime)
                    .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase);

                foreach (IStartupItem item in ordered)
                {
                    // Columns: New, First Detected, Type, Name, Starts, Source, Path
                    var row = new ListViewItem(item.IsFirstDetection ? "NEW" : string.Empty);
                    row.SubItems.Add(UiHelpers.FormatDetected(item.FirstDetectionTime));
                    row.SubItems.Add(UiHelpers.GetTypeLabel(item));
                    row.SubItems.Add(UiHelpers.GetDisplayName(item));
                    row.SubItems.Add(UiHelpers.GetDetail(item));
                    row.SubItems.Add(UiHelpers.GetSourceHint(item));
                    row.SubItems.Add(item.Path);
                    row.Tag = item;

                    if (item.IsFirstDetection)
                    {
                        row.UseItemStyleForSubItems = true;
                        row.BackColor = Color.FromArgb(255, 249, 196); // light yellow highlight
                    }

                    DateTime day = item.FirstDetectionTime.Date;
                    if (!groupsByDate.TryGetValue(day, out ListViewGroup group))
                    {
                        group = new ListViewGroup(day.ToLongDateString()) { HeaderAlignment = HorizontalAlignment.Left };
                        groupsByDate[day] = group;
                        listViewItems.Groups.Add(group);
                    }
                    row.Group = group;

                    listViewItems.Items.Add(row);
                }
            }
            finally
            {
                listViewItems.EndUpdate();
            }

            UpdatePlaceholder(result, items.Count == 0);
        }

        private void UpdatePlaceholder(ScanResult result, bool isEmpty)
        {
            if (isEmpty)
            {
                string lead = result.IsFirstRun
                    ? "No new startup apps yet — this first run just recorded what's already set to run."
                    : "No new startup apps have appeared since the baseline was recorded.";

                labelPlaceholder.Text = lead + "\r\n\r\n"
                    + "Use “All Startup Services” or “All Startup Tasks” above to see everything currently set to run.";
                labelPlaceholder.Visible = true;
                labelPlaceholder.BringToFront();
            }
            else
            {
                labelPlaceholder.Visible = false;
            }
        }

        private void OpenLogFolder()
        {
            try
            {
                Directory.CreateDirectory(DetectionStore.StoreDirectory);
                Process.Start(new ProcessStartInfo
                {
                    FileName = DetectionStore.StoreDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Couldn't open the log folder:\r\n" + ex.Message,
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ---------------------------------------------------------------------
        // Buttons / interactions
        // ---------------------------------------------------------------------

        // Kept so an already-open "All" window is reused/focused instead of stacking duplicates.
        private ItemListForm? _servicesForm;
        private ItemListForm? _tasksForm;

        private void buttonAllStartupServices_Click(object sender, EventArgs e)
        {
            _servicesForm = ShowOrFocus(_servicesForm,
                "All Startup Services (" + _result.Services.Count + ")", _result.Services);
        }

        private void buttonAllStartupTasks_Click(object sender, EventArgs e)
        {
            _tasksForm = ShowOrFocus(_tasksForm,
                "All Startup Tasks (" + _result.Tasks.Count + ")", _result.Tasks);
        }

        // Shows the list window non-modally (so the main window stays usable). If one is already
        // open, brings it to the front instead of opening another.
        private ItemListForm ShowOrFocus(ItemListForm? existing, string title, IEnumerable<IStartupItem> items)
        {
            if (existing != null && !existing.IsDisposed)
            {
                existing.BringToFront();
                existing.Activate();
                return existing;
            }

            var form = new ItemListForm(title, items);
            form.Show(this);
            return form;
        }

        private void buttonRescan_Click(object sender, EventArgs e)
        {
            Cursor previous = this.Cursor;
            this.Cursor = Cursors.WaitCursor;
            try
            {
                DisplayResult(DetectionStore.PerformScan());
                // The window is already open, so record the results immediately.
                _result.CommitShown();
            }
            finally
            {
                this.Cursor = previous;
            }
        }

        private void listViewItems_DoubleClick(object sender, EventArgs e)
        {
            if (listViewItems.SelectedItems.Count == 0)
                return;
            if (listViewItems.SelectedItems[0].Tag is IStartupItem item)
                UiHelpers.ShowDetails(this, item);
        }

        // ---------------------------------------------------------------------
        // Run-at-startup toggle
        // ---------------------------------------------------------------------

        private void RefreshStartupCheckbox()
        {
            _updatingStartupCheckbox = true;
            checkBoxRunAtStartup.Checked = StartupShortcut.IsEnabled;
            _updatingStartupCheckbox = false;
        }

        private void checkBoxRunAtStartup_CheckedChanged(object sender, EventArgs e)
        {
            if (_updatingStartupCheckbox)
                return;

            bool wantEnabled = checkBoxRunAtStartup.Checked;
            string error;
            bool ok = wantEnabled
                ? StartupShortcut.Enable(out error)
                : StartupShortcut.Disable(out error);

            if (!ok)
            {
                MessageBox.Show(this,
                    (wantEnabled ? "Couldn't add the startup shortcut:\r\n" : "Couldn't remove the startup shortcut:\r\n") + error,
                    AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Put the checkbox back to whatever the real on-disk state is.
                RefreshStartupCheckbox();
            }
        }

        // ---------------------------------------------------------------------
        // Windows' own "regular startup app" notification
        // ---------------------------------------------------------------------

        // Shows the current Windows "Startup app notifications" setting as a prominent colored value.
        // Only the value label changes here; its caption/hint are fixed designer labels. The details
        // and actions live in a dedicated info form (buttonWinNotify).
        private void RefreshWindowsNotificationState()
        {
            bool? enabled = WindowsStartupNotification.IsEnabled();

            if (enabled == true)
            {
                labelWinNotifyValue.Text = "On";
                labelWinNotifyValue.ForeColor = Color.FromArgb(0, 128, 0);
            }
            else if (enabled == false)
            {
                labelWinNotifyValue.Text = "Off";
                labelWinNotifyValue.ForeColor = Color.FromArgb(192, 0, 0);
            }
            else
            {
                labelWinNotifyValue.Text = "Unknown";
                labelWinNotifyValue.ForeColor = SystemColors.GrayText;
            }
        }

        private void buttonWinNotify_Click(object sender, EventArgs e)
        {
            using (var form = new WindowsNotifyInfoForm())
                form.ShowDialog(this);

            // The setting may have changed in the info form; reflect it.
            RefreshWindowsNotificationState();
        }

        // ---------------------------------------------------------------------
        // About / Help
        // ---------------------------------------------------------------------

        private void buttonAbout_Click(object sender, EventArgs e)
        {
            const string repoUrl = "https://github.com/ThioJoe/New-Startup-App-Notifier";

            string tips =
                "Watches for new background services and scheduled tasks that are set to run at startup, and points out anything that appeared since you last checked.\r\n\r\n"
                + "Tips:\r\n"
                + "•  It doesn't run in the background - open it to scan (or choose to run once each startup).\r\n"
                + "•  Use “All Startup Services” / “All Startup Tasks” to browse everything currently set to run.\r\n"
                + "•  New items are highlighted. Double-click any row for full details.\r\n"
                + "•  Keep Windows' own \"Startup App Notifications\" turned On so Windows alerts you about \"regular\" startup apps.\r\n\r\n"
                + "Created by ThioJoe";

            try
            {
                ModernTaskDialog.Template.ShowInfoWithHyperlinks(
                    title: AppName,
                    mainInstruction: AppName,
                    content: tips + "\r\n" + $"<a href=\"{repoUrl}\">{repoUrl}</a>",
                    parentHandle: this.Handle);
            }
            catch (Exception)
            {
                // Fall back to a plain message box if the modern task dialog can't be shown
                // (e.g. missing common-controls v6). The URL is shown as plain text here.
                MessageBox.Show(this, tips + "\r\n" + repoUrl, AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonDevView_Click(object sender, EventArgs e)
        {
            // Open the DevViewForm window
            if (_devViewForm == null)
                _devViewForm = new DevViewForm();

            _devViewForm.Show();
            _devViewForm.Focus();
            _devViewForm.BringToFront();
        }
    } // End of MainForm Class

} // End of Namespace
