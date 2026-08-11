namespace InventarioTI.Domain.Models;

public class SwitchPuerto
{
    public Guid Id { get; set; }
    public Guid SwitchId { get; set; }
    public int NumeroPuerto { get; set; }
    public string? Estado { get; set; }
    public int? Vlan { get; set; }
    public decimal? TraficoInMb { get; set; }
    public decimal? TraficoOutMb { get; set; }

    public SwitchDispositivo Switch { get; set; } = null!;
}
