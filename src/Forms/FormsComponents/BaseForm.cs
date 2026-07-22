using System.Drawing;
using System.Windows.Forms;

namespace Thio_Background_App_Notifier;

// Base form with icon preset with the main exe's icon
public class BaseForm : Form
{
    public BaseForm()
    {
        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
    }
}
