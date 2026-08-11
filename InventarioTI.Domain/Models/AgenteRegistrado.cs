namespace InventarioTI.Domain.Models;

public class AgenteRegistrado
{
    public Guid Id { get; set; }
    public string NombreEquipo { get; set; } = string.Empty;
    public string ApiKeyHash { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;
}
