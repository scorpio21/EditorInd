using System.Globalization;

namespace IndLib;

public static class TxtImporter
{
    public static IndFileData Import(string text, IndFormat format, byte[] headerBytes, string? graficsPath = null)
    {
        var data = new IndFileData { Format = format, FileName = "", HeaderBytes = headerBytes };
        var lines = text.Split('\n');
        IndFormatVariant? variant = null;
        IndRecord? current = null;
        GrhEntry? grh = null;
        for (int lineNo = 1; lineNo <= lines.Length; lineNo++)
        {
            var raw = lines[lineNo - 1];
            var line = raw.TrimEnd('\r').Trim();
            if (line.Length == 0) continue;
            if (line.StartsWith("# Variante:", StringComparison.OrdinalIgnoreCase))
            {
                var vname = line["# Variante:".Length..].Trim();
                variant = format.Variants.FirstOrDefault(v => v.Name.Equals(vname, StringComparison.OrdinalIgnoreCase));
                continue;
            }
            if (line.StartsWith('#')) continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                current = new IndRecord { Index = ParseInt(line[1..^1], lineNo, "[sección]") };
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
                    grh = new GrhEntry { Grh = ParseInt(value, lineNo, key) };
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
                        case "NumFrames": grh.NumFrames = ParseInt(value, lineNo, key); break;
                        case "Frames":
                            grh.Frames = value.Split(',')
                                .Select(v => ParseInt(v.Trim(), lineNo, key)).ToArray();
                            break;
                        case "Velocidad": grh.Speed = ParseFloat(value, lineNo, key); break;
                        case "FileNum": grh.FileNum = ParseInt(value, lineNo, key); break;
                        case "SX": grh.SX = ParseInt(value, lineNo, key); break;
                        case "SY": grh.SY = ParseInt(value, lineNo, key); break;
                        case "Ancho": grh.PixelWidth = ParseInt(value, lineNo, key); break;
                        case "Alto": grh.PixelHeight = ParseInt(value, lineNo, key); break;
                        default: throw new FormatException($"Línea {lineNo}: campo desconocido '{key}'.");
                    }
                }
                continue;
            }

            if (format.Kind == IndFormatKind.Minimap)
            {
                if (key == "Color")
                    data.MinimapEntries.Add(new MinimapEntry { Grh = 0, Color = ParseUint(value, lineNo, key) });
                continue;
            }

            // FixedRecords / TexDefault
            if (current == null)
                throw new FormatException($"Línea {lineNo}: valor '{key}' antes de la sección [n].");
            var fields = variant?.Fields ?? format.Fields;
            var dot = key.LastIndexOf('.');
            if (dot > 0)
            {
                var baseName = key[..dot];
                var idx = ParseInt(key[(dot + 1)..], lineNo, key);
                if (idx < 1)
                    throw new FormatException($"Línea {lineNo}: índice inválido en '{key}'.");
                var field = fields.FirstOrDefault(f => f.Name == baseName && f.Type is IndFieldType.Int32Array or IndFieldType.Int16Array)
                    ?? throw new FormatException($"Línea {lineNo}: campo desconocido '{key}'.");
                if (idx > field.Count)
                    throw new FormatException($"Línea {lineNo}: índice {idx} fuera de rango para '{field.Name}' ({field.Count} elementos).");
                if (!current.Values.TryGetValue(field.Name, out var existing))
                    existing = new int[field.Count];
                var arr = (int[])existing;
                arr[idx - 1] = field.Type == IndFieldType.Int16Array
                    ? ParseShort(value, lineNo, key)
                    : ParseInt(value, lineNo, key);
                current.Values[field.Name] = arr;
                continue;
            }
            var f2 = fields.FirstOrDefault(f => f.Name == key)
                ?? throw new FormatException($"Línea {lineNo}: campo desconocido '{key}'.");
            current.Values[f2.Name] = f2.Type switch
            {
                IndFieldType.Int16 => ParseShort(value, lineNo, key),
                IndFieldType.Int32 => ParseInt(value, lineNo, key),
                IndFieldType.Single => ParseFloat(value, lineNo, key),
                IndFieldType.Byte => ParseByte(value, lineNo, key),
                IndFieldType.Boolean => (short)(value is "True" or "1" ? -1 : 0),
                IndFieldType.ByteArray => value.Split(',')
                    .Select(v => ParseByte(v.Trim(), lineNo, key)).ToArray(),
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
        data.Variant = variant;
        return data;
    }

    // I3: todo error numérico/de clave lleva "Línea N" — el spec exige
    // mensaje con número de línea para TXT inválido (design doc, line 187).
    private static int ParseInt(string value, int lineNo, string key)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new FormatException($"Línea {lineNo}: valor '{value}' inválido para '{key}'.");
        return v;
    }

    private static short ParseShort(string value, int lineNo, string key)
    {
        if (!short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new FormatException($"Línea {lineNo}: valor '{value}' inválido para '{key}'.");
        return v;
    }

    private static float ParseFloat(string value, int lineNo, string key)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
            throw new FormatException($"Línea {lineNo}: valor '{value}' inválido para '{key}'.");
        return v;
    }

    private static byte ParseByte(string value, int lineNo, string key)
    {
        if (!byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            throw new FormatException($"Línea {lineNo}: valor '{value}' inválido para '{key}'.");
        return v;
    }

    private static uint ParseUint(string value, int lineNo, string key)
    {
        if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            throw new FormatException($"Línea {lineNo}: valor '{value}' inválido para '{key}'.");
        return v;
    }
}
