using LetterConfig.Models;
using LetterConfig.Services;
using ABDTools.Core.Configuration;

namespace LetterConfig.Forms;

public partial class PreferencesForm : Form
{
    private readonly AppConfig _config;
    private readonly ConfigManager<AppConfig> _configManager;
    private TextBox txtPath;
    private ComboBox cmbLang;
    
    public PreferencesForm(AppConfig config, ConfigManager<AppConfig> manager)
    {
        _config = config;
        _configManager = manager;
        InitializeComponent();
        LoadConfig();
    }

    private void InitializeComponent()
    {
        this.Text = "Preferencias";
        this.Size = new Size(500, 250);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        int y = 20;

        // Path
        var lblPath = new Label { Text = "Ruta por defecto (Guardar):", Location = new Point(20, y), AutoSize = true };
        this.Controls.Add(lblPath);
        y += 25;

        txtPath = new TextBox { Location = new Point(20, y), Width = 350, ReadOnly = true };
        var btnBrowse = new Button { Text = "...", Location = new Point(380, y-1), Width = 40 };
        btnBrowse.Click += BtnBrowse_Click;
        
        this.Controls.Add(txtPath);
        this.Controls.Add(btnBrowse);
        y += 50;

        // Language
        var lblLang = new Label { Text = "Idioma por defecto (JSON):", Location = new Point(20, y), AutoSize = true };
        this.Controls.Add(lblLang);
        y += 25;

        cmbLang = new ComboBox { Location = new Point(20, y), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbLang.Items.AddRange(ReferenceData.Idiomas.ToArray());
        cmbLang.DisplayMember = "Label";
        cmbLang.ValueMember = "GlobalId";
        this.Controls.Add(cmbLang);
        y += 50;
        
        // Buttons
        var btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(380, 170) };
        var btnSave = new Button { Text = "Guardar", DialogResult = DialogResult.OK, Location = new Point(290, 170) };
        btnSave.Click += BtnSave_Click;

        this.Controls.Add(btnSave);
        this.Controls.Add(btnCancel);
    }

    private void LoadConfig()
    {
        txtPath.Text = _config.DefaultSavePath;
        
        foreach (ReferenceItem item in cmbLang.Items)
        {
            if (item.GlobalId == _config.DefaultLanguage)
            {
                cmbLang.SelectedItem = item;
                break;
            }
        }
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using (var dlg = new FolderBrowserDialog())
        {
            if (Directory.Exists(txtPath.Text)) dlg.SelectedPath = txtPath.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = dlg.SelectedPath;
            }
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        _config.DefaultSavePath = txtPath.Text;
        if (cmbLang.SelectedItem is ReferenceItem item)
        {
            _config.DefaultLanguage = item.GlobalId;
        }
        
        try 
        {
             _ = _configManager.SaveAsync(_config); // async fire-and-forget or sync wait? 
             // Best effort for dialog.
        }
        catch (Exception ex)
        {
             MessageBox.Show("Error: "+ex.Message);
        }
        
        this.Close();
    }
}
