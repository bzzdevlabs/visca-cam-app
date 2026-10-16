using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TenveoPtz.App.Views.Controls;
using TenveoPtz.App.Views.Theming;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Presentation;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.App.Views;

/// <summary>
/// Settings panel: camera directions and move mode, keyboard, window and language, updates, the
/// data folder and a reset of every setting. Edits a copy of the settings; the presenter applies the result.
/// </summary>
internal sealed class SettingsDialog : Form
{
    private const int ContentWidth = 440;

    private readonly AppSettings settings;
    private readonly ToggleSwitch invertPan = new(Strings.ReverseLeftRight);
    private readonly ToggleSwitch invertTilt = new(Strings.ReverseUpDown);
    private readonly ToggleSwitch continuousMoves = new(Strings.ContinuousUsbMoves);
    private readonly ToggleSwitch backgroundNumpad = new(Strings.BackgroundNumpad);
    private readonly ToggleSwitch alwaysOnTop = new(Strings.KeepWindowOnTop);
    private readonly DropDownSelector theme = new(200, Strings.Theme);
    private readonly DropDownSelector language = new(200, Strings.LanguageLabel);
    private readonly ToggleSwitch checkForUpdates = new(Strings.CheckForUpdates);
    private bool resetRequested;

    private SettingsDialog(AppSettings settings, string dataFolder)
    {
        this.settings = settings;
        Text = Strings.Settings;
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
        theme.SetItems(new object[]
        {
            new Choice<AppTheme>(AppTheme.System, Strings.ThemeSameAsSystem("Windows")),
            new Choice<AppTheme>(AppTheme.Light, Strings.ThemeLight),
            new Choice<AppTheme>(AppTheme.Dark, Strings.ThemeDark),
        });
        theme.SelectedItem = theme.Items.OfType<Choice<AppTheme>>().First(c => c.Value == settings.Theme);
        language.SetItems(new object[] { Language(AppLanguage.French), Language(AppLanguage.English) });
        language.SelectedItem = language.Items.OfType<Choice<AppLanguage>>().First(c => c.Value == settings.Language);

        var content = new TableLayoutPanel { ColumnCount = 1, AutoSize = true, Padding = new Padding(24, 20, 24, 16), Margin = new Padding(0) };
        content.Controls.Add(new Label { Text = Strings.Settings, Font = FluentFonts.Subtitle, ForeColor = Theme.Current.Text, AutoSize = true, Margin = new Padding(0, 0, 0, 8) });

        Section(content, Strings.Camera);
        content.Controls.Add(invertPan);
        content.Controls.Add(invertTilt);
        content.Controls.Add(continuousMoves);
        Note(content, Strings.ContinuousUsbMovesNote);

        Section(content, Strings.Keyboard);
        content.Controls.Add(backgroundNumpad);
        Note(content, Strings.BackgroundNumpadNote);

        Section(content, Strings.Window);
        content.Controls.Add(alwaysOnTop);
        var choices = new TableLayoutPanel { AutoSize = true, ColumnCount = 2, Margin = new Padding(0, 4, 0, 0) };
        choices.Controls.Add(ChoiceLabel(Strings.Theme), 0, 0);
        choices.Controls.Add(theme, 1, 0);
        choices.Controls.Add(ChoiceLabel(Strings.LanguageLabel), 0, 1);
        choices.Controls.Add(language, 1, 1);
        content.Controls.Add(choices);
        Note(content, Strings.LanguageNote);

        Section(content, Strings.Updates);
        content.Controls.Add(checkForUpdates);

        Section(content, Strings.SettingsAndPresets);
        Note(content, Strings.StoredIn(dataFolder));
        var openFolder = new FluentButton(Strings.OpenFolder) { Margin = new Padding(0, 4, 8, 0) };
        openFolder.Click += (_, _) => OpenFolder(dataFolder);
        var reset = new FluentButton(Strings.ResetAllSettings) { Margin = new Padding(0, 4, 0, 0) };
        reset.Click += (_, _) =>
        {
            resetRequested = true;
            CloseWith(DialogResult.OK);
        };
        var dataRow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
        dataRow.Controls.Add(openFolder);
        dataRow.Controls.Add(reset);
        content.Controls.Add(dataRow);

        var save = new FluentButton(Strings.Save, appearance: ButtonAppearance.Accent) { AutoSize = false, Size = new Size(150, 32), Margin = new Padding(0, 0, 8, 0) };
        var cancel = new FluentButton(Strings.Cancel) { AutoSize = false, Size = new Size(150, 32), Margin = new Padding(0) };
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

    private static Label ChoiceLabel(string text) =>
        new() { Text = text, AutoSize = true, ForeColor = Theme.Current.Text, Anchor = AnchorStyles.Left, Margin = new Padding(2, 4, 12, 4) };

    /// <summary>Languages are listed by their own name, whatever the current language.</summary>
    private static Choice<AppLanguage> Language(AppLanguage value) => new(value, Strings.NameOf(value));

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
        settings.Theme = (theme.SelectedItem as Choice<AppTheme>)?.Value ?? AppTheme.System;
        settings.Language = (language.SelectedItem as Choice<AppLanguage>)?.Value ?? settings.Language;
        return settings;
    }

    private void CloseWith(DialogResult result)
    {
        DialogResult = result;
        Close();
    }
}
