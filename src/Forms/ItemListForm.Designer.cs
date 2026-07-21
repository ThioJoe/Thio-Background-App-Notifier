namespace Thio_Background_App_Notifier;

partial class ItemListForm
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
        this.listView = new Thio_Background_App_Notifier.BufferedListView();
        this.colName = new System.Windows.Forms.ColumnHeader();
        this.colStarts = new System.Windows.Forms.ColumnHeader();
        this.colSource = new System.Windows.Forms.ColumnHeader();
        this.colFirstDetected = new System.Windows.Forms.ColumnHeader();
        this.colPath = new System.Windows.Forms.ColumnHeader();
        this.buttonClose = new System.Windows.Forms.Button();
        this.SuspendLayout();
        //
        // labelTitle
        //
        this.labelTitle.AutoSize = false;
        this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
        this.labelTitle.Location = new System.Drawing.Point(16, 14);
        this.labelTitle.Name = "labelTitle";
        this.labelTitle.Size = new System.Drawing.Size(1000, 34);
        this.labelTitle.TabIndex = 0;
        this.labelTitle.Text = "";
        //
        // listView
        //
        this.listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
        | System.Windows.Forms.AnchorStyles.Left)
        | System.Windows.Forms.AnchorStyles.Right)));
        this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
        this.colName,
        this.colStarts,
        this.colSource,
        this.colFirstDetected,
        this.colPath});
        this.listView.FullRowSelect = true;
        this.listView.GridLines = true;
        this.listView.HideSelection = false;
        this.listView.Location = new System.Drawing.Point(16, 58);
        this.listView.Name = "listView";
        this.listView.Size = new System.Drawing.Size(1000, 512);
        this.listView.TabIndex = 1;
        this.listView.UseCompatibleStateImageBehavior = false;
        this.listView.View = System.Windows.Forms.View.Details;
        this.listView.DoubleClick += new System.EventHandler(this.listView_DoubleClick);
        //
        // colName
        //
        this.colName.Text = "Name";
        this.colName.Width = 300;
        //
        // colStarts
        //
        this.colStarts.Text = "Starts";
        this.colStarts.Width = 120;
        //
        // colSource
        //
        this.colSource.Text = "Source";
        this.colSource.Width = 110;
        //
        // colFirstDetected
        //
        this.colFirstDetected.Text = "First Detected";
        this.colFirstDetected.Width = 180;
        //
        // colPath
        //
        this.colPath.Text = "Path";
        this.colPath.Width = 460;
        //
        // buttonClose
        //
        this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonClose.Location = new System.Drawing.Point(872, 584);
        this.buttonClose.Name = "buttonClose";
        this.buttonClose.Size = new System.Drawing.Size(144, 46);
        this.buttonClose.TabIndex = 2;
        this.buttonClose.Text = "Close";
        this.buttonClose.UseVisualStyleBackColor = true;
        this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
        //
        // ItemListForm
        //
        this.AcceptButton = this.buttonClose;
        this.CancelButton = this.buttonClose;
        this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(1032, 648);
        this.Controls.Add(this.buttonClose);
        this.Controls.Add(this.listView);
        this.Controls.Add(this.labelTitle);
        this.MinimumSize = new System.Drawing.Size(700, 420);
        this.Name = "ItemListForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "Startup Items";
        this.ResumeLayout(false);
    }

    #endregion

    private System.Windows.Forms.Label labelTitle;
    private Thio_Background_App_Notifier.BufferedListView listView;
    private System.Windows.Forms.ColumnHeader colName;
    private System.Windows.Forms.ColumnHeader colStarts;
    private System.Windows.Forms.ColumnHeader colSource;
    private System.Windows.Forms.ColumnHeader colFirstDetected;
    private System.Windows.Forms.ColumnHeader colPath;
    private System.Windows.Forms.Button buttonClose;
}
