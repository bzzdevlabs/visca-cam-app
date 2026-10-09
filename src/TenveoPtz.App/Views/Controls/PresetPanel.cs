using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Preset list with its actions. Double-click or Enter recalls a preset; 1..9 are hotkeys.</summary>
internal sealed class PresetPanel : UserControl
{
    private const int HotkeyCount = 9;

    private readonly ListBox list;

    public PresetPanel(ToolTip toolTips)
    {
        list = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, Height = 150, FormattingEnabled = true };
        list.Format += OnFormatItem;
        list.DoubleClick += (_, _) => RaiseForSelection(RecallRequested);
        list.KeyDown += OnListKeyDown;

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, WrapContents = true };
        buttons.Controls.Add(Action("Go", "Move the camera to the selected preset (double-click)", toolTips, () => RaiseForSelection(RecallRequested)));
        buttons.Controls.Add(Action("Save new", "Save the current camera position as a new preset", toolTips, () => AddRequested?.Invoke(this, EventArgs.Empty)));
        buttons.Controls.Add(Action("Update", "Replace the selected preset with the current position", toolTips, () => RaiseForSelection(OverwriteRequested)));
        buttons.Controls.Add(Action("Rename", "Rename the selected preset", toolTips, () => RaiseForSelection(RenameRequested)));
        buttons.Controls.Add(Action("Delete", "Delete the selected preset", toolTips, () => RaiseForSelection(DeleteRequested)));
        buttons.Controls.Add(Action("▲", "Move up (changes its hotkey)", toolTips, () => RaiseMove(-1)));
        buttons.Controls.Add(Action("▼", "Move down (changes its hotkey)", toolTips, () => RaiseMove(1)));

        Controls.Add(list);
        Controls.Add(buttons);
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
    }

    private static Button Action(string text, string toolTip, ToolTip toolTips, Action onClick)
    {
        var button = UiFactory.Button(text, toolTip, toolTips);
        button.Click += (_, _) => onClick();
        return button;
    }

    private void OnFormatItem(object? sender, ListControlConvertEventArgs e)
    {
        var index = list.Items.IndexOf(e.ListItem);
        var prefix = index >= 0 && index < HotkeyCount ? $"{index + 1}.  " : "     ";
        e.Value = prefix + e.ListItem;
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
