namespace Thio_Background_App_Notifier;

partial class DevViewForm
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
            this.buttonTestModernDialog = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // richTextBoxDevOutput
            // 
            this.richTextBoxDevOutput.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxDevOutput.Location = new System.Drawing.Point(12, 118);
            this.richTextBoxDevOutput.Name = "richTextBoxDevOutput";
            this.richTextBoxDevOutput.Size = new System.Drawing.Size(1561, 649);
            this.richTextBoxDevOutput.TabIndex = 1;
            this.richTextBoxDevOutput.Text = "";
            // 
            // buttonDevTest
            // 
            this.buttonDevTest.Location = new System.Drawing.Point(709, 40);
            this.buttonDevTest.Name = "buttonDevTest";
            this.buttonDevTest.Size = new System.Drawing.Size(145, 47);
            this.buttonDevTest.TabIndex = 2;
            this.buttonDevTest.Text = "Test";
            this.buttonDevTest.UseVisualStyleBackColor = true;
            this.buttonDevTest.Click += new System.EventHandler(this.buttonDevTest_Click);
            // 
            // buttonTestModernDialog
            // 
            this.buttonTestModernDialog.Location = new System.Drawing.Point(145, 40);
            this.buttonTestModernDialog.Name = "buttonTestModernDialog";
            this.buttonTestModernDialog.Size = new System.Drawing.Size(139, 52);
            this.buttonTestModernDialog.TabIndex = 3;
            this.buttonTestModernDialog.Text = "Test Dialog";
            this.buttonTestModernDialog.UseVisualStyleBackColor = true;
            this.buttonTestModernDialog.Click += new System.EventHandler(this.buttonTestModernDialog_Click);
            // 
            // DevViewForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1585, 779);
            this.Controls.Add(this.buttonTestModernDialog);
            this.Controls.Add(this.buttonDevTest);
            this.Controls.Add(this.richTextBoxDevOutput);
            this.Name = "DevViewForm";
            this.Text = "DevViewForm";
            this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.RichTextBox richTextBoxDevOutput;
    private System.Windows.Forms.Button buttonDevTest;
    private System.Windows.Forms.Button buttonTestModernDialog;
}