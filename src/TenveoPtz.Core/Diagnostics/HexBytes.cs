using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TenveoPtz.Core.Diagnostics;

/// <summary>Formats and parses bytes as space-separated hexadecimal, e.g. <c>81 01 06 04 FF</c>.</summary>
public static class HexBytes
{
    public static string Format(IReadOnlyList<byte> bytes)
    {
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        var text = new StringBuilder(bytes.Count * 3);
        for (var i = 0; i < bytes.Count; i++)
        {
            if (i > 0)
            {
                text.Append(' ');
            }

            text.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
        }

        return text.ToString();
    }

    /// <summary>
    /// Parses hexadecimal bytes separated by spaces, commas or nothing ("8101", "81 01", "0x81,0x01").
    /// Returns <c>null</c> when the text is empty or not valid hexadecimal.
    /// </summary>
    public static byte[]? TryParse(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var digits = new StringBuilder(text.Length);
        foreach (var token in text.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var value = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? token.Substring(2) : token;
            if (value.Length % 2 != 0)
            {
                value = "0" + value; // "1" in "81 1 FF" means 0x01.
            }

            digits.Append(value);
        }

        if (digits.Length == 0)
        {
            return null;
        }

        var bytes = new byte[digits.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            if (!byte.TryParse(digits.ToString(2 * i, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out bytes[i]))
            {
                return null;
            }
        }

        return bytes;
    }
}
