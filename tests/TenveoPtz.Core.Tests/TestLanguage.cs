using System.Runtime.CompilerServices;
using TenveoPtz.Core.Localization;
using TenveoPtz.Core.Settings;

namespace TenveoPtz.Core.Tests;

/// <summary>The tests read the messages in English; <see cref="Localization.FrenchTests"/> switches to French on its own.</summary>
internal static class TestLanguage
{
    [ModuleInitializer]
    internal static void UseEnglish() => Strings.Language = AppLanguage.English;
}
