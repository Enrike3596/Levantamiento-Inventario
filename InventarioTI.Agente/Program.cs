using InventarioTI.Agente.Api;
using InventarioTI.Agente.Configuration;
using InventarioTI.Agente.Servicios;
using InventarioTI.Agente.Sistema;
using InventarioTI.Agente.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<AgenteOptions>(builder.Configuration.GetSection(AgenteOptions.Seccion));

if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "InventarioTI.Agente";
    });
}

builder.Services.AddHttpClient<ApiClientService>((sp, client) =>
{
    var opciones = sp.GetRequiredService<IOptions<AgenteOptions>>().Value;
    client.BaseAddress = new Uri(opciones.ApiUrl);
    client.Timeout = TimeSpan.FromSeconds(45);
});

builder.Services.AddSingleton<SystemInfoService>();
builder.Services.AddSingleton<AgenteRegistrador>();
builder.Services.AddSingleton<AgenteService>();
builder.Services.AddHostedService<AgenteWorker>();

var host = builder.Build();

if (args.Contains("--once"))
{
    // Modo consola/prueba: recopila y envía un solo reporte y termina.
    using var scope = host.Services.CreateScope();
    var servicio = scope.ServiceProvider.GetRequiredService<AgenteService>();
    await servicio.EjecutarReporteAsync(CancellationToken.None);
    return;
}

await host.RunAsync();
