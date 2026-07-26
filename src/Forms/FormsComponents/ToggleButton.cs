using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

#nullable enable

namespace Thio_Background_App_Notifier;

public partial class ToggleButton : Button
{
    private bool _pushed;

    /// <summary>
    /// Whether the button will appear "pushed in" or not.
    /// </summary>
    [DefaultValue(false)]
    public bool Pushed
    {
        get => _pushed;
        set
        {
            if (_pushed == value)
                return;

            _pushed = value;
            Invalidate();
        }
    }

    public ToggleButton()
    {
        InitializeComponent();

        FlatStyle = FlatStyle.Standard;
    }

    public ToggleButton(IContainer container)
    {
        container.Add(this);

        InitializeComponent();

        FlatStyle = FlatStyle.Standard;
    }

    // Toggles the pushed state whenever the button is clicked.
    public void Toggle()
    {
        Pushed = !Pushed;
    }
    private void Toggle(object? sender, EventArgs e)
    {
        Toggle();
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        // Only when pushed do we need to override the default rendering, to keep the
        // button looking permanently "pressed in" using the native (standard) theme.
        if (!Pushed)
        {
            base.OnPaint(pevent);
            return;
        }

        // Draw just the pressed-in chrome (no text), then draw the text ourselves at a fixed
        // position. ButtonRenderer.DrawButton's own text drawing shifts the text slightly for
        // the pressed state, which made the label visibly jump when toggling.
        //PushButtonState state = Enabled ? PushButtonState.Pressed : PushButtonState.Disabled;

        PushButtonState state;
        Font updatedFont;

        if (this.Enabled == true)
        {
            state = PushButtonState.Pressed;
            updatedFont = new(Font, FontStyle.Bold);
        }
        else
        {
            state = PushButtonState.Disabled;
            updatedFont = new(Font, FontStyle.Regular);
        }

        ButtonRenderer.DrawButton(pevent.Graphics, ClientRectangle, state);
        TextRenderer.DrawText(
            dc: pevent.Graphics,
            text: Text,
            font: updatedFont,
            bounds: ClientRectangle,
            foreColor: Enabled ? ForeColor : SystemColors.GrayText,
            flags: TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
