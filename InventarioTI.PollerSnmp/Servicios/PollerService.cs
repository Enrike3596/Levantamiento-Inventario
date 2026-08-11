using System.Net;
using InventarioTI.PollerSnmp.Api;
using InventarioTI.PollerSnmp.Configuration;
using InventarioTI.PollerSnmp.Snmp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventarioTI.PollerSnmp.Servicios;

public class PollerService
{
    private readonly IOptions<PollerOptions> _opciones;
    private readonly AgenteRegistrador _registrador;
    private readonly ApiClientService _api;
    private readonly TraficoCache _trafico = new();
    private readonly ILogger<PollerService> _logger;

    public PollerService(IOptions<PollerOptions> opciones, AgenteRegistrador registrador, ApiClientService api, ILogger<PollerService> logger)
    {
        _opciones = opciones;
        _registrador = registrador;
        _api = api;
        _logger = logger;
    }

    public async Task EjecutarCicloAsync(CancellationToken ct)
    {
        var opciones = _opciones.Value;
        var apiKey = await _registrador.ObtenerApiKeyAsync(ct);
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Sin ApiKey válida; se omite el ciclo de sondeo.");
            return;
        }

        if (opciones.Switches.Count == 0)
        {
            _logger.LogWarning("No hay switches configurados en Poller:Switches.");
            return;
        }

        foreach (var ipTexto in opciones.Switches)
        {
            if (ct.IsCancellationRequested)
                break;

            if (!IPAddress.TryParse(ipTexto, out var ip))
            {
                _logger.LogWarning("IP de switch inválida: {ip}.", ipTexto);
                continue;
            }

            try
            {
                var servicio = new SnmpSwitchService(
                    ip, opciones.PuertoSnmp, opciones.Comunidad, opciones.Version,
                    Math.Max(500, opciones.TiempoEsperaSegundos * 1000),
                    _trafico, _logger);

                var reporte = servicio.Recopilar();
                if (reporte is null)
                {
                    _logger.LogWarning("No se obtuvo respuesta SNMP del switch {ip}.", ipTexto);
                    continue;
                }

                _logger.LogInformation(
                    "Switch {ip} sondeado: {marca} {modelo}, {n} puertos, {up} activos.",
                    ipTexto, reporte.Marca, reporte.Modelo, reporte.Puertos.Count,
                    reporte.Puertos.Count(p => p.Estado == "up"));

                var ok = await _api.EnviarReporteSwitchAsync(reporte, apiKey, ct);
                if (!ok)
                    _logger.LogError("No se pudo enviar el reporte del switch {ip}.", ipTexto);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error sondeando el switch {ip}.", ipTexto);
            }
        }

        _trafico.LimpiarViejos(DateTime.UtcNow, TimeSpan.FromHours(2));
    }
}
