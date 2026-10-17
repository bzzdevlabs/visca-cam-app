using System;
using TenveoPtz.Core.Presets;
using TenveoPtz.Core.Ptz;
using TenveoPtz.Core.Video;

namespace TenveoPtz.Core.Tests.Ptz;

public sealed class ValueTypesTests
{
    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(-0.5, 0)]
    [InlineData(0.5, 0.5)]
    [InlineData(2, 1)]
    public void SpeedScale_ClampsToTheUnitRange(double fraction, double expected)
    {
        Assert.Equal(expected, SpeedScale.Clamp(fraction));
    }

    [Fact]
    public void SpeedScale_EasesSlowSpeeds()
    {
        Assert.Equal(0.25, SpeedScale.Ease(0.5));
        Assert.Equal(1, SpeedScale.ToRange(0, 1, 24));
        Assert.Equal(24, SpeedScale.ToRange(1, 1, 24));
    }

    [Fact]
    public void Ranges_RejectAMaximumBelowTheMinimum()
    {
        Assert.Throws<ArgumentException>(() => SpeedScale.ToRange(0.5, 10, 1));
        Assert.Throws<ArgumentException>(() => new AxisRange(10, 1, 1, 5));
    }

    [Fact]
    public void Names_AreShownInLists()
    {
        Assert.Equal("Tenveo USB Camera", new VideoDevice(2, "Tenveo USB Camera").ToString());
        Assert.Equal(2, new VideoDevice(2, "Tenveo USB Camera").Index);
        Assert.Equal("Lectern", new Preset { Name = "Lectern" }.ToString());
    }
}
