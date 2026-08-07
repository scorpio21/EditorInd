namespace IndLib;

public static class IndFileWriter
{
    public static byte[] ToBytes(IndFileData data)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        switch (data.Format.Kind)
        {
            case IndFormatKind.FixedRecords:
                w.Write(data.HeaderBytes);
                w.Write((short)data.Records.Count);
                foreach (var rec in data.Records) WriteRecord(w, data.Variant?.Fields ?? data.Format.Fields, rec);
                break;
            case IndFormatKind.GrhData:
                w.Write(data.HeaderBytes);
                foreach (var e in data.GrhEntries) WriteGrhEntry(w, e);
                break;
            case IndFormatKind.TexDefault:
                WriteTexDefault(w, data);
                break;
            case IndFormatKind.Minimap:
                foreach (var e in data.MinimapEntries) w.Write((int)e.Color);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return ms.ToArray();
    }

    public static void Save(IndFileData data, string path) => File.WriteAllBytes(path, ToBytes(data));

    private static void WriteRecord(BinaryWriter w, IndField[] fields, IndRecord rec)
    {
        foreach (var f in fields)
        {
            switch (f.Type)
            {
                case IndFieldType.Int16: w.Write((short)rec.Values[f.Name]); break;
                case IndFieldType.Int32: w.Write((int)rec.Values[f.Name]); break;
                case IndFieldType.Single: w.Write((float)rec.Values[f.Name]); break;
                case IndFieldType.Boolean: w.Write((short)rec.Values[f.Name]); break;
                case IndFieldType.Byte: w.Write((byte)rec.Values[f.Name]); break;
                case IndFieldType.Int16Array:
                    foreach (var v in (int[])rec.Values[f.Name]) w.Write((short)v);
                    break;
                case IndFieldType.Int32Array:
                    foreach (var v in (int[])rec.Values[f.Name]) w.Write(v);
                    break;
                case IndFieldType.ByteArray:
                    foreach (var v in (byte[])rec.Values[f.Name]) w.Write(v);
                    break;
                default:
                    throw new InvalidDataException($"Tipo de campo no soportado: {f.Type}");
            }
        }
    }

    private static void WriteGrhEntry(BinaryWriter w, GrhEntry e)
    {
        w.Write(e.Grh);
        if (!e.HasData) return;
        // I1: el writer rechaza estado inconsistente (NumFrames != Frames.Length)
        // como defensa en profundidad — el save-time check en MainForm da el error
        // de fila, y este guard impide cualquier write corrupto llegue al disco.
        if (e.NumFrames > 1 && e.Frames.Length != e.NumFrames)
            throw new InvalidOperationException(
                $"Grh {e.Grh}: NumFrames = {e.NumFrames} pero hay {e.Frames.Length} frames.");
        w.Write(AsShort(e.NumFrames, nameof(e.NumFrames), e.Grh));
        if (e.NumFrames > 1)
        {
            foreach (var f in e.Frames) w.Write(f);
            w.Write(e.Speed);
        }
        else
        {
            w.Write(e.FileNum);
            w.Write(AsShort(e.SX, nameof(e.SX), e.Grh));
            w.Write(AsShort(e.SY, nameof(e.SY), e.Grh));
            w.Write(AsShort(e.PixelWidth, nameof(e.PixelWidth), e.Grh));
            w.Write(AsShort(e.PixelHeight, nameof(e.PixelHeight), e.Grh));
        }
    }

    // I2: rechaza wraparound silencioso de Int32 a Int16 en campos del formato
    // (NumFrames/SX/SY/Ancho/Alto son Int16) — el grid valida con ColKind.Int16,
    // y este guard protege cualquier otra ruta de escritura.
    private static short AsShort(int value, string field, int grh)
    {
        if (value is < short.MinValue or > short.MaxValue)
            throw new InvalidOperationException($"Grh {grh}: {field} = {value} está fuera del rango Int16.");
        return (short)value;
    }

    private static void WriteTexDefault(BinaryWriter w, IndFileData data)
    {
        var rec = data.Records[0];
        w.Write((int)rec.Values["BitmapWidth"]);
        w.Write((int)rec.Values["BitmapHeight"]);
        w.Write((int)rec.Values["CellWidth"]);
        w.Write((int)rec.Values["CellHeight"]);
        w.Write((byte)rec.Values["BaseCharOffset"]);
        foreach (var b in (byte[])rec.Values["CharWidth"]) w.Write(b);
    }
}
