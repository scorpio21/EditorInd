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
                else if (f.Type == IndFieldType.Boolean)
                    cells.Add(((short)rec.Values[f.Name]) != 0);
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
                else if (f.Type == IndFieldType.Boolean)
                {
                    rec.Values[f.Name] = (bool)CellValue(r, col) ? (short)-1 : (short)0; col++;
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
