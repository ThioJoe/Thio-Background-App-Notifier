using System;
using System.Drawing;
using System.Windows.Forms;

namespace New_Startup_App_Notifier;

/// <summary>
/// Explains Windows' built-in "Startup app notifications" feature (which covers the "regular"
/// startup apps this tool doesn't track) and lets the user turn it on or open its settings page.
/// </summary>
public partial class WindowsNotifyInfoForm : Form
{
    public WindowsNotifyInfoForm()
    {
        InitializeComponent();

        labelBody.Text =
            $"{AppName} watches for new startup services and scheduled tasks — the sneaky startup "
            + "items that are easy to miss.\r\n\r\n"
            + "It does not track \"regular\" startup apps: programs that add themselves to your Startup "
            + "folder or the Run registry keys. Windows can notify you about those itself.\r\n\r\n"
            + "This is controlled by a built-in Windows setting:\r\n"
            + "        Settings  ▸  System  ▸  Notifications  ▸  Startup app notifications\r\n\r\n"
            + "When it's on, Windows shows a notification whenever a new app is set to run at startup. "
            + "Turning it on complements this app, so you're covered for both kinds of startup items.";

        RefreshState();
    }

    private void RefreshState()
    {
        bool? enabled = WindowsStartupNotification.IsEnabled();

        if (enabled == true)
        {
            labelStatus.Text = "On";
            labelStatus.ForeColor = Color.FromArgb(0, 128, 0);
            buttonEnable.Enabled = false;
        }
        else if (enabled == false)
        {
            labelStatus.Text = "Off";
            labelStatus.ForeColor = Color.FromArgb(192, 0, 0);
            buttonEnable.Enabled = true;
        }
        else
        {
            labelStatus.Text = "Unknown (couldn't read the setting)";
            labelStatus.ForeColor = SystemColors.GrayText;
            buttonEnable.Enabled = true;
        }
    }

    private void buttonEnable_Click(object sender, EventArgs e)
    {
        if (WindowsStartupNotification.Enable(out string error))
        {
            RefreshState();
            MessageBox.Show(this, "Windows startup-app notifications have been turned on.",
                AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
            MessageBox.Show(this,
                "Couldn't change the setting automatically:\r\n" + error
                + "\r\n\r\nUse \"Open Windows Settings\" to change it manually.",
                AppName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void buttonOpenSettings_Click(object sender, EventArgs e)
    {
        WindowsStartupNotification.OpenSettings();
    }

    private void buttonClose_Click(object sender, EventArgs e)
    {
        this.Close();
    }
}
