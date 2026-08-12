namespace IndEditor;

public partial class UserManualForm : Form
{
    public UserManualForm()
    {
        Text = "IndEditor - Ayuda";
        Size = new Size(750, 650);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(650, 500);
        BackColor = Color.FromArgb(240, 240, 240);
        
        var richTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("MS Sans Serif", 9),
            BorderStyle = BorderStyle.Fixed3D,
            Margin = new Padding(5),
            ScrollBars = RichTextBoxScrollBars.Both
        };
        
        richTextBox.Text = @"IND EDITOR - DOCUMENTACIÓN DE AYUDA
====================================

ÍNDICE
------
1. INTRODUCCIÓN
   1a. Descripción del programa
   1b. Formatos de archivo soportados
   1c. Requisitos del sistema

2. OPERACIONES BÁSICAS
   2a. Abrir archivos
   2b. Guardar archivos
   2c. Cerrar archivos

3. EDICIÓN DE DATOS
   3a. Modificar celdas
   3b. Tipos de datos válidos
   3c. Añadir registros
   3d. Eliminar registros
   3e. Validación de datos

4. VISTA PREVIA DE GRÁFICOS
   4a. Configuración de rutas
   4b. Visualización de animaciones
   4c. Referencias de GRH

5. FUNCIONES ADICIONALES
   5a. Filtro de registros
   5b. Importar/Exportar
   5c. Atajos de teclado

6. CONFIGURACIÓN
   6a. Archivo de configuración
   6b. Persistencia de rutas

7. SOLUCIÓN DE PROBLEMAS
   7a. Errores comunes
   7b. Recuperación de datos


1. INTRODUCCIÓN
===============

1a. Descripción del programa
---------------------------
IndEditor es una herramienta de edición de archivos binarios .ind y .dat utilizados en sistemas Init16 y Init32. Permite la visualización, modificación y validación de datos estructurados en formato binario.

El programa opera mediante una interfaz de tabla (grid) que representa los registros del archivo en formato legible, permitiendo la edición directa de valores con validación de tipos.


1b. Formatos de archivo soportados
----------------------------------
- Archivos .ind: Índices de datos (ataques, personajes, objetos)
- Archivos .dat: Datos binarios variados
- Variantes: Init16 (Int16) e Init32 (Int32)
- Formatos: Registros fijos, datos GRH, texturas, minimapas


1c. Requisitos del sistema
--------------------------
- Sistema operativo: Windows 10 o superior
- .NET 9.0 Runtime
- Memoria RAM: 512 MB mínimo
- Espacio en disco: 10 MB


2. OPERACIONES BÁSICAS
======================

2a. Abrir archivos
-----------------
Método 1: Menú Archivo → Abrir...
Método 2: Arrastrar archivo a la ventana
Método 3: Atajo de teclado Ctrl+O

El programa detecta automáticamente el formato del archivo y presenta los datos en la tabla correspondiente.


2b. Guardar archivos
-------------------
Guardar (sobrescribir): Archivo → Guardar (Ctrl+S)
Guardar como: Archivo → Guardar como...

Nota: Se recomienda crear copias de seguridad antes de modificar archivos originales.


2c. Cerrar archivos
------------------
Cerrar archivo actual: Archivo → Cerrar
Cerrar aplicación: Archivo → Salir o Alt+F4

Si hay cambios sin guardar, el programa solicitará confirmación antes de cerrar.


3. EDICIÓN DE DATOS
===================

3a. Modificar celdas
-------------------
1. Doble clic en la celda a editar
2. Introducir el nuevo valor
3. Enter para confirmar o Escape para cancelar

La validación de tipos se realiza automáticamente al confirmar.


3b. Tipos de datos válidos
--------------------------
Int16: Entero de 16 bits (-32768 a 32767)
Int32: Entero de 32 bits (-2147483648 a 2147483647)
Single: Decimal de precisión simple (usar punto como separador)
Byte: Entero de 8 bits (0 a 255)
Boolean: Verdadero/Falso
IntCsv: Lista de enteros separados por comas
ByteCsv: Lista de bytes separados por comas


3c. Añadir registros
--------------------
1. Archivo → Añadir fila
2. Se inserta un registro vacío al final de la tabla
3. Editar los campos según sea necesario

Nota: No todos los formatos permiten añadir registros.


3d. Eliminar registros
----------------------
1. Seleccionar la fila haciendo clic en el encabezado
2. Archivo → Eliminar fila
3. Confirmar la eliminación

Advertencia: Esta operación no se puede deshacer.


3e. Validación de datos
------------------------
El programa valida automáticamente:
- Rangos de valores numéricos
- Tipos de datos correctos
- Longitud de arrays
- Valores nulos en campos obligatorios

Si se introduce un valor inválido, se muestra un mensaje de error y la celda no se actualiza.


4. VISTA PREVIA DE GRÁFICOS
============================

4a. Configuración de rutas
---------------------------
1. Ver → Carpeta de gráficos (PNG)...
   Seleccionar carpeta con archivos PNG de gráficos

2. Ver → Carpeta de graficos.ind...
   Seleccionar carpeta con archivo graficos.ind (ej: Init/)

Las rutas se guardan automáticamente en indeditor_config.json.


4b. Visualización de animaciones
--------------------------------
1. Abrir archivo .ind (ej: ataques.ind)
2. Clic en celda con número de GRH
3. La animación se muestra en panel inferior

El panel inferior es redimensionable arrastrando el divisor.


4c. Referencias de GRH
---------------------
Los GRH animados contienen referencias a otros GRH que representan cada frame. El programa resuelve estas referencias automáticamente mediante graficos.ind para mostrar la animación correcta.

Ejemplo: GRH 20466 → frames [20430, 20431, 20432, ...] → cada frame busca su FileNum correspondiente.


5. FUNCIONES ADICIONALES
=========================

5a. Filtro de registros
-----------------------
Ver → Ocultar registros vacíos

Oculta registros donde el primer campo de array es 0. Útil para filtrar entradas no utilizadas.


5b. Importar/Exportar
---------------------
Exportar TXT: Archivo → Exportar TXT...
Genera archivo de texto legible con los datos actuales.

Importar TXT: Archivo → Importar TXT...
Carga datos desde archivo de texto previamente exportado.


5c. Atajos de teclado
---------------------
Ctrl+O: Abrir archivo
Ctrl+S: Guardar archivo
Ctrl+C: Copiar celda seleccionada
Ctrl+V: Pegar en celda seleccionada
Enter: Confirmar edición de celda
Escape: Cancelar edición de celda
Alt+F4: Cerrar aplicación


6. CONFIGURACIÓN
================

6a. Archivo de configuración
-----------------------------
Ubicación: indeditor_config.json (misma carpeta que el ejecutable)

Contenido:
- GraphicsPath: Ruta a carpeta de PNG
- GraficosIndPath: Ruta a carpeta de graficos.ind

Formato: JSON


6b. Persistencia de rutas
-------------------------
Las rutas configuradas se guardan automáticamente y se cargan al iniciar la aplicación. No es necesario reconfigurarlas en cada sesión.


7. SOLUCIÓN DE PROBLEMAS
========================

7a. Errores comunes
-------------------
Error: ""No hay carpeta de gráficos configurada""
Solución: Configurar rutas en menú Ver

Error: ""Valor inválido en la celda""
Solución: Verificar tipo de dato y rango permitidos

Error: ""Archivo no encontrado""
Solución: Verificar que la ruta de gráficos sea correcta


7b. Recuperación de datos
-------------------------
Si se produce un error al guardar:
1. Verificar espacio en disco
2. Verificar permisos de escritura
3. Restaurar desde copia de seguridad

Si el archivo se corrompe:
1. Restaurar desde backup original
2. Verificar que los valores editados sean válidos


FIN DE LA DOCUMENTACIÓN
======================
Versión 1.0
IndEditor - Editor de archivos .ind/.dat";
        
        Controls.Add(richTextBox);
    }
}
