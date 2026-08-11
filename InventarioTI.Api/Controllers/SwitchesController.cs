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
public class SwitchesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SwitchesController(AppDbContext db) => _db = db;

    [HttpPost("reporte")]
    [Authorize(Policy = "Agente")]
    public async Task<IActionResult> Reporte([FromBody] ReporteSwitchDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Ip))
            return BadRequest(ResponseHelper.Error<object>("La IP del switch es obligatoria."));

        var ip = dto.Ip.Trim();
        var sw = await _db.Switches
            .Include(s => s.Puertos)
            .FirstOrDefaultAsync(s => s.Ip == ip);

        if (sw is null)
        {
            sw = new SwitchDispositivo { Ip = ip, Puertos = new List<SwitchPuerto>() };
            _db.Switches.Add(sw);
        }

        sw.Nombre = dto.Nombre;
        sw.Marca = dto.Marca;
        sw.Modelo = dto.Modelo;
        sw.Serial = dto.Serial;
        sw.Firmware = dto.Firmware;
        sw.TotalPuertos = dto.TotalPuertos;
        sw.Estado = dto.Estado;
        sw.UltimoPoll = DateTimeOffset.UtcNow;

        foreach (var p in dto.Puertos)
        {
            var puerto = sw.Puertos.FirstOrDefault(x => x.NumeroPuerto == p.NumeroPuerto);
            if (puerto is null)
            {
                puerto = new SwitchPuerto { NumeroPuerto = p.NumeroPuerto };
                sw.Puertos.Add(puerto);
            }

            puerto.Estado = p.Estado;
            puerto.Vlan = p.Vlan;
            puerto.TraficoInMb = p.TraficoInMb;
            puerto.TraficoOutMb = p.TraficoOutMb;
        }

        await _db.SaveChangesAsync();
        return Ok(ResponseHelper.OkVacio<object>("Reporte de switch procesado."));
    }

    [HttpGet]
    [Authorize(Policy = "Mixto")]
    public async Task<IActionResult> Listar()
    {
        var switches = await _db.Switches
            .OrderBy(s => s.Ip)
            .Select(s => new SwitchResponseDTO
            {
                Id = s.Id,
                Ip = s.Ip,
                Nombre = s.Nombre,
                Marca = s.Marca,
                Modelo = s.Modelo,
                Serial = s.Serial,
                Firmware = s.Firmware,
                TotalPuertos = s.TotalPuertos,
                Estado = s.Estado,
                UltimoPoll = s.UltimoPoll
            })
            .ToListAsync();

        return Ok(ResponseHelper.Ok(switches));
    }

    [HttpGet("{id:guid}/puertos")]
    [Authorize(Policy = "Mixto")]
    public async Task<IActionResult> Puertos(Guid id)
    {
        var sw = await _db.Switches
            .Include(s => s.Puertos)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sw is null)
            return NotFound(ResponseHelper.Error<object>("Switch no encontrado."));

        var resp = new SwitchResponseDTO
        {
            Id = sw.Id,
            Ip = sw.Ip,
            Nombre = sw.Nombre,
            Marca = sw.Marca,
            Modelo = sw.Modelo,
            Serial = sw.Serial,
            Firmware = sw.Firmware,
            TotalPuertos = sw.TotalPuertos,
            Estado = sw.Estado,
            UltimoPoll = sw.UltimoPoll,
            Puertos = sw.Puertos
                .OrderBy(p => p.NumeroPuerto)
                .Select(p => new PuertoResponseDTO
                {
                    Id = p.Id,
                    NumeroPuerto = p.NumeroPuerto,
                    Estado = p.Estado,
                    Vlan = p.Vlan,
                    TraficoInMb = p.TraficoInMb,
                    TraficoOutMb = p.TraficoOutMb
                })
                .ToList()
        };

        return Ok(ResponseHelper.Ok(resp));
    }
}
