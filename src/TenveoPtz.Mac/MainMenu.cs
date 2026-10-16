using AppKit;
using ObjCRuntime;

namespace TenveoPtz.Mac;

/// <summary>Standard menu bar: application, Edit (copy and paste in dialogs) and Window menus.</summary>
internal static class MainMenu
{
    public static NSMenu Create()
    {
        var menu = new NSMenu();
        menu.AddItem(Submenu("Tenveo PTZ",
            Item("About Tenveo PTZ", "orderFrontStandardAboutPanel:"),
            NSMenuItem.SeparatorItem,
            Item("Settings...", "showSettings:", ","),
            NSMenuItem.SeparatorItem,
            Item("Hide Tenveo PTZ", "hide:", "h"),
            Item("Hide Others", "hideOtherApplications:", "h", NSEventModifierMask.CommandKeyMask | NSEventModifierMask.AlternateKeyMask),
            Item("Show All", "unhideAllApplications:"),
            NSMenuItem.SeparatorItem,
            Item("Quit Tenveo PTZ", "terminate:", "q")));
        menu.AddItem(Submenu("Edit",
            Item("Undo", "undo:", "z"),
            Item("Redo", "redo:", "Z"),
            NSMenuItem.SeparatorItem,
            Item("Cut", "cut:", "x"),
            Item("Copy", "copy:", "c"),
            Item("Paste", "paste:", "v"),
            Item("Select All", "selectAll:", "a")));
        var window = Submenu("Window",
            Item("Minimize", "performMiniaturize:", "m"),
            Item("Zoom", "performZoom:"),
            Item("Close", "performClose:", "w"));
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
