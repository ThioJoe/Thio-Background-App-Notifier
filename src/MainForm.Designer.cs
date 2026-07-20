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
            this.richTextBoxTemporaryDevOutput = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // richTextBoxTemporaryDevOutput
            // 
            this.richTextBoxTemporaryDevOutput.Location = new System.Drawing.Point(12, 132);
            this.richTextBoxTemporaryDevOutput.Name = "richTextBoxTemporaryDevOutput";
            this.richTextBoxTemporaryDevOutput.Size = new System.Drawing.Size(966, 508);
            this.richTextBoxTemporaryDevOutput.TabIndex = 0;
            this.richTextBoxTemporaryDevOutput.Text = "";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(990, 662);
            this.Controls.Add(this.richTextBoxTemporaryDevOutput);
            this.Name = "MainForm";
            this.Text = "Sneaky Startup App Notifier";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBoxTemporaryDevOutput;
    }
}

