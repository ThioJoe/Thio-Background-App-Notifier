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

namespace New_Startup_App_Notifier;

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
        ModernTaskDialog.Template.VerificationResult vr =
                    ModernTaskDialog.Template.ShowYesNoWithVerification(
                        title: "Sneaky Startup App Notifier",
                        mainInstruction: "Main Instruction Headline",
                        content: "Body Content",
                        verificationText: "Remind me again at next startup",
                        icon: ModernTaskDialog.TaskDialogIcon.Information);
    }
}
