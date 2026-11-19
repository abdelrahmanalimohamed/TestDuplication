using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ItemDeduplication.Application.Common.Interfaces;

namespace ItemDeduplication.Infrastructure.Embeddings;

public sealed class EmbeddingOptions
{
    public const string SectionName = "Embedding";
    public string BaseUrl { get; set; } = "http://localhost:8000";
}

/// <summary>Calls the Python sentence-transformers sidecar (see /embedding-service).</summary>
public sealed class HttpEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _http;

    public HttpEmbeddingService(HttpClient http) => _http = http;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        var result = await EmbedBatchAsync(new[] { text }, ct);
        return result[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        if (texts.Count == 0) return Array.Empty<float[]>();

        using var response = await _http.PostAsJsonAsync("/embed", new EmbedRequest(texts), ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<EmbedResponse>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Empty response from embedding service.");

        if (body.Embeddings.Count != texts.Count)
            throw new InvalidOperationException("Embedding service returned an unexpected number of vectors.");

        return body.Embeddings;
    }

    private sealed record EmbedRequest([property: JsonPropertyName("texts")] IReadOnlyList<string> Texts);

    private sealed record EmbedResponse([property: JsonPropertyName("embeddings")] List<float[]> Embeddings);
}
