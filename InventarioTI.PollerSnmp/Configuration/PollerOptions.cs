namespace InventarioTI.PollerSnmp.Configuration;

public class PollerOptions
{
    public const string Seccion = "Poller";

    public string ApiUrl { get; set; } = "http://localhost:5007";
    public string ApiKey { get; set; } = string.Empty;
    public bool AutoRegistro { get; set; } = true;
    public string NombreAgente { get; set; } = "POLLER-SNMP";
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public int IntervaloMinutos { get; set; } = 10;
    public int PuertoSnmp { get; set; } = 161;
    public string Comunidad { get; set; } = "public";
    public string Version { get; set; } = "v2c";
    public int TiempoEsperaSegundos { get; set; } = 3;
    public List<string> Switches { get; set; } = new();
}
