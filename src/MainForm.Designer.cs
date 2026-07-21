namespace New_Startup_App_Notifier
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonDevView = new System.Windows.Forms.Button();
            this.buttonAllStartupServices = new System.Windows.Forms.Button();
            this.buttonAllStartupTasks = new System.Windows.Forms.Button();
            this.labelStatus = new System.Windows.Forms.Label();
            this.labelSubStatus = new System.Windows.Forms.Label();
            this.checkBoxRunAtStartup = new System.Windows.Forms.CheckBox();
            this.buttonRescan = new System.Windows.Forms.Button();
            this.buttonOpenLogFolder = new System.Windows.Forms.Button();
            this.listViewItems = new System.Windows.Forms.ListView();
            this.colNew = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colStarts = new System.Windows.Forms.ColumnHeader();
            this.colSource = new System.Windows.Forms.ColumnHeader();
            this.colFirstDetected = new System.Windows.Forms.ColumnHeader();
            this.colPath = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            //
            // buttonDevView
            //
            this.buttonDevView.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDevView.Enabled = false;
            this.buttonDevView.Location = new System.Drawing.Point(1321, 122);
            this.buttonDevView.Name = "buttonDevView";
            this.buttonDevView.Size = new System.Drawing.Size(210, 40);
            this.buttonDevView.TabIndex = 8;
            this.buttonDevView.Text = "Dev Window";
            this.buttonDevView.UseVisualStyleBackColor = true;
            this.buttonDevView.Visible = false;
            this.buttonDevView.Click += new System.EventHandler(this.buttonDevView_Click);
            //
            // buttonAllStartupServices
            //
            this.buttonAllStartupServices.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAllStartupServices.Location = new System.Drawing.Point(1097, 18);
            this.buttonAllStartupServices.Name = "buttonAllStartupServices";
            this.buttonAllStartupServices.Size = new System.Drawing.Size(210, 46);
            this.buttonAllStartupServices.TabIndex = 4;
            this.buttonAllStartupServices.Text = "All Startup Services";
            this.buttonAllStartupServices.UseVisualStyleBackColor = true;
            this.buttonAllStartupServices.Click += new System.EventHandler(this.buttonAllStartupServices_Click);
            //
            // buttonAllStartupTasks
            //
            this.buttonAllStartupTasks.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAllStartupTasks.Location = new System.Drawing.Point(1097, 72);
            this.buttonAllStartupTasks.Name = "buttonAllStartupTasks";
            this.buttonAllStartupTasks.Size = new System.Drawing.Size(210, 46);
            this.buttonAllStartupTasks.TabIndex = 5;
            this.buttonAllStartupTasks.Text = "All Startup Tasks";
            this.buttonAllStartupTasks.UseVisualStyleBackColor = true;
            this.buttonAllStartupTasks.Click += new System.EventHandler(this.buttonAllStartupTasks_Click);
            //
            // labelStatus
            //
            this.labelStatus.AutoSize = false;
            this.labelStatus.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelStatus.Location = new System.Drawing.Point(24, 18);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(1040, 40);
            this.labelStatus.TabIndex = 0;
            this.labelStatus.Text = "Scanning...";
            //
            // labelSubStatus
            //
            this.labelSubStatus.AutoSize = false;
            this.labelSubStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelSubStatus.Location = new System.Drawing.Point(26, 62);
            this.labelSubStatus.Name = "labelSubStatus";
            this.labelSubStatus.Size = new System.Drawing.Size(1040, 58);
            this.labelSubStatus.TabIndex = 1;
            this.labelSubStatus.Text = "This app does not run in the background. It only checks when you run it (for example at startup).";
            //
            // checkBoxRunAtStartup
            //
            this.checkBoxRunAtStartup.AutoSize = true;
            this.checkBoxRunAtStartup.Location = new System.Drawing.Point(26, 128);
            this.checkBoxRunAtStartup.Name = "checkBoxRunAtStartup";
            this.checkBoxRunAtStartup.Size = new System.Drawing.Size(15, 14);
            this.checkBoxRunAtStartup.TabIndex = 2;
            this.checkBoxRunAtStartup.Text = "Check quietly at Windows startup (adds a shortcut to your Startup folder)";
            this.checkBoxRunAtStartup.UseVisualStyleBackColor = true;
            this.checkBoxRunAtStartup.CheckedChanged += new System.EventHandler(this.checkBoxRunAtStartup_CheckedChanged);
            //
            // buttonRescan
            //
            this.buttonRescan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRescan.Location = new System.Drawing.Point(1321, 18);
            this.buttonRescan.Name = "buttonRescan";
            this.buttonRescan.Size = new System.Drawing.Size(210, 46);
            this.buttonRescan.TabIndex = 6;
            this.buttonRescan.Text = "Rescan Now";
            this.buttonRescan.UseVisualStyleBackColor = true;
            this.buttonRescan.Click += new System.EventHandler(this.buttonRescan_Click);
            //
            // buttonOpenLogFolder
            //
            this.buttonOpenLogFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOpenLogFolder.Location = new System.Drawing.Point(1321, 72);
            this.buttonOpenLogFolder.Name = "buttonOpenLogFolder";
            this.buttonOpenLogFolder.Size = new System.Drawing.Size(210, 46);
            this.buttonOpenLogFolder.TabIndex = 7;
            this.buttonOpenLogFolder.Text = "Open Log Folder";
            this.buttonOpenLogFolder.UseVisualStyleBackColor = true;
            this.buttonOpenLogFolder.Click += new System.EventHandler(this.buttonOpenLogFolder_Click);
            //
            // listViewItems
            //
            this.listViewItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewItems.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colNew,
            this.colType,
            this.colName,
            this.colStarts,
            this.colSource,
            this.colFirstDetected,
            this.colPath});
            this.listViewItems.FullRowSelect = true;
            this.listViewItems.GridLines = true;
            this.listViewItems.HideSelection = false;
            this.listViewItems.Location = new System.Drawing.Point(24, 172);
            this.listViewItems.Name = "listViewItems";
            this.listViewItems.Size = new System.Drawing.Size(1513, 466);
            this.listViewItems.TabIndex = 3;
            this.listViewItems.UseCompatibleStateImageBehavior = false;
            this.listViewItems.View = System.Windows.Forms.View.Details;
            this.listViewItems.DoubleClick += new System.EventHandler(this.listViewItems_DoubleClick);
            //
            // colNew
            //
            this.colNew.Text = "";
            this.colNew.Width = 54;
            //
            // colType
            //
            this.colType.Text = "Type";
            this.colType.Width = 130;
            //
            // colName
            //
            this.colName.Text = "Name";
            this.colName.Width = 320;
            //
            // colStarts
            //
            this.colStarts.Text = "Starts";
            this.colStarts.Width = 130;
            //
            // colSource
            //
            this.colSource.Text = "Source";
            this.colSource.Width = 110;
            //
            // colFirstDetected
            //
            this.colFirstDetected.Text = "First Detected";
            this.colFirstDetected.Width = 190;
            //
            // colPath
            //
            this.colPath.Text = "Path";
            this.colPath.Width = 460;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1561, 662);
            this.Controls.Add(this.listViewItems);
            this.Controls.Add(this.buttonOpenLogFolder);
            this.Controls.Add(this.buttonRescan);
            this.Controls.Add(this.checkBoxRunAtStartup);
            this.Controls.Add(this.labelSubStatus);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.buttonAllStartupTasks);
            this.Controls.Add(this.buttonAllStartupServices);
            this.Controls.Add(this.buttonDevView);
            this.MinimumSize = new System.Drawing.Size(900, 500);
            this.Name = "MainForm";
            this.Text = "Sneaky Startup App Notifier";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonDevView;
        private System.Windows.Forms.Button buttonAllStartupServices;
        private System.Windows.Forms.Button buttonAllStartupTasks;
        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.Label labelSubStatus;
        private System.Windows.Forms.CheckBox checkBoxRunAtStartup;
        private System.Windows.Forms.Button buttonRescan;
        private System.Windows.Forms.Button buttonOpenLogFolder;
        private System.Windows.Forms.ListView listViewItems;
        private System.Windows.Forms.ColumnHeader colNew;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colStarts;
        private System.Windows.Forms.ColumnHeader colSource;
        private System.Windows.Forms.ColumnHeader colFirstDetected;
        private System.Windows.Forms.ColumnHeader colPath;
    }
}
