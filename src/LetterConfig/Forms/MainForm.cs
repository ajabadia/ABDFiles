using ABDTools.Core.Configuration;
using ABDTools.Core.Gaweb.Models;
using LetterConfig.Services;
using LetterConfig.Controls;
using LetterConfig.Models;
using System.Text.Json;

namespace LetterConfig.Forms;

public partial class MainForm : Form
{
    private PresetEditorControl editor;
    private MenuStrip menuStrip;
    private AppConfig _config = new AppConfig();
    private readonly ConfigManager<AppConfig> _configManager;

    // State
    private string? _currentFilePath;
    private GawebPreset _currentPreset;
    
    public MainForm()
    {
        _configManager = new ConfigManager<AppConfig>("LetterConfig");
        InitializeComponent();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await LoadConfigAsync();
        
        // Start with a new empty preset
        DoNew();
    }

    private async Task LoadConfigAsync()
    {
        var res = await _configManager.LoadAsync();
        if (res.IsCorrupted)
        {
            MessageBox.Show($"Configuración corrupta. Se han restaurado los valores por defecto.\n{res.Error?.Message}", "Error Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        if (res.Exists || res.IsCorrupted)
        {
            _config = res.Value;
        }
    }

    private void InitializeComponent()
    {
        this.Text = "LetterConfig - Generador de Cartas";
        this.Size = new Size(1000, 800);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MainMenuStrip = new MenuStrip();

        // --- Menu: Archivo ---
        var menuFile = new ToolStripMenuItem("Archivo");
        menuFile.DropDownItems.Add(new ToolStripMenuItem("Nuevo", null, (s, e) => DoNew()) { ShortcutKeys = Keys.Control | Keys.N });
        menuFile.DropDownItems.Add(new ToolStripMenuItem("Abrir...", null, (s, e) => DoOpen()) { ShortcutKeys = Keys.Control | Keys.O });
        menuFile.DropDownItems.Add(new ToolStripSeparator());
        menuFile.DropDownItems.Add(new ToolStripMenuItem("Guardar", null, (s, e) => DoSave()) { ShortcutKeys = Keys.Control | Keys.S });
        menuFile.DropDownItems.Add(new ToolStripMenuItem("Guardar Como...", null, (s, e) => DoSaveAs()));
        menuFile.DropDownItems.Add(new ToolStripSeparator());
        menuFile.DropDownItems.Add(new ToolStripMenuItem("Salir", null, (s, e) => Close()));

        // --- Menu: Editar ---
        var menuEdit = new ToolStripMenuItem("Editar");
        // Standard clipboard actions (generic implementation, might not work on custom controls without focus logic, but standard for menu)
        menuEdit.DropDownItems.Add(new ToolStripMenuItem("Copiar", null, (s, e) => SendKeys.Send("^c"))); 
        menuEdit.DropDownItems.Add(new ToolStripMenuItem("Cortar", null, (s, e) => SendKeys.Send("^x")));
        menuEdit.DropDownItems.Add(new ToolStripMenuItem("Pegar", null, (s, e) => SendKeys.Send("^v")));
        menuEdit.DropDownItems.Add(new ToolStripSeparator());
        menuEdit.DropDownItems.Add(new ToolStripMenuItem("Preferencias...", null, (s, e) => DoPrefs()));

        // --- Menu: Ayuda ---
        var menuHelp = new ToolStripMenuItem("Ayuda");
        menuHelp.DropDownItems.Add(new ToolStripMenuItem("Manual de Usuario", null, (s, e) => DoManual()));
        menuHelp.DropDownItems.Add(new ToolStripSeparator());
        menuHelp.DropDownItems.Add(new ToolStripMenuItem("Acerca de...", null, (s, e) => DoAbout()));

        this.MainMenuStrip.Items.Add(menuFile);
        this.MainMenuStrip.Items.Add(menuEdit);
        this.MainMenuStrip.Items.Add(menuHelp);
        this.Controls.Add(this.MainMenuStrip);

        // --- Editor ---
        editor = new PresetEditorControl
        {
            Dock = DockStyle.Fill
        };
        // Hook into save event from editor button too? 
        // Logic: Editor has "Save" button. We should wire it to DoSave() or hide it if we want menu-only.
        // For standard Windows, usually both exist.
        editor.PresetSaved += (s, e) => DoSave(); 
        editor.PresetCancelled += (s, e) => Close(); // Or New? Cancel usually means close dialog, here maybe close app or nothing.
        
        this.Controls.Add(editor);
        // Ensure menu is top
        this.MainMenuStrip.SendToBack(); // Dock fill takes space, Menu strip is Dock Top by default.
    }

    // --- Logic ---

    private void DoNew()
    {
        _currentFilePath = null;
        _currentPreset = new GawebPreset
        {
            Id = Guid.NewGuid().ToString(), // ID is less important in file mode, but good to keep
            Name = "Nueva Configuración",
            Active = true,
            FechaGeneracion = DateTime.Now.ToString("yyyyMMdd"),
            FechaCarta = DateTime.Now.ToString("yyyyMMdd"),
            PaginasDefecto = 4,
            TipoSoporte = "PDF",
            FormatoCarta = "04",
            ForzarMetodo = " ",
            IndicadorDestino = "0",
            TipoDestino = "CL",
            ViaReparto = _config != null ? _config.DefaultLanguage : "ES", // Use logic? No, just default
            // Wait, DefaultLanguage in Prefs should apply here?
            Idioma = _config?.DefaultLanguage ?? "ES"
        };
        
        UpdateTitle();
        editor.LoadPreset(_currentPreset);
    }

    private void DoOpen()
    {
        using (var dlg = new OpenFileDialog())
        {
            dlg.Filter = "Archivos JSON (*.json)|*.json|Todos los archivos (*.*)|*.*";
            if (_config != null && Directory.Exists(_config.DefaultSavePath)) dlg.InitialDirectory = _config.DefaultSavePath;
            
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string json = File.ReadAllText(dlg.FileName);
                    var loaded = JsonSerializer.Deserialize<GawebPreset>(json);
                    if (loaded != null)
                    {
                        _currentPreset = loaded;
                        _currentFilePath = dlg.FileName;
                        editor.LoadPreset(_currentPreset);
                        UpdateTitle();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al abrir el archivo:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    private void DoSave()
    {
        // 1. Trigger validation/update from Editor to Object
        // Editor usually updates object on 'Save' click. 
        // We need internal method in editor to "Flush" data to object without clicking button?
        // Or we assume user filled it.
        // Refactor Editor to expose "ApplyChanges()" method.
        // For now, let's assume 'PresetEditorControl' updates the object reference passed in LoadPreset *LIVE* or on events?
        // Looking at previous PresetEditorControl code: It updates on 'BtnSave_Click'.
        // We generally want "Save" menu to trigger that logic.
        // Let's call a public method on Editor.
        
        if (!editor.ApplyChanges()) return; // Validation failed

        if (string.IsNullOrEmpty(_currentFilePath))
        {
            DoSaveAs();
        }
        else
        {
            SaveToFileAsync(_currentFilePath);
            MessageBox.Show("Guardado correctamente.", "LetterConfig", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void DoSaveAs()
    {
        if (!editor.ApplyChanges()) return;

        using (var dlg = new SaveFileDialog())
        {
            dlg.Filter = "Archivos JSON (*.json)|*.json|Todos los archivos (*.*)|*.*";
            dlg.FileName = $"{_currentPreset.Name}.json";
            if (_config != null && Directory.Exists(_config.DefaultSavePath)) dlg.InitialDirectory = _config.DefaultSavePath;

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _currentFilePath = dlg.FileName;
                SaveToFileAsync(_currentFilePath);
                UpdateTitle();
                MessageBox.Show("Guardado correctamente.", "LetterConfig", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private async void SaveToFileAsync(string path)
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_currentPreset, options);
            await File.WriteAllTextAsync(path, json);
            
            // Update config default path
             _config.DefaultSavePath = Path.GetDirectoryName(path) ?? _config.DefaultSavePath;
             await _configManager.SaveAsync(_config);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void DoPrefs()
    {
        using (var dlg = new PreferencesForm(_config, _configManager))
        {
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                // Config saved internally by form
                // Reload or apply if needed (e.g. language default only applies to new)
            }
        }
    }

    private void DoManual()
    {
        // Placeholder for now
        MessageBox.Show("El manual de usuario no está disponible en esta versión.", "Manual", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void DoAbout()
    {
        using (var dlg = new AboutForm())
        {
            dlg.ShowDialog();
        }
    }

    private void UpdateTitle()
    {
        string filename = string.IsNullOrEmpty(_currentFilePath) ? "Sin Título" : Path.GetFileName(_currentFilePath);
        this.Text = $"LetterConfig - {filename}";
        
        // Also update editor? No, editor is fine.
    }
}
