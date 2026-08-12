namespace IndEditor;

public partial class UserManualForm : Form
{
    public UserManualForm()
    {
        Text = "Manual de uso - IndEditor";
        Size = new Size(700, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(600, 500);
        
        var richTextBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 10),
            BorderStyle = BorderStyle.None,
            Margin = new Padding(10)
        };
        
        richTextBox.Text = @"=== IND EDITOR - MANUAL DE USO ===

¿QUÉ ES IND EDITOR?
IndEditor es una aplicación para editar archivos .ind y .dat utilizados en sistemas como Init16 y Init32. Estos archivos contienen datos del juego como ataques, personajes, gráficos, etc.

=== ABRIR ARCHIVOS ===

1. Desde el menú:
   - Ve a Archivo → Abrir...
   - Selecciona el archivo .ind o .dat que deseas editar

2. Arrastrando archivos:
   - Arrastra un archivo .ind o .dat directamente a la ventana del editor

=== EDITAR DATOS ===

1. Editar celdas:
   - Haz doble clic en cualquier celda para editar su valor
   - Presiona Enter para guardar o Escape para cancelar

2. Tipos de datos:
   - Int16/Int32: Números enteros
   - Single: Números decimales (usar punto como separador)
   - Boolean: Verdadero/Falso
   - Byte: Números de 0 a 255
   - Arrays: Listas de números separados por comas

3. Añadir filas:
   - Ve a Archivo → Añadir fila
   - Se agregará una nueva fila al final

4. Eliminar filas:
   - Selecciona una fila haciendo clic en el encabezado
   - Ve a Archivo → Eliminar fila

=== GUARDAR CAMBIOS ===

1. Guardar (sobrescribir):
   - Ve a Archivo → Guardar (Ctrl+S)
   - Sobrescribe el archivo original

2. Guardar como:
   - Ve a Archivo → Guardar como...
   - Guarda una copia con otro nombre

=== VISTA PREVIA DE GRÁFICOS (GRH) ===

1. Configurar carpetas:
   - Ve a Ver → Carpeta de gráficos (PNG)...
   - Selecciona la carpeta que contiene los archivos PNG de gráficos
   - Ve a Ver → Carpeta de graficos.ind...
   - Selecciona la carpeta que contiene el archivo graficos.ind (ej: Init/)

2. Ver animación:
   - Abre un archivo .ind (ej: ataques.ind)
   - Haz clic en cualquier celda que contenga un número de GRH
   - La animación se mostrará en el panel inferior del formulario

3. Las rutas se guardan automáticamente, no necesitas configurarlas cada vez

=== FILTRO DE REGISTROS VACÍOS ===

1. Ocultar registros vacíos:
   - Ve a Ver → Ocultar registros vacíos
   - Los registros donde el primer campo de array es 0 se ocultarán

=== IMPORTAR/EXPORTAR ===

1. Exportar a texto:
   - Ve a Archivo → Exportar TXT...
   - Guarda los datos en formato de texto legible

2. Importar desde texto:
   - Ve a Archivo → Importar TXT...
   - Carga datos desde un archivo de texto

=== ATAJOS DE TECLADO ===

- Ctrl+O: Abrir archivo
- Ctrl+S: Guardar archivo
- Ctrl+C: Copiar celda seleccionada
- Ctrl+V: Pegar en celda seleccionada
- Enter: Confirmar edición de celda
- Escape: Cancelar edición de celda

=== NOTAS IMPORTANTES ===

- Siempre guarda una copia de seguridad antes de editar archivos importantes
- Los archivos .ind/.dat son binarios, asegúrate de editar solo valores válidos
- La validación de celdas previene valores inválidos
- Si la aplicación muestra un error al cerrar, verifica que no haya celdas con valores inválidos

=== CONFIGURACIÓN ===

La aplicación guarda automáticamente:
- Ruta de la carpeta de gráficos (PNG)
- Ruta de la carpeta de graficos.ind

Esta configuración se guarda en el archivo 'indeditor_config.json' en la misma carpeta que el ejecutable.

=== SOPORTE ===

Para más información o reportar problemas, consulta el repositorio del proyecto.";
        
        Controls.Add(richTextBox);
    }
}
