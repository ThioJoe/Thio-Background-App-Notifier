using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Thio_Background_App_Notifier
{
    /// <summary>
    /// A <see cref="ListView"/> with double buffering enabled so it doesn't flicker while
    /// redrawing (most noticeably when dragging column widths). Also comes with a built-in
    /// right-click "Copy" context menu (and Ctrl+C support) that copies the selected rows to the
    /// clipboard as tab-separated text, so every list in the app gets this for free.
    /// </summary>
    public class BufferedListView : ListView
    {
        private readonly ToolStripMenuItem _copyMenuItem;

        public BufferedListView()
        {
            // Managed double buffering.
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;

            //---- Context Menu Stuff -------
            _copyMenuItem = new ToolStripMenuItem("Copy") { ShortcutKeyDisplayString = "Ctrl+C" };
            _copyMenuItem.Click += (s, e) => UiHelpers.CopySelectedRows(this);
            ContextMenuStrip menu = new();
            menu.Items.Add(_copyMenuItem);
            menu.Opening += (s, e) => e.Cancel = SelectedItems.Count == 0; // Only offer the menu when at least one row is selected.
            ContextMenuStrip = menu;
            // ------------------------------
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Control && e.KeyCode == Keys.C)
            {
                UiHelpers.CopySelectedRows(this);
                e.Handled = true;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            // Also turn on the native list-view double-buffer extended style, which is what
            // actually stops the header/column-resize flicker on the underlying Win32 control.
            const int LVM_FIRST = 0x1000;
            const int LVM_SETEXTENDEDLISTVIEWSTYLE = LVM_FIRST + 54;
            const int LVS_EX_DOUBLEBUFFER = 0x00010000;

            SendMessage(this.Handle, LVM_SETEXTENDEDLISTVIEWSTYLE,
                (IntPtr)LVS_EX_DOUBLEBUFFER, (IntPtr)LVS_EX_DOUBLEBUFFER);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    }
}
