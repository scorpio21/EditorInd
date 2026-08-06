# IndEditor - Compatibilidad dual binaria y export TXT estilo DESINDDAT

**Fecha:** 2026-08-07
**Estado:** Aprobado por el usuario
**Propósito:** Que el IndEditor pueda cargar, editar y guardar los `.ind` de **ambos formatos binarios** (Aom2018 de Int16 y Aodrag9 de Int32) detectándolos automáticamente, y que pueda exportar TXT con el **formato clásico de DESINDDAT** (`[INIT]`/`NumXXX=`/secciones `[BodyN]`/`WalkN=`) además del formato actual.

## Antecedentes

El usuario trabaja con dos mods que usan estructuras binarias distintas:

| Archivo | Variante Aom2018 (leída por DESINDDAT) | Variante Aodrag9 (leída por IndEditor) |
|---|---|---|
| **personajes.ind / ataques.ind** | count@263, 4×Int16 Walk + 2×Int16 HeadOffset = **12 B/reg** | count@263, 4×Int32 Body + 2×Int16 = **20 B/reg** |
| **fxs.ind** | count@263, Int16 Animación + 2×Int16 = **6 B/reg** (sin FXTransparente) | count@263, Int32 + 2×Int16 + Bool = **10 B/reg** |
| **cabezas.ind / cascos.ind** | count@**263**, 4×Int16 (Head0-3) = **8 B/reg** | count@**0**, 3×Int16 (Texture/startX/startY) = **6 B/reg** |

El IndEditor hoy solo lee/escribe la variante Aodrag9. Al abrir un `.ind` de Aom2018, los registros se desalinean (lee 20 B en vez de 12 B, o campos equivocados) y la exportación TXT estilo DESINDDAT no es posible. Además, DESINDDAT (el C original) no puede leer el `personajes.ind` de Aodrag9 (registros Int32) y genera un archivo de tamaño 0.

Referencias verificadas:
- `K:\Argentum\Aomania\Aom2018\Caom\AomUtilidad2012\configurador\DESINDDAT\main.c` — formato TXT y estructuras leídas.
- `k:\Descargas\aaoo\init\` — `.ind` reales de Aodrag9.
- `K:\Argentum\...\DESINDDAT\` — `.ind` reales de Aom2018 (Cabezas.ind 6913 B, Personajes.ind 8185 B, etc.).

## Diseño

### 1. Variantes binarias

Cada formato `FixedRecords` pasa a declarar **una o más variantes** de layout binario. Una variante define: `CountOffset`, campos (con tipo por campo) y `RecordSize`. La variante usada en runtime se elige por detección automática al cargar y se conserva en el `IndFileData` para la escritura.

Variantes a declarar en `IndFormatCatalog`:

- **Personajes/Ataques** (`tIndiceCuerpo`):
  - Variante Int16 (Aom2018): `CountOffset=263`, campos `Body Int16Array[4]`, `HeadOffsetX Int16`, `HeadOffsetY Int16`, `RecordSize=12`.
  - Variante Int32 (Aodrag9): `CountOffset=263`, campos `Body Int32Array[4]`, `HeadOffsetX Int16`, `HeadOffsetY Int16`, `RecordSize=20`.
- **Fxs**:
  - Variante Int16 (Aom2018): `CountOffset=263`, campos `Animacion Int16`, `offsetX Int16`, `offsetY Int16`, `RecordSize=6`. **Sin** campo `FXTransparente`.
  - Variante Int32 (Aodrag9): `CountOffset=263`, campos `Animacion Int32`, `offsetX Int16`, `offsetY Int16`, `FXTransparente Boolean`, `RecordSize=10`.
- **Cabezas/Cascos** (`tHead`):
  - Variante Aom2018: `CountOffset=263`, campos 4×Int16 `Head0..3`, `RecordSize=8`.
  - Variante Aodrag9: `CountOffset=0`, campos 3×Int16 `Texture/startX/startY`, `RecordSize=6`.

### 2. Detección automática al cargar

`IndFileReader.ReadFixedRecords` recibe la lista de variantes del formato y elige la correcta:

1. Para cada variante: leer `Count` en `CountOffset`, verificar `tamañoArchivo == CountOffset + 2 + Count × RecordSize`.
2. La variante que calza exactamente es la usada. Si hay empate, prioridad a la variante Aodrag9 (la actual).
3. Si ninguna calza, usar la variante por defecto (Aodrag9) y continuar con las validaciones de truncamiento existentes.

La variante elegida se guarda en `IndFileData` (p. ej. `data.Variant`) para que el writer escriba el mismo layout.

### 3. Escritura conservando la variante

`IndFileWriter` debe escribir usando los campos y `RecordSize` de la variante cargada, no la por defecto. Así:
- Aom2018 Int16 → se guarda como Int16 (compatible con las herramientas viejas).
- Aodrag9 Int32 → se guarda como Int32.

### 4. TXT actual (sin cambios)

El export/import TXT **actual** del IndEditor (formato verboso `[N]`/`campo = valor`, y el compacto para `graficos.ind`) se mantiene idéntico. Esta opción se llamará en la UI **"Formato actual"**.

### 5. Export TXT estilo DESINDDAT (nuevo)

Nueva opción de exportación **"Formato DESINDDAT"**, disponible al exportar TXT. Formato objetivo por archivo (tomado de `main.c`):

**Personajes/Ataques**
```
' Personajes.ind, desindexado: <fecha hora>

[INIT]
NumBodies=660

[Body1]
Walk1=4582
Walk2=4584
Walk3=4581
Walk4=4583
HeadOffsetX=0
HeadOffsetY=-38

[Body2]
...
```
- `NumAtaques=` en el caso de ataques.ind (nota: `ataques.ind` no existe en `main.c` de DESINDDAT; se usa el mismo patrón de `tIndiceCuerpo` que personajes, con el nombre `NumAtaques`/`[BodyN]`).
- Los 4 campos `WalkN` salen del `Body[N]` (Int32 o Int16 según variante).

**Fxs**
```
' Fxs.ind, desindexado: <fecha hora>

[INIT]
NumFxs=103

[FX1]
Animacion=123
OffsetX=0
OffsetY=0
```

**Cabezas**
```
' Cabezas.ind, desindexado: <fecha hora>

[INIT]
NumHeads=831

[Head1]
Head0=...
Head1=...
Head2=...
Head3=...
```

**Cascos**
```
[INIT]
NumCascos=137

[Casco1]
Head1=...
Head2=...
Head3=...
Head4=...
```

Reglas generales del export DESINDDAT:
- Primera línea: `' <Nombre>, desindexado: <fecha hora>` con apóstrofo inicial. La fecha/hora usa el formato local actual (el C usa `ctime`, no se replica exactamente).
- `[INIT]` + `NumXXX=count`, luego sección por registro con su nombre.
- Nombres de sección/archivo por formato: `NumBodies`/`[BodyN]`, `NumAtaques`/`[BodyN]`, `NumFxs`/`[FXn]`, `NumHeads`/`[HeadN]`, `NumCascos`/`[CascoN]`.
- Cabezas/Cascos: si la variante cargada es Aom2018 (4 campos), se escriben los 4 reales. Si es Aodrag9 (3 campos Texture/startX/startY), se mapea `Head0=Texture, Head1=startX, Head2=startY, Head3=0`.
- Campos `Boolean` no existen en el formato DESINDDAT (fxs Aom2018 no tiene FXTransparente) → no se escriben.
- `graficos.ind` no usa esta opción: su formato DESINDDAT (DESINDGRH) ya es el compacto implementado. El selector de formato queda deshabilitado para GrhData.

### 6. Selector de formato en la UI

En el diálogo "Exportar TXT…" se agrega un selector con dos opciones:
- **Formato actual** (default): el formato verboso/compacto actual del IndEditor.
- **Formato DESINDDAT**: el formato clásico descrito arriba.

El selector solo se muestra/habilita para formatos `FixedRecords` con variante DESINDDAT definida (personajes, ataques, fxs, cabezas, cascos). Para `GrhData`, `TexDefault`, `Minimap` queda deshabilitado (solo formato actual).

### 7. Importación

Solo el **formato actual** se importa (sin cambios). El formato DESINDDAT es solo exportación; importar un TXT estilo DESINDDAT no está en alcance (requeriría un parser de secciones con nombre y no fue solicitado).

## Alcance

- `src/IndLib`: catálogo de variantes, detección en `IndFileReader`, escritura por variante en `IndFileWriter`, exportador DESINDDAT en `TxtExporter` (nuevo método `ExportDesinddat` o similar).
- `src/IndEditor`: selector de formato en `ExportTxt`.
- Tests: detección de ambas variantes, round-trip por variante, formato DESINDDAT esperado por archivo.

Fuera de alcance: Botas/Alas (no soportados por el IndEditor), importación de TXT DESINDDAT, cambios al formato TXT actual de `graficos.ind`.

## Compatibilidad binaria

La escritura conserva la variante detectada, por lo que:
- Un `.ind` de Aom2018 abierto y guardado sin cambios produce bytes idénticos.
- Un `.ind` de Aodrag9 idéntico a como se guarda hoy.

## Verificación

- Abrir `k:\Descargas\aaoo\init\personajes.ind` (Aodrag9, Int32): se detecta variante Int32, 470 registros, round-trip byte-exacto.
- Abrir `K:\Argentum\...\DESINDDAT\Personajes.ind` (Aom2018, Int16): se detecta variante Int16, 660 registros, round-trip byte-exacto.
- Cabezas: `init\cabezas.ind` (654 regs, count@0, 3 campos) vs `DESINDDAT\Cabezas.ind` (831 regs, count@263, 4 campos).
- Fxs: `init\fxs.ind` (59 regs, 10 B) vs `DESINDDAT\Fxs.ind` (103 regs, 6 B).
- Export DESINDDAT de `personajes.ind` Aom2018 produce exactamente el formato del ejemplo del usuario (`[INIT]`/`NumBodies=660`/`[Body1]`/`Walk1=4582`...).
- Export DESINDDAT de `personajes.ind` Aodrag9 produce el mismo formato con los valores Int32 correctos (los que DESINDDAT no puede leer).
- Todos los tests existentes siguen verdes.
