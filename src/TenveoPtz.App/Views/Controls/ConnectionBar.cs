using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Settings;
using TenveoPtz.Core.Video;

namespace TenveoPtz.App.Views.Controls;

/// <summary>
/// Header bar: camera and control selection, preview switch, serial settings (VISCA only), refresh,
/// pin and connect. In a narrow window the selectors go on rows of their own.
/// </summary>
internal sealed class ConnectionBar : UserControl
{
    private static readonly int[] BaudRates = { 2400, 4800, 9600, 19200, 38400, 57600, 115200 };

    private readonly DropDownSelector devices = new(260, Strings.NoCameraFound, Glyphs.Camera);
    private readonly DropDownSelector mode = new(190, Strings.ControlPlaceholder);
    private readonly DropDownSelector ports = new(120, Strings.NoComPort);
    private readonly DropDownSelector baudRates = new(120, Strings.BaudRatePlaceholder);
    private readonly DropDownSelector addresses = new(120, Strings.AddressPlaceholder);
    private readonly FlowLayoutPanel serialSettings;
    private readonly FlowLayoutPanel selection;
    private readonly FlowLayoutPanel modeRow;
    private readonly FlowLayoutPanel actions;
    private readonly TableLayoutPanel layout;
    private bool compact;
    private bool settingOptions;
    private readonly FluentButton connect = new(Strings.Connect, Glyphs.Connect, ButtonAppearance.Accent);
    private readonly FluentButton pin = new(string.Empty, Glyphs.Pin, ButtonAppearance.Subtle);
    private readonly ToggleSwitch preview = new(Strings.Preview) { Checked = true, Margin = new Padding(8, 6, 2, 6) };
    private bool connected;

    public ConnectionBar(ToolTip toolTips)
    {
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        mode.SetItems(new object[] { new Choice<ControlMode>(ControlMode.Uvc, Strings.UsbControl), new Choice<ControlMode>(ControlMode.Visca, Strings.SerialControl) });
        mode.SelectedItem = mode.Items[0];
        mode.SelectionChanged += (_, _) => UpdateSerialSettings();
        baudRates.SetItems(BaudRates.Select(rate => (object)new Choice<int>(rate, Strings.Baud(rate))));
        addresses.SetItems(Enumerable.Range(1, 7).Select(address => (object)new Choice<int>(address, Strings.Address(address))));

        toolTips.SetToolTip(devices, Strings.VideoDeviceTip);
        toolTips.SetToolTip(mode, Strings.ModeTip);
        toolTips.SetToolTip(ports, Strings.ComPortTip);
        toolTips.SetToolTip(baudRates, Strings.BaudRateTip);
        toolTips.SetToolTip(addresses, Strings.AddressTip);
        toolTips.SetToolTip(preview, Strings.PreviewTip);
        preview.CheckedChanged += (_, _) =>
        {
            if (!settingOptions)
            {
                PreviewChanged?.Invoke(this, EventArgs.Empty);
            }
        };
        toolTips.SetToolTip(pin, Strings.PinTip);
        var settingsButton = new FluentButton(string.Empty, Glyphs.Settings, ButtonAppearance.Subtle);
        toolTips.SetToolTip(settingsButton, Strings.SettingsTip("Ctrl+,"));
        settingsButton.AccessibleName = Strings.Settings;
        settingsButton.Name = "SettingsButton"; // UI Automation id, used by the CI test.
        settingsButton.Click += (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        toolTips.SetToolTip(connect, Strings.ConnectTip);

        var refresh = new FluentButton(string.Empty, Glyphs.Refresh, ButtonAppearance.Subtle);
        toolTips.SetToolTip(refresh, Strings.RefreshTip);
        refresh.Click += (_, _) => RefreshRequested?.Invoke(this, EventArgs.Empty);
        pin.Click += (_, _) =>
        {
            AlwaysOnTop = !AlwaysOnTop;
            AlwaysOnTopChanged?.Invoke(this, EventArgs.Empty);
        };
        connect.Click += (_, _) => (connected ? DisconnectRequested : ConnectRequested)?.Invoke(this, EventArgs.Empty);

        // Rows never wrap: a wrapping row inside an auto-sized table reserves extra height.
        selection = Row(devices, mode, preview);
        modeRow = Row();
        modeRow.Margin = new Padding(0, 4, 0, 0);
        actions = Row(refresh, settingsButton, pin, connect);
        serialSettings = Row(ports, baudRates, addresses);
        serialSettings.Margin = new Padding(0, 4, 0, 0);

        layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
        ArrangeWide();

        var card = new Card { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(10) };
        card.Controls.Add(layout);
        Controls.Add(card);
        UpdateSerialSettings();
    }

    public event EventHandler? RefreshRequested;

    public event EventHandler? ConnectRequested;

    public event EventHandler? DisconnectRequested;

    public event EventHandler? AlwaysOnTopChanged;

    public event EventHandler? SettingsRequested;

    /// <summary>The Preview switch was toggled by the user.</summary>
    public event EventHandler? PreviewChanged;

    /// <summary>Stacks the selectors on separate rows, for a narrow window.</summary>
    public bool Compact
    {
        get => compact;
        set
        {
            if (compact == value)
            {
                return;
            }

            compact = value;
            layout.SuspendLayout();
            if (value)
            {
                ArrangeCompact();
            }
            else
            {
                ArrangeWide();
            }

            layout.ResumeLayout(performLayout: true);
        }
    }

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
            ShowPreview = preview.Checked,
        };
        set
        {
            settingOptions = true;
            devices.SelectedItem = devices.Items.OfType<VideoDevice>().FirstOrDefault(d => d.Name == value.VideoDeviceName)
                ?? devices.Items.FirstOrDefault();
            mode.SelectedItem = Choice<ControlMode>.Find(mode, value.Mode) ?? mode.Items[0];
            ports.SelectedItem = ports.Items.Contains(value.PortName) ? value.PortName : ports.Items.FirstOrDefault();
            baudRates.SelectedItem = Choice<int>.Find(baudRates, value.BaudRate) ?? Choice<int>.Find(baudRates, ConnectionOptions.DefaultBaudRate);
            addresses.SelectedItem = Choice<int>.Find(addresses, value.Address) ?? addresses.Items[0];
            preview.Checked = value.ShowPreview;
            settingOptions = false;
        }
    }

    public void ShowDevices(IReadOnlyList<VideoDevice> videoDevices) => devices.SetItems(videoDevices);

    public void ShowPorts(IReadOnlyList<string> portNames) => ports.SetItems(portNames);

    public void ShowConnected(bool isConnected)
    {
        connected = isConnected;
        connect.Text = isConnected ? Strings.Disconnect : Strings.Connect;
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

    /// <summary>One row: camera, control and preview, then the buttons on the right; serial settings below.</summary>
    private void ArrangeWide()
    {
        Reset(columns: 2, rows: 2);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        selection.Controls.AddRange(new Control[] { mode, preview });
        actions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        layout.Controls.Add(selection, 0, 0);
        layout.Controls.Add(actions, 1, 0);
        layout.Controls.Add(serialSettings, 0, 1);
        layout.SetColumnSpan(serialSettings, 2);
    }

    /// <summary>Camera; control and preview; serial settings; buttons, each on its own row.</summary>
    private void ArrangeCompact()
    {
        Reset(columns: 1, rows: 4);
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        modeRow.Controls.AddRange(new Control[] { mode, preview });
        actions.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        layout.Controls.Add(selection, 0, 0);
        layout.Controls.Add(modeRow, 0, 1);
        layout.Controls.Add(serialSettings, 0, 2);
        layout.Controls.Add(actions, 0, 3);
    }

    private void Reset(int columns, int rows)
    {
        layout.Controls.Clear();
        layout.ColumnStyles.Clear();
        layout.RowStyles.Clear();
        layout.ColumnCount = columns;
        layout.RowCount = rows;
        for (var row = 0; row < rows; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }
    }

    private void UpdateSerialSettings() =>
        serialSettings.Visible = (mode.SelectedItem as Choice<ControlMode>)?.Value == ControlMode.Visca;
}
