namespace InventarioTI.Api.DTOs;

public class RegistrarAgenteDTO
{
    public string NombreEquipo { get; set; } = string.Empty;
}

public class AgenteResponseDTO
{
    public Guid Id { get; set; }
    public string NombreEquipo { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTimeOffset CreadoEn { get; set; }
    public string? ApiKey { get; set; }
}
