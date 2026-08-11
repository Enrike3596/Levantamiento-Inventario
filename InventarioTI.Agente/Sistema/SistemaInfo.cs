using System.Text.Json.Nodes;

namespace InventarioTI.Agente.Sistema;

public class SistemaInfo
{
    public string NombreEquipo { get; set; } = string.Empty;
    public string? UsuarioConectado { get; set; }
    public string? Dominio { get; set; }
    public string? Ip { get; set; }
    public string? Mac { get; set; }
    public string? SistemaOperativo { get; set; }
    public string? VersionSo { get; set; }
    public decimal? RamGb { get; set; }
    public string? Procesador { get; set; }
    public decimal? DiscoGb { get; set; }
    public decimal? EspacioLibreGb { get; set; }
    public string? NumeroSerie { get; set; }
    public string? Fabricante { get; set; }
    public string? Modelo { get; set; }
    public string? Antivirus { get; set; }
    public DateTimeOffset? UltimoReinicio { get; set; }
    public JsonNode? Programas { get; set; }
}
