using System.Drawing;
using System.Windows.Forms;

namespace TenveoPtz.App.Views;

/// <summary>Small modal dialog asking for one line of text.</summary>
internal sealed class InputDialog : Form
{
    private readonly TextBox input;

    private InputDialog(string title, string prompt, string initialValue)
    {
        Text = title;
        Font = SystemFonts.MessageBoxFont;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(10);

        input = new TextBox { Text = initialValue, Width = 280, Dock = DockStyle.Fill };
        var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, AutoSize = true, Dock = DockStyle.Fill };
        buttons.Controls.AddRange(new Control[] { cancel, ok });

        var layout = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Dock = DockStyle.Fill };
        layout.Controls.Add(new Label { Text = prompt, AutoSize = true });
        layout.Controls.Add(input);
        layout.Controls.Add(buttons);
        Controls.Add(layout);
    }

    /// <summary>Shows the dialog; returns the entered text, or <c>null</c> when cancelled.</summary>
    public static string? Ask(IWin32Window owner, string title, string prompt, string initialValue)
    {
        using var dialog = new InputDialog(title, prompt, initialValue);
        dialog.Shown += (_, _) => dialog.input.SelectAll();
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.input.Text : null;
    }
}
