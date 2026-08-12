using IndLib;

namespace IndEditor;

public partial class GrhPreviewForm : Form
{
    private readonly int _grhNumber;
    private readonly GrhEntry? _grhEntry;
    private readonly string _graphicsPath;
    private readonly List<GrhEntry> _allGrhEntries;
    private readonly PictureBox _pictureBox;
    private readonly System.Windows.Forms.Timer _animationTimer;
    private readonly Label _infoLabel;
    private int _currentFrameIndex;
    private Image?[]? _frameImages;

    public GrhPreviewForm(int grhNumber, GrhEntry? grhEntry, string graphicsPath, List<GrhEntry>? allGrhEntries = null)
    {
        _grhNumber = grhNumber;
        _grhEntry = grhEntry;
        _graphicsPath = graphicsPath;
        _allGrhEntries = allGrhEntries ?? new List<GrhEntry>();
        
        Text = $"Vista previa GRH {_grhNumber}";
        Size = new Size(400, 450);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        MinimumSize = new Size(300, 350);
        
        _pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };
        
        _infoLabel = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            Text = $"GRH: {_grhNumber}",
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.WhiteSmoke
        };
        
        _animationTimer = new System.Windows.Forms.Timer { Interval = 100 };
        _animationTimer.Tick += AnimationTimer_Tick;
        
        Controls.Add(_pictureBox);
        Controls.Add(_infoLabel);
        
        LoadGrh();
    }

    private void LoadGrh()
    {
        try
        {
            if (_grhEntry == null || !_grhEntry.HasData)
            {
                // GRH no tiene datos en graficos.ind, intentar cargar imagen directa
                LoadSingleImage(_grhNumber);
                _infoLabel.Text = $"GRH: {_grhNumber} (Sin datos en graficos.ind)";
                return;
            }

            if (_grhEntry.NumFrames <= 1)
            {
                // GRH estático
                LoadSingleImage(_grhEntry.FileNum);
                _infoLabel.Text = $"GRH: {_grhNumber} | FileNum: {_grhEntry.FileNum} | SX: {_grhEntry.SX} SY: {_grhEntry.SY}";
            }
            else
            {
                // GRH animado - los frames son números de GRH que apuntan a otros GRH
                LoadAnimationFramesFromGrh();
                _infoLabel.Text = $"GRH: {_grhNumber} | Frames: {_grhEntry.NumFrames} | Velocidad: {_grhEntry.Speed:F2}";
                _animationTimer.Interval = (int)(_grhEntry.Speed * 1000);
                _animationTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _pictureBox.Image = null;
            _infoLabel.Text = $"Error al cargar GRH {_grhNumber}:\n{ex.Message}";
        }
    }

    private void LoadSingleImage(int fileNumber)
    {
        string imagePath = Path.Combine(_graphicsPath, $"{fileNumber}.png");
        if (File.Exists(imagePath))
        {
            _pictureBox.Image = Image.FromFile(imagePath);
        }
        else
        {
            _pictureBox.Image = null;
            _infoLabel.Text = $"Archivo no encontrado: {imagePath}";
        }
    }

    private void LoadAnimationFramesFromGrh()
    {
        if (_grhEntry?.Frames == null || _grhEntry.Frames.Length == 0)
            return;

        _frameImages = new Image[_grhEntry.Frames.Length];
        
        for (int i = 0; i < _grhEntry.Frames.Length; i++)
        {
            int frameGrhNumber = _grhEntry.Frames[i];
            
            // Buscar el GRH del frame en la lista completa
            GrhEntry? frameGrh = null;
            foreach (var entry in _allGrhEntries)
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
                    _frameImages[i] = Image.FromFile(imagePath);
                }
            }
            else if (frameGrh == null)
            {
                // Si no se encuentra el GRH, intentar cargar directamente por número
                string imagePath = Path.Combine(_graphicsPath, $"{frameGrhNumber}.png");
                if (File.Exists(imagePath))
                {
                    _frameImages[i] = Image.FromFile(imagePath);
                }
            }
        }

        if (_frameImages.Length > 0 && _frameImages[0] != null)
        {
            _currentFrameIndex = 0;
            _pictureBox.Image = _frameImages[0];
        }
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_frameImages == null || _frameImages.Length == 0)
            return;

        _currentFrameIndex = (_currentFrameIndex + 1) % _frameImages.Length;
        if (_frameImages[_currentFrameIndex] != null)
        {
            _pictureBox.Image = _frameImages[_currentFrameIndex];
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer.Stop();
            _animationTimer.Dispose();
            if (_frameImages != null)
            {
                foreach (var img in _frameImages)
                {
                    img?.Dispose();
                }
            }
            _pictureBox.Image?.Dispose();
        }
        base.Dispose(disposing);
    }
}
