namespace InventarioTI.Domain.Models;

public class SwitchDispositivo
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
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<SwitchPuerto> Puertos { get; set; } = new List<SwitchPuerto>();
}
