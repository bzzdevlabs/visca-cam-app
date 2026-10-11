using TenveoPtz.Core.Diagnostics;

namespace TenveoPtz.Core.Tests.Diagnostics;

public sealed class HexBytesTests
{
    [Fact]
    public void Format_UsesUppercasePairsSeparatedBySpaces()
    {
        Assert.Equal("81 01 06 04 FF", HexBytes.Format(new byte[] { 0x81, 0x01, 0x06, 0x04, 0xFF }));
    }

    [Theory]
    [InlineData("81 01 06 04 FF")]
    [InlineData("810106 04ff")]
    [InlineData("0x81,0x01, 0x06;0x04 0xFF")]
    [InlineData("81 1 6 4 FF")]
    public void TryParse_AcceptsCommonSpellings(string text)
    {
        Assert.Equal(new byte[] { 0x81, 0x01, 0x06, 0x04, 0xFF }, HexBytes.TryParse(text));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("81 0G")]
    [InlineData("hello")]
    public void TryParse_RejectsInvalidText(string? text)
    {
        Assert.Null(HexBytes.TryParse(text));
    }
}
