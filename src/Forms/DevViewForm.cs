using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThioWinUtils;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using static ThioWinUtils.ModernTaskDialog;

namespace Thio_Background_App_Notifier;

public partial class DevViewForm : Form
{
    public DevViewForm()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fetches startup tasks and services and shows them in the dev text box control
    /// </summary>
    public void SimpleListStartupItems()
    {
        // Fetch startup tasks and services
        List<StartupService> startupServices = StartupScanner.GetStartupServices();
        List<StartupTask> startupScheduledTasks = StartupScanner.GetStartupScheduledTasks();

        // Generate text list
        string displayString = string.Empty;

        displayString += "========= SERVICES =========\n\n";
        foreach (StartupService item in startupServices)
        {
            displayString += $"{item.Name}:\n";
            displayString += $"\tPath: {item.ExecPath}" + "\n\n";
        }

        displayString += "\n\n========= SCHEDULED TASKS =========\n\n";
        foreach (StartupTask item in startupScheduledTasks)
        {
            string currTaskPaths = "\n\t" + string.Join("\n\t", item.ExecActionPathsWithArgs);

            displayString += $"{item.Name}:";
            displayString += currTaskPaths + "\n\n";
        }

        // Clear the text box before adding new items
        richTextBoxDevOutput.Clear();
        richTextBoxDevOutput.Text = displayString;
    }

    private void buttonDevTest_Click(object sender, EventArgs e)
    {
        SimpleListStartupItems();
    }

    private void buttonTestModernDialog_Click(object sender, EventArgs e)
    {
        int count = 2; // Dummy number
        string headline = count == 1
                ? "1 new startup item detected:"
                : count + " new startup items detected:";

        // List a few of the names so the user knows what turned up.
        const int maxNames = 5;
        List<string> names = ["Example Startup Name 1", "Example 2"]; // Dummy Names

        string body = "• " + string.Join("\n• ", names);

        if (count > maxNames)
            body += "\n… and " + (count - maxNames) + " more";
        body += $"\n\n———————\nSee details now?";


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
        //(bool open, bool VerificationChecked) result = (open, dialog.VerificationChecked);
    }
}
