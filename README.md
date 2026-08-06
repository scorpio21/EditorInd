# IndEditor

Editor de archivos binarios `.ind` / `.dat` de **Argentum Online** (mod Aodrag9). Aplicación de escritorio para Windows (.NET 9, WinForms) con interfaz en español: lee, edita y guarda los archivos de datos del juego con **round-trip byte-exacto**, y permite exportar/importar a un formato de texto legible.

## Características

- **Edición en cuadrícula** de todos los archivos de datos principales del juego.
- **Round-trip byte-exacto**: al abrir y guardar sin modificar nada, el archivo resultante es idéntico byte a byte al original (la cabecera `tCabecera` de 263 bytes se preserva intacta).
- **Copia de seguridad automática** (`.bak`) antes de guardar.
- **Exportar a TXT** legible y **reimportar** desde TXT.
- **Detector de formato** automático por nombre de archivo (insensible a mayúsculas).
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
4. **TXT**: `Archivo → Exportar TXT…` para exportar; `Importar TXT…` para reimportar (los errores indican el número de línea).

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

La suite incluye 50 pruebas: detección de formatos, parseo contra archivos reales, round-trip byte-exacto (10 archivos), export/import TXT y regresiones de integridad.

## Licencia

Consulta el repositorio para más detalles. Proyecto no oficial, sin afiliación con los creadores de Argentum Online.
