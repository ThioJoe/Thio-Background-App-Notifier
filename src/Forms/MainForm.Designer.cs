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
            this.components = new System.ComponentModel.Container();
            this.labelAppTitle = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.labelAppSubtitle = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.buttonAbout = new System.Windows.Forms.Button();
            this.buttonRescan = new System.Windows.Forms.Button();
            this.buttonAllStartupServices = new System.Windows.Forms.Button();
            this.buttonAllStartupTasks = new System.Windows.Forms.Button();
            this.checkBoxRunAtStartup = new System.Windows.Forms.CheckBox();
            this.buttonDevView = new System.Windows.Forms.Button();
            this.panelHeaderDivider = new System.Windows.Forms.Panel();
            this.panelStatus = new System.Windows.Forms.Panel();
            this.labelTrackedCount = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.labelTrackedCaption = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.labelStatusDetail = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.labelStatusValue = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.panelWinNotify = new System.Windows.Forms.Panel();
            this.labelWinNotifyCaption = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.labelWinNotifyHint = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.labelWinNotifyValue = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.buttonWinNotify = new System.Windows.Forms.Button();
            this.labelMainListTitle = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.labelPlaceholder = new System.Windows.Forms.Label();
            this.labelRecheckSubtitle = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.groupBoxStartup = new System.Windows.Forms.GroupBox();
            this.listViewItems = new Thio_Background_App_Notifier.BufferedListView();
            this.colNew = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colFirstDetected = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStarts = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.labelVersion = new Thio_Background_App_Notifier.TrueTransparentLabel(this.components);
            this.panelStatus.SuspendLayout();
            this.panelWinNotify.SuspendLayout();
            this.groupBoxStartup.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelAppTitle
            // 
            this.labelAppTitle.AutoSize = true;
            this.labelAppTitle.BackColor = System.Drawing.Color.Transparent;
            this.labelAppTitle.Font = new System.Drawing.Font("Segoe UI", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAppTitle.Location = new System.Drawing.Point(22, 14);
            this.labelAppTitle.Name = "labelAppTitle";
            this.labelAppTitle.Size = new System.Drawing.Size(629, 55);
            this.labelAppTitle.TabIndex = 20;
            this.labelAppTitle.Text = "Thio\'s Background App Notifier";
            // 
            // labelAppSubtitle
            // 
            this.labelAppSubtitle.BackColor = System.Drawing.Color.Transparent;
            this.labelAppSubtitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAppSubtitle.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.labelAppSubtitle.Location = new System.Drawing.Point(28, 69);
            this.labelAppSubtitle.Name = "labelAppSubtitle";
            this.labelAppSubtitle.Size = new System.Drawing.Size(982, 40);
            this.labelAppSubtitle.TabIndex = 21;
            this.labelAppSubtitle.Text = "Tracks new background services and scheduled tasks that are set to automatically " +
    "run.";
            // 
            // buttonAbout
            // 
            this.buttonAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonAbout.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.buttonAbout.Location = new System.Drawing.Point(1397, 48);
            this.buttonAbout.Name = "buttonAbout";
            this.buttonAbout.Size = new System.Drawing.Size(140, 41);
            this.buttonAbout.TabIndex = 4;
            this.buttonAbout.Text = "About / Help";
            this.buttonAbout.UseCompatibleTextRendering = true;
            this.buttonAbout.UseVisualStyleBackColor = true;
            this.buttonAbout.Click += new System.EventHandler(this.buttonAbout_Click);
            // 
            // buttonRescan
            // 
            this.buttonRescan.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
            this.buttonRescan.Location = new System.Drawing.Point(24, 112);
            this.buttonRescan.Name = "buttonRescan";
            this.buttonRescan.Size = new System.Drawing.Size(294, 98);
            this.buttonRescan.TabIndex = 0;
            this.buttonRescan.Text = "Rescan Now";
            this.buttonRescan.UseVisualStyleBackColor = true;
            this.buttonRescan.Click += new System.EventHandler(this.buttonRescan_Click);
            // 
            // buttonAllStartupServices
            // 
            this.buttonAllStartupServices.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAllStartupServices.Location = new System.Drawing.Point(346, 112);
            this.buttonAllStartupServices.Name = "buttonAllStartupServices";
            this.buttonAllStartupServices.Size = new System.Drawing.Size(221, 46);
            this.buttonAllStartupServices.TabIndex = 1;
            this.buttonAllStartupServices.Text = "All Auto-Run Services";
            this.buttonAllStartupServices.UseVisualStyleBackColor = true;
            this.buttonAllStartupServices.Click += new System.EventHandler(this.buttonAllStartupServices_Click);
            // 
            // buttonAllStartupTasks
            // 
            this.buttonAllStartupTasks.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonAllStartupTasks.Location = new System.Drawing.Point(346, 164);
            this.buttonAllStartupTasks.Name = "buttonAllStartupTasks";
            this.buttonAllStartupTasks.Size = new System.Drawing.Size(221, 46);
            this.buttonAllStartupTasks.TabIndex = 2;
            this.buttonAllStartupTasks.Text = "All Auto-Run Tasks";
            this.buttonAllStartupTasks.UseVisualStyleBackColor = true;
            this.buttonAllStartupTasks.Click += new System.EventHandler(this.buttonAllStartupTasks_Click);
            // 
            // checkBoxRunAtStartup
            // 
            this.checkBoxRunAtStartup.AutoSize = true;
            this.checkBoxRunAtStartup.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.checkBoxRunAtStartup.Location = new System.Drawing.Point(16, 25);
            this.checkBoxRunAtStartup.Name = "checkBoxRunAtStartup";
            this.checkBoxRunAtStartup.Size = new System.Drawing.Size(346, 29);
            this.checkBoxRunAtStartup.TabIndex = 3;
            this.checkBoxRunAtStartup.Text = "Re-check on each Windows startup";
            this.checkBoxRunAtStartup.UseVisualStyleBackColor = true;
            this.checkBoxRunAtStartup.CheckedChanged += new System.EventHandler(this.checkBoxRunAtStartup_CheckedChanged);
            // 
            // buttonDevView
            // 
            this.buttonDevView.Enabled = false;
            this.buttonDevView.Location = new System.Drawing.Point(1314, 48);
            this.buttonDevView.Name = "buttonDevView";
            this.buttonDevView.Size = new System.Drawing.Size(66, 41);
            this.buttonDevView.TabIndex = 7;
            this.buttonDevView.Text = "Dev";
            this.buttonDevView.UseVisualStyleBackColor = true;
            this.buttonDevView.Visible = false;
            this.buttonDevView.Click += new System.EventHandler(this.buttonDevView_Click);
            // 
            // panelHeaderDivider
            // 
            this.panelHeaderDivider.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelHeaderDivider.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.panelHeaderDivider.Location = new System.Drawing.Point(24, 226);
            this.panelHeaderDivider.Name = "panelHeaderDivider";
            this.panelHeaderDivider.Size = new System.Drawing.Size(1513, 2);
            this.panelHeaderDivider.TabIndex = 22;
            // 
            // panelStatus
            // 
            this.panelStatus.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(248)))), ((int)(((byte)(248)))));
            this.panelStatus.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelStatus.Controls.Add(this.labelTrackedCount);
            this.panelStatus.Controls.Add(this.labelTrackedCaption);
            this.panelStatus.Controls.Add(this.labelStatusDetail);
            this.panelStatus.Controls.Add(this.labelStatusValue);
            this.panelStatus.Location = new System.Drawing.Point(24, 241);
            this.panelStatus.Name = "panelStatus";
            this.panelStatus.Size = new System.Drawing.Size(704, 163);
            this.panelStatus.TabIndex = 23;
            // 
            // labelTrackedCount
            // 
            this.labelTrackedCount.BackColor = System.Drawing.Color.Transparent;
            this.labelTrackedCount.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTrackedCount.Location = new System.Drawing.Point(227, 69);
            this.labelTrackedCount.Name = "labelTrackedCount";
            this.labelTrackedCount.Size = new System.Drawing.Size(226, 57);
            this.labelTrackedCount.TabIndex = 2;
            this.labelTrackedCount.Text = "__";
            this.labelTrackedCount.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // labelTrackedCaption
            // 
            this.labelTrackedCaption.BackColor = System.Drawing.Color.Transparent;
            this.labelTrackedCaption.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTrackedCaption.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.labelTrackedCaption.Location = new System.Drawing.Point(227, 126);
            this.labelTrackedCaption.Name = "labelTrackedCaption";
            this.labelTrackedCaption.Size = new System.Drawing.Size(226, 31);
            this.labelTrackedCaption.TabIndex = 3;
            this.labelTrackedCaption.Text = "startup items tracked";
            this.labelTrackedCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelStatusDetail
            // 
            this.labelStatusDetail.BackColor = System.Drawing.Color.Transparent;
            this.labelStatusDetail.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStatusDetail.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.labelStatusDetail.Location = new System.Drawing.Point(18, 49);
            this.labelStatusDetail.Name = "labelStatusDetail";
            this.labelStatusDetail.Size = new System.Drawing.Size(681, 24);
            this.labelStatusDetail.TabIndex = 1;
            this.labelStatusDetail.Text = "Last Checked:";
            // 
            // labelStatusValue
            // 
            this.labelStatusValue.BackColor = System.Drawing.Color.Transparent;
            this.labelStatusValue.Font = new System.Drawing.Font("Segoe UI", 13.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelStatusValue.Location = new System.Drawing.Point(15, 8);
            this.labelStatusValue.Name = "labelStatusValue";
            this.labelStatusValue.Size = new System.Drawing.Size(684, 41);
            this.labelStatusValue.TabIndex = 0;
            this.labelStatusValue.Text = "Fetching auto-starting items...";
            // 
            // panelWinNotify
            // 
            this.panelWinNotify.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.panelWinNotify.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(250)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.panelWinNotify.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panelWinNotify.Controls.Add(this.labelWinNotifyCaption);
            this.panelWinNotify.Controls.Add(this.labelWinNotifyHint);
            this.panelWinNotify.Controls.Add(this.labelWinNotifyValue);
            this.panelWinNotify.Controls.Add(this.buttonWinNotify);
            this.panelWinNotify.Location = new System.Drawing.Point(943, 241);
            this.panelWinNotify.Name = "panelWinNotify";
            this.panelWinNotify.Size = new System.Drawing.Size(594, 163);
            this.panelWinNotify.TabIndex = 24;
            // 
            // labelWinNotifyCaption
            // 
            this.labelWinNotifyCaption.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWinNotifyCaption.AutoSize = true;
            this.labelWinNotifyCaption.BackColor = System.Drawing.Color.Transparent;
            this.labelWinNotifyCaption.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWinNotifyCaption.Location = new System.Drawing.Point(16, 12);
            this.labelWinNotifyCaption.Name = "labelWinNotifyCaption";
            this.labelWinNotifyCaption.Size = new System.Drawing.Size(420, 32);
            this.labelWinNotifyCaption.TabIndex = 0;
            this.labelWinNotifyCaption.Text = "Windows Startup App Notifications";
            // 
            // labelWinNotifyHint
            // 
            this.labelWinNotifyHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWinNotifyHint.AutoSize = true;
            this.labelWinNotifyHint.BackColor = System.Drawing.Color.Transparent;
            this.labelWinNotifyHint.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWinNotifyHint.ForeColor = System.Drawing.SystemColors.ControlDarkDark;
            this.labelWinNotifyHint.Location = new System.Drawing.Point(17, 46);
            this.labelWinNotifyHint.Name = "labelWinNotifyHint";
            this.labelWinNotifyHint.Size = new System.Drawing.Size(440, 50);
            this.labelWinNotifyHint.TabIndex = 1;
            this.labelWinNotifyHint.Text = "Windows\' own alert when a new startup app is added.\r\nRecommended you turn this On" +
    ".";
            // 
            // labelWinNotifyValue
            // 
            this.labelWinNotifyValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelWinNotifyValue.BackColor = System.Drawing.Color.Transparent;
            this.labelWinNotifyValue.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelWinNotifyValue.Location = new System.Drawing.Point(463, 52);
            this.labelWinNotifyValue.Name = "labelWinNotifyValue";
            this.labelWinNotifyValue.Size = new System.Drawing.Size(114, 44);
            this.labelWinNotifyValue.TabIndex = 2;
            this.labelWinNotifyValue.Text = "—";
            this.labelWinNotifyValue.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonWinNotify
            // 
            this.buttonWinNotify.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonWinNotify.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.buttonWinNotify.Location = new System.Drawing.Point(22, 104);
            this.buttonWinNotify.Name = "buttonWinNotify";
            this.buttonWinNotify.Size = new System.Drawing.Size(122, 40);
            this.buttonWinNotify.TabIndex = 3;
            this.buttonWinNotify.Text = "More Info…";
            this.buttonWinNotify.UseVisualStyleBackColor = true;
            this.buttonWinNotify.Click += new System.EventHandler(this.buttonWinNotify_Click);
            // 
            // labelMainListTitle
            // 
            this.labelMainListTitle.AutoSize = true;
            this.labelMainListTitle.BackColor = System.Drawing.Color.Transparent;
            this.labelMainListTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelMainListTitle.Location = new System.Drawing.Point(24, 413);
            this.labelMainListTitle.Name = "labelMainListTitle";
            this.labelMainListTitle.Size = new System.Drawing.Size(559, 32);
            this.labelMainListTitle.TabIndex = 25;
            this.labelMainListTitle.Text = "New automatic starting items since your last check:";
            // 
            // labelPlaceholder
            // 
            this.labelPlaceholder.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelPlaceholder.Font = new System.Drawing.Font("Segoe UI", 11F);
            this.labelPlaceholder.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelPlaceholder.Location = new System.Drawing.Point(24, 534);
            this.labelPlaceholder.Name = "labelPlaceholder";
            this.labelPlaceholder.Size = new System.Drawing.Size(1513, 468);
            this.labelPlaceholder.TabIndex = 26;
            this.labelPlaceholder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelPlaceholder.Visible = false;
            // 
            // labelRecheckSubtitle
            // 
            this.labelRecheckSubtitle.AutoSize = true;
            this.labelRecheckSubtitle.BackColor = System.Drawing.Color.Transparent;
            this.labelRecheckSubtitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.labelRecheckSubtitle.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelRecheckSubtitle.Location = new System.Drawing.Point(13, 55);
            this.labelRecheckSubtitle.Name = "labelRecheckSubtitle";
            this.labelRecheckSubtitle.Size = new System.Drawing.Size(377, 22);
            this.labelRecheckSubtitle.TabIndex = 27;
            this.labelRecheckSubtitle.Text = "(Shows a message box only if new one found)";
            // 
            // groupBoxStartup
            // 
            this.groupBoxStartup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxStartup.Controls.Add(this.checkBoxRunAtStartup);
            this.groupBoxStartup.Controls.Add(this.labelRecheckSubtitle);
            this.groupBoxStartup.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBoxStartup.Location = new System.Drawing.Point(1108, 112);
            this.groupBoxStartup.Name = "groupBoxStartup";
            this.groupBoxStartup.Size = new System.Drawing.Size(429, 92);
            this.groupBoxStartup.TabIndex = 28;
            this.groupBoxStartup.TabStop = false;
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
            this.colPath});
            this.listViewItems.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listViewItems.FullRowSelect = true;
            this.listViewItems.GridLines = true;
            this.listViewItems.HideSelection = false;
            this.listViewItems.Location = new System.Drawing.Point(24, 448);
            this.listViewItems.Name = "listViewItems";
            this.listViewItems.Size = new System.Drawing.Size(1513, 554);
            this.listViewItems.TabIndex = 5;
            this.listViewItems.UseCompatibleStateImageBehavior = false;
            this.listViewItems.View = System.Windows.Forms.View.Details;
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
            // colPath
            // 
            this.colPath.Text = "Path";
            this.colPath.Width = 460;
            // 
            // labelVersion
            // 
            this.labelVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelVersion.AutoSize = true;
            this.labelVersion.BackColor = System.Drawing.Color.Transparent;
            this.labelVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.labelVersion.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelVersion.Location = new System.Drawing.Point(1365, 14);
            this.labelVersion.Margin = new System.Windows.Forms.Padding(0);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(172, 25);
            this.labelVersion.TabIndex = 29;
            this.labelVersion.Text = "Version: Unknown";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1561, 1026);
            this.Controls.Add(this.labelVersion);
            this.Controls.Add(this.groupBoxStartup);
            this.Controls.Add(this.panelStatus);
            this.Controls.Add(this.panelWinNotify);
            this.Controls.Add(this.labelAppTitle);
            this.Controls.Add(this.labelAppSubtitle);
            this.Controls.Add(this.buttonAbout);
            this.Controls.Add(this.buttonRescan);
            this.Controls.Add(this.buttonAllStartupServices);
            this.Controls.Add(this.buttonAllStartupTasks);
            this.Controls.Add(this.panelHeaderDivider);
            this.Controls.Add(this.labelMainListTitle);
            this.Controls.Add(this.labelPlaceholder);
            this.Controls.Add(this.listViewItems);
            this.Controls.Add(this.buttonDevView);
            this.MinimumSize = new System.Drawing.Size(1300, 560);
            this.Name = "MainForm";
            this.Text = "Thio\'s Background App Notifier";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.panelStatus.ResumeLayout(false);
            this.panelWinNotify.ResumeLayout(false);
            this.panelWinNotify.PerformLayout();
            this.groupBoxStartup.ResumeLayout(false);
            this.groupBoxStartup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TrueTransparentLabel labelAppTitle;
        private TrueTransparentLabel labelAppSubtitle;
        private System.Windows.Forms.Button buttonAbout;
        private System.Windows.Forms.Button buttonRescan;
        private System.Windows.Forms.Button buttonAllStartupServices;
        private System.Windows.Forms.Button buttonAllStartupTasks;
        private System.Windows.Forms.CheckBox checkBoxRunAtStartup;
        private System.Windows.Forms.Button buttonDevView;
        private System.Windows.Forms.Panel panelHeaderDivider;
        private System.Windows.Forms.Panel panelStatus;
        private TrueTransparentLabel labelTrackedCount;
        private TrueTransparentLabel labelTrackedCaption;
        private System.Windows.Forms.Panel panelWinNotify;
        private TrueTransparentLabel labelWinNotifyCaption;
        private TrueTransparentLabel labelWinNotifyHint;
        private TrueTransparentLabel labelWinNotifyValue;
        private System.Windows.Forms.Button buttonWinNotify;
        private Thio_Background_App_Notifier.BufferedListView listViewItems;
        private System.Windows.Forms.Label labelPlaceholder;
        private System.Windows.Forms.ColumnHeader colNew;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colStarts;
        private System.Windows.Forms.ColumnHeader colFirstDetected;
        private System.Windows.Forms.ColumnHeader colPath;
        private TrueTransparentLabel labelMainListTitle;
        private TrueTransparentLabel labelRecheckSubtitle;
        private System.Windows.Forms.GroupBox groupBoxStartup;
        private TrueTransparentLabel labelStatusValue;
        private TrueTransparentLabel labelStatusDetail;
        private TrueTransparentLabel labelVersion;
    }
}
