namespace IndLib;

public static class IndFileReader
{
    public static IndFileData Read(string path, string? graficsPath = null)
    {
        var fileName = Path.GetFileName(path);
        var format = IndFormatDetector.Detect(fileName)
            ?? throw new InvalidDataException($"Formato no reconocido para '{fileName}'.");
        var bytes = File.ReadAllBytes(path);
        return format.Kind switch
        {
            IndFormatKind.FixedRecords => ReadFixedRecords(bytes, format, fileName),
            IndFormatKind.GrhData => ReadGrh(bytes, format, fileName),
            IndFormatKind.TexDefault => ReadTexDefault(bytes, format, fileName),
            IndFormatKind.Minimap => ReadMinimap(bytes, format, fileName, graficsPath),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static IndFileData ReadFixedRecords(byte[] bytes, IndFormat format, string fileName)
    {
        var data = new IndFileData { Format = format, FileName = fileName };
        if (bytes.Length < format.CountOffset + 2)
            throw new InvalidDataException($"Archivo '{fileName}' demasiado corto.");
        data.HeaderBytes = bytes.AsSpan(0, format.HeaderSize).ToArray();
        data.Count = BitConverter.ToInt16(bytes, format.CountOffset);
        int start = format.CountOffset + 2;
        for (int i = 0; i < data.Count; i++)
        {
            int off = start + i * format.RecordSize;
            if (off + format.RecordSize > bytes.Length)
                throw new InvalidDataException($"Archivo '{fileName}' truncado: falta el registro {i + 1}.");
            data.Records.Add(ParseRecord(bytes.AsSpan(off, format.RecordSize), format, i + 1));
        }
        return data;
    }

    private static IndRecord ParseRecord(ReadOnlySpan<byte> s, IndFormat format, int index)
    {
        var rec = new IndRecord { Index = index };
        int off = 0;
        foreach (var f in format.Fields)
        {
            switch (f.Type)
            {
                case IndFieldType.Int16:
                    rec.Values[f.Name] = BitConverter.ToInt16(s.Slice(off, 2)); off += 2; break;
                case IndFieldType.Int32:
                    rec.Values[f.Name] = BitConverter.ToInt32(s.Slice(off, 4)); off += 4; break;
                case IndFieldType.Single:
                    rec.Values[f.Name] = BitConverter.ToSingle(s.Slice(off, 4)); off += 4; break;
                case IndFieldType.Boolean:
                    rec.Values[f.Name] = BitConverter.ToInt16(s.Slice(off, 2)) != 0; off += 2; break;
                case IndFieldType.Byte:
                    rec.Values[f.Name] = s[off]; off += 1; break;
                case IndFieldType.Int32Array:
                    var arr = new int[f.Count];
                    for (int j = 0; j < f.Count; j++) arr[j] = BitConverter.ToInt32(s.Slice(off + j * 4, 4));
                    off += f.Count * 4;
                    rec.Values[f.Name] = arr;
                    break;
                case IndFieldType.ByteArray:
                    var barr = new byte[f.Count];
                    for (int j = 0; j < f.Count; j++) barr[j] = s[off + j];
                    off += f.Count;
                    rec.Values[f.Name] = barr;
                    break;
                default:
                    throw new InvalidDataException($"Tipo de campo no soportado: {f.Type}");
            }
        }
        return rec;
    }

    private static IndFileData ReadGrh(byte[] bytes, IndFormat format, string fileName)
        => throw new NotImplementedException("Task 4");
    private static IndFileData ReadTexDefault(byte[] bytes, IndFormat format, string fileName)
        => throw new NotImplementedException("Task 5");
    private static IndFileData ReadMinimap(byte[] bytes, IndFormat format, string fileName, string? graficsPath)
        => throw new NotImplementedException("Task 5");

    // ReadGrh, ReadTexDefault, ReadMinimap, ReadGrhEntries, GetActiveGrhIndices se añaden en Tasks 4 y 5.
}
