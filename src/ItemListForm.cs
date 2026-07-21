using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace New_Startup_App_Notifier;

/// <summary>
/// A simple, reusable "see all" window that lists a set of startup items (services or tasks)
/// with their first-detection dates.
/// </summary>
public partial class ItemListForm : Form
{
    // colFirstDetected is the 4th column (index 3); sort it chronologically.
    private readonly ListViewColumnSorter _sorter = new ListViewColumnSorter { DateColumnIndex = 3 };

    public ItemListForm(string title, IEnumerable<IStartupItem> items)
    {
        InitializeComponent();

        this.Text = title;
        labelTitle.Text = title;

        UiHelpers.AttachCopyContextMenu(listView);
        listView.ListViewItemSorter = _sorter;
        listView.ColumnClick += listView_ColumnClick;

        Populate(items);
    }

    private void listView_ColumnClick(object sender, ColumnClickEventArgs e)
    {
        if (e.Column == _sorter.SortColumn)
        {
            // Same column clicked again: flip the direction.
            _sorter.Order = _sorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }
        else
        {
            _sorter.SortColumn = e.Column;
            _sorter.Order = SortOrder.Ascending;
        }

        listView.Sort();
    }

    private void Populate(IEnumerable<IStartupItem> items)
    {
        listView.BeginUpdate();
        try
        {
            listView.Items.Clear();

            foreach (IStartupItem item in items.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase))
            {
                var row = new ListViewItem(item.Name);
                row.SubItems.Add(UiHelpers.GetDetail(item));
                row.SubItems.Add(UiHelpers.GetSourceHint(item));
                row.SubItems.Add(item.FirstDetectionTime.ToString("g"));
                row.SubItems.Add(item.Path);
                row.Tag = item;

                if (item.IsFirstDetection)
                {
                    row.UseItemStyleForSubItems = true;
                    row.BackColor = Color.FromArgb(255, 249, 196); // light yellow highlight
                }

                listView.Items.Add(row);
            }
        }
        finally
        {
            listView.EndUpdate();
        }
    }

    private void listView_DoubleClick(object sender, EventArgs e)
    {
        if (listView.SelectedItems.Count == 0)
            return;
        if (listView.SelectedItems[0].Tag is IStartupItem item)
            UiHelpers.ShowDetails(this, item);
    }

    private void buttonClose_Click(object sender, EventArgs e)
    {
        this.Close();
    }
}
