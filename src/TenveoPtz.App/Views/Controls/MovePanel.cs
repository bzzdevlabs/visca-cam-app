using System;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Presentation;

namespace TenveoPtz.App.Views.Controls;

/// <summary>"Pan and tilt" card: circular direction pad and speed slider.</summary>
internal sealed class MovePanel : UserControl
{
    private const int SpeedSteps = 10;

    private readonly FluentSlider speed = new(1, SpeedSteps, SpeedSteps / 2) { Dock = DockStyle.Fill };

    public MovePanel(ToolTip toolTips)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var pad = new DirectionPad { Anchor = AnchorStyles.None, Margin = new Padding(0, 0, 0, 4) };
        pad.PanTiltRequested += (_, e) => PanTiltRequested?.Invoke(this, e);
        pad.HomeRequested += (_, e) => HomeRequested?.Invoke(this, e);
        toolTips.SetToolTip(pad, "Hold a direction to move (or use the arrow keys). Centre: home position.");
        toolTips.SetToolTip(speed, "Pan, tilt and zoom speed");

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(pad, 0, 0);
        layout.SetColumnSpan(pad, 2);
        layout.Controls.Add(new Label { Text = "Speed", AutoSize = true, Anchor = AnchorStyles.Left, Font = FluentFonts.Body, ForeColor = Theme.Current.TextSecondary }, 0, 1);
        layout.Controls.Add(speed, 1, 1);

        var card = new Card("Pan and tilt") { Dock = DockStyle.Fill, AutoSize = true };
        card.Controls.Add(layout);
        Controls.Add(card);
        Theme.Changed += OnThemeChanged;
    }

    public event EventHandler<PanTiltEventArgs>? PanTiltRequested;

    public event EventHandler? HomeRequested;

    /// <summary>Relative speed, 0..1.</summary>
    public double Speed
    {
        get => (speed.Value - speed.Minimum) / (double)(speed.Maximum - speed.Minimum);
        set => speed.Value = speed.Minimum + (int)Math.Round(Math.Max(0, Math.Min(1, value)) * (speed.Maximum - speed.Minimum));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Theme.Changed -= OnThemeChanged;
        }

        base.Dispose(disposing);
    }

    private void OnThemeChanged(object? sender, EventArgs e) => ThemedLabels.Refresh(this);
}
