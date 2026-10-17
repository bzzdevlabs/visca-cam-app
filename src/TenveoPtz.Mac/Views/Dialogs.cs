using System;
using System.Linq;
using AppKit;
using CoreGraphics;
using TenveoPtz.Core.Localization;
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
        alert.AddButton(Strings.Ok);
        alert.AddButton(Strings.Cancel);
        alert.Window.InitialFirstResponder = field;
        return alert.RunModal() == (long)NSAlertButtonReturn.First ? field.StringValue : null;
    }

    public static bool Confirm(string title, string message, string accept, string? cancel = null)
    {
        using var alert = new NSAlert { MessageText = title, InformativeText = message };
        alert.AddButton(accept);
        alert.AddButton(cancel ?? Strings.Cancel);
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
            MessageText = Strings.SettingsMac,
            InformativeText = Strings.SettingsKeptIn("~/Library/Application Support/TenveoPTZ"),
        };
        var invertPan = Checkbox(Strings.ReverseLeftRight, settings.InvertPan);
        var invertTilt = Checkbox(Strings.ReverseUpDown, settings.InvertTilt);
        var alwaysOnTop = Checkbox(Strings.KeepWindowOnTop, settings.AlwaysOnTop);
        var checkForUpdates = Checkbox(Strings.CheckForUpdates, settings.CheckForUpdates);
        var theme = new NSPopUpButton();
        theme.AddItems(new[] { Strings.ThemeSameAsSystem("macOS"), Strings.ThemeLight, Strings.ThemeDark }.Select(Strings.ThemeMenuItem).ToArray());
        theme.SelectItem((int)settings.Theme);
        // Languages are listed by their own name, in the order of AppLanguage.
        var language = new NSPopUpButton();
        language.AddItems(new[] { AppLanguage.French, AppLanguage.English }.Select(l => Strings.LanguageMenuItem(Strings.NameOf(l))).ToArray());
        language.SelectItem((int)settings.Language);
        var languageNote = NSTextField.CreateLabel(Strings.LanguageNote);
        languageNote.TextColor = NSColor.SecondaryLabel;
        languageNote.Font = NSFont.SystemFontOfSize(NSFont.SmallSystemFontSize)!;

        var stack = NSStackView.FromViews(new NSView[] { invertPan, invertTilt, alwaysOnTop, checkForUpdates, theme, language, languageNote });
        stack.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
        stack.Alignment = NSLayoutAttribute.Leading;
        stack.Spacing = 8;
        stack.LayoutSubtreeIfNeeded();
        stack.SetFrameSize(new CGSize(Math.Max(320, stack.FittingSize.Width), stack.FittingSize.Height));
        alert.AccessoryView = stack;
        alert.AddButton(Strings.Save);
        alert.AddButton(Strings.Cancel);
        alert.AddButton(Strings.ResetAllSettingsMac);

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
        settings.Language = (AppLanguage)Math.Max(0, (int)language.IndexOfSelectedItem);
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
