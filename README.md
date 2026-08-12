# IndEditor

<div align="center">

![IndEditor](img/ico/Ao.ico)

**Editor profesional de archivos binarios `.ind` / `.dat` de Argentum Online**

[.NET 9] [WinForms] [Windows]

</div>

---

IndEditor es una aplicación de escritorio para Windows que permite leer, editar y guardar los archivos de datos binarios de **Argentum Online**. Desarrollada con .NET 9 y WinForms, ofrece una interfaz en español con edición en cuadrícula, **round-trip byte-exacto**, y soporte para exportar/importar a formato de texto legible.

## ✨ Características

- 🎯 **Edición visual** en cuadrícula de todos los archivos de datos principales del juego
- 🔒 **Round-trip byte-exacto**: al guardar sin modificar, el archivo resultante es idéntico byte a byte al original
- 💾 **Copia de seguridad automática** (`.bak`) antes de guardar
- 📝 **Exportar a TXT** legible y **reimportar** desde TXT
- 🔍 **Detección automática** de formato por nombre de archivo (case-insensitive)
- 🔄 **Detección automática de variante binaria**: distingue Int32 de Int16 al cargar
- 📋 **Export TXT estilo DESINDDAT**: formato clásico del indexador de Argentum Online
- ✅ **Validación en vivo** con mensajes de error en español
- 🖱️ **Arrastrar y soltar** (drag & drop) para abrir archivos
- 🛡️ **Guards de integridad** que impiden guardar archivos corruptos

## 📁 Formatos soportados

| Formato | Archivos | Registro |
|---|---|---|
| Ataques (`tIndiceAtaque`) | `ataques.ind` | `Body[1..4]` Int32, `HeadOffsetX/Y` Int16 |
| Personajes (`tIndiceCuerpo`) | `personajes.ind` | `Body[1..4]` Int32, `HeadOffsetX/Y` Int16 |
| FXs (`tIndiceFx`) | `fxs.ind` | `Animacion` Int32, `offsetX/Y` Int16, `FXTransparente` Boolean |
| Cabezas / Cascos (`tHead`) | `cabezas.ind`, `cascos.ind` | `Texture`, `startX`, `startY` Int16 |
| Gráficos (`GrhData`) | `graficos.ind` | Variable: estático o animación (`NumFrames`, `Frames`, `Velocidad`) |
| Fuentes (`texdefault`) | `texdefault1.dat` … `texdefault3.dat` | Cabecera VFH (273 B, un registro) |
| Minimapa | `minimap.dat` | Colores AARRGGBB |

> **Nota**: Los formatos de registros fijos existen en dos variantes binarias: **Int32** e **Int16**. La variante se detecta automáticamente al cargar y se conserva al guardar.

## 🚀 Instalación

### Requisitos previos

- Windows 10/11
- [.NET SDK 10](https://dotnet.microsoft.com/download)

### Desde código fuente

```bash
git clone https://github.com/scorpio21/EditorInd.git
cd EditorInd
dotnet run --project src/IndEditor
```

### Desde Visual Studio

1. Abre la solución `IndEditor.sln` en Visual Studio 2022+
2. Ejecuta el proyecto `IndEditor`

## 📖 Uso

### Abrir y editar archivos

1. **Abrir archivo**: `Archivo → Abrir…` (o arrastra el archivo a la ventana)
2. **Editar valores**: modifica directamente en la cuadrícula
3. **Añadir/eliminar filas**: usa la barra de herramientas
4. **Guardar**: `Archivo → Guardar` (Ctrl+S) o `Guardar como…`

### Exportar/Importar TXT

- **Exportar**: `Archivo → Exportar TXT…` - elige entre formato actual o DESINDDAT
- **Importar**: `Archivo → Importar TXT…` - los errores indican el número de línea

### Formato TXT estándar

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

## 🏗️ Arquitectura

```
src/IndEditor/    Aplicación WinForms (interfaz en español)
src/IndLib/       Librería: lectura/escritura binaria, export/import TXT
```

**Componentes de IndLib:**

- `IndFormatCatalog` — definición de formatos binarios
- `IndFormatDetector` — detección por nombre de archivo
- `IndFileReader` / `IndFileWriter` — lectura y escritura binaria
- `TxtExporter` / `TxtImporter` — exportación e importación de texto
- `IndValueLogic` — resolución de valores booleanos para round-trip exacto

## 📄 Licencia

Proyecto no oficial, sin afiliación con los creadores de Argentum Online.

---

<div align="center">

**[⬆ Volver al inicio](#indeditor)**

</div>
