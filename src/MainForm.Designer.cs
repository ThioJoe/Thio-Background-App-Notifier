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
            this.richTextBoxDevOutput = new System.Windows.Forms.RichTextBox();
            this.buttonDevTest = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBoxDevOutput
            // 
            this.richTextBoxDevOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxDevOutput.Location = new System.Drawing.Point(12, 132);
            this.richTextBoxDevOutput.Name = "richTextBoxDevOutput";
            this.richTextBoxDevOutput.Size = new System.Drawing.Size(1521, 508);
            this.richTextBoxDevOutput.TabIndex = 0;
            this.richTextBoxDevOutput.Text = "";
            // 
            // buttonDevTest
            // 
            this.buttonDevTest.Location = new System.Drawing.Point(725, 56);
            this.buttonDevTest.Name = "buttonDevTest";
            this.buttonDevTest.Size = new System.Drawing.Size(145, 47);
            this.buttonDevTest.TabIndex = 1;
            this.buttonDevTest.Text = "Test";
            this.buttonDevTest.UseVisualStyleBackColor = true;
            this.buttonDevTest.Click += new System.EventHandler(this.buttonDevTest_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1561, 662);
            this.Controls.Add(this.buttonDevTest);
            this.Controls.Add(this.richTextBoxDevOutput);
            this.Name = "MainForm";
            this.Text = "Sneaky Startup App Notifier";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBoxDevOutput;
        private System.Windows.Forms.Button buttonDevTest;
    }
}

