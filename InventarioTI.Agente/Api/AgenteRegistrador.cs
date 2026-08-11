using InventarioTI.Agente.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventarioTI.Agente.Api;

public class AgenteRegistrador
{
    private readonly IOptions<AgenteOptions> _opciones;
    private readonly ApiClientService _api;
    private readonly ILogger<AgenteRegistrador> _logger;
    private string? _apiKeyEnMemoria;

    public AgenteRegistrador(
        IOptions<AgenteOptions> opciones,
        ApiClientService api,
        ILogger<AgenteRegistrador> logger)
    {
        _opciones = opciones;
        _api = api;
        _logger = logger;
    }

    public async Task<string?> ObtenerApiKeyAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_apiKeyEnMemoria)) return _apiKeyEnMemoria;

        var opciones = _opciones.Value;

        if (!string.IsNullOrWhiteSpace(opciones.ApiKey))
        {
            _apiKeyEnMemoria = opciones.ApiKey.Trim();
            return _apiKeyEnMemoria;
        }

        var persistida = LeerClavePersistida();
        if (!string.IsNullOrWhiteSpace(persistida))
        {
            _apiKeyEnMemoria = persistida;
            _logger.LogInformation("API Key cargada del almacenamiento local.");
            return persistida;
        }

        if (opciones.AutoRegistro && !string.IsNullOrWhiteSpace(opciones.AdminEmail))
        {
            _logger.LogInformation("Intentando auto-registro del agente en {apiUrl}...", opciones.ApiUrl);
            var jwt = await _api.LoginAsync(opciones.AdminEmail, opciones.AdminPassword, ct);
            if (jwt is null)
            {
                _logger.LogWarning("No se pudo autenticar el administrador para el auto-registro.");
                return null;
            }

            var nombre = Environment.MachineName;
            var key = await _api.RegistrarAgenteAsync(nombre, jwt, ct);
            if (key is not null)
            {
                _apiKeyEnMemoria = key;
                GuardarClavePersistida(key);
                _logger.LogWarning(
                    "Agente auto-registrado como '{nombre}'. API Key persistida localmente: {key}",
                    nombre, key);
            }
            else
            {
                _logger.LogWarning(
                    "El auto-registro falló. Es posible que el agente '{nombre}' ya exista.", nombre);
            }
            return key;
        }

        _logger.LogWarning("No hay API Key configurada y el auto-registro está deshabilitado.");
        return null;
    }

    private static string RutaClavePersistida()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "InventarioTI", "Agente");
        return Path.Combine(dir, "api.key");
    }

    private string? LeerClavePersistida()
    {
        try
        {
            var ruta = RutaClavePersistida();
            return File.Exists(ruta) ? File.ReadAllText(ruta).Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo leer la API Key persistida.");
            return null;
        }
    }

    private void GuardarClavePersistida(string apiKey)
    {
        try
        {
            var ruta = RutaClavePersistida();
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            File.WriteAllText(ruta, apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo persistir la API Key. Se mantendrá solo en memoria.");
        }
    }
}
