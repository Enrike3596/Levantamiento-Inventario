using System.Security.Cryptography;
using System.Text;

namespace InventarioTI.Api.Helpers;

public static class ApiKeyHelper
{
    public static string Generar() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=');

    public static string Hash(string apiKey) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));

    public static bool Comparar(string apiKey, string hashAlmacenado) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)),
            Convert.FromBase64String(hashAlmacenado));
}
