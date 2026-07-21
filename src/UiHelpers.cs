using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TaskScheduler;

#nullable enable

namespace New_Startup_App_Notifier;

/// <summary>
/// Small formatting helpers shared by the detection log and the UI so that a startup item
/// is described the same way everywhere.
/// </summary>
internal static class UiHelpers
{
    public static string GetTypeLabel(IStartupItem item)
        => item.Type == StartupItemType.Service ? "Service" : "Scheduled Task";

    /// <summary>
    /// A short, human-friendly description of when/how the item starts.
    /// For services this is the start type (Automatic / Boot / System); for scheduled tasks
    /// it is the list of auto-start triggers (Logon, Boot, Daily, Idle).
    /// </summary>
    public static string GetDetail(IStartupItem item)
    {
        switch (item)
        {
            case StartupService s:
                return GetServiceStartTypeName(s.StartupType);

            case StartupTask t:
                if (t.StartupTaskTypes == null || t.StartupTaskTypes.Count == 0)
                    return "—"; // em dash
                return string.Join(", ", t.StartupTaskTypes.Select(GetTriggerName).Distinct());

            default:
                return "—";
        }
    }

    public static string GetServiceStartTypeName(int startType)
    {
        switch (startType)
        {
            case 0: return "Boot";
            case 1: return "System";
            case 2: return "Automatic";
            case 3: return "Manual";
            case 4: return "Disabled";
            default: return "Unknown (" + startType + ")";
        }
    }

    public static string GetTriggerName(_TASK_TRIGGER_TYPE2 trigger)
    {
        switch (trigger)
        {
            case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT: return "Boot";
            case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON: return "Logon";
            case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_IDLE: return "Idle";
            case _TASK_TRIGGER_TYPE2.TASK_TRIGGER_DAILY: return "Daily";
            default: return trigger.ToString();
        }
    }

    private static readonly string WindowsDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    /// <summary>
    /// Rough, best-effort hint about whether an item ships with Windows or belongs to a
    /// third-party app, based only on where its executable lives. This is a guess, not a
    /// verified publisher check, so it deliberately never gives advice about disabling anything.
    /// </summary>
    public static string GetSourceHint(IStartupItem item)
    {
        string path = item.Path;
        if (string.IsNullOrWhiteSpace(path)) return "—";

        string trimmed = path.Trim();
        if (trimmed.StartsWith("\"")) trimmed = trimmed.Substring(1);
        if (trimmed.StartsWith(@"\??\")) trimmed = trimmed.Substring(4);

        if (!string.IsNullOrEmpty(WindowsDir) &&
            trimmed.StartsWith(WindowsDir, StringComparison.OrdinalIgnoreCase))
        {
            return "Windows";
        }

        return "Third-party";
    }

    /// <summary>
    /// Shows a read-only details dialog for a single startup item.
    /// </summary>
    public static void ShowDetails(IWin32Window owner, IStartupItem item)
    {
        string details =
            "Name:\r\n  " + item.Name + "\r\n\r\n" +
            "Type:\r\n  " + GetTypeLabel(item) + "\r\n\r\n" +
            "Starts:\r\n  " + GetDetail(item) + "\r\n\r\n" +
            "Source (guess):\r\n  " + GetSourceHint(item) + "\r\n\r\n" +
            "First detected:\r\n  " + item.FirstDetectionTime.ToString("F") + "\r\n\r\n" +
            "Path:\r\n  " + (string.IsNullOrEmpty(item.Path) ? "(none)" : item.Path);

        if (item is StartupService service)
            details += "\r\n\r\nRegistry key:\r\n  " + service.RegPath;
        else if (item is StartupTask task)
            details += "\r\n\r\nTask Scheduler path:\r\n  " + task.TaskSchedulerPath;

        MessageBox.Show(owner, details, "Startup Item Details",
            MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    /// <summary>
    /// Formats a first-detection timestamp with a wider gap between the date and the time so the
    /// two read apart cleanly.
    /// </summary>
    public static string FormatDetected(DateTime when)
        => when.ToShortDateString() + "     " + when.ToShortTimeString();

    /// <summary>
    /// Attaches a right-click "Copy" context menu (and Ctrl+C support) that copies the selected
    /// rows to the clipboard as tab-separated text. Reusable across every list in the app.
    /// </summary>
    public static void AttachCopyContextMenu(ListView listView)
    {
        var menu = new ContextMenuStrip();
        var copyItem = new ToolStripMenuItem("Copy")
        {
            ShortcutKeyDisplayString = "Ctrl+C"
        };
        copyItem.Click += (s, e) => CopySelectedRows(listView);
        menu.Items.Add(copyItem);

        // Only offer the menu when at least one row is selected.
        menu.Opening += (s, e) => e.Cancel = listView.SelectedItems.Count == 0;
        listView.ContextMenuStrip = menu;

        listView.KeyDown += (s, e) =>
        {
            if (e.Control && e.KeyCode == Keys.C)
            {
                CopySelectedRows(listView);
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// Copies the currently selected rows (all their columns) to the clipboard as tab-separated
    /// lines, in the order they appear in the list.
    /// </summary>
    public static void CopySelectedRows(ListView listView)
    {
        if (listView.SelectedItems.Count == 0)
            return;

        var sb = new StringBuilder();
        foreach (ListViewItem row in listView.SelectedItems)
        {
            var cells = new List<string>();
            foreach (ListViewItem.ListViewSubItem sub in row.SubItems)
                cells.Add(sub.Text);
            sb.AppendLine(string.Join("\t", cells));
        }

        try
        {
            Clipboard.SetText(sb.ToString());
        }
        catch
        {
            // The clipboard can occasionally be locked by another process; ignore rather than crash.
        }
    }
}
