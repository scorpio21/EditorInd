# Compatibilidad Dual Binaria + Export TXT estilo DESINDDAT — Plan de Implementación

> **Para workers agénticos:** SUB-HABILIDAD REQUERIDA: usar superpowers:subagent-driven-development (recomendado) o superpowers:executing-plans para implementar este plan tarea por tarea. Los pasos usan checkboxes (`- [ ]`) para seguimiento.

**Goal:** Que el IndEditor detecte automáticamente la variante binaria de los `.ind` (Aom2018 Int16 vs Aodrag9 Int32), la conserve al guardar, y permita exportar TXT en el formato clásico DESINDDAT además del formato actual.

**Architecture:** Se agrega un concepto de "variante" a `IndFormat` (`IndFormatVariant`: `HeaderSize`, `CountOffset`, `Fields`, `RecordSize`). `IndFileReader` detecta la variante por tamaño de archivo y la guarda en `IndFileData.Variant`. El writer y la UI usan siempre `Variant?.Fields ?? Format.Fields`. Se agrega `IndFieldType.Int16Array` (leído como `int[]` de 2 bytes) para los campos Body de Aom2018. `TxtExporter.ExportDesinddat` genera el formato clásico de `DESINDDAT/main.c`.

**Tech Stack:** C#/.NET 9, xUnit (ya en el repo). Referencia del formato clásico: `K:\Argentum\Aomania\Aom2018\Caom\AomUtilidad2012\configurador\DESINDDAT\main.c`.

## Global Constraints

- Round-trip byte-exacto: abrir+guardar sin modificar produce bytes idénticos al original.
- La variante detectada al cargar se conserva al guardar (Int16→Int16, Int32→Int32).
- Los mensajes de error son en español e incluyen el número de línea (`Línea N: ...`) en la importación de TXT.
- Los 55 tests existentes deben seguir pasando; solo se añaden tests nuevos.
- Las variantes Aodrag9 siguen siendo el layout por defecto de cada `IndFormat` (no cambia `RecordSize`/`CountOffset` base).
- Referencia verificada de formatos binarios (variante Aom2018): Personajes/Cabezas/Cascos/Fxs en `K:\Argentum\Aomania\Aom2018\Caom\AomUtilidad2012\configurador\DESINDDAT\`.
- Referencia verificada (variante Aodrag9): `k:\Descargas\aaoo\init\`.

---

### Task 1: Modelo de variantes + catálogo + detección en el reader

**Files:**
- Modify: `src/IndLib/Models.cs`
- Modify: `src/IndLib/IndFormatCatalog.cs`
- Modify: `src/IndLib/IndFileReader.cs`
- Test: `tests/IndLib.Tests/DualFormatTests.cs` (crear), `tests/IndLib.Tests/TestPaths.cs`

**Interfaces:**
- Produces: `IndFieldType.Int16Array` (nuevo enum), `class IndFormatVariant { string Name; int HeaderSize; int CountOffset; IndField[] Fields; int RecordSize }`, `IndFormat.Variants` (IndFormatVariant[]), `IndFileData.Variant` (IndFormatVariant?), `IndFormatCatalog.VariantAom2018Personajes` (y Ataques/Fxs/Cabezas/Cascos), `IndFileReader.Read` detecta y setea `data.Variant`.

- [ ] **Step 1: Escribir tests fallidos de detección**

`tests/IndLib.Tests/TestPaths.cs` — añadir:

```csharp
public static string Aom2018Dir =>
    Environment.GetEnvironmentVariable("AO_AOM2018_DIR") ?? @"K:\Argentum\Aomania\Aom2018\Caom\AomUtilidad2012\configurador\DESINDDAT";
```

`tests/IndLib.Tests/DualFormatTests.cs` (crear):

```csharp
using IndLib;
using Xunit;

namespace IndLib.Tests;

public class DualFormatTests
{
    private static string A(string name) => Path.Combine(TestPaths.Aom2018Dir, name);
    private static string I(string name) => Path.Combine(TestPaths.InitDir, name);

    [Fact]
    public void Personajes_Aom2018_DetectaVariantInt16()
    {
        var data = IndFileReader.Read(A("Personajes.ind"));
        Assert.NotNull(data.Variant);
        Assert.Equal("Aom2018-Int16", data.Variant!.Name);
        Assert.Equal(660, data.Count);
        var rec = data.Records[0];
        Assert.Equal(new[] { 4582, 4584, 4581, 4583 }, (int[])rec.Values["Body"]);
        Assert.Equal((short)0, (short)rec.Values["HeadOffsetX"]);
        Assert.Equal((short)-38, (short)rec.Values["HeadOffsetY"]);
    }

    [Fact]
    public void Personajes_Aodrag9_UsaLayoutPorDefecto()
    {
        var data = IndFileReader.Read(I("personajes.ind"));
        Assert.Null(data.Variant);
        Assert.Equal(470, data.Count);
    }

    [Fact]
    public void Cabezas_Aom2018_DetectaVariant4Campos()
    {
        var data = IndFileReader.Read(A("Cabezas.ind"));
        Assert.NotNull(data.Variant);
        Assert.Equal("Aom2018-4Head", data.Variant!.Name);
        Assert.Equal(831, data.Count);
        Assert.Equal(4, data.Variant.Fields.Length);
    }
}
```

- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test tests/IndLib.Tests --filter DualFormatTests -v q`
Expected: ERROR de compilación (`IndFormatVariant` no existe) → los tests no compilan.

- [ ] **Step 3: Implementar modelo (`Models.cs`)**

Añadir al enum: `public enum IndFieldType { Int16, Int32, Single, Boolean, Byte, Int16Array, Int32Array, ByteArray }`

Añadir clase y propiedad a `IndFormat`:

```csharp
public sealed class IndFormatVariant
{
    public string Name { get; init; } = "";
    public int HeaderSize { get; init; }
    public int CountOffset { get; init; }
    public IndField[] Fields { get; init; } = Array.Empty<IndField>();
    public int RecordSize { get; init; }
}
```

En `IndFormat` añadir: `public IndFormatVariant[] Variants { get; init; } = Array.Empty<IndFormatVariant>();`

En `IndFileData` añadir: `public IndFormatVariant? Variant { get; set; }`

- [ ] **Step 4: Implementar variantes en el catálogo (`IndFormatCatalog.cs`)**

```csharp
private static readonly IndField[] IndiceInt16Fields =
{
    new() { Name = "Body", Type = IndFieldType.Int16Array, Count = 4, Label = "Cuerpo" },
    new() { Name = "HeadOffsetX", Type = IndFieldType.Int16, Label = "Despl. X" },
    new() { Name = "HeadOffsetY", Type = IndFieldType.Int16, Label = "Despl. Y" },
};

private static readonly IndField[] FxInt16Fields =
{
    new() { Name = "Animacion", Type = IndFieldType.Int16, Label = "Animación" },
    new() { Name = "offsetX", Type = IndFieldType.Int16, Label = "Offset X" },
    new() { Name = "offsetY", Type = IndFieldType.Int16, Label = "Offset Y" },
};

private static readonly IndField[] Head4Fields =
{
    new() { Name = "Head0", Type = IndFieldType.Int16, Label = "Cabeza 0" },
    new() { Name = "Head1", Type = IndFieldType.Int16, Label = "Cabeza 1" },
    new() { Name = "Head2", Type = IndFieldType.Int16, Label = "Cabeza 2" },
    new() { Name = "Head3", Type = IndFieldType.Int16, Label = "Cabeza 3" },
};

public static IndFormatVariant VariantAom2018Personajes { get; } = new()
{
    Name = "Aom2018-Int16", HeaderSize = 263, CountOffset = 263,
    Fields = IndiceInt16Fields, RecordSize = 12,
};

public static IndFormatVariant VariantAom2018Fxs { get; } = new()
{
    Name = "Aom2018-Int16", HeaderSize = 263, CountOffset = 263,
    Fields = FxInt16Fields, RecordSize = 6,
};

public static IndFormatVariant VariantAom2018Cabezas { get; } = new()
{
    Name = "Aom2018-4Head", HeaderSize = 263, CountOffset = 263,
    Fields = Head4Fields, RecordSize = 8,
};
```

Añadir `Variants = new[] { VariantAom2018Personajes }` a `Ataques` y `Personajes`; `Variants = new[] { VariantAom2018Fxs }` a `Fxs`; `Variants = new[] { VariantAom2018Cabezas }` a `Cabezas` y `Cascos`.

- [ ] **Step 5: Implementar detección en `IndFileReader.cs`**

Refactorizar `ReadFixedRecords` y añadir helpers:

```csharp
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
```

En `ParseRecord` añadir el caso:

```csharp
case IndFieldType.Int16Array:
    var i16arr = new int[f.Count];
    for (int j = 0; j < f.Count; j++) i16arr[j] = BitConverter.ToInt16(s.Slice(off + j * 2, 2));
    off += f.Count * 2;
    rec.Values[f.Name] = i16arr;
    break;
```

Nota: `ParseRecord(ReadOnlySpan<byte> s, IndFormat format, int index)` ahora recibe `fields` en vez de `format`; actualizar la firma a `ParseRecord(ReadOnlySpan<byte> s, IndField[] fields, int index)` y el `foreach (var f in format.Fields)` → `foreach (var f in fields)`.

- [ ] **Step 6: Ejecutar tests**

Run: `dotnet test tests/IndLib.Tests --filter DualFormatTests -v q`
Expected: PASS (3 tests).

- [ ] **Step 7: Commit**

```bash
git add src/IndLib/Models.cs src/IndLib/IndFormatCatalog.cs src/IndLib/IndFileReader.cs tests/IndLib.Tests/DualFormatTests.cs tests/IndLib.Tests/TestPaths.cs
git commit -m "feat: variantes binarias Int16/Int32 con detección automática al cargar"
```

---

### Task 2: Writer conserva la variante + soporte Int16Array

**Files:**
- Modify: `src/IndLib/IndFileWriter.cs`
- Test: `tests/IndLib.Tests/DualFormatTests.cs`

**Interfaces:**
- Consumes: `IndFileData.Variant`, `IndFieldType.Int16Array`.
- Produces: escritura byte-exacta para archivos Aom2018.

- [ ] **Step 1: Escribir tests fallidos de round-trip Aom2018**

En `tests/IndLib.Tests/DualFormatTests.cs` añadir:

```csharp
[Theory]
[InlineData("Personajes.ind")]
[InlineData("Cabezas.ind")]
[InlineData("Cascos.ind")]
[InlineData("Fxs.ind")]
public void Aom2018_RoundTripByteExacto(string file)
{
    var original = File.ReadAllBytes(A(file));
    var data = IndFileReader.Read(A(file));
    Assert.Equal(original, IndFileWriter.ToBytes(data));
}
```

- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test tests/IndLib.Tests --filter "Aom2018_RoundTripByteExacto" -v q`
Expected: FAIL (el writer aún escribe con campos Int32 de 20 bytes).

- [ ] **Step 3: Implementar writer**

En `IndFileWriter.ToBytes` caso `IndFormatKind.FixedRecords`:

```csharp
case IndFormatKind.FixedRecords:
    w.Write(data.HeaderBytes);
    w.Write((short)data.Records.Count);
    foreach (var rec in data.Records) WriteRecord(w, data.Variant?.Fields ?? data.Format.Fields, rec);
    break;
```

En `WriteRecord` añadir caso:

```csharp
case IndFieldType.Int16Array:
    foreach (var v in (int[])rec.Values[f.Name]) w.Write((short)v);
    break;
```

- [ ] **Step 4: Ejecutar tests**

Run: `dotnet test tests/IndLib.Tests --filter "Aom2018_RoundTripByteExacto" -v q`
Expected: PASS (4 casos).

- [ ] **Step 5: Commit**

```bash
git add src/IndLib/IndFileWriter.cs tests/IndLib.Tests/DualFormatTests.cs
git commit -m "feat: writer conserva variante binaria y soporta Int16Array"
```

---

### Task 3: TXT actual (export/import) con variante + Int16Array

**Files:**
- Modify: `src/IndLib/TxtExporter.cs`
- Modify: `src/IndLib/TxtImporter.cs`
- Test: `tests/IndLib.Tests/DualFormatTests.cs`

**Interfaces:**
- Consumes: `IndFileData.Variant`.
- Produces: `TxtExporter.Export` escribe `# Variante: <Name|default>`; `TxtImporter.Import` lo lee y setea `data.Variant`; ambos manejan `Int16Array`.

- [ ] **Step 1: Escribir tests fallidos de round-trip TXT Aom2018**

En `tests/IndLib.Tests/DualFormatTests.cs` añadir:

```csharp
[Fact]
public void Aom2018_TxtRoundTripByteExacto()
{
    var data = IndFileReader.Read(A("Personajes.ind"));
    var txt = TxtExporter.Export(data);
    var imported = TxtImporter.Import(txt, data.Format, data.HeaderBytes);
    Assert.Equal(IndFileWriter.ToBytes(data), IndFileWriter.ToBytes(imported));
}
```

- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test tests/IndLib.Tests --filter "Aom2018_TxtRoundTripByteExacto" -v q`
Expected: FAIL (bytes distintos: el import pierde la variante).

- [ ] **Step 3: Implementar exporter actual**

En `TxtExporter.Export` caso `IndFormatKind.FixedRecords`:

```csharp
case IndFormatKind.FixedRecords:
    sb.AppendLine($"# Registros: {data.Records.Count}");
    sb.AppendLine($"# Variante: {data.Variant?.Name ?? "default"}");
    var fields = data.Variant?.Fields ?? data.Format.Fields;
    foreach (var rec in data.Records)
    {
        sb.AppendLine();
        sb.AppendLine($"[{rec.Index}]");
        WriteRecord(sb, fields, rec);
    }
    break;
```

En `WriteRecord` el bloque de arrays debe aceptar `Int16Array` y `Int32Array`:

```csharp
if (f.Type == IndFieldType.Int32Array || f.Type == IndFieldType.Int16Array)
{
    var arr = (int[])rec.Values[f.Name];
    for (int j = 0; j < arr.Length; j++)
        sb.AppendLine($"{f.Name}.{j + 1} = {arr[j]}");
}
```

- [ ] **Step 4: Implementar importador actual**

En `TxtImporter.Import`, antes del loop de líneas añadir `IndFormatVariant? variant = null;`. En el loop, reemplazar el chequeo de comentarios:

```csharp
if (line.Length == 0) continue;
if (line.StartsWith("# Variante:", StringComparison.OrdinalIgnoreCase))
{
    var vname = line["# Variante:".Length..].Trim();
    variant = format.Variants.FirstOrDefault(v => v.Name.Equals(vname, StringComparison.OrdinalIgnoreCase));
    continue;
}
if (line.StartsWith('#')) continue;
```

Antes de procesar campos, `var fields = variant?.Fields ?? format.Fields;`. En la rama de punto (array), cambiar la búsqueda:

```csharp
var field = fields.FirstOrDefault(f => f.Name == baseName && f.Type is IndFieldType.Int32Array or IndFieldType.Int16Array)
    ?? throw new FormatException($"Línea {lineNo}: campo desconocido '{key}'.");
```

Y la búsqueda de campo simple `format.Fields.FirstOrDefault(...)` → `fields.FirstOrDefault(...)`. Al final, antes del `return data`, `data.Variant = variant;`.

- [ ] **Step 5: Ejecutar tests**

Run: `dotnet test tests/IndLib.Tests -v q`
Expected: PASS (incluye `Aom2018_TxtRoundTripByteExacto` y los 55 existentes).

- [ ] **Step 6: Commit**

```bash
git add src/IndLib/TxtExporter.cs src/IndLib/TxtImporter.cs tests/IndLib.Tests/DualFormatTests.cs
git commit -m "feat: TXT actual conserva la variante (export/import) y soporta Int16Array"
```

---

### Task 4: Export TXT estilo DESINDDAT

**Files:**
- Modify: `src/IndLib/TxtExporter.cs`
- Test: `tests/IndLib.Tests/DualFormatTests.cs`

**Interfaces:**
- Produces: `string TxtExporter.ExportDesinddat(IndFileData data)`.

- [ ] **Step 1: Escribir tests fallidos del formato DESINDDAT**

En `tests/IndLib.Tests/DualFormatTests.cs` añadir:

```csharp
[Fact]
public void Personajes_Aom2018_ExportDesinddat()
{
    var data = IndFileReader.Read(A("Personajes.ind"));
    var txt = TxtExporter.ExportDesinddat(data);
    Assert.Contains("[INIT]", txt);
    Assert.Contains("NumBodies=660", txt);
    Assert.Contains("[Body1]", txt);
    Assert.Contains("Walk1=4582", txt);
    Assert.Contains("Walk2=4584", txt);
    Assert.Contains("Walk3=4581", txt);
    Assert.Contains("Walk4=4583", txt);
    Assert.Contains("HeadOffsetX=0", txt);
    Assert.Contains("HeadOffsetY=-38", txt);
}

[Fact]
public void Personajes_Aodrag9_ExportDesinddat()
{
    var data = IndFileReader.Read(I("personajes.ind"));
    var txt = TxtExporter.ExportDesinddat(data);
    Assert.Contains("NumBodies=470", txt);
    Assert.Contains("[Body1]", txt);
    var rec = data.Records[0];
    var body = (int[])rec.Values["Body"];
    Assert.Contains($"Walk1={body[0]}", txt);
}

[Fact]
public void Cabezas_Aodrag9_ExportDesinddat_Mapea3a4()
{
    var data = IndFileReader.Read(I("cabezas.ind"));
    var txt = TxtExporter.ExportDesinddat(data);
    Assert.Contains("NumHeads=654", txt);
    Assert.Contains("[Head1]", txt);
    var rec = data.Records[0];
    Assert.Contains($"Head0={(short)rec.Values["Texture"]}", txt);
    Assert.Contains($"Head1={(short)rec.Values["startX"]}", txt);
    Assert.Contains($"Head2={(short)rec.Values["startY"]}", txt);
    Assert.Contains("Head3=0", txt);
}

[Fact]
public void Cabezas_Aom2018_ExportDesinddat()
{
    var data = IndFileReader.Read(A("Cabezas.ind"));
    var txt = TxtExporter.ExportDesinddat(data);
    Assert.Contains("NumHeads=831", txt);
    Assert.Contains("[Head1]", txt);
    var rec = data.Records[0];
    Assert.Contains($"Head0={(short)rec.Values["Head0"]}", txt);
}

[Fact]
public void Fxs_Aodrag9_ExportDesinddat()
{
    var data = IndFileReader.Read(I("fxs.ind"));
    var txt = TxtExporter.ExportDesinddat(data);
    Assert.Contains("NumFxs=59", txt);
    Assert.Contains("[FX1]", txt);
    Assert.Contains($"Animacion={Convert.ToInt32(data.Records[0].Values["Animacion"])}", txt);
    Assert.Contains($"OffsetX={(short)data.Records[0].Values["offsetX"]}", txt);
}
```

- [ ] **Step 2: Verificar que fallan**

Run: `dotnet test tests/IndLib.Tests --filter "ExportDesinddat" -v q`
Expected: ERROR de compilación (`ExportDesinddat` no existe).

- [ ] **Step 3: Implementar `ExportDesinddat`**

En `TxtExporter.cs` añadir (con `using System.Globalization;` ya presente):

```csharp
public static string ExportDesinddat(IndFileData data)
{
    if (data.Format.Kind != IndFormatKind.FixedRecords)
        throw new InvalidOperationException("El formato DESINDDAT solo aplica a archivos de registros fijos.");
    var sb = new StringBuilder();
    var fecha = DateTime.Now.ToString("ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture);
    sb.AppendLine($"' {data.FileName}, desindexado: {fecha}");
    sb.AppendLine();
    sb.AppendLine();
    var fields = data.Variant?.Fields ?? data.Format.Fields;
    switch (data.Format.DisplayName)
    {
        case "Personajes": WriteDesinddatBody(sb, data, "NumBodies", "Body", fields); break;
        case "Ataques": WriteDesinddatBody(sb, data, "NumAtaques", "Body", fields); break;
        case "FXs": WriteDesinddatFx(sb, data, fields); break;
        case "Cabezas": WriteDesinddatHead(sb, data, "NumHeads", "Head", zeroBased: true, fields); break;
        case "Cascos": WriteDesinddatHead(sb, data, "NumCascos", "Casco", zeroBased: false, fields); break;
        default:
            throw new InvalidOperationException($"No hay exportación DESINDDAT para '{data.Format.DisplayName}'.");
    }
    return sb.ToString();
}

private static void WriteDesinddatBody(StringBuilder sb, IndFileData data, string initKey, string section, IndField[] fields)
{
    sb.AppendLine("[INIT]");
    sb.AppendLine($"{initKey}={data.Records.Count}");
    sb.AppendLine();
    foreach (var rec in data.Records)
    {
        sb.AppendLine($"[{section}{rec.Index}]");
        var body = (int[])rec.Values["Body"];
        for (int j = 0; j < body.Length; j++)
            sb.AppendLine($"Walk{j + 1}={body[j]}");
        sb.AppendLine($"HeadOffsetX={Convert.ToInt32(rec.Values["HeadOffsetX"])}");
        sb.AppendLine($"HeadOffsetY={Convert.ToInt32(rec.Values["HeadOffsetY"])}");
        sb.AppendLine();
    }
}

private static void WriteDesinddatFx(StringBuilder sb, IndFileData data, IndField[] fields)
{
    sb.AppendLine("[INIT]");
    sb.AppendLine($"NumFxs={data.Records.Count}");
    sb.AppendLine();
    foreach (var rec in data.Records)
    {
        sb.AppendLine($"[FX{rec.Index}]");
        sb.AppendLine($"Animacion={Convert.ToInt32(rec.Values["Animacion"])}");
        sb.AppendLine($"OffsetX={Convert.ToInt32(rec.Values["offsetX"])}");
        sb.AppendLine($"OffsetY={Convert.ToInt32(rec.Values["offsetY"])}");
        sb.AppendLine();
    }
}

private static void WriteDesinddatHead(StringBuilder sb, IndFileData data, string initKey, string section, bool zeroBased, IndField[] fields)
{
    sb.AppendLine("[INIT]");
    sb.AppendLine($"{initKey}={data.Records.Count}");
    sb.AppendLine();
    bool has4 = fields.Length >= 4;
    foreach (var rec in data.Records)
    {
        sb.AppendLine($"[{section}{rec.Index}]");
        int[] vals = has4
            ? new[] { Convert.ToInt32(rec.Values["Head0"]), Convert.ToInt32(rec.Values["Head1"]), Convert.ToInt32(rec.Values["Head2"]), Convert.ToInt32(rec.Values["Head3"]) }
            : new[] { Convert.ToInt32(rec.Values["Texture"]), Convert.ToInt32(rec.Values["startX"]), Convert.ToInt32(rec.Values["startY"]), 0 };
        for (int j = 0; j < 4; j++)
            sb.AppendLine($"Head{j + (zeroBased ? 0 : 1)}={vals[j]}");
        sb.AppendLine();
    }
}
```

- [ ] **Step 4: Ejecutar tests**

Run: `dotnet test tests/IndLib.Tests --filter "ExportDesinddat" -v q`
Expected: PASS (5 tests).

- [ ] **Step 5: Commit**

```bash
git add src/IndLib/TxtExporter.cs tests/IndLib.Tests/DualFormatTests.cs
git commit -m "feat: export TXT estilo DESINDDAT para personajes/ataques/fxs/cabezas/cascos"
```

---

### Task 5: UI — selector de formato + grid con variante

**Files:**
- Modify: `src/IndEditor/MainForm.cs`

**Interfaces:**
- Consumes: `IndFileData.Variant`, `TxtExporter.ExportDesinddat`, `IndFieldType.Int16Array`.

- [ ] **Step 1: Implementar grid con variante**

En `PopulateFixedGrid` reemplazar `foreach (var f in _data!.Format.Fields)` (ambos bucles) por `var fields = _data!.Variant?.Fields ?? _data.Format.Fields; foreach (var f in fields)`. Añadir el caso Int16Array al crear columnas (igual que Int32Array pero `ColKind.Int16`):

```csharp
case IndFieldType.Int16Array:
    for (int j = 0; j < f.Count; j++) AddCol($"{f.Name}.{j + 1}", $"{f.Label} {j + 1}", ColKind.Int16);
    break;
```

En la creación de filas, el bloque `if (f.Type == IndFieldType.Int32Array)` debe ser `if (f.Type is IndFieldType.Int32Array or IndFieldType.Int16Array)` (los valores ya son `int[]`).

En `SaveFixedFromGrid`, reemplazar `_data!.Format.Fields` por `var fields = _data!.Variant?.Fields ?? _data.Format.Fields;` y el `if (f.Type == IndFieldType.Int32Array)` → `if (f.Type is IndFieldType.Int32Array or IndFieldType.Int16Array)`.

En `AddRow` (caso FixedRecords), reemplazar `_data.Format.Fields` por `var fields = _data.Variant?.Fields ?? _data.Format.Fields;` y el `if (f.Type == IndFieldType.Int32Array)` → `if (f.Type is IndFieldType.Int32Array or IndFieldType.Int16Array)`.

En `UpdateStatus`, mostrar la variante:

```csharp
_lblFormat.Text = _data == null ? "" : _data.Variant == null ? _data.Format.DisplayName : $"{_data.Format.DisplayName} ({_data.Variant.Name})";
```

- [ ] **Step 2: Implementar selector de formato en ExportTxt**

Añadir enum privado y reemplazar `ExportTxt`:

```csharp
private enum TxtFormatChoice { Current, Desinddat }

private TxtFormatChoice? ChooseTxtFormat()
{
    using var dlg = new Form
    {
        Text = "Formato de exportación",
        StartPosition = FormStartPosition.CenterParent,
        FormBorderStyle = FormBorderStyle.FixedDialog,
        MaximizeBox = false,
        MinimizeBox = false,
    };
    dlg.Width = 320; dlg.Height = 150;
    var rbCurrent = new RadioButton { Text = "Formato actual", Left = 15, Top = 15, AutoSize = true, Checked = true };
    var rbDesinddat = new RadioButton { Text = "Formato DESINDDAT (AO clásico)", Left = 15, Top = 42, AutoSize = true };
    var btnOk = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Left = 110, Top = 80, Width = 85 };
    var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Left = 205, Top = 80, Width = 85 };
    dlg.Controls.AddRange(new Control[] { rbCurrent, rbDesinddat, btnOk, btnCancel });
    dlg.AcceptButton = btnOk;
    dlg.CancelButton = btnCancel;
    if (dlg.ShowDialog(this) != DialogResult.OK) return null;
    return rbDesinddat.Checked ? TxtFormatChoice.Desinddat : TxtFormatChoice.Current;
}

private void ExportTxt(object? sender, EventArgs e)
{
    if (_data == null) return;
    using var dlg = new SaveFileDialog { Filter = "Texto|*.txt|Todos|*.*", FileName = _data.FileName + ".txt" };
    if (dlg.ShowDialog(this) != DialogResult.OK) return;
    try
    {
        ApplyEdits();
        string txt;
        if (_data.Format.Kind == IndFormatKind.FixedRecords)
        {
            var choice = ChooseTxtFormat();
            if (choice == null) return;
            txt = choice == TxtFormatChoice.Desinddat ? TxtExporter.ExportDesinddat(_data) : TxtExporter.Export(_data);
        }
        else
        {
            txt = TxtExporter.Export(_data);
        }
        File.WriteAllText(dlg.FileName, txt);
    }
    catch (Exception ex)
    {
        MessageBox.Show(this, $"Error al exportar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
```

- [ ] **Step 3: Compilar**

Run: `dotnet build src/IndEditor -v q`
Expected: BUILD SUCCEEDED.

- [ ] **Step 4: Ejecutar todos los tests**

Run: `dotnet test tests/IndLib.Tests -v q`
Expected: PASS (55 existentes + nuevos).

- [ ] **Step 5: Commit**

```bash
git add src/IndEditor/MainForm.cs
git commit -m "feat: selector de formato TXT (actual/DESINDDAT) y grid con variante binaria"
```

---

### Task 6: README + verificación final + push

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Actualizar README**

- En la introducción: mencionar soporte de ambos formatos binarios (Aom2018 Int16 y Aodrag9 Int32) con detección automática.
- Añadir a "Características": detección automática de variante binaria y exportación estilo DESINDDAT.
- En "Formatos soportados": añadir nota sobre las variantes Int16/Int32.
- En la sección "Uso"/"Formato TXT": documentar el selector de formato y el formato DESINDDAT.
- Actualizar el conteo de pruebas (55 → el total real de la suite).

- [ ] **Step 2: Verificación completa**

Run: `dotnet test tests/IndLib.Tests -v q`
Expected: PASS, sin warnings ni fallos.

- [ ] **Step 3: Commit final**

```bash
git add README.md
git commit -m "docs: README con compatibilidad dual binaria y export DESINDDAT"
```

- [ ] **Step 4: Push a GitHub**

```bash
git push origin main
```
Expected: push exitoso de todos los commits a `https://github.com/scorpio21/EditorInd`.

- [ ] **Step 5: Confirmar con el usuario**

Reportar: commits hechos, push OK, y ofrecer crear un PR si se prefiere en lugar de `main` directo.
