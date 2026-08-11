using System.Globalization;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using InventarioTI.Agente.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace InventarioTI.Agente.Sistema;

public class SystemInfoService
{
    private readonly IOptions<AgenteOptions> _opciones;
    private readonly ILogger<SystemInfoService> _logger;

    public SystemInfoService(IOptions<AgenteOptions> opciones, ILogger<SystemInfoService> logger)
    {
        _opciones = opciones;
        _logger = logger;
    }

    public SistemaInfo Recopilar()
    {
        var info = new SistemaInfo
        {
            NombreEquipo = Environment.MachineName,
            SistemaOperativo = Consultar("Win32_OperatingSystem", "Caption"),
            VersionSo = Consultar("Win32_OperatingSystem", "Version"),
            Dominio = Consultar("Win32_ComputerSystem", "Domain"),
            Fabricante = Consultar("Win32_ComputerSystem", "Manufacturer"),
            Modelo = Consultar("Win32_ComputerSystem", "Model"),
            NumeroSerie = Consultar("Win32_BIOS", "SerialNumber"),
            Procesador = LimpiarEspacios(Consultar("Win32_Processor", "Name")),
            Antivirus = ConsultarAntivirus(),
            UsuarioConectado = SoloUsuario(Consultar("Win32_ComputerSystem", "UserName")),
            RamGb = Gb(Consultar("Win32_ComputerSystem", "TotalPhysicalMemory")),
            UltimoReinicio = Fecha(Consultar("Win32_OperatingSystem", "LastBootUpTime")),
            Programas = ObtenerProgramas()
        };

        var red = ObtenerRed();
        info.Ip = red.ip;
        info.Mac = red.mac;

        var discos = ObtenerDiscos();
        info.DiscoGb = discos.total;
        info.EspacioLibreGb = discos.libre;

        _logger.LogInformation(
            "Información recopilada de '{equipo}': SO={so} RAM={ram}GB Disco={disco}GB Libre={libre}GB Programas={n}",
            info.NombreEquipo, info.SistemaOperativo, info.RamGb, info.DiscoGb, info.EspacioLibreGb,
            info.Programas is null ? 0 : (info.Programas.AsArray()?.Count ?? 0));

        return info;
    }

    // ---------------- WMI ----------------

    private string? Consultar(string clase, string propiedad, string? where = null)
    {
        try
        {
            var wmi = $"SELECT {propiedad} FROM {clase}" + (string.IsNullOrEmpty(where) ? string.Empty : $" WHERE {where}");
            using var searcher = new ManagementObjectSearcher(wmi);
            foreach (ManagementObject obj in searcher.Get())
            {
                var valor = obj[propiedad];
                if (valor is not null && !string.IsNullOrWhiteSpace(valor.ToString()))
                    return valor.ToString()!.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando WMI {clase}.{propiedad}", clase, propiedad);
        }
        return null;
    }

    private List<string> ConsultarTodas(string clase, string propiedad, string? where = null)
    {
        var resultado = new List<string>();
        try
        {
            var wmi = $"SELECT {propiedad} FROM {clase}" + (string.IsNullOrEmpty(where) ? string.Empty : $" WHERE {where}");
            using var searcher = new ManagementObjectSearcher(wmi);
            foreach (ManagementObject obj in searcher.Get())
            {
                var valor = obj[propiedad]?.ToString();
                if (!string.IsNullOrWhiteSpace(valor))
                    resultado.Add(valor!);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando WMI {clase}.{propiedad}", clase, propiedad);
        }
        return resultado;
    }

    private string? ConsultarAntivirus()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\SecurityCenter2", "SELECT displayName FROM AntiVirusProduct");
            foreach (ManagementObject obj in searcher.Get())
            {
                var nombre = obj["displayName"]?.ToString();
                if (!string.IsNullOrWhiteSpace(nombre))
                    return nombre.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "No se pudo consultar el antivirus (SecurityCenter2).");
        }
        return null;
    }

    private (string? ip, string? mac) ObtenerRed()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT IPAddress, MACAddress FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE");
            foreach (ManagementObject obj in searcher.Get())
            {
                var mac = obj["MACAddress"]?.ToString();
                var ips = obj["IPAddress"] as string[];
                var ip = ips?.FirstOrDefault(x =>
                    IPAddress.TryParse(x, out var p) && p.AddressFamily == AddressFamily.InterNetwork);
                if (!string.IsNullOrEmpty(ip))
                    return (ip, mac);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error consultando la configuración de red.");
        }
        return (null, null);
    }

    private (decimal? total, decimal? libre) ObtenerDiscos()
    {
        decimal total = 0, libre = 0;
        foreach (var valor in ConsultarTodas("Win32_LogicalDisk", "Size", "DriveType = 3"))
            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) total += v;
        foreach (var valor in ConsultarTodas("Win32_LogicalDisk", "FreeSpace", "DriveType = 3"))
            if (decimal.TryParse(valor, NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) libre += v;
        return (total <= 0 ? null : Math.Round(total / 1024m / 1024m / 1024m, 1),
                libre <= 0 ? null : Math.Round(libre / 1024m / 1024m / 1024m, 1));
    }

    // ---------------- Programas instalados (registro de Windows) ----------------

    private JsonNode? ObtenerProgramas()
    {
        var maximo = Math.Max(1, _opciones.Value.MaxProgramas);
        var lista = new List<object>();

        string[] rutas =
        {
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
        };

        try
        {
            foreach (var ruta in rutas)
            {
                using var raiz = Registry.LocalMachine.OpenSubKey(ruta);
                if (raiz is null) continue;

                foreach (var sub in raiz.GetSubKeyNames())
                {
                    if (lista.Count >= maximo) break;
                    using var app = raiz.OpenSubKey(sub);
                    if (app is null) continue;

                    var nombre = app.GetValue("DisplayName") as string;
                    if (string.IsNullOrWhiteSpace(nombre)) continue;
                    if (app.GetValue("SystemComponent") is int esComponente && esComponente == 1) continue;
                    if (app.GetValue("ReleaseType") is string tipo && tipo == "Update") continue;

                    var version = app.GetValue("DisplayVersion") as string;
                    lista.Add(new { nombre, version = string.IsNullOrWhiteSpace(version) ? null : version });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error leyendo los programas instalados del registro.");
        }

        return lista.Count == 0 ? null : JsonNode.Parse(JsonSerializer.Serialize(lista));
    }

    // ---------------- Utilidades ----------------

    private static decimal? Gb(string? bytes)
    {
        if (string.IsNullOrWhiteSpace(bytes)) return null;
        if (!decimal.TryParse(bytes, NumberStyles.Any, CultureInfo.InvariantCulture, out var b)) return null;
        return Math.Round(b / 1024m / 1024m / 1024m, 1);
    }

    private static DateTimeOffset? Fecha(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        if (DateTimeOffset.TryParse(valor, out var fecha)) return fecha.ToUniversalTime();
        try
        {
            var dt = ManagementDateTimeConverter.ToDateTime(valor);
            return new DateTimeOffset(dt).ToUniversalTime();
        }
        catch
        {
            return null;
        }
    }

    private static string? SoloUsuario(string? dominioUsuario)
    {
        if (string.IsNullOrWhiteSpace(dominioUsuario)) return null;
        var indice = dominioUsuario.LastIndexOf('\\');
        return indice >= 0 ? dominioUsuario[(indice + 1)..] : dominioUsuario;
    }

    private static string? LimpiarEspacios(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        return Regex.Replace(texto, @"\s+", " ").Trim();
    }
}
