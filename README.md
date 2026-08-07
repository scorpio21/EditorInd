# IndEditor

Editor de archivos binarios `.ind` / `.dat` de **Argentum Online** (mods Aodrag9 y Aom2018). Aplicación de escritorio para Windows (.NET 9, WinForms) con interfaz en español: lee, edita y guarda los archivos de datos del juego con **round-trip byte-exacto**, y permite exportar/importar a un formato de texto legible. Soporta las dos variantes binarias de registro —Int32 (Aodrag9) e Int16 (Aom2018)— con **detección automática** al abrir el archivo.

## Características

- **Edición en cuadrícula** de todos los archivos de datos principales del juego.
- **Round-trip byte-exacto**: al abrir y guardar sin modificar nada, el archivo resultante es idéntico byte a byte al original (la cabecera `tCabecera` de 263 bytes se preserva intacta).
- **Copia de seguridad automática** (`.bak`) antes de guardar.
- **Exportar a TXT** legible y **reimportar** desde TXT.
- **Detector de formato** automático por nombre de archivo (insensible a mayúsculas).
- **Detección automática de variante binaria**: distingue la variante Int32 (Aodrag9) de la Int16 (Aom2018) al cargar y la conserva al guardar, también vía TXT.
- **Export TXT estilo DESINDDAT**: exportación en el formato `[INIT]`/`NumXXX=`/`[BodyN]`/`WalkN=` del indexador clásico de Argentum Online.
- **Validación de celdas** con mensajes de error en español, incluido el número de línea en la importación de TXT.
- **Arrastrar y soltar** (drag & drop) para abrir archivos.
- **Guards de integridad** que impiden guardar archivos corruptos (conteo de frames, rangos de 16 bits).

## Formatos soportados

| Formato | Archivos | Registro |
|---|---|---|
| Ataques (`tIndiceAtaque`) | `ataques.ind` | `Body[1..4]` Int32, `HeadOffsetX/Y` Int16 |
| Personajes (`tIndiceCuerpo`) | `personajes.ind` | `Body[1..4]` Int32, `HeadOffsetX/Y` Int16 |
| FXs (`tIndiceFx`) | `fxs.ind` | `Animacion` Int32, `offsetX/Y` Int16, `FXTransparente` Boolean |
| Cabezas / Cascos (`tHead`) | `cabezas.ind`, `cascos.ind` | `Texture`, `startX`, `startY` Int16 |
| Gráficos (`GrhData`) | `graficos.ind` | Variable: estático o animación (`NumFrames`, `Frames`, `Velocidad`) |
| Fuentes (`texdefault`) | `texdefault1.dat` … `texdefault3.dat` | Cabecera VFH (273 B, un registro) |
| Minimapa | `minimap.dat` | Colores AARRGGBB |

> **Nota:** los formatos de registros fijos (`ataques.ind`, `personajes.ind`, `fxs.ind`, `cabezas.ind`, `cascos.ind`) existen en dos variantes binarias: **Int32** (Aodrag9, la moderna) e **Int16** (Aom2018, la clásica). La variante se detecta automáticamente al cargar y se conserva al guardar.

## Requisitos

- Windows 10/11 (o plataforma compatible con .NET 9 y WinForms).
- [.NET SDK 10](https://dotnet.microsoft.com/download) para compilar.

## Compilar y ejecutar

```bash
git clone https://github.com/scorpio21/EditorInd.git
cd EditorInd
dotnet run --project src/IndEditor
```

También puedes abrir la solución `IndEditor.sln` en Visual Studio 2022+ y ejecutar el proyecto `IndEditor`.

## Uso

1. **Abrir**: `Archivo → Abrir…` (o arrastra el archivo a la ventana). El formato se detecta automáticamente.
2. **Editar**: modifica los valores directamente en la cuadrícula.
   - Añadir/eliminar filas con la barra de herramientas.
   - Validación en vivo: un valor inválido se rechaza con un aviso.
3. **Guardar**: `Archivo → Guardar` (Ctrl+S). Se ofrece crear una copia de seguridad `.bak`. *Guardar como…* permite elegir otro destino.
4. **TXT**: `Archivo → Exportar TXT…` para exportar; `Importar TXT…` para reimportar (los errores indican el número de línea). En los formatos de registros fijos la exportación ofrece un selector entre el **formato actual** (el estándar de IndEditor) y el **formato DESINDDAT** (AO clásico).

### Formato TXT

La exportación produce una sección `[N]` por registro con pares `campo = valor`:

```text
[1]
Body.1 = 20466
Body.2 = 20467
Body.3 = 20469
Body.4 = 20468
HeadOffsetX = 0
HeadOffsetY = 0
```

### Formato DESINDDAT

La exportación en formato DESINDDAT reproduce el estilo del indexador clásico de Argentum Online, con una sección `[INIT]` que indica el número de registros (`NumBodies=`, `NumAtaques=`, `NumFxs=`, `NumHeads=`, `NumCascos=` según el archivo) y una sección `[BodyN]` / `[FXn]` / `[HeadN]` / `[CascoN]` por registro:

```text
[INIT]
NumBodies=1

[Body1]
Walk1=20466
Walk2=20467
Walk3=20469
Walk4=20468
HeadOffsetX=0
HeadOffsetY=0
```

Está disponible para los formatos de registros fijos; `graficos.ind`, `texdefault*.dat` y `minimap.dat` se exportan siempre en el formato actual.

## Arquitectura

```
src/IndEditor/       Aplicación WinForms (interfaz en español)
src/IndLib/          Librería: lectura/escritura binaria, export/import TXT, catálogo de formatos
tests/IndLib.Tests/  Pruebas unitarias (xUnit)
```

La librería `IndLib` es independiente de la UI y cubre toda la lógica de formatos:

- `IndFormatCatalog` — definición de los formatos binarios.
- `IndFormatDetector` — detección por nombre de archivo.
- `IndFileReader` / `IndFileWriter` — lectura y escritura binaria.
- `TxtExporter` / `TxtImporter` — exportación e importación de texto.
- `IndValueLogic` — resolución de valores booleanos para round-trip exacto.

## Pruebas

```bash
dotnet test tests/IndLib.Tests
```

La suite incluye 63 pruebas: detección de formatos, parseo contra archivos reales, round-trip byte-exacto (10 archivos), export/import TXT y regresiones de integridad.

## Licencia

Consulta el repositorio para más detalles. Proyecto no oficial, sin afiliación con los creadores de Argentum Online.
