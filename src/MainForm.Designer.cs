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
            this.SuspendLayout();
            // 
            // buttonDevView
            // 
            this.buttonDevView.Enabled = false;
            this.buttonDevView.Location = new System.Drawing.Point(1392, 36);
            this.buttonDevView.Name = "buttonDevView";
            this.buttonDevView.Size = new System.Drawing.Size(140, 48);
            this.buttonDevView.TabIndex = 0;
            this.buttonDevView.Text = "Dev Window";
            this.buttonDevView.UseVisualStyleBackColor = true;
            this.buttonDevView.Visible = false;
            this.buttonDevView.Click += new System.EventHandler(this.buttonDevView_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1561, 662);
            this.Controls.Add(this.buttonDevView);
            this.Name = "MainForm";
            this.Text = "Sneaky Startup App Notifier";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonDevView;
    }
}

