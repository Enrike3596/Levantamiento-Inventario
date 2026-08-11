using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Microsoft.Extensions.Logging;

namespace InventarioTI.PollerSnmp.Snmp;

public class SnmpSwitchService
{
    private const string OidSysDescr = "1.3.6.1.2.1.1.1.0";
    private const string OidSysName = "1.3.6.1.2.1.1.5.0";
    private const string OidSysObjectId = "1.3.6.1.2.1.1.2.0";

    private const string OidIfTabla = "1.3.6.1.2.1.2.2.1";
    private const string OidIfDescr = "1.3.6.1.2.1.2.2.1.2";
    private const string OidIfTipo = "1.3.6.1.2.1.2.2.1.3";
    private const string OidIfOper = "1.3.6.1.2.1.2.2.1.8";
    private const string OidIfIn = "1.3.6.1.2.1.2.2.1.10";
    private const string OidIfOut = "1.3.6.1.2.1.2.2.1.16";
    private const string OidIfName = "1.3.6.1.2.1.31.1.1.1.1";
    private const string OidIfVelocidad = "1.3.6.1.2.1.31.1.1.1.15";
    private const string OidEntTabla = "1.3.6.1.2.1.47.1.1.1.1";
    private const string OidDot1qPvid = "1.3.6.1.2.1.17.7.1.4.5.1.1";

    private static readonly Dictionary<string, string> Empresas = new()
    {
        ["1.3.6.1.4.1.9.1.3"] = "Cisco",
        ["1.3.6.1.4.1.9"] = "Cisco",
        ["1.3.6.1.4.1.11"] = "HP",
        ["1.3.6.1.4.1.43"] = "3Com",
        ["1.3.6.1.4.1.171"] = "D-Link",
        ["1.3.6.1.4.1.236"] = "Dell",
        ["1.3.6.1.4.1.1916"] = "Extreme Networks",
        ["1.3.6.1.4.1.1588"] = "Brocade",
        ["1.3.6.1.4.1.2636"] = "Juniper",
        ["1.3.6.1.4.1.4526"] = "NETGEAR",
        ["1.3.6.1.4.1.14988"] = "MikroTik",
        ["1.3.6.1.4.1.12356"] = "Fortinet",
        ["1.3.6.1.4.1.30065"] = "Arista",
        ["1.3.6.1.4.1.41112"] = "Ubiquiti",
        ["1.3.6.1.4.1.8072"] = "Linux",
        ["1.3.6.1.4.1.29671"] = "Meraki",
    };

    private readonly VersionCode _version;
    private readonly IPEndPoint _endPoint;
    private readonly OctetString _comunidad;
    private readonly int _timeoutMs;
    private readonly TraficoCache _trafico;
    private readonly ILogger _logger;

    public SnmpSwitchService(IPAddress ip, int puertoSnmp, string comunidad, string version, int timeoutMs, TraficoCache trafico, ILogger logger)
    {
        _version = version.ToLowerInvariant() switch
        {
            "v1" => VersionCode.V1,
            "v2" => VersionCode.V2,
            _ => VersionCode.V2,
        };
        _endPoint = new IPEndPoint(ip, puertoSnmp);
        _comunidad = new OctetString(comunidad);
        _timeoutMs = timeoutMs;
        _trafico = trafico;
        _logger = logger;
    }

    public ReporteSwitch? Recopilar()
    {
        var ahora = DateTime.UtcNow;
        var reporte = new ReporteSwitch { Ip = _endPoint.Address.ToString() };

        string? sysDescr, sysName, sysObjectId;
        try
        {
            sysDescr = ObtenerEscalar(OidSysDescr);
            sysName = ObtenerEscalar(OidSysName);
            sysObjectId = ObtenerEscalar(OidSysObjectId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sin respuesta SNMP de {ip} ({comunidad}, {version}).", _endPoint.Address, _comunidad.ToString(), _version);
            return null;
        }

        reporte.Nombre = sysName;
        reporte.Marca = MarcaDesde(sysObjectId, sysDescr);

        var entidades = CaminarTabla(OidEntTabla);
        reporte.Serial = ValorEntidad(entidades, 11);
        reporte.Firmware = ValorEntidad(entidades, 10);
        reporte.Modelo = ValorEntidad(entidades, 13) ?? ValorEntidad(entidades, 2);

        var descr = MapaColumnas(CaminarTabla(OidIfDescr), OidIfDescr);
        var tipos = MapaColumnas(CaminarTabla(OidIfTipo), OidIfTipo);
        var oper = MapaColumnas(CaminarTabla(OidIfOper), OidIfOper);
        var entrada = MapaColumnas(CaminarTabla(OidIfIn), OidIfIn);
        var salida = MapaColumnas(CaminarTabla(OidIfOut), OidIfOut);
        var nombres = MapaColumnas(CaminarTabla(OidIfName), OidIfName);
        var pvids = MapaColumnas(CaminarTabla(OidDot1qPvid), OidDot1qPvid);

        var indices = tipos.Keys.Concat(oper.Keys).Distinct().OrderBy(i => i);
        var puertos = new List<ReportePuerto>();

        foreach (var i in indices)
        {
            if (tipos.TryGetValue(i, out var t) && t != "6" && t != "62")
                continue;

            var puerto = new ReportePuerto { NumeroPuerto = i };

            puerto.Estado = oper.TryGetValue(i, out var o) && o == "1" ? "up" : "down";

            if (pvids.TryGetValue(i, out var vlan) && int.TryParse(vlan, out var v))
                puerto.Vlan = v;

            if (entrada.TryGetValue(i, out var e) && ulong.TryParse(e, out var ein))
                puerto.TraficoInMb = _trafico.Calcular($"{reporte.Ip}:{i}:in", ein, ahora);

            if (salida.TryGetValue(i, out var s) && ulong.TryParse(s, out var sout))
                puerto.TraficoOutMb = _trafico.Calcular($"{reporte.Ip}:{i}:out", sout, ahora);

            puertos.Add(puerto);
        }

        reporte.Puertos = puertos;
        reporte.TotalPuertos = puertos.Count;
        reporte.Estado = puertos.Any(p => p.Estado == "up")
            ? "Operativo"
            : puertos.Count == 0
                ? "Sin datos"
                : "Inactivo";

        _logger.LogDebug(
            "Switch {ip} recopilado: {marca} {modelo}, {n} puertos.",
            reporte.Ip, reporte.Marca, reporte.Modelo, puertos.Count);

        return reporte;
    }

    private string? ObtenerEscalar(string oid)
    {
        var resultado = Messenger.Get(
            _version, _endPoint, _comunidad,
            new List<Variable> { new(new ObjectIdentifier(oid)) },
            _timeoutMs);

        return resultado.Count > 0 ? Texto(resultado[0].Data) : null;
    }

    private IReadOnlyList<Variable> CaminarTabla(string prefijoOid)
    {
        try
        {
            var lista = new List<Variable>();
            Messenger.Walk(
                _version, _endPoint, _comunidad,
                new ObjectIdentifier(prefijoOid),
                lista, _timeoutMs, WalkMode.WithinSubtree);
            return lista;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Tabla SNMP {oid} no disponible en {ip}.", prefijoOid, _endPoint.Address);
            return Array.Empty<Variable>();
        }
    }

    private static Dictionary<int, string> MapaColumnas(IReadOnlyList<Variable> variables, string prefijoOid)
    {
        var mapa = new Dictionary<int, string>();
        var prefijo = prefijoOid + ".";
        foreach (var v in variables)
        {
            var oid = v.Id.ToString();
            if (!oid.StartsWith(prefijo, StringComparison.Ordinal))
                continue;
            if (!int.TryParse(oid[prefijo.Length..], out var indice))
                continue;
            var texto = Texto(v.Data);
            if (texto is not null)
                mapa[indice] = texto;
        }
        return mapa;
    }

    private static string? ValorEntidad(IReadOnlyList<Variable> entidades, int columna)
    {
        var prefijo = $"{OidEntTabla}.{columna}.";
        foreach (var v in entidades)
        {
            if (!v.Id.ToString().StartsWith(prefijo, StringComparison.Ordinal))
                continue;
            var texto = Texto(v.Data);
            if (texto is not null)
                return texto;
        }
        return null;
    }

    private static string? MarcaDesde(string? objectId, string? descripcion)
    {
        if (!string.IsNullOrWhiteSpace(objectId))
        {
            var mejor = Empresas.Keys
                .Where(k => objectId.StartsWith(k, StringComparison.Ordinal))
                .OrderByDescending(k => k.Length)
                .FirstOrDefault();
            if (mejor is not null)
                return Empresas[mejor];
        }
        if (!string.IsNullOrWhiteSpace(descripcion))
        {
            var primera = descripcion.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(primera))
                return primera.Trim();
        }
        return null;
    }

    private static string? Texto(ISnmpData data)
    {
        if (data is OctetString os)
        {
            var s = os.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }
        if (data is Null)
            return null;
        var v = data.ToString();
        return string.IsNullOrWhiteSpace(v) ? null : v.Trim();
    }
}
