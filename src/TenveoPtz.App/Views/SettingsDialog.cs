using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TenveoPtz.App.Views.Controls;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.App.Views;

/// <summary>
/// Settings panel: camera directions and move mode, keyboard, window, updates, the data folder and
/// a reset of every setting. Edits a copy of the settings; the presenter applies the result.
/// </summary>
internal sealed class SettingsDialog : Form
{
    private const int ContentWidth = 440;

    private readonly AppSettings settings;
    private readonly ToggleSwitch invertPan = new("Reverse left and right");
    private readonly ToggleSwitch invertTilt = new("Reverse up and down");
    private readonly ToggleSwitch continuousMoves = new("Continuous USB moves (experimental)");
    private readonly ToggleSwitch backgroundNumpad = new("Numeric keypad works while another app is in front");
    private readonly ToggleSwitch alwaysOnTop = new("Keep the window on top of other windows");
    private readonly DropDownSelector theme = new(200, "Theme");
    private readonly ToggleSwitch checkForUpdates = new("Look for a new version on start-up");
    private bool resetRequested;

    private SettingsDialog(AppSettings settings, string dataFolder)
    {
        this.settings = settings;
        Text = "Settings";
        Font = FluentFonts.Body;
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96F, 96F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Theme.Current.Surface;
        KeyPreview = true;

        invertPan.Checked = settings.InvertPan;
        invertTilt.Checked = settings.InvertTilt;
        continuousMoves.Checked = settings.UvcContinuousMoves;
        backgroundNumpad.Checked = settings.BackgroundNumpad;
        alwaysOnTop.Checked = settings.AlwaysOnTop;
        checkForUpdates.Checked = settings.CheckForUpdates;
        theme.SetItems(new object[] { new ThemeChoice(AppTheme.System, "Same as Windows"), new ThemeChoice(AppTheme.Light, "Light"), new ThemeChoice(AppTheme.Dark, "Dark") });
        theme.SelectedItem = theme.Items.OfType<ThemeChoice>().First(c => c.Value == settings.Theme);

        var content = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Padding = new Padding(24, 20, 24, 16), Margin = new Padding(0) };
        content.Controls.Add(new Label { Text = "Settings", Font = FluentFonts.Subtitle, ForeColor = Theme.Current.Text, AutoSize = true, Margin = new Padding(0, 0, 0, 8) });

        Section(content, "Camera");
        content.Controls.Add(invertPan);
        content.Controls.Add(invertTilt);
        content.Controls.Add(continuousMoves);
        Note(content, "USB (UVC) only: smoother moves using the camera's own motion. Some cameras ignore it or report wrong positions afterwards; turn it off if the controls stop working or presets come back wrong.");

        Section(content, "Keyboard");
        content.Controls.Add(backgroundNumpad);
        Note(content, "Windows takes the keypad keys from the app in front while the camera is connected. Num Lock off: 8 4 6 2 move, 7 home, 9 3 zoom. Num Lock on: 1-9 presets. + and - zoom.");

        Section(content, "Window");
        content.Controls.Add(alwaysOnTop);
        var themeRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 4, 0, 0) };
        themeRow.Controls.Add(new Label { Text = "Theme", AutoSize = true, ForeColor = Theme.Current.Text, Margin = new Padding(2, 8, 12, 0) });
        themeRow.Controls.Add(theme);
        content.Controls.Add(themeRow);

        Section(content, "Updates");
        content.Controls.Add(checkForUpdates);

        Section(content, "Settings and presets");
        Note(content, $"Stored in {dataFolder}.");
        var openFolder = new FluentButton("Open folder") { Margin = new Padding(0, 4, 8, 0) };
        openFolder.Click += (_, _) => OpenFolder(dataFolder);
        var reset = new FluentButton("Reset all settings...") { Margin = new Padding(0, 4, 0, 0) };
        reset.Click += (_, _) =>
        {
            resetRequested = true;
            CloseWith(DialogResult.OK);
        };
        var dataRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        dataRow.Controls.Add(openFolder);
        dataRow.Controls.Add(reset);
        content.Controls.Add(dataRow);

        var save = new FluentButton("Save", appearance: ButtonAppearance.Accent) { AutoSize = false, Size = new Size(150, 32), Margin = new Padding(0, 0, 8, 0) };
        var cancel = new FluentButton("Cancel") { AutoSize = false, Size = new Size(150, 32), Margin = new Padding(0) };
        save.Click += (_, _) => CloseWith(DialogResult.OK);
        cancel.Click += (_, _) => CloseWith(DialogResult.Cancel);
        var footer = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            AutoSize = true,
            Margin = new Padding(0),
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(24),
            BackColor = Theme.Current.Background,
        };
        footer.Controls.Add(cancel);
        footer.Controls.Add(save);

        // As in FluentDialog: a table sizes the dialog to its content.
        var root = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0) };
        root.Controls.Add(content, 0, 0);
        root.Controls.Add(footer, 0, 1);
        Controls.Add(root);
    }

    /// <summary>Shows the panel on <paramref name="settings"/> (a copy); returns <c>null</c> when cancelled.</summary>
    public static SettingsEdit? Show(IWin32Window owner, AppSettings settings, string dataFolder)
    {
        using var dialog = new SettingsDialog(settings, dataFolder);
        if (dialog.ShowDialog(owner) != DialogResult.OK)
        {
            return null;
        }

        return dialog.resetRequested ? SettingsEdit.Reset() : SettingsEdit.Save(dialog.Collect());
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        WindowChrome.Apply(this, Theme.Current);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape)
        {
            CloseWith(DialogResult.Cancel);
            e.Handled = true;
        }
    }

    private static void Section(TableLayoutPanel content, string title) =>
        content.Controls.Add(new Label { Text = title, Font = FluentFonts.BodyStrong, ForeColor = Theme.Current.Text, AutoSize = true, Margin = new Padding(0, 16, 0, 2) });

    private static void Note(TableLayoutPanel content, string text) =>
        content.Controls.Add(new Label
        {
            Text = text,
            Font = FluentFonts.Caption,
            ForeColor = Theme.Current.TextSecondary,
            AutoSize = true,
            MaximumSize = new Size(ContentWidth, 0),
            Margin = new Padding(2, 2, 0, 4),
        });

    private static void OpenFolder(string folder)
    {
        try
        {
            System.IO.Directory.CreateDirectory(folder);
            Process.Start("explorer.exe", "\"" + folder + "\"")?.Dispose();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or System.IO.IOException or UnauthorizedAccessException)
        {
            // Nothing to do: the path is shown in the panel.
        }
    }

    private AppSettings Collect()
    {
        settings.InvertPan = invertPan.Checked;
        settings.InvertTilt = invertTilt.Checked;
        settings.UvcContinuousMoves = continuousMoves.Checked;
        settings.BackgroundNumpad = backgroundNumpad.Checked;
        settings.AlwaysOnTop = alwaysOnTop.Checked;
        settings.CheckForUpdates = checkForUpdates.Checked;
        settings.Theme = (theme.SelectedItem as ThemeChoice)?.Value ?? AppTheme.System;
        return settings;
    }

    private void CloseWith(DialogResult result)
    {
        DialogResult = result;
        Close();
    }

    private sealed class ThemeChoice
    {
        public ThemeChoice(AppTheme value, string label)
        {
            Value = value;
            Label = label;
        }

        public AppTheme Value { get; }

        public string Label { get; }

        public override string ToString() => Label;
    }
}
