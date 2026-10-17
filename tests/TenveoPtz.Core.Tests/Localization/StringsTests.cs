using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Tests.Localization;

/// <summary>Every text exists in both languages, and the French one is actually translated.</summary>
[Collection(nameof(FrenchTests))]
public sealed class StringsTests : IDisposable
{
    /// <summary>Texts that read the same in both languages.</summary>
    private static readonly HashSet<string> SameInBothLanguages = new()
    {
        nameof(Strings.Ok),
        nameof(Strings.Zoom),
        nameof(Strings.NameOf),
        nameof(Strings.AxisName),
    };

    public void Dispose() => Strings.Language = AppLanguage.English;

    public static IEnumerable<object[]> Texts() =>
        typeof(Strings).GetMembers(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m is PropertyInfo { PropertyType: var t } && t == typeof(string)
                     || m is MethodInfo { ReturnType: var r, IsSpecialName: false } && r == typeof(string))
            .Select(m => new object[] { m.Name });

    [Theory]
    [MemberData(nameof(Texts))]
    public void Text_IsWrittenInBothLanguages(string name)
    {
        var english = Read(name, AppLanguage.English);
        var french = Read(name, AppLanguage.French);

        Assert.False(string.IsNullOrWhiteSpace(english));
        Assert.False(string.IsNullOrWhiteSpace(french));
        if (!SameInBothLanguages.Contains(name))
        {
            Assert.NotEqual(english, french);
        }
    }

    [Fact]
    public void Texts_CoverEveryPublicMember()
    {
        // Guards the reflection above: a renamed or retyped member would silently drop out.
        Assert.True(Texts().Count() > 150);
    }

    [Theory]
    [InlineData("Pan", "pan", "rotation")]
    [InlineData("Tilt", "tilt", "inclinaison")]
    [InlineData("Zoom", "zoom", "zoom")]
    [InlineData("Focus", "focus", "mise au point")]
    [InlineData("Iris", "iris", "iris")]
    public void AxisName_IsTranslated(string axis, string english, string french)
    {
        Assert.Equal(english, Read(nameof(Strings.AxisName), AppLanguage.English, axis));
        Assert.Equal(french, Read(nameof(Strings.AxisName), AppLanguage.French, axis));
    }

    [Theory]
    [InlineData(AppLanguage.English, "Restart Tenveo PTZ now to show it in English?", "Redémarrer Tenveo PTZ maintenant pour l'afficher en anglais ?")]
    [InlineData(AppLanguage.French, "Restart Tenveo PTZ now to show it in French?", "Redémarrer Tenveo PTZ maintenant pour l'afficher en français ?")]
    public void RestartForLanguage_NamesTheNewLanguage(AppLanguage target, string english, string french)
    {
        Strings.Language = AppLanguage.English;
        Assert.Equal(english, Strings.RestartForLanguage(target));
        Strings.Language = AppLanguage.French;
        Assert.Equal(french, Strings.RestartForLanguage(target));
    }

    [Fact]
    public void Connected_MentionsOtherAppsOnlyWithoutPreview()
    {
        Strings.Language = AppLanguage.English;

        Assert.Equal("Connected: Cam, UVC (USB).", Strings.Connected("Cam, UVC (USB)", preview: true));
        Assert.Contains("other apps such as Zoom", Strings.Connected("Cam", preview: false));
    }

    [Fact]
    public void CannotOpenCamera_HasAFallbackReason()
    {
        Strings.Language = AppLanguage.English;

        Assert.Equal("Cannot open \"Cam\": unknown error.", Strings.CannotOpenCamera("Cam", null));
        Assert.Equal("Cannot open \"Cam\": busy.", Strings.CannotOpenCamera("Cam", "busy"));
    }

    /// <summary>Reads a text in <paramref name="language"/>, with sample arguments for the formatted ones.</summary>
    private static string Read(string name, AppLanguage language, string? axis = null)
    {
        Strings.Language = language;
        var member = typeof(Strings).GetMember(name, BindingFlags.Public | BindingFlags.Static).Single();
        if (member is PropertyInfo property)
        {
            return (string)property.GetValue(null)!;
        }

        var method = (MethodInfo)member;
        var arguments = method.GetParameters().Select(p => Sample(p.ParameterType, axis)).ToArray();
        return (string)method.Invoke(null, arguments)!;
    }

    private static object? Sample(Type type, string? axis) =>
        type == typeof(string) ? axis ?? "sample"
        : type == typeof(int) ? 3
        : type == typeof(bool) ? false
        : type == typeof(Version) ? new Version(1, 2, 3)
        : type == typeof(Uri) ? new Uri("https://example.com/release")
        : type == typeof(AppLanguage) ? AppLanguage.English
        : throw new NotSupportedException($"No sample value for {type}.");
}
