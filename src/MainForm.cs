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

#nullable enable

namespace New_Startup_App_Notifier
{
    public partial class MainForm : Form
    {
        DevViewForm? _devViewForm = null;
        public MainForm()
        {
            InitializeComponent();

            TrayContextMenu trayContextMenu = new TrayContextMenu( // TODO
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

        private void buttonDevView_Click(object sender, EventArgs e)
        {
            // Open the DevViewForm window
            if (_devViewForm == null)
                _devViewForm = new DevViewForm();

            _devViewForm.Show();
            _devViewForm.Focus();
            _devViewForm.BringToFront();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            #if DEBUG
                _devViewForm?.Show();
                _devViewForm?.Focus();
                _devViewForm?.BringToFront();
            #endif
        }
    } // End of MainForm Class

} // End of Namespace
