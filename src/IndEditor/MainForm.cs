using System.Globalization;
using System.Text.Json;
using IndLib;

namespace IndEditor;

public partial class MainForm : Form
{
    private enum ColKind { Int16, Int32, Single, Byte, Bool, IntCsv, ByteCsv, HexColor, ReadOnlyInt }

    private readonly MenuStrip _menu = new();
    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _lblFile = new() { Text = "Sin archivo" };
    private readonly ToolStripStatusLabel _lblFormat = new();
    private readonly ToolStripStatusLabel _lblCount = new();
    private readonly ToolStripStatusLabel _lblSize = new();
    private readonly DataGridView _grid = new();
    private readonly Panel _singlePanel = new();
    private readonly SplitContainer _splitContainer = new();
    private readonly ToolStripMenuItem _menuAdd = new("&Añadir fila");
    private readonly ToolStripMenuItem _menuRemove = new("&Eliminar fila");

    private readonly List<(string Name, ColKind Kind)> _colKinds = new();
    // _boolRaw[r] = mapa columna -> short crudo original del campo Boolean
    // (preserva valores no estándar como 0x00FF en round-trip; ver Global Constraints)
    private readonly List<Dictionary<int, short>> _boolRaw = new();
    private IndFileData? _data;
    private string _currentPath = "";
    private string? _graficsPath;
    private IndFileData? _graficosData; // Datos persistentes de graficos.ind

    private bool _isClosing = false;
    private bool _filterEmpty = false;
    private string _graphicsPath = ""; // Carpeta con PNG
    private string _graficosIndPath = ""; // Carpeta con graficos.ind
    private PictureBox? _previewPictureBox;
    private System.Windows.Forms.Timer? _previewTimer;
    private Image?[]? _previewFrameImages;
    private int _previewCurrentFrameIndex;
    private GrhEntry? _previewGrhEntry;
    private const string ConfigFileName = "indeditor_config.json";

    public MainForm()
    {
        InitializeComponent();
        Text = "IndEditor — Editor de archivos .ind/.dat";
        StartPosition = FormStartPosition.CenterScreen;
        try
        {
            var iconStream = System.Reflection.Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("IndEditor.img.ico.Ao.ico");
            if (iconStream != null)
                Icon = new Icon(iconStream);
        }
        catch
        {
            // Si falla el icono, continuar sin él
        }
        
        // Configurar SplitContainer
        _splitContainer.Dock = DockStyle.Fill;
        _splitContainer.Orientation = Orientation.Horizontal;
        _splitContainer.SplitterDistance = 400;
        
        // Panel superior para el grid
        _grid.Dock = DockStyle.Fill;
        _splitContainer.Panel1.Controls.Add(_grid);
        
        // Panel inferior para vista previa
        _previewPictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black,
            Visible = false
        };
        _splitContainer.Panel2.Controls.Add(_previewPictureBox);
        
        Controls.Add(_splitContainer);
        _singlePanel.Dock = DockStyle.Fill;
        _singlePanel.Visible = false;
        
        // Cargar configuración guardada
        LoadConfiguration();
        
        BuildMenu();
        BuildGrid();
        BuildStatus();
        AllowDrop = true;
        FormClosing += (_, _) => _isClosing = true;
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
        _menuAdd.Click += AddRow;
        _menuRemove.Click += RemoveRow;
        file.DropDownItems.Add(_menuAdd);
        file.DropDownItems.Add(_menuRemove);
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("&Salir", (_, _) => Close()));
        _menu.Items.Add(file);
        
        var view = new ToolStripMenuItem("&Ver");
        var filterItem = new ToolStripMenuItem("Ocultar registros vacíos");
        filterItem.CheckOnClick = true;
        filterItem.CheckedChanged += (_, _) => ToggleFilterEmpty();
        view.DropDownItems.Add(filterItem);
        view.DropDownItems.Add(new ToolStripSeparator());
        view.DropDownItems.Add(Item("Carpeta de gráficos (PNG)...", SelectGraphicsFolder));
        view.DropDownItems.Add(Item("Carpeta de graficos.ind...", SelectGraficosIndFolder));
        _menu.Items.Add(view);
        
        var help = new ToolStripMenuItem("&Ayuda");
        help.DropDownItems.Add(Item("&Manual de uso", ShowUserManual));
        help.DropDownItems.Add(new ToolStripSeparator());
        help.DropDownItems.Add(Item("&Acerca de...", About));
        _menu.Items.Add(help);
        _menu.Dock = DockStyle.Top;
        Controls.Add(_menu);
    }

    private void BuildGrid()
    {
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.CellValidating += Grid_CellValidating;
        _grid.CellClick += Grid_CellClick;
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
        _boolRaw.Clear();
        _grid.Columns.Clear();
        _grid.Rows.Clear();
        _singlePanel.Visible = false;
        _grid.Visible = true;
        _menuAdd.Enabled = _data!.Format.Kind != IndFormatKind.TexDefault;
        _menuRemove.Enabled = _data.Format.Kind != IndFormatKind.TexDefault;
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
        var fields = _data!.Variant?.Fields ?? _data.Format.Fields;
        foreach (var f in fields)
        {
            switch (f.Type)
            {
                case IndFieldType.Int16: AddCol(f.Name, f.Label, ColKind.Int16); break;
                case IndFieldType.Int32: AddCol(f.Name, f.Label, ColKind.Int32); break;
                case IndFieldType.Single: AddCol(f.Name, f.Label, ColKind.Single); break;
                case IndFieldType.Boolean: AddCol(f.Name, f.Label, ColKind.Bool); break;
                case IndFieldType.Byte: AddCol(f.Name, f.Label, ColKind.Byte); break;
                case IndFieldType.Int16Array:
                    for (int j = 0; j < f.Count; j++) AddCol($"{f.Name}.{j + 1}", $"{f.Label} {j + 1}", ColKind.Int16);
                    break;
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
            // Filtrar registros vacíos si está activo el filtro
            if (_filterEmpty && IsRecordEmpty(rec, fields))
                continue;

            var cells = new List<object>();
            var raw = new Dictionary<int, short>();
            int cellCol = 0;
            foreach (var f in fields)
            {
                if (f.Type is IndFieldType.Int32Array or IndFieldType.Int16Array)
                {
                    foreach (var v in (int[])rec.Values[f.Name]) { cells.Add(v); cellCol++; }
                }
                else if (f.Type == IndFieldType.ByteArray)
                {
                    cells.Add(string.Join(",", (byte[])rec.Values[f.Name])); cellCol++;
                }
                else if (f.Type == IndFieldType.Boolean)
                {
                    var s = (short)rec.Values[f.Name];
                    raw[cellCol] = s;
                    cells.Add(s != 0); cellCol++;
                }
                else
                {
                    cells.Add(rec.Values[f.Name]); cellCol++;
                }
            }
            _grid.Rows.Add(cells.ToArray());
            _boolRaw.Add(raw);
        }
    }

    private bool IsRecordEmpty(IndRecord rec, IndField[] fields)
    {
        // Un registro se considera vacío si el primer campo de array es 0
        // (similar al código VB6: If MisAtaques(i).Body(1) Then)
        foreach (var f in fields)
        {
            if (f.Type == IndFieldType.Int32Array)
            {
                var arr = (int[])rec.Values[f.Name];
                if (arr.Length > 0 && arr[0] != 0)
                    return false;
            }
            else if (f.Type == IndFieldType.Int16Array)
            {
                var arr = (int[])rec.Values[f.Name];
                if (arr.Length > 0 && arr[0] != 0)
                    return false;
            }
        }
        return true;
    }

    private void ToggleFilterEmpty()
    {
        _filterEmpty = !_filterEmpty;
        ReloadViews();
    }

    private void PopulateGrhGrid()
    {
        AddCol("Grh", "Grh", ColKind.Int32);
        // NumFrames, SX, SY, Ancho, Alto son Int16 en el formato → ColKind.Int16
        // (validación de rango en la celda; evita wraparound del cast (short) en el writer)
        AddCol("NumFrames", "Nº frames", ColKind.Int16);
        AddCol("Frames", "Frames", ColKind.IntCsv);
        AddCol("Velocidad", "Velocidad", ColKind.Single);
        AddCol("FileNum", "Archivo", ColKind.Int32);
        AddCol("SX", "SX", ColKind.Int16);
        AddCol("SY", "SY", ColKind.Int16);
        AddCol("Ancho", "Ancho", ColKind.Int16);
        AddCol("Alto", "Alto", ColKind.Int16);
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
            var fields = _data!.Variant?.Fields ?? _data.Format.Fields;
            foreach (var f in fields)
            {
                if (f.Type is IndFieldType.Int32Array or IndFieldType.Int16Array)
                {
                    var arr = new int[f.Count];
                    for (int j = 0; j < f.Count; j++) { arr[j] = CellInt(r, col); col++; }
                    rec.Values[f.Name] = arr;
                }
                else if (f.Type == IndFieldType.ByteArray)
                {
                    rec.Values[f.Name] = CellBytes(r, col); col++;
                }
                else if (f.Type == IndFieldType.Boolean)
                {
                    // C1: preservar el short crudo original si la celda no cambió
                    // (round-trip byte-exacto para valores no estándar como 0x00FF);
                    // solo normalizar a -1/0 cuando el usuario realmente conmutó la casilla.
                    var current = (bool)CellValue(r, col);
                    var raw = _boolRaw[r].TryGetValue(col, out var rv) ? rv : (short)0;
                    rec.Values[f.Name] = IndValueLogic.ResolveBoolean(current, raw);
                    col++;
                }
                else
                {
                    rec.Values[f.Name] = CellValue(r, col); col++;
                }
            }
            records.Add(rec);
        }
        _data!.Records.Clear();
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
                // I1: validar NumFrames vs Frames antes de escribir — un desajuste
                // desincroniza silenciosamente todos los registros siguientes en el archivo.
                e.Frames = ParseIntCsv(_grid.Rows[r].Cells[2].Value?.ToString());
                if (e.Frames.Length != e.NumFrames)
                    throw new InvalidOperationException(
                        $"Fila {r + 1}: NumFrames = {e.NumFrames} pero hay {e.Frames.Length} frames.");
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
        _data!.GrhEntries.Clear();
        _data.GrhEntries.AddRange(entries);
        _data.Count = entries.Count;
    }

    private void SaveMinimapFromGrid()
    {
        _data!.MinimapEntries.Clear();
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
                var fields = _data.Variant?.Fields ?? _data.Format.Fields;
                foreach (var f in fields)
                {
                    if (f.Type is IndFieldType.Int32Array or IndFieldType.Int16Array)
                        for (int j = 0; j < f.Count; j++) cells.Add(0);
                    else if (f.Type == IndFieldType.ByteArray) cells.Add("");
                    else if (f.Type == IndFieldType.Boolean) cells.Add(false);
                    else cells.Add(0);
                }
                _grid.Rows.Add(cells.ToArray());
                _boolRaw.Add(new Dictionary<int, short>()); // fila nueva: Boolean raw 0 por defecto
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
        var idx = _grid.SelectedRows[0].Index;
        _grid.Rows.RemoveAt(idx);
        _boolRaw.RemoveAt(idx);
        UpdateStatus();
    }

    // ---------- TXT ----------

    private enum TxtFormatChoice { Current, Desinddat }

    private TxtFormatChoice? ChooseTxtFormat()
    {
        using var dlg = new Form
        {
            Text = "Formato de exportación",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
        };
        dlg.Width = 320; dlg.Height = 150;
        var rbCurrent = new RadioButton { Text = "Formato actual", Left = 15, Top = 15, AutoSize = true, Checked = true };
        var rbDesinddat = new RadioButton { Text = "Formato DESINDDAT (AO clásico)", Left = 15, Top = 42, AutoSize = true };
        var btnOk = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Left = 110, Top = 80, Width = 85 };
        var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Left = 205, Top = 80, Width = 85 };
        dlg.Controls.AddRange(new Control[] { rbCurrent, rbDesinddat, btnOk, btnCancel });
        dlg.AcceptButton = btnOk;
        dlg.CancelButton = btnCancel;
        if (dlg.ShowDialog(this) != DialogResult.OK) return null;
        return rbDesinddat.Checked ? TxtFormatChoice.Desinddat : TxtFormatChoice.Current;
    }

    private void ExportTxt(object? sender, EventArgs e)
    {
        if (_data == null) return;
        using var dlg = new SaveFileDialog { Filter = "Texto|*.txt|Todos|*.*", FileName = _data.FileName + ".txt" };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ApplyEdits();
            string txt;
            if (_data.Format.Kind == IndFormatKind.FixedRecords)
            {
                var choice = ChooseTxtFormat();
                if (choice == null) return;
                txt = choice == TxtFormatChoice.Desinddat ? TxtExporter.ExportDesinddat(_data) : TxtExporter.Export(_data);
            }
            else
            {
                txt = TxtExporter.Export(_data);
            }
            File.WriteAllText(dlg.FileName, txt);
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
        if (_isClosing) return;
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

    private void Grid_CellClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (_data == null) return;
        
        var cell = _grid[e.ColumnIndex, e.RowIndex];
        if (cell.Value == null) return;
        
        // Solo procesar celdas con valores numéricos (GRH)
        if (int.TryParse(cell.Value.ToString(), out int grhNumber) && grhNumber > 0)
        {
            if (string.IsNullOrEmpty(_graphicsPath))
            {
                MessageBox.Show(this, 
                    "No hay carpeta de gráficos configurada.\n\nVe a Ver → Carpeta de gráficos... para configurarla.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ShowGrhPreviewInPanel(grhNumber);
        }
    }

    private void SelectGraphicsFolder(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        dlg.Description = "Selecciona la carpeta de gráficos (PNG)";
        if (!string.IsNullOrEmpty(_graphicsPath))
            dlg.SelectedPath = _graphicsPath;
        
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _graphicsPath = dlg.SelectedPath;
            SaveConfiguration();
            MessageBox.Show(this, $"Carpeta de gráficos (PNG) configurada:\n{_graphicsPath}", 
                "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void SelectGraficosIndFolder(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog();
        dlg.Description = "Selecciona la carpeta con graficos.ind (ej: Init/)";
        if (!string.IsNullOrEmpty(_graficosIndPath))
            dlg.SelectedPath = _graficosIndPath;
        
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            _graficosIndPath = dlg.SelectedPath;
            SaveConfiguration();
            
            // Intentar cargar graficos.ind
            string graficosIndFile = Path.Combine(_graficosIndPath, "graficos.ind");
            if (File.Exists(graficosIndFile))
            {
                try
                {
                    _graficosData = IndFileReader.Read(graficosIndFile);
                    MessageBox.Show(this, $"Carpeta de graficos.ind configurada:\n{_graficosIndPath}\n\ngraficos.ind cargado: {_graficosData.GrhEntries.Count} GRH entries", 
                        "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Carpeta de graficos.ind configurada:\n{_graficosIndPath}\n\nError al cargar graficos.ind:\n{ex.Message}", 
                        "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show(this, $"Carpeta de graficos.ind configurada:\n{_graficosIndPath}\n\nNo se encontró graficos.ind en la carpeta", 
                    "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    private void ShowGrhPreviewInPanel(int grhNumber)
    {
        // Detener animación anterior si existe
        if (_previewTimer != null)
        {
            _previewTimer.Stop();
            _previewTimer.Dispose();
            _previewTimer = null;
        }
        
        // Liberar imágenes anteriores
        if (_previewFrameImages != null)
        {
            foreach (var img in _previewFrameImages)
            {
                img?.Dispose();
            }
            _previewFrameImages = null;
        }
        
        // Buscar el GRH en graficos.ind (datos persistentes) o en el archivo actual
        GrhEntry? grhEntry = null;
        
        // Primero buscar en graficos.ind cargado
        if (_graficosData?.GrhEntries != null)
        {
            foreach (var entry in _graficosData.GrhEntries)
            {
                if (entry.Grh == grhNumber)
                {
                    grhEntry = entry;
                    break;
                }
            }
        }
        
        // Si no se encuentra, buscar en el archivo actual (si es graficos.ind)
        if (grhEntry == null && _data?.GrhEntries != null)
        {
            foreach (var entry in _data.GrhEntries)
            {
                if (entry.Grh == grhNumber)
                {
                    grhEntry = entry;
                    break;
                }
            }
        }

        _previewGrhEntry = grhEntry;
        _previewPictureBox!.Visible = true;
        
        try
        {
            if (grhEntry == null || !grhEntry.HasData)
            {
                // GRH no tiene datos en graficos.ind, intentar cargar imagen directa
                LoadSingleImageInPanel(grhNumber);
                _splitContainer.Panel2.Text = $"GRH {grhNumber} (Sin datos en graficos.ind)";
                return;
            }

            if (grhEntry.NumFrames <= 1)
            {
                // GRH estático
                LoadSingleImageInPanel(grhEntry.FileNum);
                _splitContainer.Panel2.Text = $"GRH {grhNumber} | FileNum: {grhEntry.FileNum} | SX: {grhEntry.SX} SY: {grhEntry.SY}";
            }
            else
            {
                // GRH animado - los frames son números de GRH que apuntan a otros GRH
                LoadAnimationFramesInPanel(grhEntry);
                _splitContainer.Panel2.Text = $"GRH {grhNumber} | Frames: {grhEntry.NumFrames} | Velocidad: {grhEntry.Speed:F2}";
                _previewTimer = new System.Windows.Forms.Timer { Interval = (int)(grhEntry.Speed * 1000) };
                _previewTimer.Tick += PreviewTimer_Tick;
                _previewTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _previewPictureBox.Image = null;
            _splitContainer.Panel2.Text = $"Error al cargar GRH {grhNumber}: {ex.Message}";
        }
    }

    private void LoadSingleImageInPanel(int fileNumber)
    {
        string imagePath = Path.Combine(_graphicsPath, $"{fileNumber}.png");
        if (File.Exists(imagePath))
        {
            _previewPictureBox!.Image = Image.FromFile(imagePath);
        }
        else
        {
            _previewPictureBox!.Image = null;
            _splitContainer.Panel2.Text = $"Archivo no encontrado: {imagePath}";
        }
    }

    private void LoadAnimationFramesInPanel(GrhEntry grhEntry)
    {
        if (grhEntry.Frames == null || grhEntry.Frames.Length == 0)
            return;

        _previewFrameImages = new Image[grhEntry.Frames.Length];
        
        // Usar graficos.ind cargado si está disponible, si no usar el archivo actual
        var allGrhEntries = _graficosData?.GrhEntries?.ToList() ?? _data?.GrhEntries?.ToList() ?? new List<GrhEntry>();
        
        for (int i = 0; i < grhEntry.Frames.Length; i++)
        {
            int frameGrhNumber = grhEntry.Frames[i];
            
            // Buscar el GRH del frame en la lista completa
            GrhEntry? frameGrh = null;
            foreach (var entry in allGrhEntries)
            {
                if (entry.Grh == frameGrhNumber && entry.HasData)
                {
                    frameGrh = entry;
                    break;
                }
            }
            
            if (frameGrh != null && frameGrh.NumFrames <= 1)
            {
                // El frame es un GRH estático, cargar su FileNum
                string imagePath = Path.Combine(_graphicsPath, $"{frameGrh.FileNum}.png");
                if (File.Exists(imagePath))
                {
                    _previewFrameImages[i] = Image.FromFile(imagePath);
                }
            }
            else if (frameGrh == null)
            {
                // Si no se encuentra el GRH, intentar cargar directamente por número
                string imagePath = Path.Combine(_graphicsPath, $"{frameGrhNumber}.png");
                if (File.Exists(imagePath))
                {
                    _previewFrameImages[i] = Image.FromFile(imagePath);
                }
            }
        }

        if (_previewFrameImages.Length > 0 && _previewFrameImages[0] != null)
        {
            _previewCurrentFrameIndex = 0;
            _previewPictureBox!.Image = _previewFrameImages[0];
        }
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        if (_previewFrameImages == null || _previewFrameImages.Length == 0)
            return;

        _previewCurrentFrameIndex = (_previewCurrentFrameIndex + 1) % _previewFrameImages.Length;
        if (_previewFrameImages[_previewCurrentFrameIndex] != null)
        {
            _previewPictureBox!.Image = _previewFrameImages[_previewCurrentFrameIndex];
        }
    }

    // ---------- Configuración ----------

    private void LoadConfiguration()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (config != null)
                {
                    if (config.ContainsKey("GraphicsPath"))
                    {
                        _graphicsPath = config["GraphicsPath"];
                    }
                    if (config.ContainsKey("GraficosIndPath"))
                    {
                        _graficosIndPath = config["GraficosIndPath"];
                        
                        // Cargar graficos.ind automáticamente si existe
                        string graficosIndFile = Path.Combine(_graficosIndPath, "graficos.ind");
                        if (File.Exists(graficosIndFile))
                        {
                            try
                            {
                                _graficosData = IndFileReader.Read(graficosIndFile);
                            }
                            catch
                            {
                                // Ignorar error al cargar graficos.ind
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignorar errores al cargar configuración
        }
    }

    private void SaveConfiguration()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
            var config = new Dictionary<string, string>
            {
                { "GraphicsPath", _graphicsPath },
                { "GraficosIndPath", _graficosIndPath }
            };
            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);
        }
        catch
        {
            // Ignorar errores al guardar configuración
        }
    }

    // ---------- Estado ----------

    private void UpdateStatus()
    {
        _lblFile.Text = string.IsNullOrEmpty(_currentPath) ? "Sin archivo" : _currentPath;
        _lblFormat.Text = _data == null ? "" : _data.Variant == null ? _data.Format.DisplayName : $"{_data.Format.DisplayName} ({_data.Variant.Name})";
        _lblCount.Text = _data == null ? "" : $"Registros: {_data.Count}";
        _lblSize.Text = _data == null ? "" : $"{(_currentPath.Length > 0 && File.Exists(_currentPath) ? new FileInfo(_currentPath).Length : 0)} bytes";
    }

    private void ShowUserManual(object? sender, EventArgs e)
    {
        using var manual = new UserManualForm();
        manual.ShowDialog(this);
    }

    private void About(object? sender, EventArgs e)
    {
        MessageBox.Show(this, "IndEditor — Editor de archivos .ind/.dat\n\nVersión 1.0", "Acerca de", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // Añadir en Task 9: ShowSingleRecordView / ReadSingleFromUi
    private void ShowSingleRecordView() { }
    private void ReadSingleFromUi() { }
}
