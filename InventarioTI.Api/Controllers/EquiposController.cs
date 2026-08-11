using InventarioTI.Api.DTOs;
using InventarioTI.Api.Helpers;
using InventarioTI.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventarioTI.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EquiposController : ControllerBase
{
    private readonly AppDbContext _db;

    public EquiposController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Policy = "Mixto")]
    public async Task<IActionResult> Listar()
    {
        var equipos = await _db.Equipos
            .OrderBy(e => e.NombreEquipo)
            .ToListAsync();

        return Ok(ResponseHelper.Ok(equipos.Select(ToDto)));
    }

    [HttpGet("conectados")]
    [Authorize(Policy = "Mixto")]
    public async Task<IActionResult> Conectados([FromQuery] int minutos = 15)
    {
        var limite = DateTimeOffset.UtcNow.AddMinutes(-minutos);
        var equipos = await _db.Equipos
            .Where(e => e.UltimoReporte != null && e.UltimoReporte >= limite)
            .OrderBy(e => e.NombreEquipo)
            .ToListAsync();

        return Ok(ResponseHelper.Ok(equipos.Select(ToDto)));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Mixto")]
    public async Task<IActionResult> Obtener(Guid id)
    {
        var equipo = await _db.Equipos.FirstOrDefaultAsync(e => e.Id == id);
        if (equipo is null)
            return NotFound(ResponseHelper.Error<object>("Equipo no encontrado."));

        return Ok(ResponseHelper.Ok(ToDto(equipo)));
    }

    private static EquipoResponseDTO ToDto(InventarioTI.Domain.Models.Equipo e) => new()
    {
        Id = e.Id,
        NombreEquipo = e.NombreEquipo,
        UsuarioConectado = e.UsuarioConectado,
        Dominio = e.Dominio,
        Ip = e.Ip,
        Mac = e.Mac,
        SistemaOperativo = e.SistemaOperativo,
        VersionSo = e.VersionSo,
        RamGb = e.RamGb,
        Procesador = e.Procesador,
        DiscoGb = e.DiscoGb,
        EspacioLibreGb = e.EspacioLibreGb,
        NumeroSerie = e.NumeroSerie,
        Fabricante = e.Fabricante,
        Modelo = e.Modelo,
        Antivirus = e.Antivirus,
        UltimoReinicio = e.UltimoReinicio,
        UltimoReporte = e.UltimoReporte,
        Programas = e.Programas
    };
}
