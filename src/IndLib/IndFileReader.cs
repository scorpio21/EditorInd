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
        var variant = DetectVariant(bytes, format);
        var headerSize = variant?.HeaderSize ?? format.HeaderSize;
        var countOffset = variant?.CountOffset ?? format.CountOffset;
        var recordSize = variant?.RecordSize ?? format.RecordSize;
        var fields = variant?.Fields ?? format.Fields;
        var data = new IndFileData { Format = format, FileName = fileName, Variant = variant };
        if (bytes.Length < countOffset + 2)
            throw new InvalidDataException($"Archivo '{fileName}' demasiado corto.");
        data.HeaderBytes = bytes.AsSpan(0, headerSize).ToArray();
        data.Count = BitConverter.ToInt16(bytes, countOffset);
        int start = countOffset + 2;
        for (int i = 0; i < data.Count; i++)
        {
            int off = start + i * recordSize;
            if (off + recordSize > bytes.Length)
                throw new InvalidDataException($"Archivo '{fileName}' truncado: falta el registro {i + 1}.");
            data.Records.Add(ParseRecord(bytes.AsSpan(off, recordSize), fields, i + 1));
        }
        return data;
    }

    private static IndFormatVariant? DetectVariant(byte[] bytes, IndFormat format)
    {
        if (Matches(bytes, format.CountOffset, format.RecordSize)) return null;
        foreach (var v in format.Variants)
            if (Matches(bytes, v.CountOffset, v.RecordSize)) return v;
        return null;
    }

    private static bool Matches(byte[] bytes, int countOffset, int recordSize)
    {
        if (bytes.Length < countOffset + 2) return false;
        int count = BitConverter.ToInt16(bytes, countOffset);
        if (count < 0) return false;
        return bytes.Length == countOffset + 2 + (long)count * recordSize;
    }

    private static IndRecord ParseRecord(ReadOnlySpan<byte> s, IndField[] fields, int index)
    {
        var rec = new IndRecord { Index = index };
        int off = 0;
        foreach (var f in fields)
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
                    rec.Values[f.Name] = BitConverter.ToInt16(s.Slice(off, 2)); off += 2; break;
                case IndFieldType.Byte:
                    rec.Values[f.Name] = s[off]; off += 1; break;
                case IndFieldType.Int16Array:
                    var i16arr = new int[f.Count];
                    for (int j = 0; j < f.Count; j++) i16arr[j] = BitConverter.ToInt16(s.Slice(off + j * 2, 2));
                    off += f.Count * 2;
                    rec.Values[f.Name] = i16arr;
                    break;
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
    {
        var data = new IndFileData { Format = format, FileName = fileName };
        if (bytes.Length < 8) throw new InvalidDataException("graficos.ind truncado.");
        data.HeaderBytes = bytes.AsSpan(0, 8).ToArray();
        data.GrhCount = BitConverter.ToInt32(bytes, 4);
        data.GrhEntries.AddRange(ReadGrhEntries(bytes.AsSpan(8)));
        data.Count = data.GrhEntries.Count;
        return data;
    }

    public static List<GrhEntry> ReadGrhEntries(ReadOnlySpan<byte> s)
    {
        var list = new List<GrhEntry>();
        int pos = 0;
        while (pos < s.Length)
        {
            if (pos + 4 > s.Length) throw new InvalidDataException("graficos.ind truncado (Grh).");
            int grh = BitConverter.ToInt32(s.Slice(pos, 4)); pos += 4;
            var e = new GrhEntry { Grh = grh, HasData = grh != 0 };
            if (grh != 0)
            {
                if (pos + 2 > s.Length) throw new InvalidDataException("graficos.ind truncado (NumFrames).");
                e.NumFrames = BitConverter.ToInt16(s.Slice(pos, 2)); pos += 2;
                if (e.NumFrames > 1)
                {
                    int n = e.NumFrames;
                    if (pos + n * 4 + 4 > s.Length) throw new InvalidDataException("graficos.ind truncado (Frames).");
                    e.Frames = new int[n];
                    for (int j = 0; j < n; j++) { e.Frames[j] = BitConverter.ToInt32(s.Slice(pos, 4)); pos += 4; }
                    e.Speed = BitConverter.ToSingle(s.Slice(pos, 4)); pos += 4;
                }
                else
                {
                    if (pos + 12 > s.Length) throw new InvalidDataException("graficos.ind truncado (estático).");
                    e.FileNum = BitConverter.ToInt32(s.Slice(pos, 4)); pos += 4;
                    e.SX = BitConverter.ToInt16(s.Slice(pos, 2)); pos += 2;
                    e.SY = BitConverter.ToInt16(s.Slice(pos, 2)); pos += 2;
                    e.PixelWidth = BitConverter.ToInt16(s.Slice(pos, 2)); pos += 2;
                    e.PixelHeight = BitConverter.ToInt16(s.Slice(pos, 2)); pos += 2;
                }
            }
            list.Add(e);
        }
        return list;
    }

    public static List<int> GetActiveGrhIndices(byte[] graficsBytes)
    {
        var set = new HashSet<int>();
        foreach (var e in ReadGrhEntries(graficsBytes.AsSpan(8)))
            if (e.Grh != 0) set.Add(e.Grh);
        var list = set.ToList();
        list.Sort();
        return list;
    }

    private static IndFileData ReadTexDefault(byte[] bytes, IndFormat format, string fileName)
    {
        var data = new IndFileData { Format = format, FileName = fileName };
        if (bytes.Length < format.RecordSize)
            throw new InvalidDataException($"Archivo '{fileName}' demasiado corto.");
        data.HeaderBytes = Array.Empty<byte>();
        data.Records.Add(ParseRecord(bytes.AsSpan(0, format.RecordSize), format.Fields, 1));
        data.Count = 1;
        return data;
    }

    private static IndFileData ReadMinimap(byte[] bytes, IndFormat format, string fileName, string? graficsPath)
    {
        var data = new IndFileData { Format = format, FileName = fileName };
        if (bytes.Length % 4 != 0)
            throw new InvalidDataException("minimap.dat con tamaño no múltiplo de 4.");
        int n = bytes.Length / 4;
        var active = new List<int>();
        if (!string.IsNullOrEmpty(graficsPath) && File.Exists(graficsPath))
            active = GetActiveGrhIndices(File.ReadAllBytes(graficsPath));
        for (int i = 0; i < n; i++)
        {
            uint color = BitConverter.ToUInt32(bytes, i * 4);
            data.MinimapEntries.Add(new MinimapEntry { Grh = i < active.Count ? active[i] : 0, Color = color });
        }
        data.Count = n;
        if (active.Count != 0 && active.Count != n)
            data.Warning = $"El nº de colores ({n}) no coincide con los grhs activos de graficos.ind ({active.Count}).";
        return data;
    }

    // Todos los branch del reader (FixedRecords, GrhData, TexDefault, Minimap) están implementados.
}
