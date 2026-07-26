using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            SetPushedStyle();
        }
    }

    public ToggleButton()
    {
        InitializeComponent();

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 1;

        SetPushedStyle();
    }

    public ToggleButton(IContainer container)
    {
        container.Add(this);

        InitializeComponent();

        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 1;

        SetPushedStyle();
    }

    private void SetPushedStyle()
    {
        if (Pushed)
        {
            FlatAppearance.BorderSize = 2;
            FlatAppearance.BorderColor = SystemColors.HotTrack;
            BackColor = SystemColors.ControlLight;
        }
        else
        {
            FlatAppearance.BorderSize = 1;
            FlatAppearance.BorderColor = SystemColors.ControlDark;
            BackColor = SystemColors.Control;
        }
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
}
