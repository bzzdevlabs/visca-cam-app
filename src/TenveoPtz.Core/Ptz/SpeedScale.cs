using System;

namespace TenveoPtz.Core.Ptz;

/// <summary>Maps a relative speed (0..1) onto a device specific integer range.</summary>
public static class SpeedScale
{
    public static int ToRange(double fraction, int min, int max)
    {
        if (max < min)
        {
            throw new ArgumentException("max must be greater than or equal to min.", nameof(max));
        }

        var clamped = Clamp(fraction);
        return min + (int)Math.Round(clamped * (max - min));
    }

    /// <summary>
    /// Ease-in curve for continuous speeds: the lower half of the slider gives fine, slow control
    /// and full speed is only reached at the top.
    /// </summary>
    public static double Ease(double fraction)
    {
        var clamped = Clamp(fraction);
        return clamped * clamped;
    }

    public static double Clamp(double fraction)
    {
        if (double.IsNaN(fraction))
        {
            return 0;
        }

        return Math.Max(0, Math.Min(1, fraction));
    }
}
