using ItemDeduplication.Application.Common.Models;

namespace ItemDeduplication.Application.Common.Interfaces;

public interface IItemNormalizationService
{
    NormalizedItem Normalize(string itemName);
}
