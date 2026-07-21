using System;
using System.Collections;
using System.Windows.Forms;

namespace Thio_Background_App_Notifier
{
    /// <summary>
    /// Sorts a <see cref="ListView"/> by a clicked column. Most columns sort as text; the column
    /// designated by <see cref="DateColumnIndex"/> sorts by the underlying detection time (read from
    /// each row's <see cref="ListViewItem.Tag"/>) so dates order chronologically rather than
    /// alphabetically.
    /// </summary>
    public class ListViewColumnSorter : IComparer
    {
        public int SortColumn { get; set; }
        public SortOrder Order { get; set; } = SortOrder.Ascending;

        /// <summary>Column index that holds a first-detected date, or -1 if none.</summary>
        public int DateColumnIndex { get; set; } = -1;

        public int Compare(object x, object y)
        {
            var a = (ListViewItem)x;
            var b = (ListViewItem)y;

            int result;
            if (SortColumn == DateColumnIndex && a.Tag is IStartupItem ia && b.Tag is IStartupItem ib)
            {
                result = DateTime.Compare(ia.FirstDetectionTime, ib.FirstDetectionTime);
            }
            else
            {
                string textA = SortColumn < a.SubItems.Count ? a.SubItems[SortColumn].Text : string.Empty;
                string textB = SortColumn < b.SubItems.Count ? b.SubItems[SortColumn].Text : string.Empty;
                result = string.Compare(textA, textB, StringComparison.CurrentCultureIgnoreCase);
            }

            return Order == SortOrder.Descending ? -result : result;
        }
    }
}
