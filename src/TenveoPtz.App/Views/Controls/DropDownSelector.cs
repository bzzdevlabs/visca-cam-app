using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>
/// Fluent combo box: a button showing the selected item that opens a themed flyout menu.
/// Items are displayed with <see cref="object.ToString"/>.
/// </summary>
internal sealed class DropDownSelector : FluentControl
{
    private const float IconSize = 16F;
    private const float ChevronSize = 12F;

    private readonly List<object> items = new();
    private readonly string? glyph;
    private readonly string placeholder;
    private object? selectedItem;

    public DropDownSelector(int logicalWidth, string placeholder, string? glyph = null)
    {
        this.placeholder = placeholder;
        this.glyph = glyph;
        Size = new Size(logicalWidth, 32);
        Margin = new Padding(2);
        Cursor = Cursors.Hand;
    }

    public event EventHandler? SelectionChanged;

    public IReadOnlyList<object> Items => items;

    public object? SelectedItem
    {
        get => selectedItem;
        set
        {
            var item = value is not null && items.Contains(value) ? value : null;
            if (!Equals(item, selectedItem))
            {
                selectedItem = item;
                Invalidate();
                SelectionChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void SetItems(IEnumerable<object> newItems)
    {
        items.Clear();
        items.AddRange(newItems);
        if (selectedItem is not null && !items.Contains(selectedItem))
        {
            SelectedItem = items.FirstOrDefault(i => i.ToString() == selectedItem.ToString());
        }

        Invalidate();
    }

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        ShowMenu();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode is Keys.Space or Keys.Enter or Keys.F4)
        {
            ShowMenu();
            e.Handled = true;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var graphics = e.Graphics;
        graphics.UseHighQuality();
        var bounds = new RectangleF(0, 0, Width, Height);
        var state = new ButtonChrome.VisualState(Enabled, IsHovered, IsPressed, false);
        var foreground = ButtonChrome.Paint(graphics, bounds, DeviceDpi / 96F, ButtonAppearance.Standard, state);

        var x = Px(11);
        if (glyph is not null)
        {
            graphics.DrawGlyph(glyph, FluentFonts.Icon(IconSize), Enabled ? Colors.TextSecondary : Colors.TextDisabled, new PointF(x + (Px(IconSize) / 2F), Height / 2F));
            x += Px(IconSize + 8);
        }

        var chevronLeft = Width - Px(11 + ChevronSize);
        var text = selectedItem?.ToString() ?? placeholder;
        var textColor = selectedItem is null || !Enabled ? Colors.TextDisabled : foreground;
        graphics.DrawText(text, Font, textColor, new Rectangle((int)x, 0, (int)(chevronLeft - x - Px(4)), Height), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        graphics.DrawGlyph(Glyphs.ChevronDownSmall, FluentFonts.Icon(ChevronSize), Enabled ? Colors.TextSecondary : Colors.TextDisabled, new PointF(chevronLeft + (Px(ChevronSize) / 2F), Height / 2F));

        if (Focused && ShowFocusCues)
        {
            ButtonChrome.PaintFocus(graphics, bounds, DeviceDpi / 96F);
        }
    }

    private void ShowMenu()
    {
        if (!Enabled || items.Count == 0)
        {
            return;
        }

        var menu = new ContextMenuStrip
        {
            Renderer = new FluentMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = false,
            Font = Font,
            BackColor = Colors.Surface,
            ForeColor = Colors.Text,
            Padding = new Padding(0, 4, 0, 4),
            MinimumSize = new Size(Width, 0),
        };

        foreach (var item in items)
        {
            var menuItem = new ToolStripMenuItem(item.ToString())
            {
                Checked = Equals(item, selectedItem),
                Padding = new Padding(8, 6, 8, 6),
                Tag = item,
            };
            menuItem.Click += (_, _) => SelectedItem = menuItem.Tag;
            menu.Items.Add(menuItem);
        }

        menu.HandleCreated += (_, _) => WindowChrome.RoundCorners(menu.Handle);
        menu.Closed += (_, _) => BeginInvoke(new Action(menu.Dispose));
        menu.Show(this, new Point(0, Height + (int)Px(2)));
    }
}
