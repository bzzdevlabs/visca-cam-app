using System.Drawing;
using TenveoPtz.App.Views.Theming;

namespace TenveoPtz.App.Views.Controls;

/// <summary>Paints the background of Fluent buttons and returns the matching foreground colour.</summary>
internal static class ButtonChrome
{
    public const float CornerRadius = 4F;

    public static Color Paint(Graphics graphics, RectangleF bounds, float scale, ButtonAppearance appearance, VisualState state)
    {
        var colors = Theme.Current;
        var radius = CornerRadius * scale;
        switch (appearance)
        {
            case ButtonAppearance.Accent when state.Enabled:
                graphics.FillRounded(state.Pressed ? colors.AccentPressed : state.Hovered ? colors.AccentHover : colors.Accent, bounds, radius);
                return state.Pressed ? Palette.Blend(colors.OnAccent, colors.Accent, 0.3) : colors.OnAccent;

            case ButtonAppearance.Subtle:
                var overlay = !state.Enabled ? Color.Transparent
                    : state.Pressed || state.Checked ? colors.SubtlePressed
                    : state.Hovered ? colors.SubtleHover : Color.Transparent;
                graphics.FillRounded(overlay, bounds, radius);
                if (state.Checked && state.Hovered && !state.Pressed)
                {
                    graphics.FillRounded(colors.SubtleHover, bounds, radius);
                }

                return !state.Enabled ? colors.TextDisabled : state.Checked ? colors.Accent : colors.Text;

            default:
                var fill = !state.Enabled ? colors.ControlFillDisabled
                    : state.Pressed ? colors.ControlFillPressed
                    : state.Hovered ? colors.ControlFillHover : colors.ControlFill;
                graphics.FillRounded(fill, bounds, radius);
                graphics.DrawRounded(colors.ControlBorder, bounds, radius);
                return !state.Enabled ? colors.TextDisabled : state.Pressed ? colors.TextSecondary : colors.Text;
        }
    }

    public static void PaintFocus(Graphics graphics, RectangleF bounds, float scale)
    {
        graphics.DrawRounded(Theme.Current.Text, bounds, (CornerRadius + 1) * scale, 2F * scale);
    }

    internal readonly struct VisualState
    {
        public VisualState(bool enabled, bool hovered, bool pressed, bool isChecked)
        {
            Enabled = enabled;
            Hovered = hovered;
            Pressed = pressed;
            Checked = isChecked;
        }

        public bool Enabled { get; }

        public bool Hovered { get; }

        public bool Pressed { get; }

        public bool Checked { get; }
    }
}
