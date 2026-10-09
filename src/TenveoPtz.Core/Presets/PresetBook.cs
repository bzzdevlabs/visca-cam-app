using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace TenveoPtz.Core.Presets;

/// <summary>Ordered collection of presets with camera memory slot allocation.</summary>
[XmlRoot("Presets")]
public sealed class PresetBook
{
    /// <summary>First VISCA memory slot handed out.</summary>
    public const int FirstSlot = 1;

    /// <summary>
    /// Last VISCA memory slot handed out. Slots from 90 upward are avoided because many PTZ
    /// cameras reserve them for special functions (for example, recalling 95 opens the OSD menu).
    /// </summary>
    public const int LastSlot = 89;

    [XmlElement("Preset")]
    public List<Preset> Items { get; set; } = new();

    public Preset Add(string name)
    {
        var preset = new Preset { Name = ValidateName(name), Slot = AllocateSlot() };
        Items.Add(preset);
        return preset;
    }

    public void Rename(Preset preset, string name)
    {
        EnsureContains(preset);
        preset.Name = ValidateName(name);
    }

    public void Remove(Preset preset)
    {
        EnsureContains(preset);
        Items.Remove(preset);
    }

    /// <summary>Moves <paramref name="preset"/> by <paramref name="offset"/> positions, clamped to the list bounds.</summary>
    public void Move(Preset preset, int offset)
    {
        EnsureContains(preset);
        var index = Items.IndexOf(preset);
        var target = Math.Max(0, Math.Min(Items.Count - 1, index + offset));
        Items.RemoveAt(index);
        Items.Insert(target, preset);
    }

    /// <summary>Returns the preset at <paramref name="index"/>, or <c>null</c> when out of range.</summary>
    public Preset? At(int index) => index >= 0 && index < Items.Count ? Items[index] : null;

    private static string ValidateName(string name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("A preset name is required.", nameof(name));
        }

        return trimmed;
    }

    private int AllocateSlot()
    {
        var used = new HashSet<int>(Items.Select(p => p.Slot));
        for (var slot = FirstSlot; slot <= LastSlot; slot++)
        {
            if (!used.Contains(slot))
            {
                return slot;
            }
        }

        throw new InvalidOperationException($"All {LastSlot - FirstSlot + 1} preset slots are in use.");
    }

    private void EnsureContains(Preset preset)
    {
        if (preset is null || !Items.Contains(preset))
        {
            throw new ArgumentException("The preset does not belong to this collection.", nameof(preset));
        }
    }
}
