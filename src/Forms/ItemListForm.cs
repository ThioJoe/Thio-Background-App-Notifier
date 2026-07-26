using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Windows.Forms;

#nullable enable

namespace Thio_Background_App_Notifier;

/// <summary>
/// A simple, reusable "see all" window that lists a set of startup items (services or tasks)
/// with their first-detection dates.
/// </summary>
public partial class ItemListForm : BaseForm
{
    // colFirstDetected is the 4th column (index 3); sort it chronologically.
    private readonly ListViewColumnSorter _sorter = new ListViewColumnSorter { DateColumnIndex = 3 };

    // Column header text without any sort arrow, so indicators can be re-applied cleanly.
    private string[] _baseHeaderText = new string[0];

    // Keys (in first-seen order) of any type-specific detail columns (e.g. TaskSchedulerPath) to add for this item set.
    private readonly List<string> _extraColumnKeys = new List<string>();

    // Italic variant of the list font, for admin-only items this (non-elevated) run couldn't verify.
    private Font? _unverifiedItemFont;

    public ItemListForm(string title, IEnumerable<IStartupItem> items)
    {
        InitializeComponent();

        this.Text = title;
        labelTitle.Text = title;

        List<IStartupItem> itemList = items.ToList();
        AddTypeSpecificColumns(itemList); // Extract out the "special" type specific data to add as extra columns if applicable

        _baseHeaderText = new string[listView.Columns.Count];
        for (int i = 0; i < listView.Columns.Count; i++)
        {
            _baseHeaderText[i] = listView.Columns[i].Text;
        }

        listView.ListViewItemSorter = _sorter;
        listView.ColumnClick += listView_ColumnClick;

        Populate(itemList);

        AutoSizeColumns();

        // The list starts sorted by the first column ascending; show that.
        UpdateSortIndicators();
    }

    private int colPadding = 25;

    private void AutoSizeColumns()
    {
        
        // The columns are all defined in the designer sheet so we can reference their indexes directly.
        UiHelpers.AutoResizeColumnToLargerOfHeaderOrContent(listView, colName, colPadding, 20);
        UiHelpers.AutoResizeColumnToLargerOfHeaderOrContent(listView, colSource, colPadding);
        UiHelpers.AutoResizeColumnToLargerOfHeaderOrContent(listView, colFirstDetected, colPadding);
        UiHelpers.AutoResizeColumnToLargerOfHeaderOrContent(listView, colStarts, colPadding, 20);
    }

    // Adds one ListView column per distinct key found across all items' TypeSpecificDetails,
    // inserted between the fixed columns and the Path column.
    private void AddTypeSpecificColumns(IEnumerable<IStartupItem> items)
    {
        var seenKeys = new HashSet<string>();
        foreach (IStartupItem item in items)
        {
            foreach (Dictionary<string, string> detail in item.TypeSpecificDetails)
            {
                foreach (string key in detail.Keys)
                {
                    if (seenKeys.Add(key))
                        _extraColumnKeys.Add(key);
                }
            }
        }

        int insertIndex = colPath.Index;
        foreach (string key in _extraColumnKeys)
        {
            var header = new ColumnHeader { Text = key, Width = 150 };
            listView.Columns.Insert(insertIndex, header);
            insertIndex++;
            UiHelpers.AutoResizeColumnToLargerOfHeaderOrContent(listView, header, colPadding, 15);
        }
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
        UpdateSortIndicators();
    }

    // Appends an up/down arrow to the header of the currently-sorted column.
    private void UpdateSortIndicators()
    {
        for (int i = 0; i < listView.Columns.Count; i++)
        {
            string text = i < _baseHeaderText.Length ? _baseHeaderText[i] : listView.Columns[i].Text;
            if (i == _sorter.SortColumn)
                text += _sorter.Order == SortOrder.Ascending ? "  ▲" : "  ▼";
            listView.Columns[i].Text = text;
        }
    }

    private void Populate(IEnumerable<IStartupItem> items)
    {
        int unverifiedCount = 0;

        listView.BeginUpdate();
        try
        {
            listView.Items.Clear();

            foreach (IStartupItem item in items.OrderBy(i => UiHelpers.GetDisplayName(i), StringComparer.OrdinalIgnoreCase))
            {
                var row = new ListViewItem(UiHelpers.GetDisplayName(item));
                row.SubItems.Add(UiHelpers.GetDetail(item));
                row.SubItems.Add(UiHelpers.GetSourceHint(item));
                row.SubItems.Add(item.FirstDetectionTime.ToString("g"));

                foreach (string key in _extraColumnKeys)
                {
                    string value = string.Empty;
                    foreach (Dictionary<string, string> detailColumnData in item.TypeSpecificDetails)
                    {
                        if (detailColumnData.TryGetValue(key, out string? found))
                        {
                            value = found;
                            break;
                        }
                    }
                    row.SubItems.Add(value);
                }

                row.SubItems.Add(item.Path);
                row.Tag = item;

                if (item.IsFirstDetection)
                {
                    row.UseItemStyleForSubItems = true;
                    row.BackColor = Color.FromArgb(255, 249, 196); // light yellow highlight
                }
                else if (item.OnlyVisibleWhenElevated && !WindowsUtils.IsRunningElevated)
                {
                    // Only ever seen while elevated, and this run isn't, so its current state
                    // can't be verified — dim it rather than showing it as a live row.
                    unverifiedCount++;
                    row.UseItemStyleForSubItems = true;
                    row.ForeColor = SystemColors.GrayText;
                    row.Font = _unverifiedItemFont ??= new Font(listView.Font, FontStyle.Italic);
                }

                listView.Items.Add(row);
            }
        }
        finally
        {
            listView.EndUpdate();
        }

        labelElevationNote.Visible = unverifiedCount > 0;
    }

    private void buttonClose_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    }
