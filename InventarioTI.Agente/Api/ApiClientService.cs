using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using InventarioTI.Agente.Sistema;
using Microsoft.Extensions.Logging;

namespace InventarioTI.Agente.Api;

public class ApiClientService
{
    private static readonly JsonSerializerOptions Json =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly HttpClient _http;
    private readonly ILogger<ApiClientService> _logger;

    public ApiClientService(HttpClient http, ILogger<ApiClientService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string?> LoginAsync(string email, string password, CancellationToken ct)
    {
        try
        {
            var body = JsonContent.Create(new { email, password }, options: Json);
            var resp = await _http.PostAsync("api/auth/login", body, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Login falló: HTTP {code}", resp.StatusCode);
                return null;
            }
            var doc = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
            return doc?["data"]?["token"]?.GetValue<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al iniciar sesión en la API.");
            return null;
        }
    }

    public async Task<string?> RegistrarAgenteAsync(string nombreEquipo, string jwt, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/agentes/registrar");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
            req.Content = JsonContent.Create(new { nombreEquipo }, options: Json);

            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var texto = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Registro de agente falló: HTTP {code}: {texto}", resp.StatusCode, texto);
                return null;
            }
            var doc = await resp.Content.ReadFromJsonAsync<JsonNode>(ct);
            return doc?["data"]?["apiKey"]?.GetValue<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar el agente en la API.");
            return null;
        }
    }

    public async Task<bool> EnviarReporteAsync(SistemaInfo info, string apiKey, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/agentes/reporte");
            req.Headers.Add("X-Api-Key", apiKey);
            req.Content = JsonContent.Create(info, options: Json);

            var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var texto = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Reporte falló: HTTP {code}: {texto}", resp.StatusCode, texto);
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar el reporte a la API.");
            return false;
        }
    }
}
