namespace EtlConfig.Forms;

public class AboutForm : Form
{
    public AboutForm()
    {
        this.Text = "Acerca de EtlConfig";
        this.Size = new Size(400, 250);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        var lblTitle = new Label 
        { 
             Text = "Procesador ETL - Configurator", 
             Font = new Font("Segoe UI", 12, FontStyle.Bold),
             Location = new Point(20, 20),
             AutoSize = true
        };

        var lblVer = new Label 
        { 
             Text = "Versión: 1.3.0", 
             Location = new Point(20, 60),
             AutoSize = true
        };

        var lblCopy = new Label 
        { 
             Text = "© 2025 Alejandro Abadía", 
             Location = new Point(20, 90),
             AutoSize = true
        };

        var lblDesc = new Label 
        { 
             Text = "Herramienta de configuración para\nprocesamiento de archivos planos.", 
             Location = new Point(20, 130),
             AutoSize = true
        };

        var btnOk = new Button 
        { 
             Text = "Aceptar", 
             Location = new Point(150, 180),
             DialogResult = DialogResult.OK 
        };

        this.Controls.Add(lblTitle);
        this.Controls.Add(lblVer);
        this.Controls.Add(lblCopy);
        this.Controls.Add(lblDesc);
        this.Controls.Add(btnOk);
    }
}
