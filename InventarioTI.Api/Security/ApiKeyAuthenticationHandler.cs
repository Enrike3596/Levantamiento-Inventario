using System.Security.Claims;
using System.Text.Encodings.Web;
using InventarioTI.Api.Helpers;
using InventarioTI.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InventarioTI.Api.Security;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly AppDbContext _db;
    private readonly string _headerName;

    public const string SchemeName = "ApiKey";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _db = db;
        _headerName = configuration["ApiKey:HeaderName"] ?? "X-Api-Key";
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(_headerName, out var value))
            return AuthenticateResult.NoResult();

        var apiKey = value.ToString();
        if (string.IsNullOrWhiteSpace(apiKey))
            return AuthenticateResult.NoResult();

        var hash = ApiKeyHelper.Hash(apiKey);
        var agente = await _db.AgentesRegistrados
            .FirstOrDefaultAsync(a => a.ApiKeyHash == hash);

        if (agente is null || !agente.Activo)
            return AuthenticateResult.Fail("API Key inválida o agente inactivo.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, agente.NombreEquipo),
            new(ClaimTypes.Role, "Agente"),
            new("Tipo", "Agente"),
            new("AgenteId", agente.Id.ToString())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
