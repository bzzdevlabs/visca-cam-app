using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Video;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Device selection and connect/disconnect toolbar.</summary>
internal sealed class ConnectionBar : UserControl
{
    private static readonly int[] BaudRates = { 2400, 4800, 9600, 19200, 38400, 57600, 115200 };

    private readonly ComboBox devices = DropDown(220);
    private readonly ComboBox mode = DropDown(130);
    private readonly ComboBox ports = DropDown(80);
    private readonly ComboBox baudRates = DropDown(75);
    private readonly NumericUpDown address = new() { Minimum = 1, Maximum = 7, Value = 1, Width = 40 };
    private readonly Button connect;
    private readonly CheckBox alwaysOnTop = new() { Text = "On top", AutoSize = true, Margin = new Padding(8, 6, 2, 2) };
    private bool connected;

    public ConnectionBar(ToolTip toolTips)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        mode.Items.AddRange(new object[] { new ModeItem(ControlMode.Uvc, "UVC (USB)"), new ModeItem(ControlMode.Visca, "VISCA (COM port)") });
        mode.SelectedIndex = 0;
        mode.SelectedIndexChanged += (_, _) => UpdateViscaFields();
        baudRates.Items.AddRange(BaudRates.Cast<object>().ToArray());
        baudRates.SelectedItem = ConnectionOptions.DefaultBaudRate;

        toolTips.SetToolTip(devices, "Video device used for the live preview");
        toolTips.SetToolTip(mode, "UVC: control through the USB cable. VISCA: control through a serial (RS-232/RS-485) cable.");
        toolTips.SetToolTip(ports, "COM port of the VISCA serial cable or adapter");
        toolTips.SetToolTip(baudRates, "VISCA baud rate (must match the camera, usually 9600)");
        toolTips.SetToolTip(address, "VISCA camera address (usually 1)");
        toolTips.SetToolTip(alwaysOnTop, "Keep this window above other windows");

        var refresh = UiFactory.Button("⟳", "Refresh device lists", toolTips);
        refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        connect = UiFactory.Button("Connect", "Open the video and camera control", toolTips);
        connect.Click += (_, _) => (connected ? DisconnectRequested : ConnectRequested)?.Invoke(this, EventArgs.Empty);
        alwaysOnTop.CheckedChanged += (_, _) => AlwaysOnTopChanged?.Invoke(this, EventArgs.Empty);

        var row = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        row.Controls.AddRange(new Control[]
        {
            UiFactory.Label("Camera"), devices, UiFactory.Label("Control"), mode,
            UiFactory.Label("Port"), ports, UiFactory.Label("Baud"), baudRates, UiFactory.Label("Addr"), address,
            refresh, connect, alwaysOnTop,
        });
        Controls.Add(row);
        UpdateViscaFields();
    }

    public event EventHandler? RefreshRequested;

    public event EventHandler? ConnectRequested;

    public event EventHandler? DisconnectRequested;

    public event EventHandler? AlwaysOnTopChanged;

    public bool AlwaysOnTop
    {
        get => alwaysOnTop.Checked;
        set => alwaysOnTop.Checked = value;
    }

    public ConnectionOptions Options
    {
        get => new()
        {
            VideoDeviceName = (devices.SelectedItem as VideoDevice)?.Name ?? string.Empty,
            Mode = (mode.SelectedItem as ModeItem)?.Mode ?? ControlMode.Uvc,
            PortName = ports.SelectedItem as string ?? string.Empty,
            BaudRate = baudRates.SelectedItem as int? ?? ConnectionOptions.DefaultBaudRate,
            Address = (int)address.Value,
        };
        set
        {
            devices.SelectedItem = devices.Items.OfType<VideoDevice>().FirstOrDefault(d => d.Name == value.VideoDeviceName)
                ?? devices.Items.OfType<VideoDevice>().FirstOrDefault();
            mode.SelectedItem = mode.Items.OfType<ModeItem>().First(m => m.Mode == value.Mode);
            ports.SelectedItem = ports.Items.Contains(value.PortName) ? value.PortName : ports.Items.OfType<string>().FirstOrDefault();
            baudRates.SelectedItem = BaudRates.Contains(value.BaudRate) ? value.BaudRate : ConnectionOptions.DefaultBaudRate;
            address.Value = Math.Max(address.Minimum, Math.Min(address.Maximum, value.Address));
        }
    }

    public void ShowDevices(IReadOnlyList<VideoDevice> videoDevices) => Replace(devices, videoDevices);

    public void ShowPorts(IReadOnlyList<string> portNames) => Replace(ports, portNames);

    public void ShowConnected(bool isConnected)
    {
        connected = isConnected;
        connect.Text = isConnected ? "Disconnect" : "Connect";
        foreach (var input in new Control[] { devices, mode })
        {
            input.Enabled = !isConnected;
        }

        UpdateViscaFields();
    }

    private static ComboBox DropDown(int width) =>
        new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = width, Margin = new Padding(2, 3, 2, 2) };

    private static void Replace<T>(ComboBox combo, IReadOnlyList<T> items)
    {
        combo.BeginUpdate();
        combo.Items.Clear();
        foreach (var item in items)
        {
            combo.Items.Add(item!);
        }

        combo.EndUpdate();
    }

    private void UpdateViscaFields()
    {
        var visca = !connected && (mode.SelectedItem as ModeItem)?.Mode == ControlMode.Visca;
        ports.Enabled = visca;
        baudRates.Enabled = visca;
        address.Enabled = visca;
    }

    private sealed class ModeItem
    {
        public ModeItem(ControlMode mode, string label)
        {
            Mode = mode;
            Label = label;
        }

        public ControlMode Mode { get; }

        public string Label { get; }

        public override string ToString() => Label;
    }
}
