using System.Drawing;
using System.Windows.Forms;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Creates consistently styled controls so the views stay declarative.</summary>
internal static class UiFactory
{
    public static GroupBox Group(string title, Control content)
    {
        var group = new GroupBox
        {
            Text = title,
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(6),
        };
        content.Dock = DockStyle.Fill;
        group.Controls.Add(content);
        return group;
    }

    public static Button Button(string text, string toolTip, ToolTip toolTips)
    {
        var button = new Button { Text = text, AutoSize = true, Margin = new Padding(2) };
        toolTips.SetToolTip(button, toolTip);
        return button;
    }

    public static HoldButton HoldButton(string text, string toolTip, ToolTip toolTips)
    {
        var button = new HoldButton
        {
            Text = text,
            Dock = DockStyle.Fill,
            Margin = new Padding(2),
            MinimumSize = new Size(40, 32),
            Font = new Font("Segoe UI Symbol", 11F),
        };
        toolTips.SetToolTip(button, toolTip);
        return button;
    }

    public static Label Label(string text) =>
        new() { Text = text, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(6, 6, 2, 2) };

    public static TableLayoutPanel Grid(int columns, int rows)
    {
        var grid = new TableLayoutPanel
        {
            ColumnCount = columns,
            RowCount = rows,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        for (var i = 0; i < columns; i++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / columns));
        }

        for (var i = 0; i < rows; i++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        return grid;
    }
}
