using InventarioTI.PollerSnmp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventarioTI.PollerSnmp.Api;

public class AgenteRegistrador
{
    private readonly IOptions<PollerOptions> _opciones;
    private readonly ApiClientService _api;
    private readonly ILogger<AgenteRegistrador> _logger;

    public AgenteRegistrador(IOptions<PollerOptions> opciones, ApiClientService api, ILogger<AgenteRegistrador> logger)
    {
        _opciones = opciones;
        _api = api;
        _logger = logger;
    }

    private string CarpetaClaves => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "InventarioTI", "PollerSnmp");

    private string RutaClave => Path.Combine(CarpetaClaves, "api.key");

    public async Task<string?> ObtenerApiKeyAsync(CancellationToken ct)
    {
        var opciones = _opciones.Value;

        if (!string.IsNullOrWhiteSpace(opciones.ApiKey))
            return opciones.ApiKey.Trim();

        if (File.Exists(RutaClave))
        {
            var guardada = (await File.ReadAllTextAsync(RutaClave, ct)).Trim();
            if (!string.IsNullOrWhiteSpace(guardada))
                return guardada;
        }

        if (!opciones.AutoRegistro)
        {
            _logger.LogWarning("No hay ApiKey configurada y AutoRegistro está desactivado.");
            return null;
        }

        return await RegistrarAsync(ct);
    }

    private async Task<string?> RegistrarAsync(CancellationToken ct)
    {
        var opciones = _opciones.Value;
        if (string.IsNullOrWhiteSpace(opciones.AdminEmail) || string.IsNullOrWhiteSpace(opciones.AdminPassword))
        {
            _logger.LogWarning("Faltan credenciales de administrador (Poller:AdminEmail/AdminPassword) para el auto-registro.");
            return null;
        }

        var token = await _api.LoginAsync(opciones.AdminEmail, opciones.AdminPassword, ct);
        if (token is null)
            return null;

        var apiKey = await _api.RegistrarAgenteAsync(token, opciones.NombreAgente, ct);
        if (apiKey is null)
            return null;

        Directory.CreateDirectory(CarpetaClaves);
        await File.WriteAllTextAsync(RutaClave, apiKey, ct);
        _logger.LogInformation("Agente {nombre} registrado; ApiKey persistida en {ruta}.", opciones.NombreAgente, RutaClave);
        return apiKey;
    }
}
