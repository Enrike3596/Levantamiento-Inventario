using InventarioTI.Agente.Configuration;
using InventarioTI.Agente.Servicios;
using Microsoft.Extensions.Options;

namespace InventarioTI.Agente.Workers;

public class AgenteWorker : BackgroundService
{
    private readonly AgenteService _servicio;
    private readonly IOptions<AgenteOptions> _opciones;
    private readonly ILogger<AgenteWorker> _logger;

    public AgenteWorker(
        AgenteService servicio,
        IOptions<AgenteOptions> opciones,
        ILogger<AgenteWorker> logger)
    {
        _servicio = servicio;
        _opciones = opciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalo = TimeSpan.FromMinutes(Math.Max(1, _opciones.Value.IntervaloMinutos));
        _logger.LogInformation("Agente iniciado. Reportará cada {minutos} min.", intervalo.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _servicio.EjecutarReporteAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado en el ciclo de reporte.");
            }

            try
            {
                await Task.Delay(intervalo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
