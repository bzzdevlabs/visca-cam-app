using System;
using System.Windows.Forms;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Button reporting press and release, for "move while held" camera controls.</summary>
internal sealed class HoldButton : Button
{
    private bool held;

    public HoldButton()
    {
        TabStop = false;
    }

    public event EventHandler? Pressed;

    public event EventHandler? Released;

    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        if (mevent.Button == MouseButtons.Left && !held)
        {
            held = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        Release();
    }

    protected override void OnMouseCaptureChanged(EventArgs e)
    {
        base.OnMouseCaptureChanged(e);
        Release();
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        Release();
    }

    private void Release()
    {
        if (held)
        {
            held = false;
            Released?.Invoke(this, EventArgs.Empty);
        }
    }
}
