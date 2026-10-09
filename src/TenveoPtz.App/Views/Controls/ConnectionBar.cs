using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Video;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Header bar: camera and control selection, serial settings (VISCA only), refresh, pin and connect.</summary>
internal sealed class ConnectionBar : UserControl
{
    private static readonly int[] BaudRates = { 2400, 4800, 9600, 19200, 38400, 57600, 115200 };

    private readonly DropDownSelector devices = new(260, "No camera found", Glyphs.Camera);
    private readonly DropDownSelector mode = new(190, "Control");
    private readonly DropDownSelector ports = new(120, "No COM port");
    private readonly DropDownSelector baudRates = new(120, "Baud rate");
    private readonly DropDownSelector addresses = new(120, "Address");
    private readonly FlowLayoutPanel serialSettings;
    private readonly FluentButton connect = new("Connect", Glyphs.Connect, ButtonAppearance.Accent);
    private readonly FluentButton pin = new(string.Empty, Glyphs.Pin, ButtonAppearance.Subtle);
    private bool connected;

    public ConnectionBar(ToolTip toolTips)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        mode.SetItems(new object[] { new Choice<ControlMode>(ControlMode.Uvc, "USB control (UVC)"), new Choice<ControlMode>(ControlMode.Visca, "Serial control (VISCA)") });
        mode.SelectedItem = mode.Items[0];
        mode.SelectionChanged += (_, _) => UpdateSerialSettings();
        baudRates.SetItems(BaudRates.Select(rate => (object)new Choice<int>(rate, $"{rate} baud")));
        addresses.SetItems(Enumerable.Range(1, 7).Select(address => (object)new Choice<int>(address, $"Address {address}")));

        toolTips.SetToolTip(devices, "Video device used for the live preview");
        toolTips.SetToolTip(mode, "USB: control through the camera's USB cable. Serial: VISCA through an RS-232/RS-485 cable.");
        toolTips.SetToolTip(ports, "COM port of the VISCA serial cable or adapter");
        toolTips.SetToolTip(baudRates, "VISCA baud rate (must match the camera, usually 9600)");
        toolTips.SetToolTip(addresses, "VISCA camera address (usually 1)");
        toolTips.SetToolTip(pin, "Keep this window on top of other windows");
        toolTips.SetToolTip(connect, "Open the video and camera control");

        var refresh = new FluentButton(string.Empty, Glyphs.Refresh, ButtonAppearance.Subtle);
        toolTips.SetToolTip(refresh, "Refresh the device lists");
        refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        pin.Click += (_, _) =>
        {
            AlwaysOnTop = !AlwaysOnTop;
            AlwaysOnTopChanged?.Invoke(this, EventArgs.Empty);
        };
        connect.Click += (_, _) => (connected ? DisconnectRequested : ConnectRequested)?.Invoke(this, EventArgs.Empty);

        // Rows never wrap: a wrapping row inside an auto-sized table reserves extra height.
        var selection = Row(devices, mode);
        var actions = Row(refresh, pin, connect);
        actions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        serialSettings = Row(ports, baudRates, addresses);
        serialSettings.Margin = new Padding(0, 4, 0, 0);

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 2 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(selection, 0, 0);
        layout.Controls.Add(actions, 1, 0);
        layout.Controls.Add(serialSettings, 0, 1);
        layout.SetColumnSpan(serialSettings, 2);

        var card = new Card { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(10) };
        card.Controls.Add(layout);
        Controls.Add(card);
        UpdateSerialSettings();
    }

    public event EventHandler? RefreshRequested;

    public event EventHandler? ConnectRequested;

    public event EventHandler? DisconnectRequested;

    public event EventHandler? AlwaysOnTopChanged;

    public bool AlwaysOnTop
    {
        get => pin.IsChecked;
        set
        {
            pin.IsChecked = value;
            pin.Glyph = value ? Glyphs.Pinned : Glyphs.Pin;
        }
    }

    public ConnectionOptions Options
    {
        get => new()
        {
            VideoDeviceName = (devices.SelectedItem as VideoDevice)?.Name ?? string.Empty,
            Mode = (mode.SelectedItem as Choice<ControlMode>)?.Value ?? ControlMode.Uvc,
            PortName = ports.SelectedItem as string ?? string.Empty,
            BaudRate = (baudRates.SelectedItem as Choice<int>)?.Value ?? ConnectionOptions.DefaultBaudRate,
            Address = (addresses.SelectedItem as Choice<int>)?.Value ?? 1,
        };
        set
        {
            devices.SelectedItem = devices.Items.OfType<VideoDevice>().FirstOrDefault(d => d.Name == value.VideoDeviceName)
                ?? devices.Items.FirstOrDefault();
            mode.SelectedItem = Choice<ControlMode>.Find(mode, value.Mode) ?? mode.Items[0];
            ports.SelectedItem = ports.Items.Contains(value.PortName) ? value.PortName : ports.Items.FirstOrDefault();
            baudRates.SelectedItem = Choice<int>.Find(baudRates, value.BaudRate) ?? Choice<int>.Find(baudRates, ConnectionOptions.DefaultBaudRate);
            addresses.SelectedItem = Choice<int>.Find(addresses, value.Address) ?? addresses.Items[0];
        }
    }

    public void ShowDevices(IReadOnlyList<VideoDevice> videoDevices) => devices.SetItems(videoDevices);

    public void ShowPorts(IReadOnlyList<string> portNames) => ports.SetItems(portNames);

    public void ShowConnected(bool isConnected)
    {
        connected = isConnected;
        connect.Text = isConnected ? "Disconnect" : "Connect";
        connect.Glyph = isConnected ? Glyphs.Stop : Glyphs.Connect;
        connect.Appearance = isConnected ? ButtonAppearance.Standard : ButtonAppearance.Accent;
        foreach (var selector in new[] { devices, mode, ports, baudRates, addresses })
        {
            selector.Enabled = !isConnected;
        }
    }

    private static FlowLayoutPanel Row(params Control[] controls)
    {
        var row = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        row.Controls.AddRange(controls);
        return row;
    }

    private void UpdateSerialSettings() =>
        serialSettings.Visible = (mode.SelectedItem as Choice<ControlMode>)?.Value == ControlMode.Visca;

    /// <summary>Selector item pairing a value with its label.</summary>
    private sealed class Choice<T>
    {
        public Choice(T value, string label)
        {
            Value = value;
            Label = label;
        }

        public T Value { get; }

        public string Label { get; }

        public static object? Find(DropDownSelector selector, T value) =>
            selector.Items.OfType<Choice<T>>().FirstOrDefault(c => EqualityComparer<T>.Default.Equals(c.Value, value));

        public override string ToString() => Label;
    }
}
