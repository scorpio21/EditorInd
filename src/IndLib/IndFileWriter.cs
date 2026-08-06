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
                foreach (var rec in data.Records) WriteRecord(w, data.Format, rec);
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

    private static void WriteRecord(BinaryWriter w, IndFormat format, IndRecord rec)
    {
        foreach (var f in format.Fields)
        {
            switch (f.Type)
            {
                case IndFieldType.Int16: w.Write((short)rec.Values[f.Name]); break;
                case IndFieldType.Int32: w.Write((int)rec.Values[f.Name]); break;
                case IndFieldType.Single: w.Write((float)rec.Values[f.Name]); break;
                case IndFieldType.Boolean: w.Write((short)rec.Values[f.Name]); break;
                case IndFieldType.Byte: w.Write((byte)rec.Values[f.Name]); break;
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
        w.Write((short)e.NumFrames);
        if (e.NumFrames > 1)
        {
            foreach (var f in e.Frames) w.Write(f);
            w.Write(e.Speed);
        }
        else
        {
            w.Write(e.FileNum);
            w.Write((short)e.SX);
            w.Write((short)e.SY);
            w.Write((short)e.PixelWidth);
            w.Write((short)e.PixelHeight);
        }
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
