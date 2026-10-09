using System.Drawing;
using System.Windows.Forms;
using TenveoPtz.App.Views.Controls;

namespace TenveoPtz.App.Views;

/// <summary>Window layout: toolbar on top, video on the left, controls on the right, status at the bottom.</summary>
internal sealed partial class MainForm
{
    private const int SidePanelWidth = 270;

    private readonly ToolTip toolTips = new();
    private ConnectionBar connectionBar = null!;
    private PtzPad ptzPad = null!;
    private LensPanel lensPanel = null!;
    private PresetPanel presetPanel = null!;
    private Panel videoHost = null!;
    private Label noVideoLabel = null!;
    private ToolStripStatusLabel statusLabel = null!;

    private void InitializeLayout()
    {
        SuspendLayout();
        Text = "Tenveo PTZ";
        Font = SystemFonts.MessageBoxFont;
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 520);
        MinimumSize = new Size(640, 440);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        connectionBar = new ConnectionBar(toolTips) { Dock = DockStyle.Top };
        ptzPad = new PtzPad(toolTips);
        lensPanel = new LensPanel(toolTips);
        presetPanel = new PresetPanel(toolTips) { Dock = DockStyle.Fill };

        var sidePanel = new Panel { Dock = DockStyle.Right, Width = SidePanelWidth, Padding = new Padding(4, 0, 4, 4) };
        var presetGroup = UiFactory.Group("Presets", presetPanel);
        presetGroup.AutoSize = false;
        presetGroup.Dock = DockStyle.Fill;
        sidePanel.Controls.Add(presetGroup);
        sidePanel.Controls.Add(UiFactory.Group("Lens", lensPanel));
        sidePanel.Controls.Add(UiFactory.Group("Pan / tilt", ptzPad));

        videoHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black, Margin = new Padding(4) };
        noVideoLabel = new Label
        {
            Text = "No video",
            ForeColor = Color.Gray,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };
        videoHost.Controls.Add(noVideoLabel);

        statusLabel = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        var statusStrip = new StatusStrip { SizingGrip = true };
        statusStrip.Items.Add(statusLabel);

        // Docking order: last added docks first.
        Controls.Add(videoHost);
        Controls.Add(sidePanel);
        Controls.Add(connectionBar);
        Controls.Add(statusStrip);
        ResumeLayout(performLayout: true);
    }
}
