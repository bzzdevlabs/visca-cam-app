using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Controls;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Localization;

namespace TenveoPtz.App.Views;

/// <summary>Windows 11 style content dialog: title, message, optional text input and a button footer.</summary>
internal sealed class FluentDialog : Form
{
    private readonly FluentTextBox? input;

    private FluentDialog(string title, string message, string primaryText, string cancelText, string? initialValue)
    {
        Text = title;
        Font = FluentFonts.Body;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Theme.Current.Surface;

        var content = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Padding = new Padding(24, 20, 24, 24), Margin = new Padding(0) };
        content.Controls.Add(new Label { Text = title, Font = FluentFonts.Subtitle, ForeColor = Theme.Current.Text, AutoSize = true, Margin = new Padding(0, 0, 0, 12) });
        content.Controls.Add(new Label { Text = message, ForeColor = Theme.Current.Text, AutoSize = true, MaximumSize = new Size(360, 0), Margin = new Padding(0, 0, 0, 12) });
        if (initialValue is not null)
        {
            input = new FluentTextBox { Value = initialValue, Width = 360, Margin = new Padding(0) };
            content.Controls.Add(input);
        }

        var primary = new FluentButton(primaryText, appearance: ButtonAppearance.Accent) { AutoSize = false, Size = new Size(150, 32), Margin = new Padding(0, 0, 8, 0) };
        var cancel = new FluentButton(cancelText) { AutoSize = false, Size = new Size(150, 32), Margin = new Padding(0) };
        primary.Click += (_, _) => CloseWith(DialogResult.OK);
        cancel.Click += (_, _) => CloseWith(DialogResult.Cancel);

        var footer = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right, // Spans the dialog width, buttons on the right.
            AutoSize = true,
            Margin = new Padding(0),
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(24),
            BackColor = Theme.Current.Background,
        };
        footer.Controls.Add(cancel);
        footer.Controls.Add(primary);

        // A table sizes the dialog to its content: with docked children, an auto-sized form gets
        // their height but not their width and shrinks to the minimum window width.
        var root = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0) };
        root.Controls.Add(content, 0, 0);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);
        KeyPreview = true;
    }

    /// <summary>Asks for one line of text; returns <c>null</c> when cancelled.</summary>
    public static string? Prompt(IWin32Window owner, string title, string message, string initialValue)
    {
        using var dialog = new FluentDialog(title, message, Strings.Save, Strings.Cancel, initialValue);
        return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.input!.Value : null;
    }

    public static bool Confirm(IWin32Window owner, string title, string message, string primaryText, string? cancelText = null)
    {
        using var dialog = new FluentDialog(title, message, primaryText, cancelText ?? Strings.Cancel, null);
        return dialog.ShowDialog(owner) == DialogResult.OK;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowChrome.Apply(this, Theme.Current);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        input?.SelectAll();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Enter && ActiveControl is FluentButton)
        {
            return; // A focused button handles Enter itself: Enter on Cancel must cancel.
        }

        if (e.KeyCode == Keys.Enter)
        {
            CloseWith(DialogResult.OK);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            CloseWith(DialogResult.Cancel);
            e.Handled = true;
        }
    }

    private void CloseWith(DialogResult result)
    {
        DialogResult = result;
        Close();
    }
}
