namespace InventarioTI.Api.DTOs;

public class ReporteSwitchDTO
{
    public string Ip { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Serial { get; set; }
    public string? Firmware { get; set; }
    public int? TotalPuertos { get; set; }
    public string? Estado { get; set; }
    public List<ReportePuertoDTO> Puertos { get; set; } = new();
}

public class ReportePuertoDTO
{
    public int NumeroPuerto { get; set; }
    public string? Estado { get; set; }
    public int? Vlan { get; set; }
    public decimal? TraficoInMb { get; set; }
    public decimal? TraficoOutMb { get; set; }
}

public class SwitchResponseDTO
{
    public Guid Id { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Serial { get; set; }
    public string? Firmware { get; set; }
    public int? TotalPuertos { get; set; }
    public string? Estado { get; set; }
    public DateTimeOffset? UltimoPoll { get; set; }
    public List<PuertoResponseDTO> Puertos { get; set; } = new();
}

public class PuertoResponseDTO
{
    public Guid Id { get; set; }
    public int NumeroPuerto { get; set; }
    public string? Estado { get; set; }
    public int? Vlan { get; set; }
    public decimal? TraficoInMb { get; set; }
    public decimal? TraficoOutMb { get; set; }
}
