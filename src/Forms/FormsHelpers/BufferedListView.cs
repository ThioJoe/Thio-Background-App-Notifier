using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace New_Startup_App_Notifier
{
    /// <summary>
    /// A <see cref="ListView"/> with double buffering enabled so it doesn't flicker while
    /// redrawing (most noticeably when dragging column widths). Behaves like a normal ListView
    /// in every other respect.
    /// </summary>
    public class BufferedListView : ListView
    {
        public BufferedListView()
        {
            // Managed double buffering.
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;
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
