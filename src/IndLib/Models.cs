namespace IndLib;

public enum IndFieldType { Int16, Int32, Single, Boolean, Byte, Int32Array, ByteArray }

public sealed class IndField
{
    public string Name { get; init; } = "";
    public IndFieldType Type { get; init; }
    public int Count { get; init; } = 1;
    public string Label { get; init; } = "";
}

public enum IndFormatKind { FixedRecords, GrhData, TexDefault, Minimap }

public sealed class IndFormat
{
    public string Name { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string[] FilePatterns { get; init; } = Array.Empty<string>();
    public IndFormatKind Kind { get; init; }
    public int HeaderSize { get; init; }
    public bool HasCount { get; init; }
    public int CountOffset { get; init; }
    public IndField[] Fields { get; init; } = Array.Empty<IndField>();
    public int RecordSize { get; init; }
    public bool RequiresGrafics { get; init; }
}

public sealed class IndRecord
{
    public int Index { get; set; }
    public Dictionary<string, object> Values { get; } = new();
}

public sealed class GrhEntry
{
    public int Grh { get; set; }
    public bool HasData { get; set; }
    public int NumFrames { get; set; }
    public int[] Frames { get; set; } = Array.Empty<int>();
    public float Speed { get; set; }
    public int FileNum { get; set; }
    public int SX { get; set; }
    public int SY { get; set; }
    public int PixelWidth { get; set; }
    public int PixelHeight { get; set; }
}

public sealed class MinimapEntry
{
    public int Grh { get; set; }
    public uint Color { get; set; }
}

public sealed class IndFileData
{
    public IndFormat Format { get; set; } = null!;
    public string FileName { get; set; } = "";
    public byte[] HeaderBytes { get; set; } = Array.Empty<byte>();
    public int Count { get; set; }
    public List<IndRecord> Records { get; } = new();
    public List<GrhEntry> GrhEntries { get; } = new();
    public int GrhCount { get; set; }
    public List<MinimapEntry> MinimapEntries { get; } = new();
    public string Warning { get; set; } = "";
}
