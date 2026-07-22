using System.ComponentModel;
using System.Windows.Forms;

namespace Thio_Background_App_Notifier;

public partial class TrueTransparentLabel : Label
{
    public TrueTransparentLabel()
    {
        InitializeComponent();
        this.SetStyle(ControlStyles.Opaque, true);
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
        this.BackColor = System.Drawing.Color.Transparent;
    }

    public TrueTransparentLabel(IContainer container)
    {
        container.Add(this);

        InitializeComponent();
        this.SetStyle(ControlStyles.Opaque, true);
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer, false);
        this.BackColor = System.Drawing.Color.Transparent;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= 0x20;
            return cp;
        }
    }
}