using System;
using System.Windows.Forms;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Ptz;

namespace TenveoPtz.App.Views.Controls;

/// <summary>3x3 directional pad (hold to move) with a home button and a speed slider.</summary>
internal sealed class PtzPad : UserControl
{
    private const int SpeedSteps = 10;

    private readonly TrackBar speed;

    public PtzPad(ToolTip toolTips)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var pad = UiFactory.Grid(3, 3);
        pad.Dock = DockStyle.Top;
        AddDirection(pad, "↖", "Up-left", Motion.Negative, Motion.Positive, 0, 0, toolTips);
        AddDirection(pad, "▲", "Up (↑ key)", Motion.None, Motion.Positive, 1, 0, toolTips);
        AddDirection(pad, "↗", "Up-right", Motion.Positive, Motion.Positive, 2, 0, toolTips);
        AddDirection(pad, "◀", "Left (← key)", Motion.Negative, Motion.None, 0, 1, toolTips);
        AddDirection(pad, "▶", "Right (→ key)", Motion.Positive, Motion.None, 2, 1, toolTips);
        AddDirection(pad, "↙", "Down-left", Motion.Negative, Motion.Negative, 0, 2, toolTips);
        AddDirection(pad, "▼", "Down (↓ key)", Motion.None, Motion.Negative, 1, 2, toolTips);
        AddDirection(pad, "↘", "Down-right", Motion.Positive, Motion.Negative, 2, 2, toolTips);

        var home = UiFactory.HoldButton("⌂", "Home position (Home key)", toolTips);
        home.Click += (_, _) => HomeRequested?.Invoke(this, EventArgs.Empty);
        pad.Controls.Add(home, 1, 1);

        speed = new TrackBar
        {
            Minimum = 1,
            Maximum = SpeedSteps,
            Value = SpeedSteps / 2,
            TickStyle = TickStyle.BottomRight,
            Dock = DockStyle.Fill,
            AutoSize = true,
        };
        toolTips.SetToolTip(speed, "Pan, tilt and zoom speed");

        var speedRow = UiFactory.Grid(2, 1);
        speedRow.Dock = DockStyle.Top;
        speedRow.ColumnStyles[0] = new ColumnStyle(SizeType.AutoSize);
        speedRow.Controls.Add(UiFactory.Label("Speed"), 0, 0);
        speedRow.Controls.Add(speed, 1, 0);

        Controls.Add(speedRow);
        Controls.Add(pad);
    }

    public event EventHandler<PanTiltEventArgs>? PanTiltRequested;

    public event EventHandler? HomeRequested;

    /// <summary>Relative speed, 0..1.</summary>
    public double Speed
    {
        get => (speed.Value - speed.Minimum) / (double)(speed.Maximum - speed.Minimum);
        set => speed.Value = speed.Minimum + (int)Math.Round(Math.Max(0, Math.Min(1, value)) * (speed.Maximum - speed.Minimum));
    }

    private void AddDirection(TableLayoutPanel pad, string glyph, string toolTip, Motion pan, Motion tilt, int column, int row, ToolTip toolTips)
    {
        var button = UiFactory.HoldButton(glyph, toolTip, toolTips);
        button.Pressed += (_, _) => PanTiltRequested?.Invoke(this, new PanTiltEventArgs(pan, tilt));
        button.Released += (_, _) => PanTiltRequested?.Invoke(this, new PanTiltEventArgs(Motion.None, Motion.None));
        pad.Controls.Add(button, column, row);
    }
}
