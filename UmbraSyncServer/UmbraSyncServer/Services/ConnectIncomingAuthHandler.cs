using System.Security.Claims;
using System.Text.Encodings.Web;
using MareSynchronosShared.Utils.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MareSynchronosServer.Services;

// Auth scheme entrant pour les requêtes en provenance d'Ashfall Connect.
// Compare un Bearer token reçu en header au secret partagé `ConnectIncomingServiceToken`.
// Distinct du JWT utilisateur : ce scheme n'est utilisé que sur quelques endpoints
// (gestion des profils RP fédérés depuis Connect).
public sealed class ConnectIncomingAuthHandler : AuthenticationHandler<ConnectIncomingAuthOptions>
{
    public const string SchemeName = "ConnectIncoming";
    public const string CallerClaim = "ashfall:caller";
    public const string CallerValue = "connect";

    private readonly IOptionsMonitor<MareSynchronosShared.Utils.Configuration.ServerConfiguration> _config;

    public ConnectIncomingAuthHandler(
        IOptionsMonitor<ConnectIncomingAuthOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IOptionsMonitor<MareSynchronosShared.Utils.Configuration.ServerConfiguration> config)
        : base(options, loggerFactory, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var raw = authHeader.ToString();
        if (!raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = raw["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(AuthenticateResult.Fail("Token manquant"));

        var expected = _config.CurrentValue.ConnectIncomingServiceToken;
        if (string.IsNullOrEmpty(expected))
            return Task.FromResult(AuthenticateResult.Fail("ConnectIncomingServiceToken non configuré"));

        if (!CryptographicEquals(token, expected))
            return Task.FromResult(AuthenticateResult.Fail("Token Connect invalide"));

        var identity = new ClaimsIdentity(
            new[] { new Claim(CallerClaim, CallerValue) },
            SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}

public sealed class ConnectIncomingAuthOptions : AuthenticationSchemeOptions
{
}
