using System;
using System.Linq;
using TenveoPtz.Core.Presets;

namespace TenveoPtz.Core.Tests.Presets;

public sealed class PresetBookTests
{
    [Fact]
    public void Add_AllocatesLowestFreeSlot()
    {
        var book = new PresetBook();
        var first = book.Add("Pulpit");
        var second = book.Add("Choir");
        book.Remove(first);

        var third = book.Add("Wide");

        Assert.Equal(PresetBook.FirstSlot, third.Slot);
        Assert.Equal(PresetBook.FirstSlot + 1, second.Slot);
    }

    [Fact]
    public void Add_FailsWhenAllSlotsAreUsed()
    {
        var book = new PresetBook();
        for (var i = PresetBook.FirstSlot; i <= PresetBook.LastSlot; i++)
        {
            book.Add($"P{i}");
        }

        Assert.Throws<InvalidOperationException>(() => book.Add("One too many"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Add_RejectsBlankNames(string name)
    {
        Assert.Throws<ArgumentException>(() => new PresetBook().Add(name));
    }

    [Fact]
    public void Rename_TrimsName()
    {
        var book = new PresetBook();
        var preset = book.Add("A");

        book.Rename(preset, "  Lectern  ");

        Assert.Equal("Lectern", preset.Name);
    }

    [Fact]
    public void Move_ClampsToBounds()
    {
        var book = new PresetBook();
        var a = book.Add("A");
        book.Add("B");
        book.Add("C");

        book.Move(a, 10);

        Assert.Equal(new[] { "B", "C", "A" }, book.Items.Select(p => p.Name));
    }

    [Fact]
    public void At_ReturnsNullOutOfRange()
    {
        var book = new PresetBook();
        book.Add("A");

        Assert.Equal("A", book.At(0)?.Name);
        Assert.Null(book.At(1));
        Assert.Null(book.At(-1));
    }
}
