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
            this.components = new System.ComponentModel.Container();
            this.labelTitle = new TrueTransparentLabel();
            this.labelElevationNote = new TrueTransparentLabel();
            this.listView = new Thio_Background_App_Notifier.BufferedListView();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colStarts = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSource = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colFirstDetected = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colPath = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.textBoxListFilter = new System.Windows.Forms.TextBox();
            this.labelFilter = new TrueTransparentLabel();
            this.buttonClearFilter = new System.Windows.Forms.Button();
            this.radioButtonFilterAll = new System.Windows.Forms.RadioButton();
            this.radioButtonFilterName = new System.Windows.Forms.RadioButton();
            this.radioButtonFilterPath = new System.Windows.Forms.RadioButton();
            this.buttonToggleCaseSensitivity = new Thio_Background_App_Notifier.ToggleButton(this.components);
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.flowLayoutPanelFilterRadios = new System.Windows.Forms.FlowLayoutPanel();
            this.labelFilterColumnRadios = new TrueTransparentLabel();
            this.flowLayoutPanelFilterRadios.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelTitle
            // 
            this.labelTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.labelTitle.Location = new System.Drawing.Point(16, 14);
            this.labelTitle.BackColor = System.Drawing.Color.Transparent;
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(1000, 34);
            this.labelTitle.TabIndex = 0;
            // 
            // labelElevationNote
            // 
            this.labelElevationNote.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelElevationNote.AutoSize = true;
            this.labelElevationNote.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelElevationNote.ForeColor = System.Drawing.SystemColors.GrayText;
            this.labelElevationNote.BackColor = System.Drawing.Color.Transparent;
            this.labelElevationNote.Location = new System.Drawing.Point(1275, 20);
            this.labelElevationNote.Name = "labelElevationNote";
            this.labelElevationNote.Size = new System.Drawing.Size(542, 25);
            this.labelElevationNote.TabIndex = 3;
            this.labelElevationNote.Text = "*Grayed-out items are only updated when running as Administrator";
            this.labelElevationNote.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.labelElevationNote.Visible = false;
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
            this.listView.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.listView.FullRowSelect = true;
            this.listView.GridLines = true;
            this.listView.HideSelection = false;
            this.listView.Location = new System.Drawing.Point(16, 58);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(1801, 828);
            this.listView.TabIndex = 1;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            // 
            // colName
            // 
            this.colName.Text = "Name";
            this.colName.Width = 300;
            // 
            // colStarts
            // 
            this.colStarts.Text = "Starts";
            this.colStarts.Width = 125;
            // 
            // colSource
            // 
            this.colSource.Text = "Source (guess)";
            this.colSource.Width = 120;
            // 
            // colFirstDetected
            // 
            this.colFirstDetected.Text = "First Detected";
            this.colFirstDetected.Width = 180;
            // 
            // colPath
            // 
            this.colPath.Text = "Path";
            this.colPath.Width = 1062;
            // 
            // textBoxListFilter
            // 
            this.textBoxListFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxListFilter.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.textBoxListFilter.Location = new System.Drawing.Point(85, 908);
            this.textBoxListFilter.Name = "textBoxListFilter";
            this.textBoxListFilter.Size = new System.Drawing.Size(442, 35);
            this.textBoxListFilter.TabIndex = 4;
            this.textBoxListFilter.TextChanged += new System.EventHandler(this.textBoxListFilter_TextChanged);
            // 
            // labelFilter
            // 
            this.labelFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelFilter.AutoSize = true;
            this.labelFilter.BackColor = System.Drawing.Color.Transparent;
            this.labelFilter.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.labelFilter.Location = new System.Drawing.Point(14, 908);
            this.labelFilter.Name = "labelFilter";
            this.labelFilter.Size = new System.Drawing.Size(74, 29);
            this.labelFilter.TabIndex = 5;
            this.labelFilter.Text = "Filter:";
            // 
            // buttonClearFilter
            // 
            this.buttonClearFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonClearFilter.BackColor = System.Drawing.SystemColors.Window;
            this.buttonClearFilter.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F);
            this.buttonClearFilter.Location = new System.Drawing.Point(85, 949);
            this.buttonClearFilter.Name = "buttonClearFilter";
            this.buttonClearFilter.Size = new System.Drawing.Size(64, 28);
            this.buttonClearFilter.TabIndex = 6;
            this.buttonClearFilter.Text = "Clear";
            this.buttonClearFilter.UseCompatibleTextRendering = true;
            this.buttonClearFilter.UseVisualStyleBackColor = false;
            this.buttonClearFilter.Click += new System.EventHandler(this.buttonClearFilter_Click);
            // 
            // radioButtonFilterAll
            // 
            this.radioButtonFilterAll.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.radioButtonFilterAll.AutoSize = true;
            this.radioButtonFilterAll.Checked = true;
            this.radioButtonFilterAll.Location = new System.Drawing.Point(3, 3);
            this.radioButtonFilterAll.Name = "radioButtonFilterAll";
            this.radioButtonFilterAll.Size = new System.Drawing.Size(55, 26);
            this.radioButtonFilterAll.TabIndex = 0;
            this.radioButtonFilterAll.TabStop = true;
            this.radioButtonFilterAll.Text = "All";
            this.radioButtonFilterAll.UseVisualStyleBackColor = true;
            this.radioButtonFilterAll.CheckedChanged += new System.EventHandler(this.onChangeFilterColumnRadioCheckChanged);
            // 
            // radioButtonFilterName
            // 
            this.radioButtonFilterName.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.radioButtonFilterName.AutoSize = true;
            this.radioButtonFilterName.Location = new System.Drawing.Point(64, 3);
            this.radioButtonFilterName.Name = "radioButtonFilterName";
            this.radioButtonFilterName.Size = new System.Drawing.Size(82, 26);
            this.radioButtonFilterName.TabIndex = 1;
            this.radioButtonFilterName.Tag = this.colName;
            this.radioButtonFilterName.Text = "Name";
            this.radioButtonFilterName.UseVisualStyleBackColor = true;
            this.radioButtonFilterName.CheckedChanged += new System.EventHandler(this.onChangeFilterColumnRadioCheckChanged);
            // 
            // radioButtonFilterPath
            // 
            this.radioButtonFilterPath.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.radioButtonFilterPath.AutoSize = true;
            this.radioButtonFilterPath.Location = new System.Drawing.Point(152, 3);
            this.radioButtonFilterPath.Name = "radioButtonFilterPath";
            this.radioButtonFilterPath.Size = new System.Drawing.Size(72, 26);
            this.radioButtonFilterPath.TabIndex = 2;
            this.radioButtonFilterPath.Tag = this.colPath;
            this.radioButtonFilterPath.Text = "Path";
            this.radioButtonFilterPath.UseVisualStyleBackColor = true;
            this.radioButtonFilterPath.CheckedChanged += new System.EventHandler(this.onChangeFilterColumnRadioCheckChanged);
            // 
            // buttonToggleCaseSensitivity
            // 
            this.buttonToggleCaseSensitivity.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonToggleCaseSensitivity.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.buttonToggleCaseSensitivity.Location = new System.Drawing.Point(533, 910);
            this.buttonToggleCaseSensitivity.Name = "buttonToggleCaseSensitivity";
            this.buttonToggleCaseSensitivity.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this.buttonToggleCaseSensitivity.Size = new System.Drawing.Size(43, 35);
            this.buttonToggleCaseSensitivity.TabIndex = 8;
            this.buttonToggleCaseSensitivity.Text = "Aa";
            this.buttonToggleCaseSensitivity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonToggleCaseSensitivity.UseCompatibleTextRendering = true;
            this.buttonToggleCaseSensitivity.UseVisualStyleBackColor = true;
            this.buttonToggleCaseSensitivity.Click += new System.EventHandler(this.buttonToggleCaseSensitivity_Click);
            // 
            // flowLayoutPanelFilterRadios
            // 
            this.flowLayoutPanelFilterRadios.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.flowLayoutPanelFilterRadios.Controls.Add(this.radioButtonFilterAll);
            this.flowLayoutPanelFilterRadios.Controls.Add(this.radioButtonFilterName);
            this.flowLayoutPanelFilterRadios.Controls.Add(this.radioButtonFilterPath);
            this.flowLayoutPanelFilterRadios.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.flowLayoutPanelFilterRadios.Location = new System.Drawing.Point(610, 926);
            this.flowLayoutPanelFilterRadios.Margin = new System.Windows.Forms.Padding(0);
            this.flowLayoutPanelFilterRadios.Name = "flowLayoutPanelFilterRadios";
            this.flowLayoutPanelFilterRadios.Size = new System.Drawing.Size(1207, 66);
            this.flowLayoutPanelFilterRadios.TabIndex = 0;
            // 
            // labelFilterColumnRadios
            // 
            this.labelFilterColumnRadios.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelFilterColumnRadios.AutoSize = true;
            this.labelFilterColumnRadios.BackColor = System.Drawing.Color.Transparent;
            this.labelFilterColumnRadios.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F);
            this.labelFilterColumnRadios.Location = new System.Drawing.Point(609, 901);
            this.labelFilterColumnRadios.Name = "labelFilterColumnRadios";
            this.labelFilterColumnRadios.Size = new System.Drawing.Size(142, 22);
            this.labelFilterColumnRadios.TabIndex = 9;
            this.labelFilterColumnRadios.Text = "Filter By Column";
            // 
            // ItemListForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1833, 996);
            this.Controls.Add(this.labelFilterColumnRadios);
            this.Controls.Add(this.flowLayoutPanelFilterRadios);
            this.Controls.Add(this.buttonToggleCaseSensitivity);
            this.Controls.Add(this.buttonClearFilter);
            this.Controls.Add(this.labelFilter);
            this.Controls.Add(this.textBoxListFilter);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.labelElevationNote);
            this.MinimumSize = new System.Drawing.Size(697, 412);
            this.Name = "ItemListForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Startup Items";
            this.flowLayoutPanelFilterRadios.ResumeLayout(false);
            this.flowLayoutPanelFilterRadios.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private TrueTransparentLabel labelTitle;
    private TrueTransparentLabel labelElevationNote;
    private Thio_Background_App_Notifier.BufferedListView listView;
    private System.Windows.Forms.ColumnHeader colName;
    private System.Windows.Forms.ColumnHeader colStarts;
    private System.Windows.Forms.ColumnHeader colSource;
    private System.Windows.Forms.ColumnHeader colFirstDetected;
    private System.Windows.Forms.ColumnHeader colPath;
    private System.Windows.Forms.TextBox textBoxListFilter;
    private TrueTransparentLabel labelFilter;
    private System.Windows.Forms.Button buttonClearFilter;
    private System.Windows.Forms.RadioButton radioButtonFilterAll;
    private System.Windows.Forms.RadioButton radioButtonFilterName;
    private System.Windows.Forms.RadioButton radioButtonFilterPath;
    private ToggleButton buttonToggleCaseSensitivity;
    private System.Windows.Forms.ToolTip toolTip1;
    private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelFilterRadios;
    private TrueTransparentLabel labelFilterColumnRadios;
}
