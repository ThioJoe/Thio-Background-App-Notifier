using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ThioWinUtils;

#nullable enable

namespace New_Startup_App_Notifier
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
                exitAppMenuOption: true
            );

            SystemTray sysTray = new SystemTray(
                trayContextMenu,
                iconHandle: null,
                useExeIcon: true,
                tooltipText: "Sneaky Startup App Notifier",
                restoreAction: null,
                hwndInput: this.Handle
            );

            #if DEBUG
                buttonDevView.Visible = true;
                buttonDevView.Enabled = true;
                _devViewForm = new DevViewForm();
            #endif
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            UiHelpers.AttachCopyContextMenu(listViewItems);
            RefreshStartupCheckbox();
            DisplayResult(_result);

            #if DEBUG
                _devViewForm?.Show();
                _devViewForm?.Focus();
                _devViewForm?.BringToFront();
            #endif
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
            string backgroundNote =
                "This app does not run in the background — it only checks when you run it (for example at startup).";

            if (result.IsFirstRun)
            {
                labelStatus.Text = $"First run — recorded a baseline of {result.AllItems.Count} startup item(s).";
                labelStatus.ForeColor = SystemColors.ControlText;
                labelSubStatus.Text =
                    "All current startup items have been recorded as the baseline. From now on you'll be told which are new.\r\n"
                    + backgroundNote;
            }
            else if (result.HasNewItems)
            {
                labelStatus.Text = $"{result.NewItems.Count} new startup item(s) since last run.";
                labelStatus.ForeColor = Color.FromArgb(176, 0, 0);
                labelSubStatus.Text = BuildTrackingText(result) + "\r\n"
                    + "New items are highlighted below. " + backgroundNote;
            }
            else
            {
                labelStatus.Text = "No new startup items since last run.";
                labelStatus.ForeColor = Color.FromArgb(0, 120, 0);
                labelSubStatus.Text = BuildTrackingText(result) + "\r\n" + backgroundNote;
            }
        }

        private static string BuildTrackingText(ScanResult result)
        {
            string tracking = "Tracking " + result.AllItems.Count + " startup item(s).";
            if (result.PreviousRunTimeLocal.HasValue)
                return "Previous run: " + result.PreviousRunTimeLocal.Value.ToString("g") + "     " + tracking;
            return tracking;
        }

        private void PopulateList(ScanResult result)
        {
            // The main list only shows items that have appeared since the first-run baseline, so a
            // fresh install doesn't look like a wall of things needing attention.
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
                    row.SubItems.Add(item.Name);
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
                    : "No new startup apps found since the first run.";

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
            }
            finally
            {
                this.Cursor = previous;
            }
        }

        private void buttonOpenLogFolder_Click(object sender, EventArgs e)
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
                    "Sneaky Startup App Notifier", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    "Sneaky Startup App Notifier", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Put the checkbox back to whatever the real on-disk state is.
                RefreshStartupCheckbox();
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
