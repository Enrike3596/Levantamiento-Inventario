namespace InventarioTI.Api.Helpers;

public class ApiResponse<T>
{
    public bool Exito { get; set; }
    public T? Data { get; set; }
    public string? Mensaje { get; set; }
    public List<string>? Errores { get; set; }
}

public static class ResponseHelper
{
    public static ApiResponse<T> Ok<T>(T data, string? mensaje = null) =>
        new() { Exito = true, Data = data, Mensaje = mensaje };

    public static ApiResponse<T> OkVacio<T>(string? mensaje = null) =>
        new() { Exito = true, Mensaje = mensaje };

    public static ApiResponse<T> Error<T>(string mensaje, List<string>? errores = null) =>
        new() { Exito = false, Mensaje = mensaje, Errores = errores };
}
