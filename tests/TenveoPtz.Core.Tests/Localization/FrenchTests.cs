using System;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Tests.Localization;

/// <summary>
/// Runs on its own, after the other tests: the language is global, and the other tests read the
/// messages in English.
/// </summary>
[CollectionDefinition(nameof(FrenchTests), DisableParallelization = true)]
public sealed class FrenchCollection
{
}

[Collection(nameof(FrenchTests))]
public sealed class FrenchTests : IDisposable
{
    public FrenchTests() => Strings.Language = AppLanguage.French;

    public void Dispose() => Strings.Language = AppLanguage.English;

    [Fact]
    public void French_IsTheDefaultLanguage()
    {
        Assert.Equal(AppLanguage.French, new AppSettings().Language);
    }

    [Fact]
    public void Texts_AreInFrench()
    {
        Assert.Equal("Connecter", Strings.Connect);
        Assert.Equal("Position « Pupitre » enregistrée.", Strings.SavedPreset("Pupitre"));
    }

    [Fact]
    public void French_KeepsPunctuationWithItsWord()
    {
        Assert.Equal("Confirmer ?", Strings.AreYouSure);
        Assert.Equal("Nouveau nom :", Strings.RenamePresetPrompt);
    }

    [Fact]
    public void Languages_AreListedByTheirOwnName()
    {
        Assert.Equal("Français", Strings.NameOf(AppLanguage.French));
        Assert.Equal("English", Strings.NameOf(AppLanguage.English));
    }

    [Fact]
    public void English_IsAvailable()
    {
        Strings.Language = AppLanguage.English;

        Assert.Equal("Connect", Strings.Connect);
        Assert.Equal("Saved preset \"Lectern\".", Strings.SavedPreset("Lectern"));
    }
}
