using System.Collections.Concurrent;

namespace InventarioTI.PollerSnmp.Snmp;

public class TraficoCache
{
    private readonly ConcurrentDictionary<string, (ulong Octetos, DateTime Marca)> _items = new();

    public decimal? Calcular(string clave, ulong octetos, DateTime ahora)
    {
        if (_items.TryGetValue(clave, out var anterior))
        {
            var segundos = (ahora - anterior.Marca).TotalSeconds;
            if (segundos > 0 && octetos >= anterior.Octetos)
            {
                var mbps = (decimal)(octetos - anterior.Octetos) * 8m / 1_000_000m / (decimal)segundos;
                _items[clave] = (octetos, ahora);
                return Math.Round(mbps, 2);
            }
        }
        _items[clave] = (octetos, ahora);
        return null;
    }

    public void LimpiarViejos(DateTime ahora, TimeSpan maximoEdad)
    {
        var limite = ahora - maximoEdad;
        foreach (var kv in _items)
            if (kv.Value.Marca < limite)
                _items.TryRemove(kv.Key, out _);
    }
}
