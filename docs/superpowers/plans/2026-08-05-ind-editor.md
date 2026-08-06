# IndEditor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir IndEditor, una aplicación .NET 10 (WinForms) para leer, editar y guardar los archivos binarios `.ind` y `.dat` de Argentum Online, con exportación/importación `.txt`.

**Architecture:** Solución con 3 proyectos: `IndLib` (librería de lógica pura: formatos declarativos, lectura/escritura binaria, TXT), `IndEditor` (UI WinForms en español), `IndLib.Tests` (xUnit). La lectura/escritura usa definiciones declarativas de formato (`IndFormat` + `IndField`); la cabecera `tCabecera` se preserva como bytes crudos para round-trip byte-exacto.

**Tech Stack:** .NET 10 (SDK 10.0.100), WinForms (`net10.0-windows`), xUnit, dotnet CLI.

## Global Constraints

- Solo .NET 10 SDK disponible; target: `net10.0-windows` (WinForms), `net10.0` (librería y tests).
- Interfaz y mensajes en español.
- Round-trip byte-exacto: leer → escribir → los bytes deben ser idénticos al original (todas las pruebas lo verifican).
- Cabecera `tCabecera` (263 B) preservada como bytes crudos, nunca recalculada.
- Formato detectado por nombre de archivo exacto (case-insensitive).
- Valores VB6: `Integer`=Int16, `Long`=Int32, `Single`=float, `Boolean`=2 bytes (True = 0xFFFF).
- Archivos reales de prueba en `K:\Descargas\aaoo\init\` (ruta sobreescribible con variable de entorno `AO_INIT_DIR`).
- No modificar el directorio del juego directamente; Guardar pregunta y ofrece copia `.bak`.

---

### Task 1: Andamiaje de la solución y proyectos

**Files:**
- Create: `IndEditor.sln` (raíz `K:\Descargas\aaoo\EditorInd`)
- Create: `src/IndLib/IndLib.csproj`
- Create: `src/IndEditor/IndEditor.csproj`, `src/IndEditor/Program.cs`, `src/IndEditor/MainForm.cs`, `src/IndEditor/MainForm.Designer.cs`
- Create: `tests/IndLib.Tests/IndLib.Tests.csproj`
- Create: `.gitignore`

**Interfaces:**
- Consumes: nada.
- Produces: estructura de carpetas `src/` y `tests/`, solución con referencias entre proyectos. Los proyectos compilan en vacío.

- [ ] **Step 1: Crear solución y proyectos con plantillas dotnet**

Run (en `K:\Descargas\aaoo\EditorInd`):
```bash
dotnet new sln -n IndEditor
dotnet new classlib -n IndLib -o src/IndLib --framework net10.0
dotnet new winforms -n IndEditor -o src/IndEditor --framework net10.0
dotnet new xunit -n IndLib.Tests -o tests/IndLib.Tests --framework net10.0
dotnet sln add src/IndLib src/IndEditor tests/IndLib.Tests
dotnet add src/IndEditor reference src/IndLib
dotnet add tests/IndLib.Tests reference src/IndLib
```

- [ ] **Step 2: Renombrar la forma por defecto a MainForm**

`git mv src/IndEditor/Form1.cs src/IndEditor/MainForm.cs` y `git mv src/IndEditor/Form1.Designer.cs src/IndEditor/MainForm.Designer.cs`.
Editar `MainForm.cs`: clase `public partial class MainForm : Form`. Editar `MainForm.Designer.cs`: mismo nombre. Editar `Program.cs`: `Application.Run(new MainForm());`.

- [ ] **Step 3: .gitignore**

Create `.gitignore`:
```gitignore
bin/
obj/
*.user
.vs/
```

- [ ] **Step 4: Compilar y verificar**

Run: `dotnet build IndEditor.sln`
Expected: build correcto (0 errores).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore: andamiaje de la solución IndEditor (IndLib, IndEditor, tests)"
```

---

### Task 2: Modelos, catálogo de formatos y detector

**Files:**
- Create: `src/IndLib/Models.cs`
- Create: `src/IndLib/IndFormatCatalog.cs`
- Create: `src/IndLib/IndFormatDetector.cs`
- Test: `tests/IndLib.Tests/DetectorTests.cs`

**Interfaces:**
- Consumes: nada (solo el runtime).
- Produces:
  - `enum IndFieldType { Int16, Int32, Single, Boolean, Byte, Int32Array, ByteArray }`
  - `class IndField { string Name; IndFieldType Type; int Count = 1; string Label; }`
  - `enum IndFormatKind { FixedRecords, GrhData, TexDefault, Minimap }`
  - `class IndFormat { string Name; string DisplayName; string[] FilePatterns; IndFormatKind Kind; int HeaderSize; bool HasCount; int CountOffset; IndField[] Fields; int RecordSize; bool RequiresGrafics; }`
  - `class IndRecord { int Index; Dictionary<string, object> Values; }`
  - `class GrhEntry { int Grh; bool HasData; int NumFrames; int[] Frames; float Speed; int FileNum; int SX; int SY; int PixelWidth; int PixelHeight; }`
  - `class MinimapEntry { int Grh; uint Color; }`
  - `class IndFileData { IndFormat Format; string FileName; byte[] HeaderBytes; int Count; List<IndRecord> Records; List<GrhEntry> GrhEntries; int GrhCount; List<MinimapEntry> MinimapEntries; string Warning; }`
  - `class IndFormatCatalog` (propiedades estáticas `Ataques`, `Personajes`, `Fxs`, `Cabezas`, `Cascos`, `Grafics`, `TexDefault1`, `TexDefault2`, `TexDefault3`, `Minimap`, y `All`)
  - `class IndFormatDetector` con `static IndFormat? Detect(string fileName)`

- [ ] **Step 1: Escribir los tests que fallan**

`tests/IndLib.Tests/DetectorTests.cs`:
```csharp
using IndLib;
using Xunit;

namespace IndLib.Tests;

public class DetectorTests
{
    [Theory]
    [InlineData("ataques.ind", "tIndiceAtaque")]
    [InlineData("personajes.ind", "tIndiceCuerpo")]
    [InlineData("fxs.ind", "tIndiceFx")]
    [InlineData("cabezas.ind", "tHead")]
    [InlineData("cascos.ind", "tHead")]
    [InlineData("graficos.ind", "GrhData")]
    [InlineData("texdefault1.dat", "texdefault")]
    [InlineData("minimap.dat", "minimap")]
    public void Detect_Conocidos(string file, string expectedName)
    {
        var f = IndFormatDetector.Detect(file);
        Assert.NotNull(f);
        Assert.Equal(expectedName, f!.Name);
    }

    [Fact]
    public void Detect_Desconocido_DevuelveNull()
    {
        Assert.Null(IndFormatDetector.Detect("basura.dat"));
    }

    [Fact]
    public void Detect_CaseInsensitive()
    {
        Assert.Equal(IndFormatCatalog.Ataques, IndFormatDetector.Detect("ATAQUES.IND"));
    }

    [Fact]
    public void Catalog_TamanosDeRegistro()
    {
        Assert.Equal(20, IndFormatCatalog.Ataques.RecordSize);
        Assert.Equal(20, IndFormatCatalog.Personajes.RecordSize);
        Assert.Equal(10, IndFormatCatalog.Fxs.RecordSize);
        Assert.Equal(6, IndFormatCatalog.Cabezas.RecordSize);
        Assert.Equal(6, IndFormatCatalog.Cascos.RecordSize);
        Assert.Equal(263, IndFormatCatalog.Ataques.HeaderSize);
        Assert.Equal(273, IndFormatCatalog.TexDefault1.RecordSize);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IndLib.Tests`
Expected: FAIL (no existe `IndLib.IndFormatDetector`).

- [ ] **Step 3: Implementar Models.cs**

`src/IndLib/Models.cs`:
```csharp
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
```

- [ ] **Step 4: Implementar IndFormatCatalog.cs**

`src/IndLib/IndFormatCatalog.cs`:
```csharp
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
```

- [ ] **Step 5: Implementar IndFormatDetector.cs**

`src/IndLib/IndFormatDetector.cs`:
```csharp
namespace IndLib;

public static class IndFormatDetector
{
    public static IndFormat? Detect(string fileName)
    {
        var name = Path.GetFileName(fileName).ToLowerInvariant();
        foreach (var f in IndFormatCatalog.All)
        {
            foreach (var p in f.FilePatterns)
            {
                if (name == p.ToLowerInvariant()) return f;
            }
        }
        return null;
    }
}
```

- [ ] **Step 6: Ejecutar y verificar que pasan**

Run: `dotnet test tests/IndLib.Tests`
Expected: PASS (todos).

- [ ] **Step 7: Commit**

```bash
git add src/IndLib tests/IndLib.Tests
git commit -m "feat: modelos, catálogo de formatos y detector de IndLib"
```

---

### Task 3: IndFileReader — formatos de registros fijos

**Files:**
- Create: `src/IndLib/IndFileReader.cs`
- Test: `tests/IndLib.Tests/TestPaths.cs`
- Test: `tests/IndLib.Tests/ParseTests.cs`

**Interfaces:**
- Consumes: `IndFormat`, `IndFormatDetector`, modelos de Task 2.
- Produces: `static IndFileData IndFileReader.Read(string path, string? graficsPath = null)`; internos `ReadFixedRecords`, `ParseRecord`. La clase se completa en Tasks 4 y 5 (ramas GrhData/TexDefault/Minimap).

- [ ] **Step 1: Escribir los tests que fallan**

`tests/IndLib.Tests/TestPaths.cs`:
```csharp
namespace IndLib.Tests;

public static class TestPaths
{
    public static string InitDir =>
        Environment.GetEnvironmentVariable("AO_INIT_DIR") ?? @"K:\Descargas\aaoo\init";
}
```

`tests/IndLib.Tests/ParseTests.cs`:
```csharp
using IndLib;
using Xunit;

namespace IndLib.Tests;

public class ParseTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    [Fact]
    public void Ataques_CuentaRegistros()
    {
        Assert.True(File.Exists(P("ataques.ind")), $"Falta {P("ataques.ind")}. Ajusta AO_INIT_DIR.");
        var data = IndFileReader.Read(P("ataques.ind"));
        Assert.Equal(62, data.Count);
        Assert.Equal(62, data.Records.Count);
    }

    [Fact] public void Personajes_CuentaRegistros() => Assert.Equal(470, IndFileReader.Read(P("personajes.ind")).Count);
    [Fact] public void Fxs_CuentaRegistros() => Assert.Equal(59, IndFileReader.Read(P("fxs.ind")).Count);
    [Fact] public void Cabezas_CuentaRegistros() => Assert.Equal(654, IndFileReader.Read(P("cabezas.ind")).Count);
    [Fact] public void Cascos_CuentaRegistros() => Assert.Equal(44, IndFileReader.Read(P("cascos.ind")).Count);

    [Fact]
    public void Ataques_PrimerRegistro_Valores()
    {
        var rec = IndFileReader.Read(P("ataques.ind")).Records[0];
        var body = (int[])rec.Values["Body"];
        Assert.Equal(20466, body[0]);
        Assert.Equal(20467, body[1]);
        Assert.Equal(20469, body[2]);
        Assert.Equal(20468, body[3]);
        Assert.Equal(0, (short)rec.Values["HeadOffsetX"]);
        Assert.Equal(0, (short)rec.Values["HeadOffsetY"]);
    }

    [Fact]
    public void Cabezas_PrimerRegistro_Valores()
    {
        var rec = IndFileReader.Read(P("cabezas.ind")).Records[0];
        Assert.Equal(202, (short)rec.Values["Texture"]);
    }

    [Fact]
    public void Lectura_ArchivoDesconocido_Lanza()
    {
        Assert.Throws<InvalidDataException>(() => IndFileReader.Read(P("armas.dat")));
    }
}
```

Nota: el primer registro de `ataques.ind` (offset 265) es `Body=(20466,20467,20469,20468)`, `HeadOffsetX=0`, `HeadOffsetY=0` (verificado byte a byte sobre `K:\Descargas\aaoo\init\ataques.ind`; el patrón `200,27,0,200` está dentro de la cabecera, no en el registro). `cabezas.ind` rec0 `Texture=202`; `cascos.ind` rec0 `Texture=200`.

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IndLib.Tests`
Expected: FAIL (no existe `IndFileReader`).

- [ ] **Step 3: Implementar IndFileReader.cs (FixedRecords + switch)** 

`src/IndLib/IndFileReader.cs`:
```csharp
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
                    for (int j = 0; j < f.Count; j++) arr[j] = BitConverter.ToInt32(s.Slice(off, 4));
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

    // ReadGrh, ReadTexDefault, ReadMinimap, ReadGrhEntries, GetActiveGrhIndices se añaden en Tasks 4 y 5.
}
```

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test tests/IndLib.Tests`
Expected: PASS (los tests de FixedRecords).

- [ ] **Step 5: Commit**

```bash
git add src/IndLib/IndFileReader.cs tests/IndLib.Tests
git commit -m "feat: lectura de formatos de registros fijos (.ind)"
```

---

### Task 4: IndFileReader — graficos.ind (GrhData) e índices activos

**Files:**
- Modify: `src/IndLib/IndFileReader.cs` (añadir `ReadGrh`, `ReadGrhEntries`, `GetActiveGrhIndices`)
- Test: `tests/IndLib.Tests/GrhTests.cs`

**Interfaces:**
- Consumes: `IndFormat` (Kind=GrhData), modelos.
- Produces: `static List<GrhEntry> ReadGrhEntries(ReadOnlySpan<byte> s)` (parsea desde el byte 8); `static List<int> GetActiveGrhIndices(byte[] graficsBytes)` (índices no-cero únicos ordenados).

- [ ] **Step 1: Escribir los tests que fallan**

`tests/IndLib.Tests/GrhTests.cs`:
```csharp
using IndLib;
using Xunit;

namespace IndLib.Tests;

public class GrhTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    [Fact]
    public void Grafics_Conteos()
    {
        var data = IndFileReader.Read(P("graficos.ind"));
        Assert.Equal(24548, data.Count);
        Assert.Equal(24548, data.GrhEntries.Count);
        Assert.Equal(25517, data.GrhCount);
        Assert.True(data.GrhEntries.Count(e => e.NumFrames > 1) > 2000, "debe haber animaciones");
    }

    [Fact]
    public void Grafics_PrimeraEntrada_EsEstatica()
    {
        var e = IndFileReader.Read(P("graficos.ind")).GrhEntries[0];
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
    public void Grafics_IndicesActivos_OrdenadosUnicos()
    {
        var active = IndFileReader.GetActiveGrhIndices(File.ReadAllBytes(P("graficos.ind")));
        Assert.Equal(24548, active.Count);
        Assert.Equal(1, active[0]);
        for (int i = 1; i < active.Count; i++) Assert.True(active[i] > active[i - 1]);
    }
}
```

Nota: 24548 y 25517 son los valores verificados contra el archivo real (size=481384, 24548 entradas, GrhCount=25517). Si el parseo real en ejecución da un conteo distinto, comprobar contra `size=8+Σ entradas` antes de ajustar la expectativa.

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IndLib.Tests`
Expected: FAIL (la rama GrhData de `Read` lanza `ArgumentOutOfRangeException` o falta `ReadGrh`).

- [ ] **Step 3: Implementar ReadGrh/ReadGrhEntries/GetActiveGrhIndices**

Añadir a `IndFileReader.cs`:
```csharp
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
```

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test tests/IndLib.Tests`
Expected: PASS (incluidos los de Task 3).

- [ ] **Step 5: Commit**

```bash
git add src/IndLib/IndFileReader.cs tests/IndLib.Tests/GrhTests.cs
git commit -m "feat: lectura de graficos.ind (GrhData) e índices activos"
```

---

### Task 5: IndFileReader — texdefault y minimap

**Files:**
- Modify: `src/IndLib/IndFileReader.cs` (añadir `ReadTexDefault`, `ReadMinimap`)
- Test: `tests/IndLib.Tests/DatTests.cs`

**Interfaces:**
- Consumes: `IndFormat` (Kinds TexDefault, Minimap), `GetActiveGrhIndices`.
- Produces: ramas TexDefault y Minimap completas en `IndFileReader.Read`.

- [ ] **Step 1: Escribir los tests que fallan**

`tests/IndLib.Tests/DatTests.cs`:
```csharp
using IndLib;
using Xunit;

namespace IndLib.Tests;

public class DatTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    [Theory]
    [InlineData("texdefault1.dat", 256, 256, 17, 17, 32)]
    [InlineData("texdefault2.dat", 2048, 1024, 70, 70, 32)]
    public void TexDefault_Valores(string file, int bw, int bh, int cw, int ch, int bco)
    {
        var data = IndFileReader.Read(P(file));
        Assert.Equal(1, data.Count);
        var rec = data.Records[0];
        Assert.Equal(bw, (int)rec.Values["BitmapWidth"]);
        Assert.Equal(bh, (int)rec.Values["BitmapHeight"]);
        Assert.Equal(cw, (int)rec.Values["CellWidth"]);
        Assert.Equal(ch, (int)rec.Values["CellHeight"]);
        Assert.Equal(bco, (byte)rec.Values["BaseCharOffset"]);
        Assert.Equal(256, ((byte[])rec.Values["CharWidth"]).Length);
    }

    [Fact]
    public void Minimap_CuentaColores()
    {
        var data = IndFileReader.Read(P("minimap.dat"), P("graficos.ind"));
        Assert.Equal(24546, data.Count);
        Assert.Equal(24546, data.MinimapEntries.Count);
        Assert.NotEqual("", data.Warning); // 24546 colores != 24548 grhs activos
    }

    [Fact]
    public void Minimap_PrimerColor()
    {
        var data = IndFileReader.Read(P("minimap.dat"), P("graficos.ind"));
        Assert.Equal(1u, data.MinimapEntries[0].Grh);
        Assert.Equal(0x00000000u, data.MinimapEntries[0].Color);
        Assert.Equal(0x000000FFu, data.MinimapEntries[1].Color);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IndLib.Tests`
Expected: FAIL (ramas TexDefault/Minimap inexistentes en `Read`).

- [ ] **Step 3: Implementar ReadTexDefault y ReadMinimap**

Añadir a `IndFileReader.cs`:
```csharp
    private static IndFileData ReadTexDefault(byte[] bytes, IndFormat format, string fileName)
    {
        var data = new IndFileData { Format = format, FileName = fileName };
        if (bytes.Length < format.RecordSize)
            throw new InvalidDataException($"Archivo '{fileName}' demasiado corto.");
        data.HeaderBytes = Array.Empty<byte>();
        data.Records.Add(ParseRecord(bytes.AsSpan(0, format.RecordSize), format, 1));
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
```

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test tests/IndLib.Tests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/IndLib/IndFileReader.cs tests/IndLib.Tests/DatTests.cs
git commit -m "feat: lectura de texdefaultN.dat y minimap.dat"
```

---

### Task 6: IndFileWriter — round-trip byte-exacto

**Files:**
- Create: `src/IndLib/IndFileWriter.cs`
- Test: `tests/IndLib.Tests/RoundTripTests.cs`

**Interfaces:**
- Consumes: `IndFileData`, `IndFormat`, todos los modelos.
- Produces: `static byte[] IndFileWriter.ToBytes(IndFileData data)`; `static void IndFileWriter.Save(IndFileData data, string path)`.

- [ ] **Step 1: Escribir los tests que fallan**

`tests/IndLib.Tests/RoundTripTests.cs`:
```csharp
using IndLib;
using Xunit;

namespace IndLib.Tests;

public class RoundTripTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    private static void AssertRoundTrip(string file, string? grafics = null)
    {
        var original = File.ReadAllBytes(P(file));
        var data = IndFileReader.Read(P(file), grafics == null ? null : P(grafics));
        var bytes = IndFileWriter.ToBytes(data);
        Assert.Equal(original, bytes);
    }

    [Fact] public void Ataques_RoundTrip() => AssertRoundTrip("ataques.ind");
    [Fact] public void Personajes_RoundTrip() => AssertRoundTrip("personajes.ind");
    [Fact] public void Fxs_RoundTrip() => AssertRoundTrip("fxs.ind");
    [Fact] public void Cabezas_RoundTrip() => AssertRoundTrip("cabezas.ind");
    [Fact] public void Cascos_RoundTrip() => AssertRoundTrip("cascos.ind");
    [Fact] public void Grafics_RoundTrip() => AssertRoundTrip("graficos.ind");
    [Fact] public void TexDefault1_RoundTrip() => AssertRoundTrip("texdefault1.dat");
    [Fact] public void TexDefault2_RoundTrip() => AssertRoundTrip("texdefault2.dat");
    [Fact] public void TexDefault3_RoundTrip() => AssertRoundTrip("texdefault3.dat");
    [Fact] public void Minimap_RoundTrip() => AssertRoundTrip("minimap.dat", "graficos.ind");
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IndLib.Tests`
Expected: FAIL (no existe `IndFileWriter`).

- [ ] **Step 3: Implementar IndFileWriter.cs**

`src/IndLib/IndFileWriter.cs`:
```csharp
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
                case IndFieldType.Boolean: w.Write((bool)rec.Values[f.Name] ? (short)-1 : (short)0); break;
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
```

- [ ] **Step 4: Ejecutar y verificar que pasan**

Run: `dotnet test tests/IndLib.Tests`
Expected: PASS (los 10 round-trip byte-exactos).

- [ ] **Step 5: Commit**

```bash
git add src/IndLib/IndFileWriter.cs tests/IndLib.Tests/RoundTripTests.cs
git commit -m "feat: escritura con round-trip byte-exacto"
```

---

### Task 7: Exportar/importar TXT

**Files:**
- Create: `src/IndLib/TxtExporter.cs`
- Create: `src/IndLib/TxtImporter.cs`
- Test: `tests/IndLib.Tests/TxtTests.cs`

**Interfaces:**
- Consumes: `IndFileData`, formatos, `IndFileReader`/`IndFileWriter` (para validar relectura).
- Produces: `static string TxtExporter.Export(IndFileData data)`; `static IndFileData TxtImporter.Import(string text, IndFormat format, byte[] headerBytes, string? graficsPath = null)`.

- [ ] **Step 1: Escribir los tests que fallan**

`tests/IndLib.Tests/TxtTests.cs`:
```csharp
using IndLib;
using Xunit;

namespace IndLib.Tests;

public class TxtTests
{
    private static string P(string name) => Path.Combine(TestPaths.InitDir, name);

    [Theory]
    [InlineData("ataques.ind", null)]
    [InlineData("graficos.ind", null)]
    [InlineData("texdefault1.dat", null)]
    [InlineData("minimap.dat", "graficos.ind")]
    public void Export_Import_RoundTrip(string file, string? grafics)
    {
        var data = IndFileReader.Read(P(file), grafics == null ? null : P(grafics));
        var txt = TxtExporter.Export(data);
        var imported = TxtImporter.Import(txt, data.Format, data.HeaderBytes, grafics == null ? null : P(grafics));
        Assert.Equal(IndFileWriter.ToBytes(data), IndFileWriter.ToBytes(imported));
    }

    [Fact]
    public void Ataques_ExportContieneBloque()
    {
        var txt = TxtExporter.Export(IndFileReader.Read(P("ataques.ind")));
        Assert.Contains("# Formato: tIndiceAtaque", txt);
        Assert.Contains("Body.1 = 200", txt);
    }
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan**

Run: `dotnet test tests/IndLib.Tests`
Expected: FAIL (no existen TxtExporter/TxtImporter).

- [ ] **Step 3: Implementar TxtExporter.cs**

`src/IndLib/TxtExporter.cs`:
```csharp
using System.Globalization;
using System.Text;

namespace IndLib;

public static class TxtExporter
{
    public static string Export(IndFileData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# IndEditor v1.0");
        sb.AppendLine($"# Archivo: {data.FileName}");
        sb.AppendLine($"# Formato: {data.Format.Name}");
        switch (data.Format.Kind)
        {
            case IndFormatKind.FixedRecords:
                sb.AppendLine($"# Registros: {data.Records.Count}");
                foreach (var rec in data.Records)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[{rec.Index}]");
                    WriteRecord(sb, data.Format, rec);
                }
                break;
            case IndFormatKind.GrhData:
                sb.AppendLine($"# Entradas: {data.GrhEntries.Count}");
                for (int i = 0; i < data.GrhEntries.Count; i++)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[{i + 1}]");
                    WriteGrhEntry(sb, data.GrhEntries[i]);
                }
                break;
            case IndFormatKind.TexDefault:
                sb.AppendLine();
                WriteRecord(sb, data.Format, data.Records[0]);
                break;
            case IndFormatKind.Minimap:
                sb.AppendLine($"# Grhs: {data.MinimapEntries.Count}");
                for (int i = 0; i < data.MinimapEntries.Count; i++)
                {
                    sb.AppendLine();
                    sb.AppendLine($"[{i + 1}]");
                    sb.AppendLine($"Grh = {data.MinimapEntries[i].Grh}");
                    sb.AppendLine($"Color = {data.MinimapEntries[i].Color:X8}");
                }
                break;
        }
        return sb.ToString();
    }

    private static void WriteRecord(StringBuilder sb, IndFormat format, IndRecord rec)
    {
        foreach (var f in format.Fields)
        {
            if (f.Type == IndFieldType.Int32Array)
            {
                var arr = (int[])rec.Values[f.Name];
                for (int j = 0; j < arr.Length; j++)
                    sb.AppendLine($"{f.Name}.{j + 1} = {arr[j]}");
            }
            else if (f.Type == IndFieldType.ByteArray)
            {
                sb.AppendLine($"{f.Name} = {string.Join(",", (byte[])rec.Values[f.Name])}");
            }
            else if (f.Type == IndFieldType.Single)
            {
                sb.AppendLine($"{f.Name} = {((float)rec.Values[f.Name]).ToString("R", CultureInfo.InvariantCulture)}");
            }
            else if (f.Type == IndFieldType.Boolean)
            {
                sb.AppendLine($"{f.Name} = {((bool)rec.Values[f.Name] ? "True" : "False")}");
            }
            else
            {
                sb.AppendLine($"{f.Name} = {rec.Values[f.Name]}");
            }
        }
    }

    private static void WriteGrhEntry(StringBuilder sb, GrhEntry e)
    {
        sb.AppendLine($"Grh = {e.Grh}");
        if (!e.HasData) return;
        sb.AppendLine($"NumFrames = {e.NumFrames}");
        if (e.NumFrames > 1)
        {
            sb.AppendLine($"Frames = {string.Join(",", e.Frames)}");
            sb.AppendLine($"Velocidad = {e.Speed.ToString("R", CultureInfo.InvariantCulture)}");
        }
        else
        {
            sb.AppendLine($"FileNum = {e.FileNum}");
            sb.AppendLine($"SX = {e.SX}");
            sb.AppendLine($"SY = {e.SY}");
            sb.AppendLine($"Ancho = {e.PixelWidth}");
            sb.AppendLine($"Alto = {e.PixelHeight}");
        }
    }
}
```

- [ ] **Step 4: Implementar TxtImporter.cs**

`src/IndLib/TxtImporter.cs`:
```csharp
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
                IndFieldType.Boolean => value is "True" or "1",
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
```

- [ ] **Step 5: Ejecutar y verificar que pasan**

Run: `dotnet test tests/IndLib.Tests`
Expected: PASS (incluidos los 4 round-trip TXT).

- [ ] **Step 6: Commit**

```bash
git add src/IndLib/TxtExporter.cs src/IndLib/TxtImporter.cs tests/IndLib.Tests/TxtTests.cs
git commit -m "feat: exportar/importar TXT"
```

---

### Task 8: UI WinForms — vista de cuadrícula y acciones principales

**Files:**
- Modify: `src/IndEditor/Program.cs`
- Modify: `src/IndEditor/MainForm.cs`
- Modify: `src/IndEditor/MainForm.Designer.cs`
- Test: manual (`dotnet run --project src/IndEditor`)

**Interfaces:**
- Consumes: `IndLib` (Reader, Writer, Exporter, Importer, modelos, `IndFormatKind`).
- Produces: `MainForm` funcional para formatos de cuadrícula (FixedRecords, GrhData, Minimap) y `texdefault` en vista de cuadrícula (una fila). Menú/toolbar, guardar con `.bak`, export/import TXT, añadir/eliminar filas, validación de celdas, barra de estado, drag&drop.

- [ ] **Step 1: Reemplazar Program.cs**

`src/IndEditor/Program.cs`:
```csharp
namespace IndEditor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
```

- [ ] **Step 2: Reemplazar MainForm.Designer.cs**

`src/IndEditor/MainForm.Designer.cs`:
```csharp
namespace IndEditor;

partial class MainForm
{
    private System.ComponentModel.IContainer? components;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        ClientSize = new System.Drawing.Size(1150, 700);
    }
}
```

- [ ] **Step 3: Implementar MainForm.cs**

`src/IndEditor/MainForm.cs`:
```csharp
using System.Globalization;
using IndLib;

namespace IndEditor;

public partial class MainForm : Form
{
    private enum ColKind { Int16, Int32, Single, Byte, Bool, IntCsv, ByteCsv, HexColor, ReadOnlyInt }

    private readonly MenuStrip _menu = new();
    private readonly ToolStrip _toolbar = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _lblFile = new() { Text = "Sin archivo" };
    private readonly ToolStripStatusLabel _lblFormat = new();
    private readonly ToolStripStatusLabel _lblCount = new();
    private readonly ToolStripStatusLabel _lblSize = new();
    private readonly DataGridView _grid = new();
    private readonly Panel _singlePanel = new();
    private readonly ToolStripButton _btnAdd = new("Añadir fila");
    private readonly ToolStripButton _btnRemove = new("Eliminar fila");

    private readonly List<(string Name, ColKind Kind)> _colKinds = new();
    private IndFileData? _data;
    private string _currentPath = "";
    private string? _graficsPath;

    public MainForm()
    {
        InitializeComponent();
        Text = "IndEditor — Editor de archivos .ind/.dat";
        StartPosition = FormStartPosition.CenterScreen;
        BuildMenu();
        BuildToolbar();
        BuildGrid();
        BuildStatus();
        Controls.Add(_grid);
        Controls.Add(_singlePanel);
        _grid.Dock = DockStyle.Fill;
        _singlePanel.Dock = DockStyle.Fill;
        _singlePanel.Visible = false;
        AllowDrop = true;
        DragEnter += (_, e) =>
        {
            if (e.Data!.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        };
        DragDrop += (_, e) =>
        {
            var files = (string[]?)e.Data!.GetData(DataFormats.FileDrop);
            if (files is { Length: > 0 }) OpenFile(files[0]);
        };
    }

    private void BuildMenu()
    {
        var file = new ToolStripMenuItem("&Archivo");
        file.DropDownItems.Add(Item("&Abrir...", OpenFileDialog, Keys.Control | Keys.O));
        file.DropDownItems.Add(Item("&Guardar", SaveFile, Keys.Control | Keys.S));
        file.DropDownItems.Add(Item("Guardar &como...", SaveFileAs));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("&Exportar TXT...", ExportTxt));
        file.DropDownItems.Add(Item("&Importar TXT...", ImportTxt));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("&Salir", (_, _) => Close()));
        _menu.Items.Add(file);
        var help = new ToolStripMenuItem("&Ayuda");
        help.DropDownItems.Add(Item("&Acerca de...", About));
        _menu.Items.Add(help);
        _menu.Dock = DockStyle.Top;
        Controls.Add(_menu);
    }

    private void BuildToolbar()
    {
        AddTool("Abrir", OpenFileDialog);
        AddTool("Guardar", SaveFile);
        _toolbar.Items.Add(new ToolStripSeparator());
        AddTool("Exportar TXT", ExportTxt);
        AddTool("Importar TXT", ImportTxt);
        _toolbar.Items.Add(new ToolStripSeparator());
        _btnAdd.Click += AddRow;
        _btnRemove.Click += RemoveRow;
        _toolbar.Items.Add(_btnAdd);
        _toolbar.Items.Add(_btnRemove);
        _toolbar.Dock = DockStyle.Top;
        Controls.Add(_toolbar);
    }

    private void BuildGrid()
    {
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.CellValidating += Grid_CellValidating;
    }

    private void BuildStatus()
    {
        _status.Dock = DockStyle.Bottom;
        _status.Items.Add(_lblFile);
        _status.Items.Add(new ToolStripStatusLabel(" | "));
        _status.Items.Add(_lblFormat);
        _status.Items.Add(new ToolStripStatusLabel(" | "));
        _status.Items.Add(_lblCount);
        _status.Items.Add(new ToolStripStatusLabel(" | "));
        _status.Items.Add(_lblSize);
        Controls.Add(_status);
    }

    private ToolStripMenuItem Item(string text, EventHandler handler, Keys? keys = null)
    {
        var item = new ToolStripMenuItem(text, null, handler);
        if (keys.HasValue) item.ShortcutKeys = keys.Value;
        return item;
    }

    private void AddTool(string text, EventHandler handler)
    {
        var b = new ToolStripButton(text);
        b.Click += handler;
        _toolbar.Items.Add(b);
    }

    private void AddCol(string name, string header, ColKind kind, bool readOnly = false)
    {
        DataGridViewColumn col = kind == ColKind.Bool
            ? new DataGridViewCheckBoxColumn { Name = name, HeaderText = header }
            : new DataGridViewTextBoxColumn { Name = name, HeaderText = header, SortMode = DataGridViewColumnSortMode.NotSortable };
        col.ReadOnly = readOnly;
        _grid.Columns.Add(col);
        _colKinds.Add((name, kind));
    }

    // ---------- Abrir ----------

    private void OpenFileDialog(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Filter = "Archivos .ind/.dat|*.ind;*.dat|Todos|*.*",
            Title = "Abrir archivo",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) OpenFile(dlg.FileName);
    }

    private void OpenFile(string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path) ?? "";
            _graficsPath = Path.Combine(dir, "graficos.ind");
            _data = IndFileReader.Read(path, _graficsPath);
            _currentPath = path;
            ReloadViews();
            UpdateStatus();
            if (!string.IsNullOrEmpty(_data.Warning))
                MessageBox.Show(this, _data.Warning, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error al abrir '{path}':\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ReloadViews()
    {
        _colKinds.Clear();
        _grid.Columns.Clear();
        _grid.Rows.Clear();
        _singlePanel.Visible = false;
        _grid.Visible = true;
        _btnAdd.Enabled = _data!.Format.Kind != IndFormatKind.TexDefault;
        _btnRemove.Enabled = _data.Format.Kind != IndFormatKind.TexDefault;
        switch (_data.Format.Kind)
        {
            case IndFormatKind.FixedRecords: PopulateFixedGrid(); break;
            case IndFormatKind.GrhData: PopulateGrhGrid(); break;
            case IndFormatKind.Minimap: PopulateMinimapGrid(); break;
            case IndFormatKind.TexDefault: PopulateFixedGrid(); break; // Task 8: vista grid; Task 9: panel dedicado
        }
    }

    private void PopulateFixedGrid()
    {
        foreach (var f in _data!.Format.Fields)
        {
            switch (f.Type)
            {
                case IndFieldType.Int16: AddCol(f.Name, f.Label, ColKind.Int16); break;
                case IndFieldType.Int32: AddCol(f.Name, f.Label, ColKind.Int32); break;
                case IndFieldType.Single: AddCol(f.Name, f.Label, ColKind.Single); break;
                case IndFieldType.Boolean: AddCol(f.Name, f.Label, ColKind.Bool); break;
                case IndFieldType.Byte: AddCol(f.Name, f.Label, ColKind.Byte); break;
                case IndFieldType.Int32Array:
                    for (int j = 0; j < f.Count; j++) AddCol($"{f.Name}.{j + 1}", $"{f.Label} {j + 1}", ColKind.Int32);
                    break;
                case IndFieldType.ByteArray:
                    AddCol(f.Name, f.Label, ColKind.ByteCsv);
                    break;
            }
        }
        foreach (var rec in _data.Records)
        {
            var cells = new List<object>();
            foreach (var f in _data.Format.Fields)
            {
                if (f.Type == IndFieldType.Int32Array)
                    foreach (var v in (int[])rec.Values[f.Name]) cells.Add(v);
                else if (f.Type == IndFieldType.ByteArray)
                    cells.Add(string.Join(",", (byte[])rec.Values[f.Name]));
                else
                    cells.Add(rec.Values[f.Name]);
            }
            _grid.Rows.Add(cells.ToArray());
        }
    }

    private void PopulateGrhGrid()
    {
        AddCol("Grh", "Grh", ColKind.Int32);
        AddCol("NumFrames", "Nº frames", ColKind.Int32);
        AddCol("Frames", "Frames", ColKind.IntCsv);
        AddCol("Velocidad", "Velocidad", ColKind.Single);
        AddCol("FileNum", "Archivo", ColKind.Int32);
        AddCol("SX", "SX", ColKind.Int32);
        AddCol("SY", "SY", ColKind.Int32);
        AddCol("Ancho", "Ancho", ColKind.Int32);
        AddCol("Alto", "Alto", ColKind.Int32);
        foreach (var e in _data!.GrhEntries)
        {
            bool anim = e.HasData && e.NumFrames > 1;
            _grid.Rows.Add(e.Grh, e.NumFrames,
                anim ? string.Join(",", e.Frames) : "",
                anim ? e.Speed : 0f,
                anim ? 0 : e.FileNum,
                anim ? 0 : e.SX,
                anim ? 0 : e.SY,
                anim ? 0 : e.PixelWidth,
                anim ? 0 : e.PixelHeight);
        }
    }

    private void PopulateMinimapGrid()
    {
        AddCol("Grh", "Grh", ColKind.ReadOnlyInt, readOnly: true);
        AddCol("Color", "Color (AARRGGBB)", ColKind.HexColor);
        foreach (var e in _data!.MinimapEntries)
            _grid.Rows.Add(e.Grh, e.Color.ToString("X8"));
    }

    // ---------- Guardar ----------

    private void SaveFileAs(object? sender, EventArgs e)
    {
        if (_data == null) return;
        using var dlg = new SaveFileDialog { Filter = "Todos|*.*", FileName = _data.FileName };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ApplyEdits();
            IndFileWriter.Save(_data, dlg.FileName);
            _currentPath = dlg.FileName;
            UpdateStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error al guardar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveFile(object? sender, EventArgs e)
    {
        if (_data == null) return;
        if (string.IsNullOrEmpty(_currentPath)) { SaveFileAs(sender, e); return; }
        try
        {
            ApplyEdits();
            if (MessageBox.Show(this, "¿Deseas crear una copia de seguridad (.bak) antes de guardar?", "Guardar",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                File.Copy(_currentPath, _currentPath + ".bak", true);
            }
            IndFileWriter.Save(_data, _currentPath);
            UpdateStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error al guardar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyEdits()
    {
        if (_data == null) return;
        if (_singlePanel.Visible) { ReadSingleFromUi(); return; }
        switch (_data.Format.Kind)
        {
            case IndFormatKind.FixedRecords: SaveFixedFromGrid(); break;
            case IndFormatKind.GrhData: SaveGrhFromGrid(); break;
            case IndFormatKind.Minimap: SaveMinimapFromGrid(); break;
            case IndFormatKind.TexDefault: SaveFixedFromGrid(); break;
        }
    }

    private void SaveFixedFromGrid()
    {
        var records = new List<IndRecord>();
        for (int r = 0; r < _grid.Rows.Count; r++)
        {
            var rec = new IndRecord { Index = r + 1 };
            int col = 0;
            foreach (var f in _data!.Format.Fields)
            {
                if (f.Type == IndFieldType.Int32Array)
                {
                    var arr = new int[f.Count];
                    for (int j = 0; j < f.Count; j++) { arr[j] = CellInt(r, col); col++; }
                    rec.Values[f.Name] = arr;
                }
                else if (f.Type == IndFieldType.ByteArray)
                {
                    rec.Values[f.Name] = CellBytes(r, col); col++;
                }
                else
                {
                    rec.Values[f.Name] = CellValue(r, col); col++;
                }
            }
            records.Add(rec);
        }
        _data.Records.Clear();
        _data.Records.AddRange(records);
        _data.Count = records.Count;
    }

    private void SaveGrhFromGrid()
    {
        var entries = new List<GrhEntry>();
        for (int r = 0; r < _grid.Rows.Count; r++)
        {
            var e = new GrhEntry { Grh = CellInt(r, 0), NumFrames = CellInt(r, 1) };
            e.HasData = e.Grh != 0;
            if (e.NumFrames > 1)
            {
                e.Frames = ParseIntCsv(_grid.Rows[r].Cells[2].Value?.ToString());
                e.Speed = CellFloat(r, 3);
            }
            else
            {
                e.FileNum = CellInt(r, 4);
                e.SX = CellInt(r, 5);
                e.SY = CellInt(r, 6);
                e.PixelWidth = CellInt(r, 7);
                e.PixelHeight = CellInt(r, 8);
            }
            entries.Add(e);
        }
        _data.GrhEntries.Clear();
        _data.GrhEntries.AddRange(entries);
        _data.Count = entries.Count;
    }

    private void SaveMinimapFromGrid()
    {
        _data.MinimapEntries.Clear();
        for (int r = 0; r < _grid.Rows.Count; r++)
        {
            var color = uint.Parse(_grid.Rows[r].Cells[1].Value?.ToString() ?? "0", NumberStyles.HexNumber);
            _data.MinimapEntries.Add(new MinimapEntry { Grh = 0, Color = color });
        }
        _data.Count = _data.MinimapEntries.Count;
    }

    private object CellValue(int r, int c)
    {
        var v = _grid.Rows[r].Cells[c].Value;
        if (v is string s && s.Trim().Length == 0) return 0;
        return v ?? 0;
    }

    private int CellInt(int r, int c) => Convert.ToInt32(CellValue(r, c));
    private float CellFloat(int r, int c) => Convert.ToSingle(CellValue(r, c), CultureInfo.InvariantCulture);

    private byte[] CellBytes(int r, int c)
    {
        var s = _grid.Rows[r].Cells[c].Value?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return new byte[256];
        return s.Split(',').Select(v => byte.Parse(v.Trim())).ToArray();
    }

    private static int[] ParseIntCsv(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return Array.Empty<int>();
        return s.Split(',').Select(v => int.Parse(v.Trim(), CultureInfo.InvariantCulture)).ToArray();
    }

    // ---------- Filas ----------

    private void AddRow(object? sender, EventArgs e)
    {
        if (_data == null) return;
        switch (_data.Format.Kind)
        {
            case IndFormatKind.FixedRecords:
            case IndFormatKind.TexDefault:
                var cells = new List<object>();
                foreach (var f in _data.Format.Fields)
                {
                    if (f.Type == IndFieldType.Int32Array)
                        for (int j = 0; j < f.Count; j++) cells.Add(0);
                    else if (f.Type == IndFieldType.ByteArray) cells.Add("");
                    else if (f.Type == IndFieldType.Boolean) cells.Add(false);
                    else cells.Add(0);
                }
                _grid.Rows.Add(cells.ToArray());
                break;
            case IndFormatKind.GrhData:
                _grid.Rows.Add(0, 0, "", 0f, 0, 0, 0, 0, 0);
                break;
            case IndFormatKind.Minimap:
                _grid.Rows.Add(0, "00000000");
                break;
        }
        UpdateStatus();
    }

    private void RemoveRow(object? sender, EventArgs e)
    {
        if (_data == null || _grid.SelectedRows.Count == 0) return;
        _grid.Rows.RemoveAt(_grid.SelectedRows[0].Index);
        UpdateStatus();
    }

    // ---------- TXT ----------

    private void ExportTxt(object? sender, EventArgs e)
    {
        if (_data == null) return;
        using var dlg = new SaveFileDialog { Filter = "Texto|*.txt|Todos|*.*", FileName = _data.FileName + ".txt" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ApplyEdits();
            File.WriteAllText(dlg.FileName, TxtExporter.Export(_data));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error al exportar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportTxt(object? sender, EventArgs e)
    {
        if (_data == null) return;
        using var dlg = new OpenFileDialog { Filter = "Texto|*.txt|Todos|*.*" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            var txt = File.ReadAllText(dlg.FileName);
            _data = TxtImporter.Import(txt, _data.Format, _data.HeaderBytes, _graficsPath);
            ReloadViews();
            UpdateStatus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Error al importar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ---------- Validación de celdas ----------

    private void Grid_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        if (_colKinds.Count == 0 || e.RowIndex < 0 || e.ColumnIndex >= _colKinds.Count) return;
        var kind = _colKinds[e.ColumnIndex].Kind;
        if (kind == ColKind.Bool || kind == ColKind.ReadOnlyInt) return;
        var val = e.FormattedValue?.ToString() ?? "";
        try
        {
            switch (kind)
            {
                case ColKind.Int16: if (val.Length > 0) Convert.ToInt16(val); break;
                case ColKind.Int32: if (val.Length > 0) Convert.ToInt32(val); break;
                case ColKind.Byte: if (val.Length > 0) byte.Parse(val); break;
                case ColKind.Single: if (val.Length > 0) float.Parse(val, CultureInfo.InvariantCulture); break;
                case ColKind.IntCsv: ParseIntCsv(val); break;
                case ColKind.ByteCsv: CellBytes(e.RowIndex, e.ColumnIndex); break;
                case ColKind.HexColor: if (val.Length > 0) uint.Parse(val, NumberStyles.HexNumber); break;
            }
        }
        catch (Exception)
        {
            e.Cancel = true;
            MessageBox.Show(this, "Valor inválido en la celda.", "Error de validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    // ---------- Estado ----------

    private void UpdateStatus()
    {
        _lblFile.Text = string.IsNullOrEmpty(_currentPath) ? "Sin archivo" : _currentPath;
        _lblFormat.Text = _data?.Format.DisplayName ?? "";
        _lblCount.Text = _data == null ? "" : $"Registros: {_data.Count}";
        _lblSize.Text = _data == null ? "" : $"{(_currentPath.Length > 0 && File.Exists(_currentPath) ? new FileInfo(_currentPath).Length : 0)} bytes";
    }

    private void About(object? sender, EventArgs e)
    {
        MessageBox.Show(this,
            "IndEditor v1.0\nEditor de archivos .ind/.dat de Argentum Online.\n\nLee, edita, guarda (con copia .bak) y exporta/importa TXT.",
            "Acerca de IndEditor", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Añadir en Task 9: ShowSingleRecordView / ReadSingleFromUi
    private void ShowSingleRecordView() { }
    private void ReadSingleFromUi() { }
}
```

- [ ] **Step 4: Ejecutar y verificar manualmente**

Run: `dotnet run --project src/IndEditor`
Expected: la ventana abre. Probar: Abrir `K:\Descargas\aaoo\init\ataques.ind` → 62 filas; editar una celda inválida → aviso; Añadir fila; Guardar como en `K:\Descargas\aaoo\EditorInd\tmp\ataques_copia.ind`; Exportar TXT; abrir el TXT y verificar formato; abrir `graficos.ind` → 24548 filas; abrir `minimap.dat` → 24546 filas.

- [ ] **Step 5: Commit**

```bash
git add src/IndEditor tests
git commit -m "feat: UI WinForms con vista de cuadrícula, guardar y exportar/importar TXT"
```
