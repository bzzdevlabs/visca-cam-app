using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Re-colours the plain secondary labels of a container after a theme change.</summary>
internal static class ThemedLabels
{
    public static void Refresh(Control root)
    {
        foreach (Control child in root.Controls)
        {
            if (child is Label label)
            {
                label.ForeColor = Theme.Current.TextSecondary;
            }

            Refresh(child);
        }
    }
}
