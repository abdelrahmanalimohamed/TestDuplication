using ItemDeduplication.Application.Common.Interfaces;
using ItemDeduplication.Infrastructure.Embeddings;
using ItemDeduplication.Infrastructure.Matching;
using ItemDeduplication.Infrastructure.Persistence;
using ItemDeduplication.Infrastructure.Vector;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ItemDeduplication.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // For MariaDB: replace UseSqlServer with UseMySql (Pomelo.EntityFrameworkCore.MySql).
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Stateless -> singleton is fine (you had Scoped; both work)
        services.AddSingleton<IItemNormalizationService, ItemNormalizationService>();
        services.AddSingleton<ITextMatcher, FuzzyTextMatcher>();

        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));
        services.Configure<QdrantOptions>(configuration.GetSection(QdrantOptions.SectionName));

        services.AddHttpClient<IEmbeddingService, HttpEmbeddingService>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
            client.BaseAddress = new Uri(o.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<IVectorStore, QdrantVectorStore>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
            client.BaseAddress = new Uri(o.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
            if (!string.IsNullOrWhiteSpace(o.ApiKey))
                client.DefaultRequestHeaders.Add("api-key", o.ApiKey);
        });

        return services;
    }
}
