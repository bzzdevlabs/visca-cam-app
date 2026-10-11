using System.Collections.Generic;
using System.Linq;
using AppKit;
using CoreGraphics;
using TenveoPtz.Core.Video;
using TenveoPtz.Mac.Views.Controls;

namespace TenveoPtz.Mac.Views;

/// <summary>
/// Window layout: connection bar on top, video on the left, control cards on the right and a
/// status line at the bottom.
/// </summary>
internal sealed partial class MainWindow
{
    private const float SideColumnWidth = 300;
    private const float Gap = 12;

    private static readonly int[] BaudRates = { 2400, 4800, 9600, 19200, 38400, 57600, 115200 };

    private readonly NSPopUpButton devicePopup = Popup(240, "Video device used for the live preview");
    private readonly NSPopUpButton modePopup = Popup(190, "USB: control through the camera's USB cable. Serial: VISCA through an RS-232/RS-485 cable.");
    private readonly NSPopUpButton portPopup = Popup(220, "Serial port of the VISCA cable or adapter");
    private readonly NSPopUpButton baudPopup = Popup(120, "VISCA baud rate (must match the camera, usually 9600)");
    private readonly NSPopUpButton addressPopup = Popup(120, "VISCA camera address (usually 1)");
    private readonly NSButton refreshButton = IconButton("arrow.clockwise", "Refresh the device lists");
    private readonly NSButton pinButton = IconButton("pin", "Keep this window on top of other windows");
    private readonly NSButton connectButton = new() { Title = "Connect", BezelStyle = NSBezelStyle.Rounded, ImagePosition = NSCellImagePosition.ImageLeading, ToolTip = "Open the video and camera control" };
    private readonly NSSlider speedSlider = new() { MinValue = 1, MaxValue = 10, DoubleValue = 5, ToolTip = "Pan, tilt and zoom speed" };
    private readonly DirectionPad directionPad = new();
    private readonly HoldButton zoomOut = Hold("minus.magnifyingglass", null, "Zoom out (hold, or - / Page Down)");
    private readonly HoldButton zoomIn = Hold("plus.magnifyingglass", null, "Zoom in (hold, or + / Page Up)");
    private readonly HoldButton focusNear = Hold(null, "Near", "Focus nearer (hold)");
    private readonly HoldButton focusFar = Hold(null, "Far", "Focus farther (hold)");
    private readonly NSButton autoFocus = NSButton.CreateCheckbox("Autofocus", () => { });
    private readonly NSButton addPreset = new() { Title = "New", BezelStyle = NSBezelStyle.Rounded, ImagePosition = NSCellImagePosition.ImageLeading, ToolTip = "Save the current camera position as a new preset" };
    private readonly NSButton recallPreset = IconButton("play", "Go to the selected preset (double-click, Return or 1-9)");
    private readonly NSButton overwritePreset = IconButton("square.and.arrow.down", "Replace the selected preset with the current position");
    private readonly NSButton renamePreset = IconButton("pencil", "Rename the selected preset");
    private readonly NSButton deletePreset = IconButton("trash", "Delete the selected preset");
    private readonly NSButton movePresetUp = IconButton("arrow.up", "Move up (changes its number key)");
    private readonly NSButton movePresetDown = IconButton("arrow.down", "Move down (changes its number key)");
    private readonly PresetTable presetTable = new();
    private readonly NSTextField emptyPresets = NSTextField.CreateLabel("No presets yet. Frame a shot, then press \"New\".");
    private readonly VideoView videoView = new();
    private readonly NSTextField statusLabel = NSTextField.CreateLabel(string.Empty);
    private NSStackView serialRow = null!;
    private List<VideoDevice> videoDevices = new();
    private List<string> serialPorts = new();
    private bool connected;

    private static NSImage? Symbol(string name, string description) => NSImage.GetSystemSymbol(name, description);

    private static NSPopUpButton Popup(float width, string toolTip)
    {
        var popup = new NSPopUpButton(CGRect.Empty, false) { ToolTip = toolTip, TranslatesAutoresizingMaskIntoConstraints = false };
        popup.WidthAnchor.ConstraintEqualTo(width).Active = true;
        return popup;
    }

    private static NSButton IconButton(string symbol, string toolTip) =>
        new() { Image = Symbol(symbol, toolTip), BezelStyle = NSBezelStyle.Rounded, ToolTip = toolTip, Title = string.Empty };

    private static HoldButton Hold(string? symbol, string? title, string toolTip)
    {
        var button = new HoldButton { ToolTip = toolTip, Title = title ?? string.Empty, TranslatesAutoresizingMaskIntoConstraints = false };
        if (symbol is not null)
        {
            button.Image = Symbol(symbol, toolTip);
        }

        button.WidthAnchor.ConstraintEqualTo(56).Active = true;
        return button;
    }

    private static NSStackView Row(params NSView[] views)
    {
        var row = NSStackView.FromViews(views);
        row.Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
        row.Spacing = 8;
        return row;
    }

    private static NSStackView Column(float spacing, params NSView[] views)
    {
        var column = NSStackView.FromViews(views);
        column.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
        column.Alignment = NSLayoutAttribute.Leading;
        column.Spacing = spacing;
        return column;
    }

    private static NSTextField SecondaryLabel(string text)
    {
        var label = NSTextField.CreateLabel(text);
        label.TextColor = NSColor.SecondaryLabel;
        return label;
    }

    /// <summary>Titled box whose content fills it with a margin.</summary>
    private static NSBox Card(string title, NSView content)
    {
        var box = new NSBox { Title = title, TitlePosition = NSTitlePosition.AtTop, TranslatesAutoresizingMaskIntoConstraints = false };
        content.TranslatesAutoresizingMaskIntoConstraints = false;
        var container = (NSView)box.ContentView!;
        container.AddSubview(content);
        Pin(content, container, 8);
        return box;
    }

    private static void Pin(NSView view, NSView container, float margin)
    {
        view.LeadingAnchor.ConstraintEqualTo(container.LeadingAnchor, margin).Active = true;
        view.TrailingAnchor.ConstraintEqualTo(container.TrailingAnchor, -margin).Active = true;
        view.TopAnchor.ConstraintEqualTo(container.TopAnchor, margin).Active = true;
        view.BottomAnchor.ConstraintEqualTo(container.BottomAnchor, -margin).Active = true;
    }

    private static void FillWidth(NSStackView stack)
    {
        foreach (var view in stack.ArrangedSubviews)
        {
            view.WidthAnchor.ConstraintEqualTo(stack.WidthAnchor).Active = true;
        }
    }

    private void InitializeLayout()
    {
        modePopup.AddItems(new[] { "USB control (UVC)", "Serial control (VISCA)" });
        baudPopup.AddItems(BaudRates.Select(rate => $"{rate} baud").ToArray());
        addressPopup.AddItems(Enumerable.Range(1, 7).Select(address => $"Address {address}").ToArray());
        devicePopup.AddItem("No camera found");
        portPopup.AddItem("No serial port");
        connectButton.Image = Symbol("play.fill", "Connect");
        connectButton.BezelColor = NSColor.ControlAccent;
        connectButton.KeyEquivalent = string.Empty;
        pinButton.SetButtonType(NSButtonType.PushOnPushOff);
        addPreset.Image = Symbol("plus", "New");
        addPreset.BezelColor = NSColor.ControlAccent;
        autoFocus.State = NSCellStateValue.On;
        autoFocus.ToolTip = "Let the camera focus by itself";
        speedSlider.TickMarksCount = 10;
        speedSlider.AllowsTickMarkValuesOnly = true;
        statusLabel.LineBreakMode = NSLineBreakMode.TruncatingTail;
        statusLabel.SetContentCompressionResistancePriority(250, NSLayoutConstraintOrientation.Horizontal);
        emptyPresets.TextColor = NSColor.SecondaryLabel;
        emptyPresets.Alignment = NSTextAlignment.Center;
        emptyPresets.LineBreakMode = NSLineBreakMode.ByWordWrapping;
        emptyPresets.PreferredMaxLayoutWidth = SideColumnWidth - 60;

        // Connection bar: selection on the left, actions on the right, serial settings below.
        var spacer = new NSView();
        spacer.SetContentHuggingPriorityForOrientation(1, NSLayoutConstraintOrientation.Horizontal);
        var selection = Row(devicePopup, modePopup, spacer, refreshButton, pinButton, connectButton);
        serialRow = Row(portPopup, baudPopup, addressPopup);
        var header = Column(8, selection, serialRow);
        selection.WidthAnchor.ConstraintEqualTo(header.WidthAnchor).Active = true;

        // Side column cards.
        var speedRow = Row(SecondaryLabel("Speed"), speedSlider);
        var move = Column(10, directionPad, speedRow);
        move.Alignment = NSLayoutAttribute.CenterX;
        speedRow.WidthAnchor.ConstraintEqualTo(move.WidthAnchor).Active = true;

        var zoomLabel = SecondaryLabel("Zoom");
        var zoomRow = Row(zoomLabel, new NSView(), zoomOut, zoomIn);
        var focusRow = Row(autoFocus, new NSView(), focusNear, focusFar);
        var lens = Column(8, zoomRow, focusRow);
        FillWidth(lens);
        UpdateFocusButtons();

        var scroll = new NSScrollView { DocumentView = presetTable, HasVerticalScroller = true, BorderType = NSBorderType.BezelBorder, TranslatesAutoresizingMaskIntoConstraints = false };
        emptyPresets.TranslatesAutoresizingMaskIntoConstraints = false;
        scroll.AddSubview(emptyPresets);
        emptyPresets.CenterXAnchor.ConstraintEqualTo(scroll.CenterXAnchor).Active = true;
        emptyPresets.CenterYAnchor.ConstraintEqualTo(scroll.CenterYAnchor).Active = true;
        var toolbar = Row(addPreset, recallPreset, overwritePreset, renamePreset, deletePreset, movePresetUp, movePresetDown);
        toolbar.Spacing = 4;
        var presets = Column(8, toolbar, scroll);
        scroll.WidthAnchor.ConstraintEqualTo(presets.WidthAnchor).Active = true;
        scroll.HeightAnchor.ConstraintGreaterThanOrEqualTo(120).Active = true;

        var presetCard = Card("Presets", presets);
        var side = Column(Gap, Card("Pan and tilt", move), Card("Zoom and focus", lens), presetCard);
        side.TranslatesAutoresizingMaskIntoConstraints = false;
        side.WidthAnchor.ConstraintEqualTo(SideColumnWidth).Active = true;
        FillWidth(side);
        presetCard.SetContentHuggingPriorityForOrientation(1, NSLayoutConstraintOrientation.Vertical);

        videoView.TranslatesAutoresizingMaskIntoConstraints = false;
        videoView.SetContentHuggingPriorityForOrientation(1, NSLayoutConstraintOrientation.Horizontal);
        var middle = Row(videoView, side);
        middle.Spacing = Gap;
        middle.Alignment = NSLayoutAttribute.Top;
        videoView.HeightAnchor.ConstraintEqualTo(middle.HeightAnchor).Active = true;
        side.HeightAnchor.ConstraintEqualTo(middle.HeightAnchor).Active = true;

        var root = Column(Gap, header, middle, statusLabel);
        root.EdgeInsets = new NSEdgeInsets(Gap, Gap, 8, Gap);
        header.WidthAnchor.ConstraintEqualTo(root.WidthAnchor, 1, -2 * Gap).Active = true;
        middle.WidthAnchor.ConstraintEqualTo(root.WidthAnchor, 1, -2 * Gap).Active = true;
        statusLabel.WidthAnchor.ConstraintEqualTo(root.WidthAnchor, 1, -2 * Gap).Active = true;
        middle.SetContentHuggingPriorityForOrientation(1, NSLayoutConstraintOrientation.Vertical);
        ContentView = root;

        ForwardControlEvents();
        UpdateSerialSettings();
    }
}
