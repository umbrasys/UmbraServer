using System.Net.Http.Headers;
using System.Net.Http.Json;
using MareSynchronosShared.Utils.Configuration;
using Microsoft.Extensions.Options;

namespace MareSynchronosServer.Services;

/// Client HTTP vers Ashfall Connect (hub d'identité fédérée).
public sealed class ConnectClient
{
    private const string ServiceName = "umbra-sync";

    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<ServerConfiguration> _config;
    private readonly ILogger<ConnectClient> _logger;

    public ConnectClient(HttpClient httpClient, IOptionsMonitor<ServerConfiguration> config, ILogger<ConnectClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public bool IsConfigured =>
        _config.CurrentValue.ConnectBaseUrl is not null &&
        !string.IsNullOrEmpty(_config.CurrentValue.ConnectServiceToken);

    public async Task<GenerateLinkCodeResult> GenerateLinkCodeAsync(string uid, string? alias, IReadOnlyList<CharacterInfo>? characters, CancellationToken ct)
    {
        var cfg = _config.CurrentValue;
        if (cfg.ConnectBaseUrl is null || string.IsNullOrEmpty(cfg.ConnectServiceToken))
            throw new InvalidOperationException("Ashfall Connect n'est pas configuré (ConnectBaseUrl/ConnectServiceToken).");

        // Metadata transmise à Connect : pour UmbraSync, on inclut la liste des persos qui partagent la SecretKey utilisée par le user. Connect stocke ça en JSONB sur le LinkedAccount.
        object? metadata = characters is { Count: > 0 }
            ? new { characters = characters.Select(c => new { name = c.Name, world = c.World }).ToArray() }
            : null;

        var req = new HttpRequestMessage(HttpMethod.Post, new Uri(cfg.ConnectBaseUrl, "/api/v1/link-codes"))
        {
            Content = JsonContent.Create(new { identifier = uid, alias, metadata }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ConnectServiceToken);

        using var res = await _httpClient.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("Connect a refusé la génération de code (status {Status}) pour le service {Service}", (int)res.StatusCode, ServiceName);
            throw new HttpRequestException($"Connect a refusé la génération de code (HTTP {(int)res.StatusCode}).");
        }

        var body = await res.Content.ReadFromJsonAsync<CreateCodeResponse>(cancellationToken: ct);
        if (body is null || string.IsNullOrEmpty(body.Code))
            throw new InvalidOperationException("Réponse vide de Connect lors de la génération de code.");

        return new GenerateLinkCodeResult(body.Code, body.ExpiresAt);
    }

    public async Task<VerificationResult?> GetVerificationAsync(string uid, CancellationToken ct)
    {
        var cfg = _config.CurrentValue;
        if (cfg.ConnectBaseUrl is null || string.IsNullOrEmpty(cfg.ConnectServiceToken))
            return null;

        var req = new HttpRequestMessage(HttpMethod.Get, new Uri(cfg.ConnectBaseUrl, $"/api/v1/verification/{ServiceName}/{Uri.EscapeDataString(uid)}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ConnectServiceToken);

        try
        {
            using var res = await _httpClient.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<VerificationResult>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Échec de l'interrogation de Connect (verification)");
            return null;
        }
    }
    
    public async Task<bool> PushMetadataAsync(string uid, string? alias, IReadOnlyList<CharacterInfo>? characters, CancellationToken ct)
    {
        var cfg = _config.CurrentValue;
        if (cfg.ConnectBaseUrl is null || string.IsNullOrEmpty(cfg.ConnectServiceToken))
            return false;

        object? metadata = characters is { Count: > 0 }
            ? new { characters = characters.Select(c => new { name = c.Name, world = c.World }).ToArray() }
            : null;

        var req = new HttpRequestMessage(HttpMethod.Post,
            new Uri(cfg.ConnectBaseUrl, $"/api/v1/links/{ServiceName}/{Uri.EscapeDataString(uid)}/metadata"))
        {
            Content = JsonContent.Create(new { alias, metadata }),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ConnectServiceToken);

        try
        {
            using var res = await _httpClient.SendAsync(req, ct);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return false;
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Connect a refusé le push de metadata (status {Status})", (int)res.StatusCode);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Échec du push de metadata vers Connect");
            return false;
        }
    }

    public async Task<LinkCodeStatus?> GetLinkCodeStatusAsync(string code, CancellationToken ct)
    {
        var cfg = _config.CurrentValue;
        if (cfg.ConnectBaseUrl is null || string.IsNullOrEmpty(cfg.ConnectServiceToken))
            return null;

        var encoded = Uri.EscapeDataString(code);
        var req = new HttpRequestMessage(HttpMethod.Get, new Uri(cfg.ConnectBaseUrl, $"/api/v1/link-codes/{encoded}/status"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.ConnectServiceToken);

        try
        {
            using var res = await _httpClient.SendAsync(req, ct);
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound)
                return new LinkCodeStatus("not_found", null, null, null);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<LinkCodeStatus>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Échec de l'interrogation de Connect (link-status)");
            return null;
        }
    }

    public sealed record GenerateLinkCodeResult(string Code, DateTimeOffset ExpiresAt);

    public sealed record CharacterInfo(string Name, string World);

    private sealed record CreateCodeResponse(string Code, DateTimeOffset ExpiresAt);

    public sealed record VerificationResult(bool Verified, string? Level, DateTimeOffset? Since);

    public sealed record LinkCodeStatus(string Status, string? LinkedTo, DateTimeOffset? ExpiresAt, DateTimeOffset? ConsumedAt);
}
