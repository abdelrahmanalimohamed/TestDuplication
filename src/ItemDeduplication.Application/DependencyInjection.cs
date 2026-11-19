using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Application.Items;
using ItemDeduplication.Application.Matching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ItemDeduplication.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MatchingOptions>(configuration.GetSection(MatchingOptions.SectionName));

        services.AddSingleton<IAttributeExtractor, AttributeExtractor>();
        services.AddSingleton<ScoreCalculator>();

        services.AddScoped<IMatchingEngine, MatchingEngine>();
        services.AddScoped<IAliasIndexer, AliasIndexer>();
        services.AddScoped<IItemIntakeService, ItemIntakeService>();
        services.AddScoped<IDuplicateReviewService, DuplicateReviewService>();

        return services;
    }
}
