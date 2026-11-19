using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Application.Common.Models;

namespace ItemDeduplication.Infrastructure.Matching;

public sealed class ItemNormalizationService : IItemNormalizationService
{
    public NormalizedItem Normalize(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return new NormalizedItem(
                itemName ?? string.Empty,
                string.Empty,
                Array.Empty<string>());
        }

        var normalized = ConvertDigits(itemName);
        normalized = NormalizeArabic(normalized);
        normalized = normalized.ToLowerInvariant();
        normalized = RemoveDiacritics(normalized);
        normalized = RemoveTatweel(normalized);
        normalized = NormalizePressureRating(normalized); // NEW: PN10 / PN-10 -> 10 bar
        normalized = SeparateDimensionX(normalized);      // NEW: 60x40 -> 60 40
        normalized = NormalizeCharacters(normalized);     // CHANGED: keeps 2.5 as one number
        normalized = SeparateDigitsFromLetters(normalized); // NEW: 60cm -> 60 cm
        normalized = NormalizeWhitespace(normalized);

        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new NormalizedItem(itemName, normalized, tokens);
    }

    private static string NormalizeArabic(string value)
    {
        return value
            .Replace('أ', 'ا')
            .Replace('إ', 'ا')
            .Replace('آ', 'ا')
            .Replace('ٱ', 'ا')
            .Replace('ى', 'ي');
    }

    private static string ConvertDigits(string value)
    {
        var result = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (character == '٫') // Arabic decimal separator
            {
                result.Append('.');
                continue;
            }

            var digit = CharUnicodeInfo.GetDecimalDigitValue(character);
            result.Append(digit >= 0 ? (char)('0' + digit) : character);
        }

        return result.ToString();
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            if (category != UnicodeCategory.NonSpacingMark &&
                category != UnicodeCategory.SpacingCombiningMark &&
                category != UnicodeCategory.EnclosingMark)
            {
                result.Append(character);
            }
        }

        return result.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string RemoveTatweel(string value) => value.Replace("\u0640", string.Empty);

    // PN10 is the nominal pressure rating in bar, so "PN10" and "10 bar" mean the same thing.
    private static string NormalizePressureRating(string value)
        => Regex.Replace(value, @"(?<![\p{L}\d])pn[\s\-]*(\d+(?:\.\d+)?)", "$1 bar");

    private static string SeparateDimensionX(string value)
        => Regex.Replace(value, @"(?<=\d)x(?=\d)", " ");

    private static string NormalizeCharacters(string value)
    {
        var result = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (char.IsLetterOrDigit(c))
            {
                result.Append(c);
            }
            else if (c == '.' && i > 0 && i < value.Length - 1 &&
                     char.IsDigit(value[i - 1]) && char.IsDigit(value[i + 1]))
            {
                result.Append('.'); // decimal point inside a number
            }
            else
            {
                result.Append(' '); // punctuation and symbols are word separators
            }
        }

        return result.ToString();
    }

    private static string SeparateDigitsFromLetters(string value)
    {
        var result = new StringBuilder(value.Length + 4);

        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsDigit(value[i - 1]) && char.IsLetter(value[i]))
                result.Append(' ');

            result.Append(value[i]);
        }

        return result.ToString();
    }

    private static string NormalizeWhitespace(string value)
        => string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
