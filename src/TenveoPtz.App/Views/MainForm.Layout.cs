using System;
using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Controls;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Localization;

namespace TenveoPtz.App.Views;

/// <summary>
/// Window layout: connection bar on top, video on the left, control cards on the right and a
/// status line at the bottom, on a Windows 11 style background. In a narrow window the control
/// cards go under the video instead.
/// </summary>
internal sealed partial class MainForm
{
    private const int SideColumnWidth = 320;
    private const int Gap = 12;

    /// <summary>Below this client width (logical pixels) the controls go under the video.</summary>
    private const int CompactBelowWidth = 900;

    /// <summary>Height of the preset list when the controls are under the video.</summary>
    private const int CompactPresetsHeight = 200;

    /// <summary>Smallest video height when the controls are under the video.</summary>
    private const int CompactMinVideoHeight = 160;

    private readonly ToolTip toolTips = new();
    private ConnectionBar connectionBar = null!;
    private MovePanel movePanel = null!;
    private LensPanel lensPanel = null!;
    private PresetPanel presetPanel = null!;
    private VideoSurface videoSurface = null!;
    private StatusLine statusLine = null!;
    private TableLayoutPanel root = null!;
    private TableLayoutPanel side = null!;

    /// <summary>Video and controls, stacked and scrollable, in a narrow window.</summary>
    private TableLayoutPanel stack = null!;
    private bool? compactLayout;

    private void InitializeLayout()
    {
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Tenveo PTZ";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = FluentFonts.Body;
        ClientSize = new Size(1120, 760);
        MinimumSize = new Size(440, 640);
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
            PlaceholderTitle = Strings.NoVideoTitle,
            PlaceholderText = Strings.NoVideoHint,
        };
        statusLine = new StatusLine { Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 0) };

        side = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        side.Controls.Add(movePanel, 0, 0);
        side.Controls.Add(lensPanel, 0, 1);
        side.Controls.Add(presetPanel, 0, 2);

        stack = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0), AutoScroll = true, ColumnCount = 1, RowCount = 2 };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, CompactMinVideoHeight));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.Resize += (_, _) => FitCompactVideo();

        root = new TableLayoutPanel { Dock = DockStyle.Fill, Margin = new Padding(0) };
        UpdateLayoutMode();

        Controls.Add(root);
        ApplyTheme();
        Theme.Changed += OnThemeChanged;
        ResumeLayout(performLayout: true);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        UpdateLayoutMode();
    }

    /// <summary>Switches between side-by-side and stacked layouts as the window width crosses the threshold.</summary>
    private void UpdateLayoutMode()
    {
        if (root is null)
        {
            return;
        }

        var compact = ClientSize.Width < Scaled(CompactBelowWidth);
        if (compact == compactLayout)
        {
            return;
        }

        compactLayout = compact;
        root.SuspendLayout();
        side.SuspendLayout();
        stack.SuspendLayout();
        stack.Controls.Clear();
        root.Controls.Clear();
        root.ColumnStyles.Clear();
        root.RowStyles.Clear();
        connectionBar.Compact = compact;
        if (compact)
        {
            // Bar on top, then video over the controls (scrolling when the window is too short),
            // status line at the bottom. The video keeps a 16:9 height for the window width.
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            videoSurface.Margin = new Padding(0, 0, 0, Gap);
            side.AutoSize = true;
            side.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            side.RowStyles[2] = new RowStyle(SizeType.Absolute, Scaled(CompactPresetsHeight));
            stack.Controls.Add(videoSurface, 0, 0);
            stack.Controls.Add(side, 0, 1);
            root.Controls.Add(connectionBar, 0, 0);
            root.Controls.Add(stack, 0, 1);
            root.Controls.Add(statusLine, 0, 2);
            FitCompactVideo();
        }
        else
        {
            // Bar on top, video and controls side by side, status line at the bottom.
            root.ColumnCount = 2;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Scaled(SideColumnWidth)));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            videoSurface.Margin = new Padding(0, 0, Gap, 0);
            side.AutoSize = false;
            side.RowStyles[2] = new RowStyle(SizeType.Percent, 100);
            root.Controls.Add(connectionBar, 0, 0);
            root.SetColumnSpan(connectionBar, 2);
            root.Controls.Add(videoSurface, 0, 1);
            root.Controls.Add(side, 1, 1);
            root.Controls.Add(statusLine, 0, 2);
            root.SetColumnSpan(statusLine, 2);
        }

        side.ResumeLayout(performLayout: false);
        stack.ResumeLayout(performLayout: false);
        root.ResumeLayout(performLayout: true);
    }

    /// <summary>Gives the stacked video a 16:9 height for the available width.</summary>
    private void FitCompactVideo()
    {
        if (compactLayout != true)
        {
            return;
        }

        var height = Math.Max(Scaled(CompactMinVideoHeight), (stack.ClientSize.Width * 9 / 16) + videoSurface.Margin.Vertical);
        if ((int)stack.RowStyles[0].Height != height)
        {
            stack.RowStyles[0].Height = height;
        }
    }

    /// <summary>
    /// Logical to device pixels. Before the window exists, values stay logical: the form's DPI
    /// auto-scaling then scales them along with the rest of the layout.
    /// </summary>
    private int Scaled(int logicalPixels) => IsHandleCreated ? LogicalToDeviceUnits(logicalPixels) : logicalPixels;

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
