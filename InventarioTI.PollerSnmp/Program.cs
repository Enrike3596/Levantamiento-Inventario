using InventarioTI.PollerSnmp.Api;
using InventarioTI.PollerSnmp.Configuration;
using InventarioTI.PollerSnmp.Servicios;
using InventarioTI.PollerSnmp.Snmp;
using InventarioTI.PollerSnmp.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "InventarioTI.PollerSnmp";
});

builder.Services.Configure<PollerOptions>(builder.Configuration.GetSection(PollerOptions.Seccion));

builder.Services.AddHttpClient("api", cliente =>
{
    var apiUrl = builder.Configuration.GetSection(PollerOptions.Seccion)["ApiUrl"] ?? "http://localhost:5007";
    cliente.BaseAddress = new Uri(apiUrl);
    cliente.Timeout = TimeSpan.FromMinutes(2);
});

builder.Services.AddSingleton<ApiClientService>();
builder.Services.AddSingleton<AgenteRegistrador>();
builder.Services.AddSingleton<TraficoCache>();
builder.Services.AddSingleton<PollerService>();

var unaVez = args.Contains("--once", StringComparer.OrdinalIgnoreCase);
if (!unaVez)
    builder.Services.AddHostedService<PollerWorker>();

var host = builder.Build();

if (unaVez)
{
    using var scope = host.Services.CreateScope();
    var servicio = scope.ServiceProvider.GetRequiredService<PollerService>();
    await servicio.EjecutarCicloAsync(CancellationToken.None);
    return;
}

await host.RunAsync();
