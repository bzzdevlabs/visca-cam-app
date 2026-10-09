using System;

namespace TenveoPtz.Core.Ptz;

/// <summary>Value range of a camera axis as reported by the driver.</summary>
public sealed class AxisRange
{
    public AxisRange(int min, int max, int step, int defaultValue)
    {
        if (max < min)
        {
            throw new ArgumentException("max must be greater than or equal to min.", nameof(max));
        }

        Min = min;
        Max = max;
        Step = Math.Max(1, step);
        Default = Clamp(defaultValue);
    }

    public int Min { get; }

    public int Max { get; }

    public int Step { get; }

    public int Default { get; }

    public int Span => Max - Min;

    public int Clamp(int value) => Math.Max(Min, Math.Min(Max, value));
}
