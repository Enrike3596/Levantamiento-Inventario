namespace InventarioTI.PollerSnmp.Snmp;

public class ReporteSwitch
{
    public string Ip { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Serial { get; set; }
    public string? Firmware { get; set; }
    public int? TotalPuertos { get; set; }
    public string? Estado { get; set; }
    public List<ReportePuerto> Puertos { get; set; } = new();
}

public class ReportePuerto
{
    public int NumeroPuerto { get; set; }
    public string? Estado { get; set; }
    public int? Vlan { get; set; }
    public decimal? TraficoInMb { get; set; }
    public decimal? TraficoOutMb { get; set; }
}
