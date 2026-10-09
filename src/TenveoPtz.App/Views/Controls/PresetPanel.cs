using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.App.Views.Controls;

/// <summary>"Presets" card: toolbar and list. Double-click or Enter recalls; 1..9 are hotkeys.</summary>
internal sealed class PresetPanel : UserControl
{
    private readonly PresetListBox list = new()
    {
        Dock = DockStyle.Fill,
        EmptyText = "No presets yet. Frame a shot, then select \"New\".",
    };

    public PresetPanel(ToolTip toolTips)
    {
        list.DoubleClick += (_, _) => RaiseForSelection(RecallRequested);
        list.KeyDown += OnListKeyDown;

        var add = new FluentButton("New", Glyphs.Add, ButtonAppearance.Accent);
        toolTips.SetToolTip(add, "Save the current camera position as a new preset");
        add.Click += (_, _) => AddRequested?.Invoke(this, EventArgs.Empty);

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false, Margin = new Padding(0), Padding = new Padding(0, 0, 0, 8) };
        toolbar.Controls.Add(add);
        toolbar.Controls.Add(Tool(Glyphs.Play, "Go to the selected preset (double-click, Enter or 1-9)", toolTips, () => RaiseForSelection(RecallRequested)));
        toolbar.Controls.Add(Tool(Glyphs.Save, "Replace the selected preset with the current position", toolTips, () => RaiseForSelection(OverwriteRequested)));
        toolbar.Controls.Add(Tool(Glyphs.Rename, "Rename the selected preset", toolTips, () => RaiseForSelection(RenameRequested)));
        toolbar.Controls.Add(Tool(Glyphs.Delete, "Delete the selected preset", toolTips, () => RaiseForSelection(DeleteRequested)));
        toolbar.Controls.Add(Tool(Glyphs.MoveUp, "Move up (changes its number key)", toolTips, () => RaiseMove(-1)));
        toolbar.Controls.Add(Tool(Glyphs.MoveDown, "Move down (changes its number key)", toolTips, () => RaiseMove(1)));

        var card = new Card("Presets") { Dock = DockStyle.Fill };
        card.Controls.Add(list);
        card.Controls.Add(toolbar);
        Controls.Add(card);
    }

    public event EventHandler? AddRequested;

    public event EventHandler<PresetEventArgs>? RecallRequested;

    public event EventHandler<PresetEventArgs>? OverwriteRequested;

    public event EventHandler<PresetEventArgs>? RenameRequested;

    public event EventHandler<PresetEventArgs>? DeleteRequested;

    public event EventHandler<PresetMoveEventArgs>? MoveRequested;

    public void ShowPresets(IReadOnlyList<Preset> presets, Preset? selected)
    {
        list.BeginUpdate();
        list.Items.Clear();
        foreach (var preset in presets)
        {
            list.Items.Add(preset);
        }

        list.SelectedItem = selected;
        list.EndUpdate();
        list.Invalidate();
    }

    private static FluentButton Tool(string glyph, string toolTip, ToolTip toolTips, Action onClick)
    {
        var button = new FluentButton(string.Empty, glyph, ButtonAppearance.Subtle);
        button.Click += (_, _) => onClick();
        toolTips.SetToolTip(button, toolTip);
        return button;
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            RaiseForSelection(RecallRequested);
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            RaiseForSelection(DeleteRequested);
            e.Handled = true;
        }
    }

    private void RaiseForSelection(EventHandler<PresetEventArgs>? handler)
    {
        if (list.SelectedItem is Preset preset)
        {
            handler?.Invoke(this, new PresetEventArgs(preset));
        }
    }

    private void RaiseMove(int offset)
    {
        if (list.SelectedItem is Preset preset)
        {
            MoveRequested?.Invoke(this, new PresetMoveEventArgs(preset, offset));
        }
    }
}
