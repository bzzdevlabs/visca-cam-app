using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.App.Views.Controls;

/// <summary>"Zoom and focus" card (hold the buttons to adjust).</summary>
internal sealed class LensPanel : UserControl
{
    private readonly ToggleSwitch autoFocus = new("Autofocus") { Checked = true, Anchor = AnchorStyles.Left };
    private readonly FluentButton focusNear;
    private readonly FluentButton focusFar;

    public LensPanel(ToolTip toolTips)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var zoomOut = Hold(Glyphs.ZoomOut, string.Empty, "Zoom out (hold, or - / Page Down)", toolTips, Motion.Negative, RaiseZoom);
        var zoomIn = Hold(Glyphs.ZoomIn, string.Empty, "Zoom in (hold, or + / Page Up)", toolTips, Motion.Positive, RaiseZoom);
        focusNear = Hold(null, "Near", "Focus nearer (hold)", toolTips, Motion.Negative, RaiseFocus);
        focusFar = Hold(null, "Far", "Focus farther (hold)", toolTips, Motion.Positive, RaiseFocus);
        autoFocus.CheckedChanged += (_, _) => OnAutoFocusChanged();

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "Zoom", AutoSize = true, Anchor = AnchorStyles.Left, Font = FluentFonts.Body, ForeColor = Theme.Current.TextSecondary }, 0, 0);
        layout.Controls.Add(zoomOut, 1, 0);
        layout.Controls.Add(zoomIn, 2, 0);
        layout.Controls.Add(autoFocus, 0, 1);
        layout.Controls.Add(focusNear, 1, 1);
        layout.Controls.Add(focusFar, 2, 1);

        var card = new Card("Zoom and focus") { Dock = DockStyle.Fill, AutoSize = true };
        card.Controls.Add(layout);
        Controls.Add(card);
        UpdateFocusButtons();
        Theme.Changed += OnThemeChanged;
    }

    public event EventHandler<MotionEventArgs>? ZoomRequested;

    public event EventHandler<MotionEventArgs>? FocusRequested;

    public event EventHandler<ToggleEventArgs>? AutoFocusRequested;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private static FluentButton Hold(string? glyph, string text, string toolTip, ToolTip toolTips, Motion direction, Action<Motion> raise)
    {
        var button = new FluentButton(text, glyph) { AutoSize = false, Size = new Size(56, 32), TabStop = false };
        button.Pressed += (_, _) => raise(direction);
        button.Released += (_, _) => raise(Motion.None);
        toolTips.SetToolTip(button, toolTip);
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

    private void OnThemeChanged(object? sender, EventArgs e) => ThemedLabels.Refresh(this);
}
