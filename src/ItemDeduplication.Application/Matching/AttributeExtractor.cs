using System.Globalization;
using System.Text.RegularExpressions;
using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Application.Common.Models;

namespace ItemDeduplication.Application.Matching;

/// <summary>
/// Pulls "number + unit" pairs out of normalized names so "60 cm" and "٦٠ سم" both become length=600 mm.
/// Two items with the same text but a different attribute (60 cm vs 80 cm) are NOT duplicates.
/// </summary>
public sealed class AttributeExtractor : IAttributeExtractor
{
    private static readonly Regex Pattern = new(
        @"(?<![\p{L}\d.])(\d+(?:\.\d+)?)\s*(\p{L}+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IReadOnlyList<ItemAttribute> Extract(string normalizedName)
    {
        var result = new List<ItemAttribute>();
        if (string.IsNullOrWhiteSpace(normalizedName)) return result;

        foreach (Match match in Pattern.Matches(normalizedName))
        {
            if (!UnitCatalog.TryGet(match.Groups[2].Value, out var unit)) continue;

            var number = decimal.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            result.Add(new ItemAttribute(unit.Type, number * unit.Factor));
        }

        return result;
    }

    public AttributeComparison Compare(IReadOnlyList<ItemAttribute> a, IReadOnlyList<ItemAttribute> b)
    {
        var matches = 0;
        var conflicts = 0;

        foreach (var group in a.GroupBy(x => x.Type))
        {
            var other = b.Where(x => x.Type == group.Key).Select(x => x.Value).ToList();
            if (other.Count == 0) continue; // attribute only on one side -> neutral

            var left = group.Select(x => x.Value).ToList();
            var (small, large) = left.Count <= other.Count ? (left, other) : (other, left);

            var pool = new List<decimal>(large);
            var allMatched = true;

            foreach (var value in small)
            {
                var index = pool.FindIndex(p => AreClose(p, value));
                if (index < 0) { allMatched = false; break; }
                pool.RemoveAt(index);
            }

            if (allMatched) matches++; else conflicts++;
        }

        return new AttributeComparison(matches, conflicts);
    }

    private static bool AreClose(decimal x, decimal y)
    {
        var max = Math.Max(Math.Abs(x), Math.Abs(y));
        return max == 0 || Math.Abs(x - y) / max <= 0.01m;
    }
}
