using System.Globalization;

namespace IndLib;

public static class TxtImporter
{
    public static IndFileData Import(string text, IndFormat format, byte[] headerBytes, string? graficsPath = null)
    {
        var data = new IndFileData { Format = format, FileName = "", HeaderBytes = headerBytes };
        var lines = text.Split('\n');
        IndRecord? current = null;
        GrhEntry? grh = null;
        for (int lineNo = 1; lineNo <= lines.Length; lineNo++)
        {
            var raw = lines[lineNo - 1];
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = new IndRecord { Index = int.Parse(line[1..^1], CultureInfo.InvariantCulture) };
                if (format.Kind == IndFormatKind.FixedRecords || format.Kind == IndFormatKind.TexDefault)
                    data.Records.Add(current);
                continue;
            }
            var eq = line.IndexOf('=');
            if (eq <= 0)
                throw new FormatException($"Línea {lineNo}: se esperaba 'campo = valor'. Línea: '{line}'");
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim();

            if (format.Kind == IndFormatKind.GrhData)
            {
                if (key == "Grh")
                {
                    grh = new GrhEntry { Grh = int.Parse(value, CultureInfo.InvariantCulture) };
                    data.GrhEntries.Add(grh);
                }
                else if (grh == null)
                {
                    throw new FormatException($"Línea {lineNo}: campo '{key}' antes de 'Grh'.");
                }
                else
                {
                    switch (key)
                    {
                        case "NumFrames": grh.NumFrames = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "Frames": grh.Frames = value.Split(',').Select(v => int.Parse(v.Trim(), CultureInfo.InvariantCulture)).ToArray(); break;
                        case "Velocidad": grh.Speed = float.Parse(value, CultureInfo.InvariantCulture); break;
                        case "FileNum": grh.FileNum = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "SX": grh.SX = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "SY": grh.SY = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "Ancho": grh.PixelWidth = int.Parse(value, CultureInfo.InvariantCulture); break;
                        case "Alto": grh.PixelHeight = int.Parse(value, CultureInfo.InvariantCulture); break;
                        default: throw new FormatException($"Línea {lineNo}: campo desconocido '{key}'.");
                    }
                }
                continue;
            }

            if (format.Kind == IndFormatKind.Minimap)
            {
                if (key == "Color")
                    data.MinimapEntries.Add(new MinimapEntry { Grh = 0, Color = uint.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture) });
                continue;
            }

            // FixedRecords / TexDefault
            if (current == null)
                throw new FormatException($"Línea {lineNo}: valor '{key}' antes de la sección [n].");
            var dot = key.LastIndexOf('.');
            if (dot > 0)
            {
                var baseName = key[..dot];
                var idx = int.Parse(key[(dot + 1)..], CultureInfo.InvariantCulture);
                var field = format.Fields.First(f => f.Name == baseName && f.Type == IndFieldType.Int32Array);
                if (!current.Values.TryGetValue(field.Name, out var existing))
                    existing = new int[field.Count];
                var arr = (int[])existing;
                arr[idx - 1] = int.Parse(value, CultureInfo.InvariantCulture);
                current.Values[field.Name] = arr;
                continue;
            }
            var f2 = format.Fields.FirstOrDefault(f => f.Name == key)
                ?? throw new FormatException($"Línea {lineNo}: campo desconocido '{key}'.");
            current.Values[f2.Name] = f2.Type switch
            {
                IndFieldType.Int16 => short.Parse(value, CultureInfo.InvariantCulture),
                IndFieldType.Int32 => int.Parse(value, CultureInfo.InvariantCulture),
                IndFieldType.Single => float.Parse(value, CultureInfo.InvariantCulture),
                IndFieldType.Byte => byte.Parse(value, CultureInfo.InvariantCulture),
                IndFieldType.Boolean => (short)(value is "True" or "1" ? -1 : 0),
                IndFieldType.ByteArray => value.Split(',').Select(v => byte.Parse(v.Trim(), CultureInfo.InvariantCulture)).ToArray(),
                _ => throw new FormatException($"Línea {lineNo}: tipo no soportado para '{key}'."),
            };
        }
        foreach (var e in data.GrhEntries) e.HasData = e.Grh != 0;
        data.Count = format.Kind switch
        {
            IndFormatKind.TexDefault => 1,
            IndFormatKind.Minimap => data.MinimapEntries.Count,
            _ => data.Records.Count,
        };
        return data;
    }
}
