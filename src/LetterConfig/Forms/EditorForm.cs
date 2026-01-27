using ABDTools.Core.Gaweb.Models;

namespace LetterConfig.Forms;

public partial class EditorForm : Form
{
    public GawebPreset Preset { get; private set; }

    private TextBox txtName;
    private TextBox txtDesc;
    private CheckBox chkActive;

    private ComboBox cmbTipoSoporte;
    private ComboBox cmbFormato;
    private TextBox txtCodEntorno;
    private TextBox txtCodDoc;
    private TextBox txtOficina;
    private TextBox txtPaginas;
    
    // New Fields
    private ComboBox cmbForzar;
    private ComboBox cmbIndDestino;
    
    // Dates
    private TextBox txtFechaGen;
    private TextBox txtFechaCarta;
    // Idioma removed/hidden as per spec "Vacío"
    private TextBox txtIdioma;

    private Button btnSave;
    private Button btnCancel;

    public EditorForm(GawebPreset? existing)
    {
        // Clone if existing to avoid modifying original ref until saved
        if (existing != null)
        {
            // Simple manual clone for now or JSON implementation
            var json = System.Text.Json.JsonSerializer.Serialize(existing);
            Preset = System.Text.Json.JsonSerializer.Deserialize<GawebPreset>(json)!;
        }
        else
        {
            Preset = new GawebPreset { Active = true };
        }

        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = "Preset Editor";
        this.Size = new Size(500, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        int x = 20, y = 20;
        int lblW = 120, txtW = 300;
        int gap = 30;

        // Name
        Label lblName = new Label { Text = "Name:", Location = new Point(x, y + 3), Width = lblW };
        txtName = new TextBox { Location = new Point(x + lblW, y), Width = txtW };
        this.Controls.Add(lblName); this.Controls.Add(txtName);
        y += gap;

        // Desc
        Label lblDesc = new Label { Text = "Description:", Location = new Point(x, y + 3), Width = lblW };
        txtDesc = new TextBox { Location = new Point(x + lblW, y), Width = txtW };
        this.Controls.Add(lblDesc); this.Controls.Add(txtDesc);
        y += gap;

        // Active
        chkActive = new CheckBox { Text = "Active", Location = new Point(x + lblW, y), Width = txtW };
        this.Controls.Add(chkActive);
        y += gap + 10;

        // --- Tech Params ---
        Label lblSep1 = new Label { Text = "Technical Parameters", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(x, y), Width = 400 };
        this.Controls.Add(lblSep1);
        y += gap;

        // Tipo Soporte
        Label lblSoporte = new Label { Text = "Tipo Soporte:", Location = new Point(x, y + 3), Width = lblW };
        cmbTipoSoporte = new ComboBox { Location = new Point(x + lblW, y), Width = txtW, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbTipoSoporte.Items.Add("OV"); // Overlay
        cmbTipoSoporte.Items.Add("PDF");
        this.Controls.Add(lblSoporte); this.Controls.Add(cmbTipoSoporte);
        y += gap;

        // Formato
        Label lblFormat = new Label { Text = "Formato:", Location = new Point(x, y + 3), Width = lblW };
        cmbFormato = new ComboBox { Location = new Point(x + lblW, y), Width = txtW, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbFormato.Items.AddRange(new object[] { "01", "02", "03", "04", "05" });
        this.Controls.Add(lblFormat); this.Controls.Add(cmbFormato);
        y += gap;

        // Forzar Envio
        Label lblForzar = new Label { Text = "Forzar Envio:", Location = new Point(x, y + 3), Width = lblW };
        cmbForzar = new ComboBox { Location = new Point(x + lblW, y), Width = txtW, DropDownStyle = ComboBoxStyle.DropDownList };
        // Empty = Cliente, 1=Papel, 3=Fax, 4=Email, 5=Buzon, 8=NoEnviar
        cmbForzar.Items.Add(new ComboBoxItem(" ", " (Cliente)")); 
        cmbForzar.Items.Add(new ComboBoxItem("1", "1 (Papel)"));
        cmbForzar.Items.Add(new ComboBoxItem("3", "3 (FAX)"));
        cmbForzar.Items.Add(new ComboBoxItem("4", "4 (Email)"));
        cmbForzar.Items.Add(new ComboBoxItem("5", "5 (Buzón)"));
        cmbForzar.Items.Add(new ComboBoxItem("8", "8 (No Enviar)"));
        cmbForzar.DisplayMember = "Display";
        cmbForzar.ValueMember = "Value";
        this.Controls.Add(lblForzar); this.Controls.Add(cmbForzar);
        y += gap;

        // Ind Destino
        Label lblInd = new Label { Text = "Ind. Destino:", Location = new Point(x, y + 3), Width = lblW };
        cmbIndDestino = new ComboBox { Location = new Point(x + lblW, y), Width = txtW, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbIndDestino.Items.Add("0"); // Clientes
        cmbIndDestino.Items.Add("O"); // Oficinas
        cmbIndDestino.Items.Add("7"); // Central
        this.Controls.Add(lblInd); this.Controls.Add(cmbIndDestino);
        y += gap;

        // Cod Entorno
        Label lblEnt = new Label { Text = "Cod. Entorno (HOST):", Location = new Point(x, y + 3), Width = lblW };
        txtCodEntorno = new TextBox { Location = new Point(x + lblW, y), Width = txtW };
        this.Controls.Add(lblEnt); this.Controls.Add(txtCodEntorno);
        y += gap;

        // Cod Doc
        Label lblDoc = new Label { Text = "Cod. Documento (6):", Location = new Point(x, y + 3), Width = lblW };
        txtCodDoc = new TextBox { Location = new Point(x + lblW, y), Width = txtW, MaxLength = 6 };
        this.Controls.Add(lblDoc); this.Controls.Add(txtCodDoc);
        y += gap;

        // --- Defaults ---
        Label lblSep2 = new Label { Text = "Defaults", Font = new Font(this.Font, FontStyle.Bold), Location = new Point(x, y), Width = 400 };
        this.Controls.Add(lblSep2);
        y += gap;

        Label lblGen = new Label { Text = "Fecha Gen (Opt):", Location = new Point(x, y + 3), Width = lblW };
        txtFechaGen = new TextBox { Location = new Point(x + lblW, y), Width = txtW };
        this.Controls.Add(lblGen); this.Controls.Add(txtFechaGen);
        y += gap;
        
        Label lblCar = new Label { Text = "Fecha Carta (Opt):", Location = new Point(x, y + 3), Width = lblW };
        txtFechaCarta = new TextBox { Location = new Point(x + lblW, y), Width = txtW };
        this.Controls.Add(lblCar); this.Controls.Add(txtFechaCarta);
        y += gap;
        
        Label lblOfi = new Label { Text = "Oficina:", Location = new Point(x, y+3), Width = lblW };
        txtOficina = new TextBox { Location = new Point(x + lblW, y), Width = 100 };
        this.Controls.Add(lblOfi); this.Controls.Add(txtOficina);
        y += gap;

        Label lblPag = new Label { Text = "Paginas:", Location = new Point(x, y+3), Width = lblW };
        txtPaginas = new TextBox { Location = new Point(x + lblW, y), Width = 100 };
        this.Controls.Add(lblPag); this.Controls.Add(txtPaginas);
        y += gap;

        Label lblLang = new Label { Text = "Idioma (Vacío):", Location = new Point(x, y+3), Width = lblW };
        txtIdioma = new TextBox { Location = new Point(x + lblW, y), Width = 100, Enabled = false, BackColor=Color.WhiteSmoke }; // Spec: Vacio
        this.Controls.Add(lblLang); this.Controls.Add(txtIdioma);

        // Buttons
        btnSave = new Button { Text = "Save", DialogResult = DialogResult.OK, Location = new Point(230, 520), Width = 80 };
        btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(320, 520), Width = 80 };
        
        btnSave.Click += BtnSave_Click;

        this.Controls.Add(btnSave);
        this.Controls.Add(btnCancel);
        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    private void LoadData()
    {
        txtName.Text = Preset.Name;
        txtDesc.Text = Preset.Description;
        chkActive.Checked = Preset.Active;
        
        if (Preset.TipoSoporte == "PDF") cmbTipoSoporte.SelectedIndex = 1;
        else cmbTipoSoporte.SelectedIndex = 0; // Default OV
        
        cmbFormato.SelectedItem = Preset.FormatoCarta;
        
        // Select Forzar
        foreach(ComboBoxItem item in cmbForzar.Items) { if (item.Value == Preset.ForzarMetodo) cmbForzar.SelectedItem = item; }
        if (cmbForzar.SelectedIndex == -1 && cmbForzar.Items.Count > 0) cmbForzar.SelectedIndex = 0;

        // Select Ind
        cmbIndDestino.SelectedItem = Preset.IndicadorDestino;
        if (cmbIndDestino.SelectedIndex == -1 && cmbIndDestino.Items.Count > 0) cmbIndDestino.SelectedIndex = 0;
        txtCodEntorno.Text = Preset.CodigoEntorno;
        txtCodDoc.Text = Preset.CodigoDocumento;
        
        txtFechaGen.Text = Preset.FechaGeneracion;
        txtFechaCarta.Text = Preset.FechaCarta;
        txtOficina.Text = Preset.Oficina;
        txtPaginas.Text = Preset.PaginasDefecto.ToString();
        txtIdioma.Text = Preset.Idioma;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        // Simple Validation
        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None; // Prevent closing
            return;
        }

        // Apply changes
        Preset.Name = txtName.Text;
        Preset.Description = txtDesc.Text;
        Preset.Active = chkActive.Checked;
        Preset.TipoSoporte = cmbTipoSoporte.SelectedItem?.ToString() ?? "OV";
        Preset.ForzarMetodo = (cmbForzar.SelectedItem as ComboBoxItem)?.Value ?? " ";
        Preset.IndicadorDestino = cmbIndDestino.SelectedItem?.ToString() ?? "0";
        Preset.FormatoCarta = cmbFormato.SelectedItem?.ToString() ?? "01";
        Preset.CodigoEntorno = txtCodEntorno.Text;
        Preset.CodigoDocumento = txtCodDoc.Text;
        Preset.FechaGeneracion = txtFechaGen.Text;
        Preset.FechaCarta = txtFechaCarta.Text;
        Preset.Oficina = txtOficina.Text;
        
        if (int.TryParse(txtPaginas.Text, out int pag)) Preset.PaginasDefecto = pag;
        Preset.Idioma = txtIdioma.Text;
    }
    private class ComboBoxItem {
        public string Value { get; set; }
        public string Display { get; set; }
        public ComboBoxItem(string v, string d) { Value = v; Display = v + d; }
        public override string ToString() { return Display; }
    }
}
