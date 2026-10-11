using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using TenveoPtz.App.Infrastructure.Diagnostics;
using TenveoPtz.App.Views.Controls;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Diagnostics;
using TenveoPtz.Core.Presentation;

namespace TenveoPtz.App.Views;

/// <summary>
/// Hidden debug window (Ctrl+Shift+D): live log, verbose logging switch, diagnostics for bug
/// reports and a console to send raw VISCA bytes to the camera.
/// </summary>
internal sealed class DebugConsole : Form
{
    /// <summary>Above this many characters the oldest half of the log view is dropped.</summary>
    private const int MaxLogLength = 400_000;

    private readonly DebugTools tools;
    private readonly ConcurrentQueue<LogEntry> pending = new();
    private readonly ToolTip toolTips = new();
    private readonly Timer flushTimer = new() { Interval = 200 };
    private readonly TextBox logView;
    private readonly ToggleSwitch verbose;
    private readonly FluentTextBox rawInput;

    public DebugConsole(DebugTools tools)
    {
        this.tools = tools ?? throw new ArgumentNullException(nameof(tools));
        Text = "Tenveo PTZ - debug console";
        Font = FluentFonts.Body;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        ClientSize = new Size(900, 520);
        MinimumSize = new Size(640, 360);
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        KeyPreview = true;
        Padding = new Padding(12);

        verbose = new ToggleSwitch("Verbose logging (protocol traffic)") { Checked = tools.Log.MinimumLevel == LogLevel.Debug, Anchor = AnchorStyles.Left };
        verbose.CheckedChanged += (_, _) => SetVerbose(verbose.Checked);
        var copy = Button("Copy diagnostics", "Copies versions, settings and the recent log, to paste into a bug report", CopyDiagnostics);
        var folder = Button("Open log folder", tools.LogFolder, OpenLogFolder);
        var clear = Button("Clear", "Clears this view (the log file is kept)", () => logView!.Clear());

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 8) };
        toolbar.Controls.AddRange(new Control[] { verbose, copy, folder, clear });

        logView = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 9F, GraphicsUnit.Point),
            Margin = new Padding(0, 0, 0, 8),
        };

        rawInput = new FluentTextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 8, 0) };
        var send = new FluentButton("Send", appearance: ButtonAppearance.Accent) { Anchor = AnchorStyles.Right };
        send.Click += (_, _) => SendRaw();
        var rawLabel = new Label { Text = "Raw VISCA", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 8, 0) };
        var rawRow = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
        rawRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rawRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rawRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        rawRow.Controls.Add(rawLabel, 0, 0);
        rawRow.Controls.Add(rawInput, 1, 0);
        rawRow.Controls.Add(send, 2, 0);
        toolTips.SetToolTip(rawLabel, "Payload (01 06 04) or full packet (81 01 06 04 FF); the reply is logged. Needs a VISCA connection.");

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Margin = new Padding(0) };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(logView, 0, 1);
        root.Controls.Add(rawRow, 0, 2);
        Controls.Add(root);

        ApplyTheme();
        Theme.Changed += OnThemeChanged;
        tools.Log.EntryWritten += OnEntryWritten;
        flushTimer.Tick += (_, _) => Flush();
    }

    /// <summary>Raw protocol bytes typed by the user.</summary>
    public event EventHandler<RawCommandEventArgs>? RawCommandRequested;

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowChrome.Apply(this, Theme.Current);
        WindowChrome.ApplyScrollBars(logView, Theme.Current);
    }

    /// <summary>Centres on the owner: <see cref="FormStartPosition.CenterParent"/> only applies to modal dialogs.</summary>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (Owner is not null)
        {
            Location = new Point(Owner.Left + ((Owner.Width - Width) / 2), Owner.Top + ((Owner.Height - Height) / 2));
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        foreach (var entry in tools.Log.Recent())
        {
            pending.Enqueue(entry);
        }

        Flush();
        flushTimer.Start();
        rawInput.SelectAll();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Enter && rawInput.ContainsFocus)
        {
            SendRaw();
            e.Handled = e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            tools.Log.EntryWritten -= OnEntryWritten;
            Theme.Changed -= OnThemeChanged;
            flushTimer.Dispose();
            toolTips.Dispose();
            logView.Font.Dispose();
        }

        base.Dispose(disposing);
    }

    private FluentButton Button(string text, string tip, Action onClick)
    {
        var button = new FluentButton(text) { Anchor = AnchorStyles.Left, Margin = new Padding(8, 2, 0, 2) };
        button.Click += (_, _) => onClick();
        toolTips.SetToolTip(button, tip);
        return button;
    }

    /// <summary>Entries may arrive on any thread: they are queued and shown by the UI timer.</summary>
    private void OnEntryWritten(object? sender, LogEntryEventArgs e) => pending.Enqueue(e.Entry);

    private void Flush()
    {
        if (pending.IsEmpty)
        {
            return;
        }

        var text = new StringBuilder();
        while (pending.TryDequeue(out var entry))
        {
            text.Append(entry).Append(Environment.NewLine);
        }

        if (logView.TextLength + text.Length > MaxLogLength)
        {
            var kept = logView.Text;
            logView.Text = kept.Substring(kept.Length / 2);
        }

        logView.AppendText(text.ToString());
    }

    private void SetVerbose(bool enabled)
    {
        tools.Log.MinimumLevel = enabled ? LogLevel.Debug : LogLevel.Info;
        tools.Logger.Info("Verbose logging " + (enabled ? "on." : "off."));
    }

    private void CopyDiagnostics()
    {
        try
        {
            Clipboard.SetText(DiagnosticsReport.Build(tools.Log, tools.DataFolder));
            tools.Logger.Info("Diagnostics copied to the clipboard.");
        }
        catch (Exception ex) when (ex is ExternalException || ex is System.Threading.ThreadStateException)
        {
            tools.Logger.Warning("Could not copy the diagnostics to the clipboard.", ex);
        }
    }

    private void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(tools.LogFolder);
            Process.Start("explorer.exe", "\"" + tools.LogFolder + "\"")?.Dispose();
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is Win32Exception)
        {
            tools.Logger.Warning("Could not open the log folder.", ex);
        }
    }

    private void SendRaw()
    {
        RawCommandRequested?.Invoke(this, new RawCommandEventArgs(rawInput.Value));
        rawInput.SelectAll();
    }

    private void ApplyTheme()
    {
        BackColor = Theme.Current.Background;
        ForeColor = Theme.Current.Text;
        logView.BackColor = Theme.Current.Surface;
        logView.ForeColor = Theme.Current.Text;
        ThemedLabels.Refresh(this);
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ApplyTheme();
        WindowChrome.Apply(this, Theme.Current);
        WindowChrome.ApplyScrollBars(logView, Theme.Current);
        Invalidate(invalidateChildren: true);
    }
}
