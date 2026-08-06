# IndEditor - Formato TXT compacto para graficos.ind (GrhData)

**Fecha:** 2026-08-07
**Estado:** Aprobado por el usuario
**Propósito:** Cambiar la exportación/importación TXT del formato `GrhData` (`graficos.ind`) del formato verboso actual (bloques `[N]` con `campo = valor`) al formato compacto clásico del GraficosEditor de Argentum Online, tomando como referencia el desindexador `DESINDGRH/main.c`.

## Antecedentes

Actualmente `TxtExporter.Export` produce para `graficos.ind`:

```text
# IndEditor v1.0
# Archivo: graficos.ind
# Formato: GrhData
# Entradas: 24548

[1]
Grh = 1
NumFrames = 1
FileNum = 1
SX = 64
SY = 0
Ancho = 32
Alto = 32
```

Se desea el formato clásico compacto, de una línea por entrada, compatible con las herramientas clásicas de AO.

## Formato TXT objetivo (GrhData)

### Cabecera

```text
'Graficos.ind desindexado con IndEditor
'<fecha hora actual>

[Graphics]

Grh1=1-1-64-0-32-32-

Grh2=1-1-32-0-32-32-
```

- Las dos primeras líneas son comentarios con apóstrofo (`'`).
- La línea de fecha/hora usa el formato de fecha local actual.
- Luego `[Graphics]`.
- Una línea `GrhN=...` por entrada, cada una con **guion final `-`** y separadas por una línea en blanco.

### Entrada estática (NumFrames = 1)

```
GrhN=1-FileNum-SX-SY-Ancho-Alto-
```

Ejemplo: `Grh1=1-1-64-0-32-32-` → Grh=1, NumFrames=1, FileNum=1, SX=64, SY=0, Ancho=32, Alto=32.

### Entrada animada (NumFrames > 1)

```
GrhN=NumFrames-F1-F2-...-Fnum-Velocidad-
```

Ejemplo: `Grh23=6-1-2-3-4-5-6-1-` → NumFrames=6, Frames=[1,2,3,4,5,6], Velocidad=1.

- La velocidad se escribe con precisión float round-trip (`"R"`, invariant culture), ej. `1` o `1.5`.

### Entradas vacías (Grh = 0)

Se omiten del export, igual que `DESINDGRH/main.c` que corta en `grh <= 0`. Nota: en `graficos.ind` real (24548 entradas) todas son activas, por lo que el round-trip byte-exacto se conserva.

## Alcance

El cambio aplica **solo** a `IndFormatKind.GrhData`. Los demás formatos (`FixedRecords`, `TexDefault`, `Minimap`) mantienen su exportación verbosa actual sin cambios.

## Importación TXT (GrhData)

`TxtImporter.Import` para `GrhData` debe:

1. Ignorar comentarios `'...`, `#...`, líneas en blanco y la línea de sección `[Graphics]`.
2. Parsear cada línea `GrhN=...`:
   - `N` = número Grh (debe ser > 0).
   - Valor separado por `-`.
   - Primer token = `NumFrames`.
     - Si `NumFrames == 1`: tokens restantes = `FileNum, SX, SY, Ancho, Alto` (5 valores).
     - Si `NumFrames > 1`: tokens restantes = `NumFrames` frames + 1 velocidad (en total `NumFrames + 1`).
   - Tolerante al guion final (con o sin `-`), ya que el split produce un token vacío final que se descarta.
3. Solo acepta el formato nuevo. Un bloque verboso viejo (`[n]`, `Grh = ...`, `NumFrames = ...`, etc.) produce error con número de línea.
4. Conserva `HeaderBytes` (pasados como parámetro) → round-trip byte-exacto.
5. Establece `Count = GrhEntries.Count`.

Errores: todos los mensajes incluyen el número de línea (`Línea N: ...`), siguiendo el patrón existente de `TxtImporter`.

## Compatibilidad binaria

No cambia la lectura/escritura binaria. El `graficos.ind` del mod Aodrag9 usa header de 8 bytes con Int32, distinto del layout de `main.c` (273 bytes, shorts). El TXT nuevo es el único cambio; el binario se sigue leyendo/escribiendo con `IndFileReader`/`IndFileWriter` tal cual.

## Verificación

- Prueba `Export_Import_RoundTrip("graficos.ind")` debe seguir pasando: `IndFileWriter.ToBytes` idéntico antes y después de exportar/importar TXT.
- El export de `graficos.ind` debe contener `[Graphics]` y líneas `Grh1=1-1-64-0-32-32-`.
- Los demás formatos (`ataques.ind`) conservan su formato verboso (`# Formato: tIndiceAtaque`).
- Mensajes de error con número de línea en TXT inválido.
