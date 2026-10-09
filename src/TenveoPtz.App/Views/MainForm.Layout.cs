using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Controls;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views;

/// <summary>
/// Window layout: connection bar on top, video on the left, control cards on the right and a
/// status line at the bottom, on a Windows 11 style background.
/// </summary>
internal sealed partial class MainForm
{
    private const int SideColumnWidth = 320;
    private const int Gap = 12;

    private readonly ToolTip toolTips = new();
    private ConnectionBar connectionBar = null!;
    private MovePanel movePanel = null!;
    private LensPanel lensPanel = null!;
    private PresetPanel presetPanel = null!;
    private VideoSurface videoSurface = null!;
    private StatusLine statusLine = null!;

    private void InitializeLayout()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Tenveo PTZ";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = FluentFonts.Body;
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(960, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(Gap, Gap, Gap, 4);
        KeyPreview = true;

        connectionBar = new ConnectionBar(toolTips) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Gap) };
        movePanel = new MovePanel(toolTips) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Gap) };
        lensPanel = new LensPanel(toolTips) { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, Gap) };
        presetPanel = new PresetPanel(toolTips) { Dock = DockStyle.Fill, Margin = new Padding(0) };
        videoSurface = new VideoSurface
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, Gap, 0),
            PlaceholderTitle = "No video",
            PlaceholderText = "Choose the camera above, then select Connect.",
        };
        statusLine = new StatusLine { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };

        var side = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        side.Controls.Add(movePanel, 0, 0);
        side.Controls.Add(lensPanel, 0, 1);
        side.Controls.Add(presetPanel, 0, 2);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 3, Margin = new Padding(0) };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, SideColumnWidth));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(connectionBar, 0, 0);
        root.SetColumnSpan(connectionBar, 2);
        root.Controls.Add(videoSurface, 0, 1);
        root.Controls.Add(side, 1, 1);
        root.Controls.Add(statusLine, 0, 2);
        root.SetColumnSpan(statusLine, 2);

        Controls.Add(root);
        ApplyTheme();
        Theme.Changed += OnThemeChanged;
        ResumeLayout(performLayout: true);
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Current.Background;
        ForeColor = Theme.Current.Text;
        WindowChrome.Apply(this, Theme.Current);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyTheme();
        Invalidate(invalidateChildren: true);
    }
}
