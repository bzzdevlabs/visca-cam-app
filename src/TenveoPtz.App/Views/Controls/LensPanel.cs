using System;
using System.Windows.Forms;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Zoom and focus controls (hold to adjust).</summary>
internal sealed class LensPanel : UserControl
{
    private readonly CheckBox autoFocus;
    private readonly HoldButton focusNear;
    private readonly HoldButton focusFar;

    public LensPanel(ToolTip toolTips)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var grid = UiFactory.Grid(3, 2);
        grid.Dock = DockStyle.Top;
        grid.ColumnStyles[0] = new ColumnStyle(SizeType.AutoSize);

        grid.Controls.Add(UiFactory.Label("Zoom"), 0, 0);
        grid.Controls.Add(Hold("−", "Zoom out (- or Page Down)", toolTips, Motion.Negative, RaiseZoom), 1, 0);
        grid.Controls.Add(Hold("+", "Zoom in (+ or Page Up)", toolTips, Motion.Positive, RaiseZoom), 2, 0);

        autoFocus = new CheckBox { Text = "Auto focus", Checked = true, AutoSize = true, Anchor = AnchorStyles.Left };
        autoFocus.CheckedChanged += (_, _) => OnAutoFocusChanged();
        focusNear = Hold("Near", "Focus nearer", toolTips, Motion.Negative, RaiseFocus);
        focusFar = Hold("Far", "Focus farther", toolTips, Motion.Positive, RaiseFocus);
        grid.Controls.Add(autoFocus, 0, 1);
        grid.Controls.Add(focusNear, 1, 1);
        grid.Controls.Add(focusFar, 2, 1);

        Controls.Add(grid);
        UpdateFocusButtons();
    }

    public event EventHandler<MotionEventArgs>? ZoomRequested;

    public event EventHandler<MotionEventArgs>? FocusRequested;

    public event EventHandler<ToggleEventArgs>? AutoFocusRequested;

    private static HoldButton Hold(string text, string toolTip, ToolTip toolTips, Motion direction, Action<Motion> raise)
    {
        var button = UiFactory.HoldButton(text, toolTip, toolTips);
        button.Pressed += (_, _) => raise(direction);
        button.Released += (_, _) => raise(Motion.None);
        return button;
    }

    private void RaiseZoom(Motion direction) => ZoomRequested?.Invoke(this, new MotionEventArgs(direction));

    private void RaiseFocus(Motion direction) => FocusRequested?.Invoke(this, new MotionEventArgs(direction));

    private void OnAutoFocusChanged()
    {
        UpdateFocusButtons();
        AutoFocusRequested?.Invoke(this, new ToggleEventArgs(autoFocus.Checked));
    }

    private void UpdateFocusButtons()
    {
        focusNear.Enabled = !autoFocus.Checked;
        focusFar.Enabled = !autoFocus.Checked;
    }
}
