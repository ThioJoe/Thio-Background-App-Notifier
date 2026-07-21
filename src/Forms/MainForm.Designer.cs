namespace Thio_Background_App_Notifier
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
            this.labelPlaceholder = new System.Windows.Forms.Label();
            this.labelMainListTitle = new System.Windows.Forms.Label();
            this.labelWinNotify = new System.Windows.Forms.Label();
            this.buttonWinNotify = new System.Windows.Forms.Button();
            this.listViewItems = new Thio_Background_App_Notifier.BufferedListView();
            this.colNew = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colFirstDetected = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStarts = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSource = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
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
            this.buttonAllStartupServices.Location = new System.Drawing.Point(1321, 20);
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
            this.buttonAllStartupTasks.Location = new System.Drawing.Point(1321, 70);
            this.buttonAllStartupTasks.Name = "buttonAllStartupTasks";
            this.buttonAllStartupTasks.Size = new System.Drawing.Size(210, 46);
            this.buttonAllStartupTasks.TabIndex = 5;
            this.buttonAllStartupTasks.Text = "All Startup Tasks";
            this.buttonAllStartupTasks.UseVisualStyleBackColor = true;
            this.buttonAllStartupTasks.Click += new System.EventHandler(this.buttonAllStartupTasks_Click);
            // 
            // labelStatus
            // 
            this.labelStatus.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.labelStatus.Location = new System.Drawing.Point(24, 18);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(1040, 40);
            this.labelStatus.TabIndex = 0;
            this.labelStatus.Text = "Scanning...";
            // 
            // labelSubStatus
            // 
            this.labelSubStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelSubStatus.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.labelSubStatus.Location = new System.Drawing.Point(26, 62);
            this.labelSubStatus.Name = "labelSubStatus";
            this.labelSubStatus.Size = new System.Drawing.Size(599, 58);
            this.labelSubStatus.TabIndex = 1;
            this.labelSubStatus.Text = "This app does not run in the background.\r\nIt only checks when you run it (for exa" +
    "mple at startup).";
            // 
            // checkBoxRunAtStartup
            // 
            this.checkBoxRunAtStartup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxRunAtStartup.AutoSize = true;
            this.checkBoxRunAtStartup.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBoxRunAtStartup.Location = new System.Drawing.Point(825, 94);
            this.checkBoxRunAtStartup.Name = "checkBoxRunAtStartup";
            this.checkBoxRunAtStartup.Size = new System.Drawing.Size(333, 26);
            this.checkBoxRunAtStartup.TabIndex = 2;
            this.checkBoxRunAtStartup.Text = "Re-check on each Windows starts up";
            this.checkBoxRunAtStartup.UseVisualStyleBackColor = true;
            this.checkBoxRunAtStartup.CheckedChanged += new System.EventHandler(this.checkBoxRunAtStartup_CheckedChanged);
            // 
            // buttonRescan
            // 
            this.buttonRescan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRescan.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonRescan.Location = new System.Drawing.Point(851, 40);
            this.buttonRescan.Name = "buttonRescan";
            this.buttonRescan.Size = new System.Drawing.Size(279, 46);
            this.buttonRescan.TabIndex = 6;
            this.buttonRescan.Text = "Rescan Now";
            this.buttonRescan.UseVisualStyleBackColor = true;
            this.buttonRescan.Click += new System.EventHandler(this.buttonRescan_Click);
            // 
            // labelPlaceholder
            // 
            this.labelPlaceholder.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPlaceholder.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.labelPlaceholder.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelPlaceholder.Location = new System.Drawing.Point(24, 248);
            this.labelPlaceholder.Name = "labelPlaceholder";
            this.labelPlaceholder.Size = new System.Drawing.Size(1513, 488);
            this.labelPlaceholder.TabIndex = 9;
            this.labelPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelPlaceholder.Visible = false;
            // 
            // labelMainListTitle
            // 
            this.labelMainListTitle.AutoSize = true;
            this.labelMainListTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMainListTitle.Location = new System.Drawing.Point(20, 212);
            this.labelMainListTitle.Name = "labelMainListTitle";
            this.labelMainListTitle.Size = new System.Drawing.Size(548, 29);
            this.labelMainListTitle.TabIndex = 10;
            this.labelMainListTitle.Text = "Latest New Startup Tasks && Background Services:\r\n";
            // 
            // labelWinNotify
            // 
            this.labelWinNotify.AutoSize = true;
            this.labelWinNotify.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.labelWinNotify.Location = new System.Drawing.Point(23, 130);
            this.labelWinNotify.Name = "labelWinNotify";
            this.labelWinNotify.Size = new System.Drawing.Size(434, 32);
            this.labelWinNotify.TabIndex = 11;
            this.labelWinNotify.Text = "Windows Startup App Notifications: ";
            // 
            // buttonWinNotify
            // 
            this.buttonWinNotify.Location = new System.Drawing.Point(507, 130);
            this.buttonWinNotify.Name = "buttonWinNotify";
            this.buttonWinNotify.Size = new System.Drawing.Size(135, 37);
            this.buttonWinNotify.TabIndex = 12;
            this.buttonWinNotify.Text = "More Info…";
            this.buttonWinNotify.UseVisualStyleBackColor = true;
            this.buttonWinNotify.Click += new System.EventHandler(this.buttonWinNotify_Click);
            // 
            // listViewItems
            // 
            this.listViewItems.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewItems.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colNew,
            this.colFirstDetected,
            this.colType,
            this.colName,
            this.colStarts,
            this.colSource,
            this.colPath});
            this.listViewItems.FullRowSelect = true;
            this.listViewItems.GridLines = true;
            this.listViewItems.HideSelection = false;
            this.listViewItems.Location = new System.Drawing.Point(24, 248);
            this.listViewItems.Name = "listViewItems";
            this.listViewItems.Size = new System.Drawing.Size(1513, 488);
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
            // colFirstDetected
            // 
            this.colFirstDetected.Text = "First Detected";
            this.colFirstDetected.Width = 210;
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
            // colPath
            // 
            this.colPath.Text = "Path";
            this.colPath.Width = 460;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1561, 760);
            this.Controls.Add(this.buttonWinNotify);
            this.Controls.Add(this.labelWinNotify);
            this.Controls.Add(this.labelMainListTitle);
            this.Controls.Add(this.labelPlaceholder);
            this.Controls.Add(this.listViewItems);
            this.Controls.Add(this.buttonRescan);
            this.Controls.Add(this.checkBoxRunAtStartup);
            this.Controls.Add(this.labelSubStatus);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.buttonAllStartupTasks);
            this.Controls.Add(this.buttonAllStartupServices);
            this.Controls.Add(this.buttonDevView);
            this.MinimumSize = new System.Drawing.Size(900, 500);
            this.Name = "MainForm";
            this.Text = "New Background App Notifier";
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
        private Thio_Background_App_Notifier.BufferedListView listViewItems;
        private System.Windows.Forms.Label labelPlaceholder;
        private System.Windows.Forms.ColumnHeader colNew;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colStarts;
        private System.Windows.Forms.ColumnHeader colSource;
        private System.Windows.Forms.ColumnHeader colFirstDetected;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.Label labelMainListTitle;
        private System.Windows.Forms.Label labelWinNotify;
        private System.Windows.Forms.Button buttonWinNotify;
    }
}
