using ABDTools.Core.Gaweb.Models;

namespace LetterConfig.Forms;

public partial class MappingForm : Form
{
    private GawebPreset _preset;
    
    private DataGridView grid;
    private ComboBox cmbGaweb;
    private ComboBox cmbExcel;
    private Button btnAdd;
    private Button btnDelete;
    private Button btnSave;

    // TODO: Ideally we would load Excel headers from a sample file.
    // For now, free text entry or placeholder list.
    public MappingForm(GawebPreset preset)
    {
        _preset = preset;
        if (_preset.Mapping == null) _preset.Mapping = new Dictionary<string, string>();
        
        InitializeComponent();
        RefreshGrid();
    }

    private void InitializeComponent()
    {
        this.Text = "Configuración de Mapeo Excel -> GAWEB";
        this.Size = new Size(600, 500);
        this.StartPosition = FormStartPosition.CenterParent;

        // Top Panel: Add Mapping
        var panelTop = new Panel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(10) };
        this.Controls.Add(panelTop);

        var lblG = new Label { Text = "Campo GAWEB:", Location = new Point(10, 10), AutoSize = true };
        cmbGaweb = new ComboBox { Location = new Point(10, 30), Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
        PopulateGawebFields();

        var lblE = new Label { Text = "Columna Excel (Cabecera):", Location = new Point(230, 10), AutoSize = true };
        cmbExcel = new ComboBox { Location = new Point(230, 30), Width = 200 }; // DropDown -> allows typing custom config
        // Mock Headers
        cmbExcel.Items.AddRange(new object[] { "NOMBRE", "DIRECCION", "CP", "POBLACION", "PROVINCIA", "NIF", "CCC", "FECHA" });

        btnAdd = new Button { Text = "Añadir", Location = new Point(450, 29), Width = 80 };
        btnAdd.Click += BtnAdd_Click;

        panelTop.Controls.AddRange(new Control[] { lblG, cmbGaweb, lblE, cmbExcel, btnAdd });

        // Bottom: Buttons
        var panelBot = new Panel { Dock = DockStyle.Bottom, Height = 50 };
        btnSave = new Button { Text = "Cerrar", DialogResult = DialogResult.OK, Location = new Point(500, 10) };
        panelBot.Controls.Add(btnSave);
        this.Controls.Add(panelBot);

        // Grid
        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false
        };
        grid.Columns.Add("Gaweb", "Campo GAWEB");
        grid.Columns.Add("Excel", "Columna Excel");
        
        // Remove context menu logic via button for simplicity
        btnDelete = new Button { Text = "Borrar Selec.", Location = new Point(10, 60), Width = 100 };
        btnDelete.Click += BtnDelete_Click;
        panelTop.Controls.Add(btnDelete);

        this.Controls.Add(grid);
        this.Controls.SetChildIndex(panelTop, 0);
    }

    private void PopulateGawebFields()
    {
        // Based on GawebRecord fields
        string[] fields = {
            "Formato", "Lote", "Secuencial", "Pagina",
            "CodDocumento", "Version", "ClaseContrato", "CodContrato",
            "TIREL", "NUREL", "CLALF", "INDOM", "ForzarEnvio", "Idioma",
            "OpAhorroCode", "OpAhorroCuenta", "OpAhorroImporte",
            "FechaCarta", "IndDestino", "ViaReparto", "CopiaPapel",
            "Oficina", "MailFax", "NombrePDF"
        };
        Array.Sort(fields);
        cmbGaweb.Items.AddRange(fields);
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        string gaweb = cmbGaweb.SelectedItem?.ToString() ?? "";
        string excel = cmbExcel.Text.Trim();

        if (string.IsNullOrEmpty(gaweb) || string.IsNullOrEmpty(excel))
        {
            MessageBox.Show("Seleccione campo y columna.");
            return;
        }

        _preset.Mapping[gaweb] = excel;
        RefreshGrid();
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (grid.SelectedRows.Count > 0)
        {
            string key = grid.SelectedRows[0].Cells[0].Value.ToString() ?? "";
            if (_preset.Mapping.ContainsKey(key))
            {
                _preset.Mapping.Remove(key);
                RefreshGrid();
            }
        }
    }

    private void RefreshGrid()
    {
        grid.Rows.Clear();
        foreach(var kvp in _preset.Mapping)
        {
            grid.Rows.Add(kvp.Key, kvp.Value);
        }
    }
}
