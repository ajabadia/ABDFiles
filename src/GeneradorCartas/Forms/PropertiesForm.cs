using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace GeneradorCartas.Forms;

public class PropertiesForm : Form
{
    public string OutputPath { get; private set; }
    public bool GawebMode { get; private set; }

    private TextBox txtOutputPath;
    private CheckBox chkGawebMode;

    public PropertiesForm(string currentPath, bool currentMode)
    {
        this.Text = "Propiedades";
        this.Size = new Size(500, 250);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        OutputPath = currentPath;
        GawebMode = currentMode;

        InitializeControls();
    }

    private void InitializeControls()
    {
        int y = 20;

        // Path
        var lblPath = new Label { Text = "Directorio de Salida por Defecto:", Location = new Point(20, y), AutoSize = true };
        y += 25;
        txtOutputPath = new TextBox { Text = OutputPath, Location = new Point(20, y), Width = 350 };
        var btnBrowse = new Button { Text = "...", Location = new Point(380, y-1), Width = 40 };
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
            Checked = GawebMode
        };
        this.Controls.Add(chkGawebMode);

        y += 60;

        // Buttons
        var btnOk = new Button { Text = "Aceptar", Location = new Point(280, y), Width = 80, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "Cancelar", Location = new Point(370, y), Width = 80, DialogResult = DialogResult.Cancel };
        
        btnOk.Click += (s, e) => 
        {
            OutputPath = txtOutputPath.Text;
            GawebMode = chkGawebMode.Checked;
        };

        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }
}
