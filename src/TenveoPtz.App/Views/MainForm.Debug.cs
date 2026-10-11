using System;
using System.Windows.Forms;
using TenveoPtz.Core.Presentation;

namespace TenveoPtz.App.Views;

/// <summary>Hidden debug console, toggled with Ctrl+Shift+D (or opened at start-up with <c>--debug</c>).</summary>
internal sealed partial class MainForm
{
    private const Keys DebugConsoleKeys = Keys.Control | Keys.Shift | Keys.D;

    private readonly DebugTools debugTools;
    private DebugConsole? debugConsole;

    public event EventHandler<RawCommandEventArgs>? RawCommandRequested;

    /// <summary>Opens the debug console once the window is shown.</summary>
    public bool ShowDebugConsoleOnStart { get; set; }

    public void ToggleDebugConsole()
    {
        if (debugConsole is { IsDisposed: false })
        {
            debugConsole.Close();
            return;
        }

        debugConsole = new DebugConsole(debugTools);
        debugConsole.RawCommandRequested += (_, e) => RawCommandRequested?.Invoke(this, e);
        debugConsole.FormClosed += (sender, _) => ((Form)sender!).Dispose();
        debugConsole.Show(this);
    }
}
