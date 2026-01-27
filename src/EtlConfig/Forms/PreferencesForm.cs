using EtlConfig.Models;

using ABDTools.Core.Configuration;

namespace EtlConfig.Forms;

public class PreferencesForm : Form
{
    private TextBox txtPath;
    private ComboBox cmbLang;
    private ComboBox cmbEncoding;
    private TextBox txtChunkSize;
    private Button btnBrowse;
    private Button btnSave;
    private Button btnCancel;
    
    private readonly AppConfig _config;
    private readonly ConfigManager<AppConfig> _configManager;

    public PreferencesForm(AppConfig config, ConfigManager<AppConfig> manager)
    {
        _config = config;
        _configManager = manager;
        InitializeComponent();
        LoadValues();
    }

    private void InitializeComponent()
    {
        this.Text = "Preferencias";
        this.Size = new Size(500, 350);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        var lblPath = new Label { Text = "Ruta por Defecto:", Location = new Point(20, 20), AutoSize = true };
        txtPath = new TextBox { Location = new Point(20, 45), Width = 350, ReadOnly = true };
        btnBrowse = new Button { Text = "...", Location = new Point(380, 44), Width = 40 };

        var lblLang = new Label { Text = "Idioma:", Location = new Point(20, 80), AutoSize = true };
        cmbLang = new ComboBox { Location = new Point(20, 105), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbLang.Items.AddRange(new object[] { "es-ES", "en-US" });

        var lblEnc = new Label { Text = "Codificación por Defecto (Nuevos):", Location = new Point(20, 140), AutoSize = true };
        cmbEncoding = new ComboBox { Location = new Point(20, 165), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbEncoding.Items.AddRange(new object[] { "utf-8", "latin1", "windows-1252" });

        var lblChunk = new Label { Text = "Tamaño Chunk por Defecto (Nuevos):", Location = new Point(240, 140), AutoSize = true };
        txtChunkSize = new TextBox { Location = new Point(240, 165), Width = 150 };

        btnSave = new Button { Text = "Guardar", Location = new Point(250, 260), DialogResult = DialogResult.OK };
        btnCancel = new Button { Text = "Cancelar", Location = new Point(340, 260), DialogResult = DialogResult.Cancel };

        btnBrowse.Click += (s, e) =>
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                    txtPath.Text = fbd.SelectedPath;
            }
        };

        btnSave.Click += (s, e) => SaveValues();

        this.Controls.Add(lblPath);
        this.Controls.Add(txtPath);
        this.Controls.Add(btnBrowse);
        this.Controls.Add(lblLang);
        this.Controls.Add(cmbLang);
        this.Controls.Add(lblEnc);
        this.Controls.Add(cmbEncoding);
        this.Controls.Add(lblChunk);
        this.Controls.Add(txtChunkSize);
        this.Controls.Add(btnSave);
        this.Controls.Add(btnCancel);
    }

    private void LoadValues()
    {
        txtPath.Text = _config.DefaultSavePath;
        cmbLang.SelectedItem = _config.Language;
        if(string.IsNullOrEmpty(cmbLang.Text)) cmbLang.SelectedIndex = 0;
        
        cmbEncoding.SelectedItem = _config.DefaultEncoding;
        if(string.IsNullOrEmpty(cmbEncoding.Text)) cmbEncoding.SelectedItem = "utf-8";
        
        txtChunkSize.Text = _config.DefaultChunkSize.ToString();
    }

    private async void SaveValues()
    {
        _config.DefaultSavePath = txtPath.Text;
        _config.Language = cmbLang.Text;
        _config.DefaultEncoding = cmbEncoding.Text;
        if (int.TryParse(txtChunkSize.Text, out int size)) _config.DefaultChunkSize = size;
        
        try 
        {
             await _configManager.SaveAsync(_config);
        }
        catch(Exception ex)
        {
             MessageBox.Show("Error al guardar preferencias: " + ex.Message);
        }
        this.Close();
    }
}
