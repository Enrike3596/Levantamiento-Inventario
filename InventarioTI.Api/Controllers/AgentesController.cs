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
public class AgentesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AgentesController(AppDbContext db) => _db = db;

    [HttpPost("registrar")]
    [Authorize(Policy = "SoloAdmin")]
    public async Task<IActionResult> Registrar([FromBody] RegistrarAgenteDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreEquipo))
            return BadRequest(ResponseHelper.Error<object>("El nombre del equipo es obligatorio."));

        var nombre = dto.NombreEquipo.Trim();
        var existe = await _db.AgentesRegistrados.AnyAsync(a => a.NombreEquipo == nombre);
        if (existe)
            return Conflict(ResponseHelper.Error<object>("Ya existe un agente con ese nombre de equipo."));

        var apiKey = ApiKeyHelper.Generar();
        var agente = new AgenteRegistrado
        {
            NombreEquipo = nombre,
            ApiKeyHash = ApiKeyHelper.Hash(apiKey)
        };

        _db.AgentesRegistrados.Add(agente);
        await _db.SaveChangesAsync();

        var resp = new AgenteResponseDTO
        {
            Id = agente.Id,
            NombreEquipo = agente.NombreEquipo,
            Activo = agente.Activo,
            CreadoEn = agente.CreadoEn,
            ApiKey = apiKey
        };

        return Ok(ResponseHelper.Ok(resp, "Agente registrado. La API Key solo se muestra una vez."));
    }

    [HttpPost("reporte")]
    [Authorize(Policy = "Agente")]
    public async Task<IActionResult> Reporte([FromBody] ReporteEquipoDTO dto)
    {
        var nombre = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(nombre)) nombre = dto.NombreEquipo;
        if (string.IsNullOrWhiteSpace(nombre))
            return BadRequest(ResponseHelper.Error<object>("El nombre del equipo es obligatorio."));

        var equipo = await _db.Equipos.FirstOrDefaultAsync(e => e.NombreEquipo == nombre);
        if (equipo is null)
        {
            equipo = new Equipo { NombreEquipo = nombre };
            _db.Equipos.Add(equipo);
        }

        equipo.UsuarioConectado = dto.UsuarioConectado;
        equipo.Dominio = dto.Dominio;
        equipo.Ip = dto.Ip;
        equipo.Mac = dto.Mac;
        equipo.SistemaOperativo = dto.SistemaOperativo;
        equipo.VersionSo = dto.VersionSo;
        equipo.RamGb = dto.RamGb;
        equipo.Procesador = dto.Procesador;
        equipo.DiscoGb = dto.DiscoGb;
        equipo.EspacioLibreGb = dto.EspacioLibreGb;
        equipo.NumeroSerie = dto.NumeroSerie;
        equipo.Fabricante = dto.Fabricante;
        equipo.Modelo = dto.Modelo;
        equipo.Antivirus = dto.Antivirus;
        equipo.UltimoReinicio = dto.UltimoReinicio;
        equipo.Programas = dto.Programas;
        equipo.UltimoReporte = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(ResponseHelper.OkVacio<object>("Reporte de equipo procesado."));
    }

    [HttpGet]
    [Authorize(Policy = "Consola")]
    public async Task<IActionResult> Listar()
    {
        var agentes = await _db.AgentesRegistrados
            .OrderBy(a => a.NombreEquipo)
            .Select(a => new AgenteResponseDTO
            {
                Id = a.Id,
                NombreEquipo = a.NombreEquipo,
                Activo = a.Activo,
                CreadoEn = a.CreadoEn
            })
            .ToListAsync();

        return Ok(ResponseHelper.Ok(agentes));
    }
}
