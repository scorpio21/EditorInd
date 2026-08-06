namespace IndLib;

public static class IndFormatCatalog
{
    private static readonly IndField[] IndiceFields =
    {
        new() { Name = "Body", Type = IndFieldType.Int32Array, Count = 4, Label = "Cuerpo" },
        new() { Name = "HeadOffsetX", Type = IndFieldType.Int16, Label = "Despl. X" },
        new() { Name = "HeadOffsetY", Type = IndFieldType.Int16, Label = "Despl. Y" },
    };

    private static readonly IndField[] FxFields =
    {
        new() { Name = "Animacion", Type = IndFieldType.Int32, Label = "Animación" },
        new() { Name = "offsetX", Type = IndFieldType.Int16, Label = "Offset X" },
        new() { Name = "offsetY", Type = IndFieldType.Int16, Label = "Offset Y" },
        new() { Name = "FXTransparente", Type = IndFieldType.Boolean, Label = "Transparente" },
    };

    private static readonly IndField[] HeadFields =
    {
        new() { Name = "Texture", Type = IndFieldType.Int16, Label = "Textura" },
        new() { Name = "startX", Type = IndFieldType.Int16, Label = "Inicio X" },
        new() { Name = "startY", Type = IndFieldType.Int16, Label = "Inicio Y" },
    };

    private static readonly IndField[] TexDefaultFields =
    {
        new() { Name = "BitmapWidth", Type = IndFieldType.Int32, Label = "Ancho mapa bits" },
        new() { Name = "BitmapHeight", Type = IndFieldType.Int32, Label = "Alto mapa bits" },
        new() { Name = "CellWidth", Type = IndFieldType.Int32, Label = "Ancho celda" },
        new() { Name = "CellHeight", Type = IndFieldType.Int32, Label = "Alto celda" },
        new() { Name = "BaseCharOffset", Type = IndFieldType.Byte, Label = "Offset carácter base" },
        new() { Name = "CharWidth", Type = IndFieldType.ByteArray, Count = 256, Label = "Anchos de carácter" },
    };

    public static IndFormat Ataques { get; } = new()
    {
        Name = "tIndiceAtaque", DisplayName = "Ataques",
        FilePatterns = new[] { "ataques.ind" },
        Kind = IndFormatKind.FixedRecords,
        HeaderSize = 263, HasCount = true, CountOffset = 263,
        Fields = IndiceFields, RecordSize = 20,
    };

    public static IndFormat Personajes { get; } = new()
    {
        Name = "tIndiceCuerpo", DisplayName = "Personajes",
        FilePatterns = new[] { "personajes.ind" },
        Kind = IndFormatKind.FixedRecords,
        HeaderSize = 263, HasCount = true, CountOffset = 263,
        Fields = IndiceFields, RecordSize = 20,
    };

    public static IndFormat Fxs { get; } = new()
    {
        Name = "tIndiceFx", DisplayName = "FXs",
        FilePatterns = new[] { "fxs.ind" },
        Kind = IndFormatKind.FixedRecords,
        HeaderSize = 263, HasCount = true, CountOffset = 263,
        Fields = FxFields, RecordSize = 10,
    };

    public static IndFormat Cabezas { get; } = new()
    {
        Name = "tHead", DisplayName = "Cabezas",
        FilePatterns = new[] { "cabezas.ind" },
        Kind = IndFormatKind.FixedRecords,
        HeaderSize = 0, HasCount = true, CountOffset = 0,
        Fields = HeadFields, RecordSize = 6,
    };

    public static IndFormat Cascos { get; } = new()
    {
        Name = "tHead", DisplayName = "Cascos",
        FilePatterns = new[] { "cascos.ind" },
        Kind = IndFormatKind.FixedRecords,
        HeaderSize = 0, HasCount = true, CountOffset = 0,
        Fields = HeadFields, RecordSize = 6,
    };

    public static IndFormat Grafics { get; } = new()
    {
        Name = "GrhData", DisplayName = "Gráficos",
        FilePatterns = new[] { "graficos.ind" },
        Kind = IndFormatKind.GrhData, HeaderSize = 8,
    };

    public static IndFormat TexDefault1 { get; } = TexDefault("texdefault1.dat", "Fuente 1");
    public static IndFormat TexDefault2 { get; } = TexDefault("texdefault2.dat", "Fuente 2");
    public static IndFormat TexDefault3 { get; } = TexDefault("texdefault3.dat", "Fuente 3");

    private static IndFormat TexDefault(string pattern, string display) => new()
    {
        Name = "texdefault", DisplayName = display,
        FilePatterns = new[] { pattern },
        Kind = IndFormatKind.TexDefault,
        Fields = TexDefaultFields, RecordSize = 273,
    };

    public static IndFormat Minimap { get; } = new()
    {
        Name = "minimap", DisplayName = "Minimapa",
        FilePatterns = new[] { "minimap.dat" },
        Kind = IndFormatKind.Minimap, RequiresGrafics = true,
    };

    public static IReadOnlyList<IndFormat> All { get; } = new[]
    {
        Ataques, Personajes, Fxs, Cabezas, Cascos, Grafics, TexDefault1, TexDefault2, TexDefault3, Minimap,
    };
}
