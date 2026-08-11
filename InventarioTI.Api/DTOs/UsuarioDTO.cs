namespace InventarioTI.Api.DTOs;

public class CrearUsuarioDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Rol { get; set; } = "Tecnico";
}

public class UsuarioResponseDTO
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
