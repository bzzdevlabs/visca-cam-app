using AppKit;
using CoreGraphics;

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

    public static void ShowError(string title, string message)
    {
        using var alert = new NSAlert { MessageText = title, InformativeText = message, AlertStyle = NSAlertStyle.Critical };
        alert.RunModal();
    }
}
