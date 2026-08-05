# IndEditor — Diseño

**Fecha:** 2026-08-05
**Estado:** Aprobado por el usuario
**Propósito:** Aplicación .NET (WinForms) para leer, editar y guardar los archivos binarios `.ind` y `.dat` binarios del juego Argentum Online (mod "Aodrag9"), así como exportarlos/importarlos a `.txt`.

## Requisitos

- Leer los archivos `.ind` y `.dat` binarios ubicados en `K:\Descargas\aaoo\init\`.
- Mostrar los registros en una cuadrícula editable (DataGridView).
- Guardar los cambios de vuelta al archivo original (round-trip byte-exacto).
- Exportar a `.txt` legible y reimportar desde `.txt`.
- App desarrollada en .NET / Visual Studio, interfaz en español.
- Cobertura de archivos: `ataques.ind`, `personajes.ind`, `fxs.ind`, `cabezas.ind`, `cascos.ind`, `graficos.ind`, `texdefault1.dat`, `texdefault2.dat`, `texdefault3.dat` y `minimap.dat`.

## Formatos binarios (confirmados contra el código fuente y los bytes)

### tCabecera (263 bytes)
```vb
desc As String * 255   ' 255 B
CRC As Long            ' 4 B
MagicWord As Long      ' 4 B
```
Se preserva como **bytes crudos** en escritura (el juego no valida CRC/MagicWord al leer).

### Registros por archivo

| Formato | Archivos | Cabecera | Contador | Registro |
|---|---|---|---|---|
| tIndiceAtaque / tIndiceCuerpo | `ataques.ind`, `personajes.ind` | tCabecera 263 B | `Int16` en offset 263 | 20 B: `Body[1..4] Int32` (16 B) + `HeadOffsetX Int16` + `HeadOffsetY Int16` |
| tIndiceFx | `fxs.ind` | tCabecera 263 B | `Int16` en offset 263 | 10 B: `Animacion Int32` + `offsetX Int16` + `offsetY Int16` + `FXTransparente Boolean` (2 B) |
| tHead | `cabezas.ind`, `cascos.ind` | — | `Int16` en offset 0 | 6 B: `Texture Int16` + `startX Int16` + `startY Int16` |
| GrhData | `graficos.ind` | 8 B (version Long + count Long) | — | variable (ver abajo) |

### graficos.ind (formato variable)

```
Long fileVersion          @0  (preservado; valor actual 1)
Long GrhCount             @4  (preservado; valor actual 25517)
Repetir hasta EOF:
  Long Grh                si == 0 → sin datos
  Int16 NumFrames
  si NumFrames > 1:  Long[NumFrames] Frames + Single Speed
  si no:            Long FileNum + Int16 SX + Int16 SY + Int16 pixelWidth + Int16 pixelHeight
```

Datos reales verificados: `fileVersion=1`, `GrhCount=25517`, 24548 entradas (22294 estáticas, 2254 animaciones, máx 35 frames). El valor de `GrhCount` se preserva tal cual en la escritura (no se recalcula).

### Tamaños de registro reales (verificados)
- `ataques.ind`: 263 + 2 + 62×20 = 1505 B ✓
- `personajes.ind`: 263 + 2 + 470×20 = 9665 B ✓
- `fxs.ind`: 263 + 2 + 59×10 = 855 B ✓
- `cabezas.ind`: 2 + 654×6 = 3926 B ✓
- `cascos.ind`: 2 + 44×6 = 266 B ✓

## Formatos `.dat` binarios

### texdefaultN.dat (cabecera VFH, 273 bytes, un solo registro)
```vb
Private Type VFH
    BitmapWidth As Long          ' Int32 (4 B)
    BitmapHeight As Long         ' Int32 (4 B)
    CellWidth As Long            ' Int32 (4 B)
    CellHeight As Long           ' Int32 (4 B)
    BaseCharOffset As Byte       ' Byte (1 B)
    CharWidth(0 To 255) As Byte  ' Byte[256]
End Type
```
- El campo `CharVA` (256 × 128 B = 32768 B) **no se guarda** en el archivo: se calcula en runtime a partir de los campos de cabecera. Por eso el archivo pesa 273 B.
- Verificado: `texdefault1`: BW=256, BH=256, CW=17, CH=17, BaseCharOffset=32; `texdefault2`: BW=2048, BH=1024, CW=70, CH=70.

### minimap.dat (colores por grh activo)
- Sin cabecera ni contador: una secuencia de `Int32` (color ARGB `0xAARRGGBB`), uno por cada **grh activo** de `graficos.ind`, en orden ascendente de índice.
- Está **acoplado** a `graficos.ind`: la app carga `graficos.ind` de la misma carpeta para mapear posición → índice grh. El juego lee los Longs posicionalmente (sin índice almacenado).
- Verificado: 98184 B = 24546 Longs. Nota: `graficos.ind` actual tiene 24548 entradas no-cero (2 más); la app debe avisar si el nº de colores no coincide con el nº de grhs activos (posible versión antigua del minimap).
- Guardar: escribe los Longs en el mismo orden (round-trip). Si se añaden/eliminan grhs en `graficos.ind`, se avisa del desfase.

### .dat que son texto (fuera de alcance)
`armas.dat`, `colores.dat`, `escudos.dat`, `fonttype.dat`, `mensajes.dat`, `particulas.dat`, `tutorial.dat` son texto/INI y ya se editan con el bloc de notas.

## Arquitectura

Solución `IndEditor.sln` en `K:\Descargas\aaoo\IndEditor` con 3 proyectos .NET 10 (SDK 10.0.100 y VS 18 Enterprise disponibles):

```
IndEditor.sln
├─ src/IndLib            Class library (net10.0)
│    ├─ Formats/         Definiciones declarativas de formato por archivo
│    ├─ Models/          IndField, IndRecord, GrhEntry, IndFileData
│    ├─ IndFileReader.cs Lectura binaria genérica según formato
│    ├─ IndFileWriter.cs Escritura binaria genérica (round-trip exacto)
│    ├─ IndFormatDetector.cs  Detección por nombre de archivo
│    ├─ TxtExporter.cs   Exportación a texto por bloques
│    └─ TxtImporter.cs   Importación desde texto
├─ src/IndEditor          App WinForms (net10.0-windows), UI en español
│    ├─ MainForm          DataGridView + menús + barra de estado + drag&drop
│    └─ Program.cs
└─ tests/IndLib.Tests     xUnit (net10.0)
```

### Modelo de formato declarativo
Cada formato se define como: nombre, patrón de nombre de archivo, tipo de vista (grid de registros / registro único), tamaño de cabecera a preservar crudo, posición/tipo del contador, y lista de campos (nombre, tipo .NET, tamaño, array). Un parser genérico serializa/deserializa usando ese esquema.

- `IndField` tipos: `Int16`, `Int32`, `Single`, `Boolean` (2 B), `Byte`, `Long[]` (Body/Frames), `Byte[]` (CharWidth).
- `IndRecord`: índice + lista de valores de campo.
- `GrhEntry`: modelo específico para graficos.ind (Grh, NumFrames, Tipo estática/animación, Frames[], Speed, FileNum, SX, SY, pixelWidth, pixelHeight).
- `TexDefaultRecord`: modelo de registro único (5 campos + CharWidth[256]).
- `MinimapRecord`: grh (índice o posición) + color ARGB.

### Detección
Por nombre de archivo exacto:
- `ataques.ind` → tIndiceAtaque
- `personajes.ind` → tIndiceCuerpo
- `fxs.ind` → tIndiceFx
- `cabezas.ind`, `cascos.ind` → tHead
- `graficos.ind` → GrhData
- `texdefault1.dat`, `texdefault2.dat`, `texdefault3.dat` → VFH (registro único)
- `minimap.dat` → minimap (requiere `graficos.ind` en la misma carpeta para el mapeo a grh)
- Otro → error "formato no reconocido".

## Interfaz (WinForms, español)

- **Abrir** (menú + botón): abre el archivo, detecta formato y carga la vista correspondiente.
- **Vista grid (DataGridView)** para los `.ind` y `minimap.dat`: una fila por registro, una columna por campo, con validación por tipo en `CellValidating` (`Int16`/`Int32`/`Single`/`Boolean`/lista separada por comas).
  - graficos.ind: columnas `Grh, Tipo, NumFrames, Frames, Velocidad, FileNum, SX, SY, Ancho, Alto`.
  - minimap.dat: columnas `Grh, Color (AARRGGBB)`; la columna Grh se calcula de `graficos.ind` (solo informativa, no editable); si no hay `graficos.ind` o no coincide el conteo, se avisa y se muestra la posición 1..N.
- **Vista registro único** para `texdefaultN.dat`: panel con `BitmapWidth, BitmapHeight, CellWidth, CellHeight, BaseCharOffset` + tabla editable `CharWidth[0..255]` (columna índice → valor).
- **Añadir fila / Eliminar fila** (botones). Filas con "Grh = 0" en graficos.ind se pueden eliminar/crear libremente (sin datos asociados).
- **Guardar** → sobrescribe el archivo preservando la cabecera cruda; **Guardar como**. Aviso antes de sobrescribir con opción de crear copia `.bak`.
- **Contador**: en los formatos con campo contador, al guardar se escribe el número real de registros. En `graficos.ind` se preserva `GrhCount` tal cual, y la app avisa si algún índice `Grh` lo supera.
- **Exportar .txt / Importar .txt**.
- Arrastrar y soltar el archivo sobre la ventana para abrirlo.
- **Barra de estado**: archivo actual, formato, nº registros, tamaño en bytes.
- **Panel/diálogo de cabecera**: muestra `desc`, `CRC`, `MagicWord` (solo lectura) para archivos con tCabecera.
- Todo en español: menús Archivo/Ayuda, botones, mensajes.

## Formato TXT

```
# IndEditor v1.0
# Archivo: ataques.ind
# Formato: tIndiceAtaque
# Registros: 62

[1]
Body.1 = 200
Body.2 = 227
Body.3 = 254
Body.4 = 281
HeadOffsetX = 0
HeadOffsetY = -28

[2]
...
```
- Para graficos.ind, `Frames = 1,2,3,4`; campos estáticos `FileNum`, `SX`, `SY`, `Ancho`, `Alto`.
- Para texdefaultN.dat (registro único):
  ```
  # Archivo: texdefault1.dat
  # Formato: texdefault

  BitmapWidth = 256
  BitmapHeight = 256
  CellWidth = 17
  CellHeight = 17
  BaseCharOffset = 32
  CharWidth = 7,13,7,7,7,7,7,7,7,7,4,...   (256 valores separados por coma)
  ```
- Para minimap.dat:
  ```
  # Archivo: minimap.dat
  # Formato: minimap
  # Grhs: 24546

  [1]
  Grh = 1
  Color = 00000000
  ...
  ```
  La columna `Grh` se escribe solo como referencia; al importar solo se usan los colores en orden.
- Importación: tolerante a espacios, líneas vacías y comentarios `#`; valida tipos y rango.

## Errores y manejo

- Archivo no encontrado / no reconocido / binario truncado (lectura corta a mitad de registro) → mensaje claro, sin cierre de app.
- Valores fuera de rango en celdas → aviso y cancelación de edición.
- TXT inválido en importación → mensaje con número de línea.
- Edición en memoria; no se escribe nada hasta que el usuario elige Guardar.

## Pruebas (xUnit)

1. **Parseo real**: abrir los archivos reales de `K:\Descargas\aaoo\init\` → contar registros correctos (62 / 470 / 59 / 654 / 44 / 24548 para los `.ind`; 273 B y campos correctos para `texdefault1/2/3`; 24546 colores para `minimap.dat`) y verificar valores concretos del primer registro.
2. **Round-trip byte-exacto**: leer → escribir a memoria/archivo temporal → releer → los bytes son idénticos al original (todos los archivos binarios soportados).
3. **TXT ↔ binario**: exportar un archivo a TXT, importarlo, y verificar que los registros coinciden.
4. **Detección**: nombre correcto → formato correcto; nombre desconocido → error.
5. **Rangos**: valores fuera de rango `Int16`/`Int32` se rechazan.
6. **minimap**: mapeo grh↔color con `graficos.ind` de la carpeta; aviso cuando el nº de colores no coincide con los grhs activos.

## Entorno

- .NET SDK 10.0.100 instalado.
- Visual Studio 18 Enterprise (`devenv.exe` en `C:\Program Files\Microsoft Visual Studio\18\Enterprise`).
- Target: `net10.0-windows` (WinForms), `net10.0` (librería y tests).
- Sistema: Windows.

## Fuera de alcance

- Los `.dat` que son texto/INI (`armas.dat`, `colores.dat`, `escudos.dat`, `fonttype.dat`, `mensajes.dat`, `particulas.dat`, `tutorial.dat`): ya editables en el bloc de notas.
- Los `.ini` (`particulas.ini`): texto.
- Escritura sobre el directorio del juego directamente (se usa Guardar como / copia `.bak`).
