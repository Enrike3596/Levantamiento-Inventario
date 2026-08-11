using InventarioTI.Api.DTOs;
using InventarioTI.Api.Helpers;
using InventarioTI.Domain.Models;
using InventarioTI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventarioTI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "SoloAdmin")]
public class UsuariosController : ControllerBase
{
    private static readonly string[] RolesValidos = { "Admin", "Supervisor", "Tecnico" };

    private readonly AppDbContext _db;

    public UsuariosController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var usuarios = await _db.Usuarios
            .OrderBy(u => u.Email)
            .Select(u => new UsuarioResponseDTO { Id = u.Id, Email = u.Email, Rol = u.Rol })
            .ToListAsync();

        return Ok(ResponseHelper.Ok(usuarios));
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearUsuarioDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(ResponseHelper.Error<object>("Email y contraseña son obligatorios."));

        var email = dto.Email.Trim().ToLower();
        var existe = await _db.Usuarios.AnyAsync(u => u.Email.ToLower() == email);
        if (existe)
            return Conflict(ResponseHelper.Error<object>("Ya existe un usuario con ese email."));

        var rol = (dto.Rol ?? "Tecnico").Trim();
        if (!RolesValidos.Contains(rol))
            return BadRequest(ResponseHelper.Error<object>($"Rol inválido. Válidos: {string.Join(", ", RolesValidos)}."));

        var usuario = new Usuario
        {
            Email = email,
            PasswordHash = PasswordHelper.Hash(dto.Password),
            Rol = rol
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return Ok(ResponseHelper.Ok(new UsuarioResponseDTO { Id = usuario.Id, Email = usuario.Email, Rol = usuario.Rol }, "Usuario creado."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Actualizar(Guid id, [FromBody] CrearUsuarioDTO dto)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null)
            return NotFound(ResponseHelper.Error<object>("Usuario no encontrado."));

        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(ResponseHelper.Error<object>("El email es obligatorio."));

        var email = dto.Email.Trim().ToLower();
        var duplicado = await _db.Usuarios.AnyAsync(u => u.Id != id && u.Email.ToLower() == email);
        if (duplicado)
            return Conflict(ResponseHelper.Error<object>("Ya existe un usuario con ese email."));

        var rol = (dto.Rol ?? usuario.Rol).Trim();
        if (!RolesValidos.Contains(rol))
            return BadRequest(ResponseHelper.Error<object>($"Rol inválido. Válidos: {string.Join(", ", RolesValidos)}."));

        usuario.Email = email;
        usuario.Rol = rol;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            usuario.PasswordHash = PasswordHelper.Hash(dto.Password);

        await _db.SaveChangesAsync();
        return Ok(ResponseHelper.Ok(new UsuarioResponseDTO { Id = usuario.Id, Email = usuario.Email, Rol = usuario.Rol }, "Usuario actualizado."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);
        if (usuario is null)
            return NotFound(ResponseHelper.Error<object>("Usuario no encontrado."));

        if (string.Equals(usuario.Email, User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            return BadRequest(ResponseHelper.Error<object>("No puede eliminar su propio usuario."));

        _db.Usuarios.Remove(usuario);
        await _db.SaveChangesAsync();
        return Ok(ResponseHelper.OkVacio<object>("Usuario eliminado."));
    }
}
