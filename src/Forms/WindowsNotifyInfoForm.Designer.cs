namespace New_Startup_App_Notifier;

partial class WindowsNotifyInfoForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.labelTitle = new System.Windows.Forms.Label();
        this.labelBody = new System.Windows.Forms.Label();
        this.labelStatusCaption = new System.Windows.Forms.Label();
        this.labelStatus = new System.Windows.Forms.Label();
        this.buttonEnable = new System.Windows.Forms.Button();
        this.buttonOpenSettings = new System.Windows.Forms.Button();
        this.buttonClose = new System.Windows.Forms.Button();
        this.SuspendLayout();
        //
        // labelTitle
        //
        this.labelTitle.AutoSize = true;
        this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.labelTitle.Location = new System.Drawing.Point(20, 18);
        this.labelTitle.Name = "labelTitle";
        this.labelTitle.Size = new System.Drawing.Size(430, 38);
        this.labelTitle.TabIndex = 0;
        this.labelTitle.Text = "Windows Startup App Notifications";
        //
        // labelBody
        //
        this.labelBody.Location = new System.Drawing.Point(24, 64);
        this.labelBody.Name = "labelBody";
        this.labelBody.Size = new System.Drawing.Size(716, 296);
        this.labelBody.TabIndex = 1;
        this.labelBody.Text = "(details)";
        //
        // labelStatusCaption
        //
        this.labelStatusCaption.AutoSize = true;
        this.labelStatusCaption.Location = new System.Drawing.Point(24, 378);
        this.labelStatusCaption.Name = "labelStatusCaption";
        this.labelStatusCaption.Size = new System.Drawing.Size(130, 20);
        this.labelStatusCaption.TabIndex = 2;
        this.labelStatusCaption.Text = "Current setting:";
        //
        // labelStatus
        //
        this.labelStatus.AutoSize = true;
        this.labelStatus.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.labelStatus.Location = new System.Drawing.Point(164, 374);
        this.labelStatus.Name = "labelStatus";
        this.labelStatus.Size = new System.Drawing.Size(90, 28);
        this.labelStatus.TabIndex = 3;
        this.labelStatus.Text = "Checking…";
        //
        // buttonEnable
        //
        this.buttonEnable.Location = new System.Drawing.Point(24, 424);
        this.buttonEnable.Name = "buttonEnable";
        this.buttonEnable.Size = new System.Drawing.Size(250, 54);
        this.buttonEnable.TabIndex = 4;
        this.buttonEnable.Text = "Turn On Notifications";
        this.buttonEnable.UseVisualStyleBackColor = true;
        this.buttonEnable.Click += new System.EventHandler(this.buttonEnable_Click);
        //
        // buttonOpenSettings
        //
        this.buttonOpenSettings.Location = new System.Drawing.Point(288, 424);
        this.buttonOpenSettings.Name = "buttonOpenSettings";
        this.buttonOpenSettings.Size = new System.Drawing.Size(300, 54);
        this.buttonOpenSettings.TabIndex = 5;
        this.buttonOpenSettings.Text = "Open Windows Settings";
        this.buttonOpenSettings.UseVisualStyleBackColor = true;
        this.buttonOpenSettings.Click += new System.EventHandler(this.buttonOpenSettings_Click);
        //
        // buttonClose
        //
        this.buttonClose.Location = new System.Drawing.Point(604, 424);
        this.buttonClose.Name = "buttonClose";
        this.buttonClose.Size = new System.Drawing.Size(136, 54);
        this.buttonClose.TabIndex = 6;
        this.buttonClose.Text = "Close";
        this.buttonClose.UseVisualStyleBackColor = true;
        this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
        //
        // WindowsNotifyInfoForm
        //
        this.AcceptButton = this.buttonClose;
        this.CancelButton = this.buttonClose;
        this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(764, 502);
        this.Controls.Add(this.buttonClose);
        this.Controls.Add(this.buttonOpenSettings);
        this.Controls.Add(this.buttonEnable);
        this.Controls.Add(this.labelStatus);
        this.Controls.Add(this.labelStatusCaption);
        this.Controls.Add(this.labelBody);
        this.Controls.Add(this.labelTitle);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "WindowsNotifyInfoForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Windows Startup App Notifications";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private System.Windows.Forms.Label labelTitle;
    private System.Windows.Forms.Label labelBody;
    private System.Windows.Forms.Label labelStatusCaption;
    private System.Windows.Forms.Label labelStatus;
    private System.Windows.Forms.Button buttonEnable;
    private System.Windows.Forms.Button buttonOpenSettings;
    private System.Windows.Forms.Button buttonClose;
}
