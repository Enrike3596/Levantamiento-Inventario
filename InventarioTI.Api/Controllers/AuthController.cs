using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InventarioTI.Api.DTOs;
using InventarioTI.Api.Helpers;
using InventarioTI.Domain.Models;
using InventarioTI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace InventarioTI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthController(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(ResponseHelper.Error<object>("Email y contraseña son obligatorios."));

        var email = dto.Email.Trim().ToLower();
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
        if (usuario is null || !PasswordHelper.Verify(dto.Password, usuario.PasswordHash))
            return Unauthorized(ResponseHelper.Error<object>("Credenciales inválidas."));

        var token = GenerarToken(usuario);
        return Ok(ResponseHelper.Ok(token, "Inicio de sesión correcto."));
    }

    [HttpGet("me")]
    [Authorize(Policy = "Consola")]
    public IActionResult Me()
    {
        var email = User.Identity?.Name ?? string.Empty;
        var rol = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        return Ok(ResponseHelper.Ok(new { Email = email, Rol = rol }, "Usuario autenticado."));
    }

    private TokenResponseDTO GenerarToken(Usuario usuario)
    {
        var jwt = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new("Tipo", "Consola")
        };

        var horas = double.TryParse(jwt["ExpirationHours"], out var h) ? h : 8;
        var expiracion = DateTimeOffset.UtcNow.AddHours(horas);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiracion.UtcDateTime,
            signingCredentials: creds);

        return new TokenResponseDTO
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Expiracion = expiracion,
            Email = usuario.Email,
            Rol = usuario.Rol
        };
    }
}
