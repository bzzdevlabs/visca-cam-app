using System;
using System.Collections.Generic;
using AppKit;
using Foundation;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.Mac.Views.Controls;

/// <summary>Preset list: numbered rows (1..9 are hotkeys); double-click or Return recalls, Delete deletes.</summary>
internal sealed class PresetTable : NSTableView
{
    private const ushort ReturnKey = 36;
    private const ushort EnterKey = 76;
    private const ushort BackspaceKey = 51;
    private const ushort ForwardDeleteKey = 117;

    private readonly PresetSource source = new();

    public PresetTable()
    {
        AddColumn(new NSTableColumn("Name") { ResizingMask = NSTableColumnResizing.Autoresizing });
        HeaderView = null;
        AllowsMultipleSelection = false;
        AllowsEmptySelection = true;
        UsesAlternatingRowBackgroundColors = true;
        RowHeight = 24;
        DataSource = source;
        DoubleClick += (_, _) =>
        {
            if (ClickedRow >= 0)
            {
                RecallRequested?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    public event EventHandler? RecallRequested;

    public event EventHandler? DeleteRequested;

    public Preset? SelectedPreset => SelectedRow >= 0 && SelectedRow < source.Presets.Count ? source.Presets[(int)SelectedRow] : null;

    public void ShowPresets(IReadOnlyList<Preset> presets, Preset? selected)
    {
        source.Presets = presets;
        ReloadData();
        var index = selected is null ? -1 : IndexOf(presets, selected);
        if (index >= 0)
        {
            SelectRow(index, byExtendingSelection: false);
            ScrollRowToVisible(index);
        }
        else
        {
            DeselectAll(this);
        }
    }

    public override void KeyDown(NSEvent theEvent)
    {
        switch (theEvent.KeyCode)
        {
            case ReturnKey:
            case EnterKey:
                RecallRequested?.Invoke(this, EventArgs.Empty);
                break;
            case BackspaceKey:
            case ForwardDeleteKey:
                DeleteRequested?.Invoke(this, EventArgs.Empty);
                break;
            default:
                base.KeyDown(theEvent);
                break;
        }
    }

    private static int IndexOf(IReadOnlyList<Preset> presets, Preset preset)
    {
        for (var i = 0; i < presets.Count; i++)
        {
            if (ReferenceEquals(presets[i], preset))
            {
                return i;
            }
        }

        return -1;
    }

    private sealed class PresetSource : NSTableViewDataSource
    {
        public IReadOnlyList<Preset> Presets { get; set; } = Array.Empty<Preset>();

        public override nint GetRowCount(NSTableView tableView) => Presets.Count;

        public override NSObject GetObjectValue(NSTableView tableView, NSTableColumn tableColumn, nint row)
        {
            var preset = Presets[(int)row];
            return new NSString(row < 9 ? $"{row + 1}   {preset.Name}" : $"     {preset.Name}");
        }
    }
}
