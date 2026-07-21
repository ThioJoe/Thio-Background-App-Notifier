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
    public ItemListForm(string title, IEnumerable<IStartupItem> items)
    {
        InitializeComponent();

        this.Text = title;
        labelTitle.Text = title;

        Populate(items);
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
