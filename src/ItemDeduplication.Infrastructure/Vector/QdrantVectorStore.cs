using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ItemDeduplication.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace ItemDeduplication.Infrastructure.Vector;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";
    public string BaseUrl { get; set; } = "http://localhost:6333";
    public string Collection { get; set; } = "items";
    /// <summary>Must match the embedding model (paraphrase-multilingual-MiniLM-L12-v2 = 384).</summary>
    public int Dimensions { get; set; } = 384;
    public string? ApiKey { get; set; }
}

/// <summary>Qdrant over its REST API (no extra NuGet dependency).</summary>
public sealed class QdrantVectorStore : IVectorStore
{
    private readonly HttpClient _http;
    private readonly QdrantOptions _options;

    public QdrantVectorStore(HttpClient http, IOptions<QdrantOptions> options)
    {
        _http = http;
        _options = options.Value;
    }

    public async Task EnsureCollectionAsync(CancellationToken ct = default)
    {
        using var get = await _http.GetAsync($"/collections/{_options.Collection}", ct);
        if (get.IsSuccessStatusCode) return;
        if (get.StatusCode != HttpStatusCode.NotFound) get.EnsureSuccessStatusCode();

        var body = new { vectors = new { size = _options.Dimensions, distance = "Cosine" } };
        using var create = await _http.PutAsJsonAsync($"/collections/{_options.Collection}", body, ct);
        create.EnsureSuccessStatusCode();
    }

    public async Task UpsertAsync(IReadOnlyCollection<VectorPoint> points, CancellationToken ct = default)
    {
        if (points.Count == 0) return;

        var body = new
        {
            points = points.Select(p => new
            {
                id = p.AliasId.ToString(),
                vector = p.Vector,
                payload = new { masterItemId = p.MasterItemId.ToString() }
            }).ToList()
        };

        using var response = await _http.PutAsJsonAsync(
            $"/collections/{_options.Collection}/points?wait=true", body, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<VectorHit>> SearchAsync(
        float[] vector, int limit, double minScore, CancellationToken ct = default)
    {
        var body = new { vector, limit, score_threshold = minScore, with_payload = true };

        using var response = await _http.PostAsJsonAsync(
            $"/collections/{_options.Collection}/points/search", body, ct);

        // Collection not created yet == nothing indexed yet
        if (response.StatusCode == HttpStatusCode.NotFound) return Array.Empty<VectorHit>();
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken: ct);
        var hits = new List<VectorHit>();

        if (json?["result"] is not JsonArray results) return hits;

        foreach (var item in results)
        {
            var id = item?["id"]?.GetValue<string>();
            var master = item?["payload"]?["masterItemId"]?.GetValue<string>();
            var score = item?["score"]?.GetValue<double>();

            if (id is null || master is null || score is null) continue;
            hits.Add(new VectorHit(Guid.Parse(id), Guid.Parse(master), score.Value));
        }

        return hits;
    }
}
