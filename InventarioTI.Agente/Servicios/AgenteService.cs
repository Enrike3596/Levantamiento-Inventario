using InventarioTI.Agente.Api;
using InventarioTI.Agente.Sistema;
using Microsoft.Extensions.Logging;

namespace InventarioTI.Agente.Servicios;

public class AgenteService
{
    private readonly SystemInfoService _sistema;
    private readonly AgenteRegistrador _registrador;
    private readonly ApiClientService _api;
    private readonly ILogger<AgenteService> _logger;

    public AgenteService(
        SystemInfoService sistema,
        AgenteRegistrador registrador,
        ApiClientService api,
        ILogger<AgenteService> logger)
    {
        _sistema = sistema;
        _registrador = registrador;
        _api = api;
        _logger = logger;
    }

    public async Task EjecutarReporteAsync(CancellationToken ct)
    {
        var apiKey = await _registrador.ObtenerApiKeyAsync(ct);
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No se puede reportar: no hay API Key disponible.");
            return;
        }

        var info = _sistema.Recopilar();

        if (await _api.EnviarReporteAsync(info, apiKey, ct))
            _logger.LogInformation("Reporte enviado correctamente para '{equipo}'.", info.NombreEquipo);
        else
            _logger.LogError("No se pudo enviar el reporte para '{equipo}'.", info.NombreEquipo);
    }
}
