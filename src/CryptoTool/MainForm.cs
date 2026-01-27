using ABDTools.Core.Crypto;
using ABDTools.Core.Configuration;
using ABDTools.Core.Common;

namespace CryptoTool;

public partial class MainForm : Form
{
    private readonly CryptoService _cryptoService;
    private readonly ConfigManager<AppConfig> _configManager;
    private readonly List<string> _files = new();

    private ListBox _fileListBox = null!;
    private TextBox _passwordTextBox = null!;
    private CheckBox _showPasswordCheckBox = null!;
    private TextBox _outputDirTextBox = null!;
    private CheckBox _batchModeCheckBox = null!;
    private TextBox _logTextBox = null!;
    private Button _encryptButton = null!;
    private Button _decryptButton = null!;

    public MainForm()
    {
        _cryptoService = new CryptoService();
        _configManager = new ConfigManager<AppConfig>("CryptoTool");
        
        InitializeComponent();
        _ = LoadConfigAsync();
    }

    private void InitializeComponent()
    {
        Text = "CryptoTool - Encriptación AES-256";
        Size = new Size(800, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // File List Section
        var fileListLabel = new Label
        {
            Text = "Archivos:",
            Location = new Point(10, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _fileListBox = new ListBox
        {
            Location = new Point(10, 35),
            Size = new Size(760, 150),
            SelectionMode = SelectionMode.MultiExtended
        };

        var addButton = new Button
        {
            Text = "Agregar",
            Location = new Point(10, 190),
            Size = new Size(100, 30)
        };
        addButton.Click += AddFiles_Click;

        var removeButton = new Button
        {
            Text = "Eliminar",
            Location = new Point(115, 190),
            Size = new Size(100, 30)
        };
        removeButton.Click += RemoveFiles_Click;

        var clearButton = new Button
        {
            Text = "Limpiar",
            Location = new Point(220, 190),
            Size = new Size(100, 30)
        };
        clearButton.Click += ClearFiles_Click;

        // Configuration Section
        var configLabel = new Label
        {
            Text = "Configuración:",
            Location = new Point(10, 230),
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        var passwordLabel = new Label
        {
            Text = "Contraseña:",
            Location = new Point(10, 260),
            AutoSize = true
        };

        _passwordTextBox = new TextBox
        {
            Location = new Point(120, 257),
            Size = new Size(400, 25),
            UseSystemPasswordChar = true
        };

        _showPasswordCheckBox = new CheckBox
        {
            Text = "Mostrar",
            Location = new Point(530, 257),
            AutoSize = true
        };
        _showPasswordCheckBox.CheckedChanged += (s, e) =>
        {
            _passwordTextBox.UseSystemPasswordChar = !_showPasswordCheckBox.Checked;
        };

        var outputDirLabel = new Label
        {
            Text = "Carpeta Salida:",
            Location = new Point(10, 295),
            AutoSize = true
        };

        _outputDirTextBox = new TextBox
        {
            Location = new Point(120, 292),
            Size = new Size(400, 25),
            ReadOnly = true
        };

        var browseButton = new Button
        {
            Text = "...",
            Location = new Point(530, 290),
            Size = new Size(40, 25)
        };
        browseButton.Click += BrowseOutputDir_Click;

        _batchModeCheckBox = new CheckBox
        {
            Text = "Modo Batch (mantener lista y contraseña)",
            Location = new Point(10, 325),
            AutoSize = true
        };

        // Action Buttons
        _encryptButton = new Button
        {
            Text = "🔒 ENCRIPTAR",
            Location = new Point(10, 360),
            Size = new Size(375, 40),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _encryptButton.Click += Encrypt_Click;

        _decryptButton = new Button
        {
            Text = "🔓 DESENCRIPTAR",
            Location = new Point(395, 360),
            Size = new Size(375, 40),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            BackColor = Color.FromArgb(33, 150, 243),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        _decryptButton.Click += Decrypt_Click;

        // Log Section
        var logLabel = new Label
        {
            Text = "Registro de Operaciones:",
            Location = new Point(10, 410),
            AutoSize = true,
            Font = new Font("Segoe UI", 10, FontStyle.Bold)
        };

        _logTextBox = new TextBox
        {
            Location = new Point(10, 435),
            Size = new Size(760, 115),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(224, 224, 224)
        };

        // Add all controls
        Controls.AddRange(new Control[]
        {
            fileListLabel, _fileListBox, addButton, removeButton, clearButton,
            configLabel, passwordLabel, _passwordTextBox, _showPasswordCheckBox,
            outputDirLabel, _outputDirTextBox, browseButton, _batchModeCheckBox,
            _encryptButton, _decryptButton, logLabel, _logTextBox
        });

        AppendLog("Listo para procesar archivos.");
    }

    private async Task LoadConfigAsync()
    {
        var result = await _configManager.LoadAsync();
        var config = result.Value;

        if (result.IsCorrupted)
        {
            MessageBox.Show(
                $"No se pudo leer la configuración. Se usarán valores por defecto.\n\n{result.Error?.Message}",
                "Configuración dañada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _outputDirTextBox.Text = config.LastOutputDirectory ?? "";
        _batchModeCheckBox.Checked = config.BatchMode;
    }

    private void AddFiles_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Seleccionar archivos"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!_files.Contains(file))
                {
                    _files.Add(file);
                    var isEnc = FileUtils.IsEncryptedFile(file);
                    _fileListBox.Items.Add(isEnc ? $"🔒 {Path.GetFileName(file)}" : Path.GetFileName(file));
                }
            }
        }
    }

    private void RemoveFiles_Click(object? sender, EventArgs e)
    {
        var selectedIndices = _fileListBox.SelectedIndices.Cast<int>().OrderByDescending(i => i).ToList();
        foreach (var index in selectedIndices)
        {
            _files.RemoveAt(index);
            _fileListBox.Items.RemoveAt(index);
        }
    }

    private void ClearFiles_Click(object? sender, EventArgs e)
    {
        if (_files.Count > 0 && MessageBox.Show("¿Limpiar la lista?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            _files.Clear();
            _fileListBox.Items.Clear();
            AppendLog("Lista limpiada");
        }
    }

    private void BrowseOutputDir_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            _outputDirTextBox.Text = dialog.SelectedPath;
        }
    }

    private async void Encrypt_Click(object? sender, EventArgs e)
    {
        await ProcessFilesAsync(true);
    }

    private async void Decrypt_Click(object? sender, EventArgs e)
    {
        await ProcessFilesAsync(false);
    }

    private async Task ProcessFilesAsync(bool encrypt)
    {
        if (string.IsNullOrWhiteSpace(_passwordTextBox.Text))
        {
            MessageBox.Show("Ingrese una contraseña.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_files.Count == 0)
        {
            MessageBox.Show("La lista está vacía.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _encryptButton.Enabled = false;
        _decryptButton.Enabled = false;

        var operation = encrypt ? "ENCRIPTACIÓN" : "DESENCRIPTACIÓN";
        AppendLog($"--- INICIANDO {operation} ---");

        int success = 0, fail = 0, skip = 0;

        foreach (var file in _files.ToList())
        {
            try
            {
                var fileName = Path.GetFileName(file);
                var isEnc = FileUtils.IsEncryptedFile(file);

                if (encrypt && isEnc)
                {
                    AppendLog($"[SKIP] {fileName} ya es .enc");
                    skip++;
                    continue;
                }

                if (!encrypt && !isEnc)
                {
                    AppendLog($"[SKIP] {fileName} no es .enc");
                    skip++;
                    continue;
                }

                string destPath;
                if (!string.IsNullOrWhiteSpace(_outputDirTextBox.Text))
                {
                    var destFileName = encrypt ? $"{fileName}.enc" : FileUtils.RemoveEncExtension(fileName);
                    destPath = Path.Combine(_outputDirTextBox.Text, destFileName);
                }
                else
                {
                    destPath = encrypt ? $"{file}.enc" : Path.Combine(
                        Path.GetDirectoryName(file)!,
                        FileUtils.RemoveEncExtension(fileName));
                }

                if (encrypt)
                {
                    await _cryptoService.EncryptFileAsync(file, destPath, _passwordTextBox.Text);
                }
                else
                {
                    await _cryptoService.DecryptFileAsync(file, destPath, _passwordTextBox.Text);
                }

                AppendLog($"[OK] {fileName} → {Path.GetFileName(destPath)}");
                success++;
            }
            catch (Exception ex)
            {
                AppendLog($"[X] {Path.GetFileName(file)}: {ex.Message}");
                fail++;
            }
        }

        AppendLog($"--- FIN: {success} OK, {fail} Errores, {skip} Saltados ---");

        if (fail == 0 && !_batchModeCheckBox.Checked)
        {
            _passwordTextBox.Clear();
            _files.Clear();
            _fileListBox.Items.Clear();
            AppendLog("Estado limpiado por seguridad.");
        }

        _encryptButton.Enabled = true;
        _decryptButton.Enabled = true;

        await SaveConfigAsync();
    }

    private void AppendLog(string message)
    {
        _logTextBox.AppendText($"{message}\r\n");
    }

    private async Task SaveConfigAsync()
    {
        var config = new AppConfig
        {
            LastOutputDirectory = _outputDirTextBox.Text,
            BatchMode = _batchModeCheckBox.Checked
        };

        try
        {
            await _configManager.SaveAsync(config);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se ha podido guardar la configuración:\n{ex.Message}",
                "Error al guardar configuración",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

public class AppConfig
{
    public string? LastOutputDirectory { get; set; }
    public bool BatchMode { get; set; }
}
