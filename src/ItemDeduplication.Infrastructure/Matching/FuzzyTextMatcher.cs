using FuzzySharp;
using FuzzySharp.PreProcess;
using ItemDeduplication.Application.Common.Interfaces;

namespace ItemDeduplication.Infrastructure.Matching;

public sealed class FuzzyTextMatcher : ITextMatcher
{
    public double Score(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
        if (a == b) return 1;

        // IMPORTANT: FuzzySharp's default (PreprocessMode.Full) deletes every character that is not
        // a-z / A-Z / 0-9, which wipes out Arabic completely. Our ItemNormalizationService has already
        // cleaned the text, so we turn FuzzySharp's own preprocessing off.
        var sort = Fuzz.TokenSortRatio(a, b, PreprocessMode.None);
        var set = Fuzz.TokenSetRatio(a, b, PreprocessMode.None);

        // Token-sort handles word order, token-set handles extra words. Blend so that
        // "vacuum" alone does not fully match "vacuum cleaner 2000 w".
        return (0.6 * sort + 0.4 * set) / 100.0;
    }
}
