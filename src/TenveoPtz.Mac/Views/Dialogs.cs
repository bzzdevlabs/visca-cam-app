using System;
using AppKit;
using CoreGraphics;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.Mac.Views;

/// <summary>Modal alerts used by the main window.</summary>
internal static class Dialogs
{
    public static string? Prompt(string title, string prompt, string initialValue)
    {
        using var alert = new NSAlert { MessageText = title, InformativeText = prompt };
        var field = new NSTextField(new CGRect(0, 0, 280, 24)) { StringValue = initialValue };
        alert.AccessoryView = field;
        alert.AddButton("OK");
        alert.AddButton("Cancel");
        alert.Window.InitialFirstResponder = field;
        return alert.RunModal() == (long)NSAlertButtonReturn.First ? field.StringValue : null;
    }

    public static bool Confirm(string title, string message, string accept, string cancel = "Cancel")
    {
        using var alert = new NSAlert { MessageText = title, InformativeText = message };
        alert.AddButton(accept);
        alert.AddButton(cancel);
        return alert.RunModal() == (long)NSAlertButtonReturn.First;
    }

    /// <summary>
    /// Settings panel for the options that apply on macOS; the Windows-only ones (background
    /// keypad, continuous USB moves) keep their values. Returns <c>null</c> when cancelled.
    /// </summary>
    public static SettingsEdit? EditSettings(AppSettings settings)
    {
        using var alert = new NSAlert
        {
            MessageText = "Settings",
            InformativeText = "Settings and presets are kept in ~/Library/Application Support/TenveoPTZ.",
        };
        var invertPan = Checkbox("Reverse left and right", settings.InvertPan);
        var invertTilt = Checkbox("Reverse up and down", settings.InvertTilt);
        var alwaysOnTop = Checkbox("Keep the window on top of other windows", settings.AlwaysOnTop);
        var checkForUpdates = Checkbox("Look for a new version on start-up", settings.CheckForUpdates);
        var theme = new NSPopUpButton();
        theme.AddItems(new[] { "Theme: same as macOS", "Theme: light", "Theme: dark" });
        theme.SelectItem((int)settings.Theme);

        var stack = NSStackView.FromViews(new NSView[] { invertPan, invertTilt, alwaysOnTop, checkForUpdates, theme });
        stack.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
        stack.Alignment = NSLayoutAttribute.Leading;
        stack.Spacing = 8;
        stack.LayoutSubtreeIfNeeded();
        stack.SetFrameSize(new CGSize(Math.Max(320, stack.FittingSize.Width), stack.FittingSize.Height));
        alert.AccessoryView = stack;
        alert.AddButton("Save");
        alert.AddButton("Cancel");
        alert.AddButton("Reset All Settings...");

        var answer = alert.RunModal();
        if (answer == (long)NSAlertButtonReturn.Third)
        {
            return SettingsEdit.Reset();
        }

        if (answer != (long)NSAlertButtonReturn.First)
        {
            return null;
        }

        settings.InvertPan = invertPan.State == NSCellStateValue.On;
        settings.InvertTilt = invertTilt.State == NSCellStateValue.On;
        settings.AlwaysOnTop = alwaysOnTop.State == NSCellStateValue.On;
        settings.CheckForUpdates = checkForUpdates.State == NSCellStateValue.On;
        settings.Theme = (AppTheme)Math.Max(0, (int)theme.IndexOfSelectedItem);
        return SettingsEdit.Save(settings);
    }

    public static void ShowError(string title, string message)
    {
        using var alert = new NSAlert { MessageText = title, InformativeText = message, AlertStyle = NSAlertStyle.Critical };
        alert.RunModal();
    }

    private static NSButton Checkbox(string title, bool on)
    {
        var box = new NSButton { Title = title, State = on ? NSCellStateValue.On : NSCellStateValue.Off };
        box.SetButtonType(NSButtonType.Switch);
        return box;
    }
}
