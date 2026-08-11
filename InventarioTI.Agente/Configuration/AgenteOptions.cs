namespace InventarioTI.Agente.Configuration;

public class AgenteOptions
{
    public const string Seccion = "Agente";

    public string ApiUrl { get; set; } = "http://localhost:5007";
    public string ApiKey { get; set; } = string.Empty;
    public bool AutoRegistro { get; set; } = true;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public int IntervaloMinutos { get; set; } = 15;
    public int MaxProgramas { get; set; } = 200;
}
