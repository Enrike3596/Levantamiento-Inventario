using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using InventarioTI.PollerSnmp.Configuration;
using InventarioTI.PollerSnmp.Snmp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventarioTI.PollerSnmp.Api;

public class ApiClientService
{
    private static readonly JsonSerializerOptions JsonOpciones = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly IHttpClientFactory _factory;
    private readonly IOptions<PollerOptions> _opciones;
    private readonly ILogger<ApiClientService> _logger;

    public ApiClientService(IHttpClientFactory factory, IOptions<PollerOptions> opciones, ILogger<ApiClientService> logger)
    {
        _factory = factory;
        _opciones = opciones;
        _logger = logger;
    }

    public async Task<string?> LoginAsync(string email, string password, CancellationToken ct)
    {
        var cliente = _factory.CreateClient("api");
        var respuesta = await cliente.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password }, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Login rechazado ({codigo}): {cuerpo}", respuesta.StatusCode, cuerpo);
            return null;
        }
        var doc = await respuesta.Content.ReadFromJsonAsync<JsonNode>(ct);
        var token = doc?["data"]?["token"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public async Task<string?> RegistrarAgenteAsync(string token, string nombre, CancellationToken ct)
    {
        var cliente = _factory.CreateClient("api");
        using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/agentes/registrar")
        {
            Content = JsonContent.Create(new { NombreEquipo = nombre }, options: JsonOpciones),
        };
        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var respuesta = await cliente.SendAsync(peticion, ct);
        if (!respuesta.IsSuccessStatusCode)
        {
            var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Registro de agente rechazado ({codigo}): {cuerpo}", respuesta.StatusCode, cuerpo);
            return null;
        }
        var doc = await respuesta.Content.ReadFromJsonAsync<JsonNode>(ct);
        var apiKey = doc?["data"]?["apiKey"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
    }

    public async Task<bool> EnviarReporteSwitchAsync(ReporteSwitch reporte, string apiKey, CancellationToken ct)
    {
        try
        {
            var cliente = _factory.CreateClient("api");
            using var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/switches/reporte")
            {
                Content = JsonContent.Create(reporte, options: JsonOpciones),
            };
            peticion.Headers.Add("X-Api-Key", apiKey);
            var respuesta = await cliente.SendAsync(peticion, ct);
            if (!respuesta.IsSuccessStatusCode)
            {
                var cuerpo = await respuesta.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Reporte de switch rechazado ({codigo}): {cuerpo}", respuesta.StatusCode, cuerpo);
                return false;
            }
            _logger.LogInformation("Reporte del switch {ip} enviado y procesado.", reporte.Ip);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando reporte del switch {ip}.", reporte.Ip);
            return false;
        }
    }
}
