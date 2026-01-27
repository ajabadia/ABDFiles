namespace LetterConfig.Forms;

public partial class AboutForm : Form
{
    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Acerca de";
        this.Size = new Size(400, 250);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;

        var lblName = new Label
        {
            Text = "Generador de Cartas\n(LetterConfig)",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 80
        };

        var lblVersion = new Label
        {
            Text = "Versión: 1.3.0 (WinForms Port)\nCopyright © Alejandro Abadía",
            TextAlign = ContentAlignment.TopCenter,
            Dock = DockStyle.Top,
            Height = 60
        };

        var btnOk = new Button
        {
            Text = "Aceptar",
            DialogResult = DialogResult.OK,
            Width = 100,
            Location = new Point(140, 150)
        };

        this.Controls.Add(btnOk); // Add button first so it sits on top if z-order matters or just layout
        this.Controls.Add(lblVersion);
        this.Controls.Add(lblName);
    }
}
