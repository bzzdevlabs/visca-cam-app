using AppKit;
using ObjCRuntime;
using TenveoPtz.Core.Localization;

namespace TenveoPtz.Mac;

/// <summary>Standard menu bar: application, Edit (copy and paste in dialogs) and Window menus.</summary>
internal static class MainMenu
{
    public static NSMenu Create()
    {
        var menu = new NSMenu();
        menu.AddItem(Submenu("Tenveo PTZ",
            Item(Strings.About, "orderFrontStandardAboutPanel:"),
            NSMenuItem.SeparatorItem,
            Item(Strings.SettingsMenu, "showSettings:", ","),
            NSMenuItem.SeparatorItem,
            Item(Strings.Hide, "hide:", "h"),
            Item(Strings.HideOthers, "hideOtherApplications:", "h", NSEventModifierMask.CommandKeyMask | NSEventModifierMask.AlternateKeyMask),
            Item(Strings.ShowAll, "unhideAllApplications:"),
            NSMenuItem.SeparatorItem,
            Item(Strings.Quit, "terminate:", "q")));
        menu.AddItem(Submenu(Strings.Edit,
            Item(Strings.Undo, "undo:", "z"),
            Item(Strings.Redo, "redo:", "Z"),
            NSMenuItem.SeparatorItem,
            Item(Strings.Cut, "cut:", "x"),
            Item(Strings.Copy, "copy:", "c"),
            Item(Strings.Paste, "paste:", "v"),
            Item(Strings.SelectAll, "selectAll:", "a")));
        var window = Submenu(Strings.Window,
            Item(Strings.Minimize, "performMiniaturize:", "m"),
            Item(Strings.ZoomWindow, "performZoom:"),
            Item(Strings.Close, "performClose:", "w"));
        menu.AddItem(window);
        NSApplication.SharedApplication.WindowsMenu = window.Submenu!;
        return menu;
    }

    private static NSMenuItem Submenu(string title, params NSMenuItem[] items)
    {
        var submenu = new NSMenu(title);
        foreach (var item in items)
        {
            submenu.AddItem(item);
        }

        return new NSMenuItem(title) { Submenu = submenu };
    }

    private static NSMenuItem Item(string title, string action, string key = "", NSEventModifierMask modifiers = NSEventModifierMask.CommandKeyMask) =>
        new(title, new Selector(action), key) { KeyEquivalentModifierMask = modifiers };
}
