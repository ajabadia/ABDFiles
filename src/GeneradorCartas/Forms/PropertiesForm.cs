using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace GeneradorCartas.Forms;

public class PropertiesForm : Form
{
    public string OutputPath { get; private set; }
    public bool GawebMode { get; private set; }
    public string SyncfusionKey { get; private set; }

    private TextBox txtOutputPath;
    private CheckBox chkGawebMode;
    private TextBox txtLicenseKey;

    public PropertiesForm(string currentPath, bool currentMode, string currentKey)
    {
        this.Text = "Propiedades";
        this.Size = new Size(500, 350);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        OutputPath = currentPath;
        GawebMode = currentMode;
        SyncfusionKey = currentKey; 

        InitializeControls();
    }

    private void InitializeControls()
    {
        this.Font = new Font("Segoe UI", 9);
        int y = 20;

        // Path
        var lblPath = new Label { Text = "Directorio de Salida por Defecto:", Location = new Point(20, y), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
        y += 25;
        txtOutputPath = new TextBox { Text = OutputPath, Location = new Point(20, y), Width = 350 };
        var btnBrowse = new Button { Text = "...", Location = new Point(380, y - 1), Width = 40 };
        btnBrowse.Click += (s, e) => 
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.SelectedPath = txtOutputPath.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtOutputPath.Text = fbd.SelectedPath;
                }
            }
        };

        this.Controls.Add(lblPath);
        this.Controls.Add(txtOutputPath);
        this.Controls.Add(btnBrowse);

        y += 50;

        // Mode
        chkGawebMode = new CheckBox 
        { 
            Text = "Modo GAWEB por defecto (Generar ZIP e Índices)", 
            Location = new Point(20, y), 
            AutoSize = true,
            Checked = GawebMode,
            Font = new Font(this.Font, FontStyle.Bold)
        };
        this.Controls.Add(chkGawebMode);

        y += 40;

        // License Key
        var lblKey = new Label { Text = "Syncfusion License Key (Gratuito - Comunidad):", Location = new Point(20, y), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
        y += 25;
        txtLicenseKey = new TextBox { Text = SyncfusionKey, Location = new Point(20, y), Width = 440 };
        this.Controls.Add(lblKey);
        this.Controls.Add(txtLicenseKey);

        y += 65;

        // Buttons
        var btnOk = new Button { 
            Text = "ACEPTAR", 
            Location = new Point(250, y), 
            Width = 100, 
            Height = 35, 
            BackColor = Color.LightSkyBlue,
            Font = new Font(this.Font, FontStyle.Bold),
            DialogResult = DialogResult.OK 
        };
        var btnCancel = new Button { 
            Text = "CANCELAR", 
            Location = new Point(360, y), 
            Width = 100, 
            Height = 35, 
            DialogResult = DialogResult.Cancel 
        };
        
        btnOk.Click += (s, e) => 
        {
            OutputPath = txtOutputPath.Text;
            GawebMode = chkGawebMode.Checked;
            SyncfusionKey = txtLicenseKey.Text;
        };

        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }
}
