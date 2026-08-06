# Formato TXT compacto para graficos.ind (GrhData) - Plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cambiar la exportación/importación TXT del formato `GrhData` al formato compacto clásico de AO (`GrhN=1-1-64-0-32-32-`).

**Architecture:** Solo se tocan `TxtExporter.cs` y `TxtImporter.cs` de `src/IndLib/` (rama `IndFormatKind.GrhData`). El binario no cambia. Tests en `tests/IndLib.Tests/TxtTests.cs`.

**Tech Stack:** .NET 9, xUnit, `dotnet test`.

## Global Constraints

- Cambio aplica SOLO a `IndFormatKind.GrhData`; los demás formatos mantienen su exportación verbosa.
- Líneas de entrada: `GrhN=1-FileNum-SX-SY-Ancho-Alto-` (estática) y `GrhN=NumFrames-F1-...-Fnum-Velocidad-` (animación), con **guion final**.
- Cabecera: `'Graficos.ind desindexado con IndEditor`, `'<fecha>`, línea en blanco, `[Graphics]`, línea en blanco antes de cada entrada.
- Importador: solo formato nuevo; errores con `Línea N: ...`.
- Velocidad con `"R"` invariant culture.
- Test command: `dotnet test tests/IndLib.Tests` desde `K:\Descargas\aaoo\EditorInd`.

---

### Task 1: Exportador GrhData en formato compacto

**Files:**
- Modify: `src/IndLib/TxtExporter.cs` (rama `case IndFormatKind.GrhData:` en `Export`, líneas 25-33)
- Test: `tests/IndLib.Tests/TxtTests.cs`

**Interfaces:**
- Consumes: `IndFileData`, `GrhEntry` (`Grh`, `HasData`, `NumFrames`, `Frames`, `Speed`, `FileNum`, `SX`, `SY`, `PixelWidth`, `PixelHeight`) de `src/IndLib/Models.cs`.
- Produces: método privado `WriteGrhCompact(StringBuilder sb, GrhEntry e)`.

- [ ] **Step 1: Escribir el test que falla**

Agregar a `tests/IndLib.Tests/TxtTests.cs`:

```csharp
[Fact]
public void Grafics_ExportFormatoCompacto()
{
    var data = IndFileReader.Read(P("graficos.ind"));
    var txt = TxtExporter.Export(data);
    Assert.Contains("[Graphics]", txt);
    Assert.Contains("\r\nGrh1=1-1-64-0-32-32-\r\n", txt);
    Assert.DoesNotContain("# Formato: GrhData", txt);
    Assert.DoesNotContain("NumFrames =", txt);
}
```

- [ ] **Step 2: Ejecutar el test para verificar que falla**

Run: `dotnet test tests/IndLib.Tests --filter "FullyQualifiedName~Grafics_ExportFormatoCompacto" -v n`
Expected: FAIL (no contiene `[Graphics]` ni la línea compacta).

- [ ] **Step 3: Implementar el exportador**

Reemplazar el `case IndFormatKind.GrhData:` en `TxtExporter.Export` (líneas 25-33) por:

```csharp
case IndFormatKind.GrhData:
    sb.AppendLine("'Graficos.ind desindexado con IndEditor");
    sb.AppendLine($"'{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    sb.AppendLine();
    sb.AppendLine("[Graphics]");
    foreach (var e in data.GrhEntries)
    {
        if (!e.HasData) continue;
        sb.AppendLine();
        WriteGrhCompact(sb, e);
    }
    break;
```

Agregar el método privado (junto a `WriteGrhEntry`):

```csharp
private static void WriteGrhCompact(StringBuilder sb, GrhEntry e)
{
    sb.Append($"Grh{e.Grh}=");
    if (e.NumFrames > 1)
    {
        sb.Append(e.NumFrames).Append('-');
        foreach (var f in e.Frames) sb.Append(f).Append('-');
        sb.Append(e.Speed.ToString("R", CultureInfo.InvariantCulture)).Append('-');
    }
    else
    {
        sb.Append("1-").Append(e.FileNum).Append('-')
          .Append(e.SX).Append('-')
          .Append(e.SY).Append('-')
          .Append(e.PixelWidth).Append('-')
          .Append(e.PixelHeight).Append('-');
    }
    sb.AppendLine();
}
```

`DateTime` usa el `using System;` implícito; `CultureInfo` y `StringBuilder` ya están importados (líneas 1-2).

- [ ] **Step 4: Ejecutar el test para verificar que pasa**

Run: `dotnet test tests/IndLib.Tests --filter "FullyQualifiedName~Grafics_ExportFormatoCompacto" -v n`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/IndLib/TxtExporter.cs tests/IndLib.Tests/TxtTests.cs
git commit -m "feat: exportar graficos.ind en formato TXT compacto"
```

---

### Task 2: Importador GrhData en formato compacto

**Files:**
- Modify: `src/IndLib/TxtImporter.cs` (rama `GrhData` en `Import`, líneas 31-61)
- Test: `tests/IndLib.Tests/TxtTests.cs`

**Interfaces:**
- Consumes: `GrhEntry`, `IndFileData`, helpers `ParseInt(string, int, string)`, `ParseFloat(string, int, string)`.
- Produces: no expone nuevas APIs públicas.

- [ ] **Step 1: Escribir los tests que fallan**

Agregar a `tests/IndLib.Tests/TxtTests.cs`:

```csharp
[Fact]
public void Grafics_ImportCompacto_Estatico()
{
    const string txt = "'comentario\r\n[Graphics]\r\n\r\nGrh1=1-1-64-0-32-32-\r\n";
    var data = TxtImporter.Import(txt, IndFormatCatalog.Grafics, new byte[8]);
    Assert.Single(data.GrhEntries);
    var e = data.GrhEntries[0];
    Assert.Equal(1, e.Grh);
    Assert.True(e.HasData);
    Assert.Equal(1, e.NumFrames);
    Assert.Equal(1, e.FileNum);
    Assert.Equal(64, e.SX);
    Assert.Equal(0, e.SY);
    Assert.Equal(32, e.PixelWidth);
    Assert.Equal(32, e.PixelHeight);
}

[Fact]
public void Grafics_ImportCompacto_Animacion()
{
    const string txt = "[Graphics]\r\n\r\nGrh23=6-1-2-3-4-5-6-1-\r\n";
    var data = TxtImporter.Import(txt, IndFormatCatalog.Grafics, new byte[8]);
    var e = Assert.Single(data.GrhEntries);
    Assert.Equal(23, e.Grh);
    Assert.Equal(6, e.NumFrames);
    Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, e.Frames);
    Assert.Equal(1f, e.Speed);
}

[Fact]
public void Grafics_Import_RechazaFormatoViejo()
{
    const string txt = "[1]\r\nGrh = 1\r\nNumFrames = 1\r\n";
    var ex = Assert.Throws<FormatException>(() =>
        TxtImporter.Import(txt, IndFormatCatalog.Grafics, new byte[8]));
    Assert.Contains("Línea 2", ex.Message);
}
```

- [ ] **Step 2: Ejecutar los tests para verificar que fallan**

Run: `dotnet test tests/IndLib.Tests --filter "FullyQualifiedName~Grafics_Import" -v n`
Expected: FAIL.

- [ ] **Step 3: Implementar el importador**

Reemplazar la rama `if (format.Kind == IndFormatKind.GrhData)` del bucle en `TxtImporter.Import` (líneas 31-61) por:

```csharp
if (format.Kind == IndFormatKind.GrhData)
{
    if (line.StartsWith('#') || line.StartsWith('\'')) continue;
    if (line.Equals("[Graphics]", StringComparison.OrdinalIgnoreCase)) continue;
    if (line.StartsWith('['))
        throw new FormatException($"Línea {lineNo}: sección no esperada '{line}'. Solo se admite el formato GrhN=...");
    var eqGrh = line.IndexOf('=');
    if (eqGrh <= 0)
        throw new FormatException($"Línea {lineNo}: se esperaba 'GrhN=valor'. Línea: '{line}'");
    var key = line[..eqGrh].Trim();
    var value = line[(eqGrh + 1)..].Trim();
    if (!key.StartsWith("Grh", StringComparison.OrdinalIgnoreCase) || key.Length <= 3)
        throw new FormatException($"Línea {lineNo}: clave '{key}' inválida. Se esperaba 'GrhN'.");
    var grhNum = ParseInt(key[3..], lineNo, key);
    var parts = value.TrimEnd('-').Split('-');
    var numFrames = ParseInt(parts[0], lineNo, key);
    var e = new GrhEntry { Grh = grhNum, HasData = true, NumFrames = numFrames };
    if (numFrames == 1)
    {
        if (parts.Length != 6)
            throw new FormatException($"Línea {lineNo}: la entrada estática requiere 6 valores. Línea: '{line}'");
        e.FileNum = ParseInt(parts[1], lineNo, key);
        e.SX = ParseInt(parts[2], lineNo, key);
        e.SY = ParseInt(parts[3], lineNo, key);
        e.PixelWidth = ParseInt(parts[4], lineNo, key);
        e.PixelHeight = ParseInt(parts[5], lineNo, key);
    }
    else if (numFrames > 1)
    {
        if (parts.Length != numFrames + 2)
            throw new FormatException($"Línea {lineNo}: la animación con {numFrames} frames requiere {numFrames + 2} valores. Línea: '{line}'");
        e.Frames = new int[numFrames];
        for (int j = 0; j < numFrames; j++) e.Frames[j] = ParseInt(parts[1 + j], lineNo, key);
        e.Speed = ParseFloat(parts[^1], lineNo, key);
    }
    else
    {
        throw new FormatException($"Línea {lineNo}: NumFrames inválido ({numFrames}) para Grh{grhNum}.");
    }
    data.GrhEntries.Add(e);
    continue;
}
```

También corregir el cálculo de `data.Count` al final del método (líneas 106-111) para que `GrhData` cuente entradas:

```csharp
data.Count = format.Kind switch
{
    IndFormatKind.TexDefault => 1,
    IndFormatKind.Minimap => data.MinimapEntries.Count,
    IndFormatKind.GrhData => data.GrhEntries.Count,
    _ => data.Records.Count,
};
```

- [ ] **Step 4: Ejecutar los tests para verificar que pasan**

Run: `dotnet test tests/IndLib.Tests --filter "FullyQualifiedName~Grafics_Import" -v n`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/IndLib/TxtImporter.cs tests/IndLib.Tests/TxtTests.cs
git commit -m "feat: importar graficos.ind en formato TXT compacto"
```

---

### Task 3: Round-trip con el archivo real y suite completa

**Files:**
- Test: `tests/IndLib.Tests/TxtTests.cs`

**Interfaces:**
- Consumes: `IndFileReader.Read`, `TxtExporter.Export`, `TxtImporter.Import`, `IndFileWriter.ToBytes`.

- [ ] **Step 1: Escribir test de round-trip byte-exacto para el formato nuevo**

Agregar a `tests/IndLib.Tests/TxtTests.cs`:

```csharp
[Fact]
public void Grafics_ExportImport_RoundTripCompacto()
{
    var data = IndFileReader.Read(P("graficos.ind"));
    var txt = TxtExporter.Export(data);
    var imported = TxtImporter.Import(txt, data.Format, data.HeaderBytes);
    Assert.Equal(IndFileWriter.ToBytes(data), IndFileWriter.ToBytes(imported));
    Assert.Equal(data.Count, imported.Count);
}
```

- [ ] **Step 2: Ejecutar el test para verificar que pasa**

Run: `dotnet test tests/IndLib.Tests --filter "FullyQualifiedName~Grafics_ExportImport_RoundTripCompacto" -v n`
Expected: PASS (la primera entrada del archivo real es `Grh1=1-1-64-0-32-32`).

- [ ] **Step 3: Ejecutar la suite completa**

Run: `dotnet test tests/IndLib.Tests -v n`
Expected: PASS (incluye `Export_Import_RoundTrip` existente para `graficos.ind` y el resto de formatos sin cambios).

- [ ] **Step 4: Commit**

```bash
git add tests/IndLib.Tests/TxtTests.cs
git commit -m "test: round-trip byte-exacto del TXT compacto de graficos.ind"
```

---

## Self-Review

- **Spec coverage:** espec cubierta: cabecera (`'` + fecha + `[Graphics]`), estática, animación, guion final, tolerancia al guion en import, solo formato nuevo con error de línea, `Count` corregido, round-trip. Todo mapeado a las 3 tareas.
- **Placeholders:** todos los pasos tienen código completo.
- **Type consistency:** `WriteGrhCompact` firmado con `StringBuilder`; `GrhEntry` propiedades usadas son las de `Models.cs`; `ParseInt`/`ParseFloat` firmas `(string, int, string)` como en el código actual.
