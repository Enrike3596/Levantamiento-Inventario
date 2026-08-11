namespace InventarioTI.Api.DTOs;

public class LoginDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class TokenResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset Expiracion { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}
