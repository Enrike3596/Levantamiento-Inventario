using InventarioTI.PollerSnmp.Configuration;
using InventarioTI.PollerSnmp.Servicios;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventarioTI.PollerSnmp.Workers;

public class PollerWorker : BackgroundService
{
    private readonly IOptions<PollerOptions> _opciones;
    private readonly PollerService _servicio;
    private readonly ILogger<PollerWorker> _logger;
    private readonly bool _unaVez;

    public PollerWorker(IOptions<PollerOptions> opciones, PollerService servicio, ILogger<PollerWorker> logger, bool unaVez = false)
    {
        _opciones = opciones;
        _servicio = servicio;
        _logger = logger;
        _unaVez = unaVez;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Poller SNMP iniciado: {comunidad}@{version}, intervalo {intervalo} min, {n} switch(es).",
            _opciones.Value.Comunidad, _opciones.Value.Version,
            _opciones.Value.IntervaloMinutos, _opciones.Value.Switches.Count);

        do
        {
            await _servicio.EjecutarCicloAsync(stoppingToken);
        } while (!_unaVez && !stoppingToken.IsCancellationRequested);

        _logger.LogInformation("Poller SNMP detenido.");
    }
}
