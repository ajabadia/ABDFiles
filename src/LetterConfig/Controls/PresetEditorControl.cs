using ABDTools.Core.Gaweb.Models;
using LetterConfig.Services;

namespace LetterConfig.Controls;

public partial class PresetEditorControl : UserControl
{
    private GawebPreset? _currentPreset;
    public event EventHandler? PresetSaved;
    public event EventHandler? PresetCancelled;

    // UI Controls
    private TextBox txtName;
    private TextBox txtDesc;
    private CheckBox chkActive;

    private ComboBox cmbTipoSoporte;
    private ComboBox cmbFormato;
    private ComboBox cmbMetodo;
    private ComboBox cmbIndDestino;
    private ComboBox cmbTipoDestino;

    private TextBox txtFechaGen;
    private TextBox txtFechaCarta;
    private TextBox txtCodEntorno;

    private TextBox txtCodDoc;
    private TextBox txtOficina;
    private TextBox txtPaginas;

    // Optional Fields
    private ComboBox cmbIdioma;
    private ComboBox cmbViaReparto;
    private ComboBox cmbCopiaPapel;

    private Button btnSave;
    private Button btnCancel;
    private Button btnMapping;
    
    private ErrorProvider errorProvider;
    private ToolTip toolTip;

    public PresetEditorControl()
    {
        InitializeComponent();
    }



    private void InitializeComponent()
    {
        this.Padding = new Padding(20);
        this.AutoScroll = true;
        
        errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };
        toolTip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 500, ReshowDelay = 500, ShowAlways = true };

        int y = 10;
        int lblW = 140;
        int txtW = 250;
        int gap = 30;

        // --- Identification ---
        AddHeader("Identificación", ref y);
        AddTextRow("Nombre:", out txtName, ref y, lblW, txtW, maxLength: 50, required: true);
        AddTextRow("Descripción:", out txtDesc, ref y, lblW, txtW, maxLength: 100);
        
        chkActive = new CheckBox { Text = "Activo (Visible)", Location = new Point(lblW + 25, y), Width = 200, Checked = true };
        this.Controls.Add(chkActive);
        y += gap;

        // --- Technical Config ---
        AddHeader("Configuración Técnica", ref y);
        AddComboRow("Tipo Soporte:", out cmbTipoSoporte, ReferenceData.Soportes, ref y, lblW, txtW);
        
        // Filter Formatos based on Soporte change
        cmbTipoSoporte.SelectedIndexChanged += (s, e) => FilterFormatos();

        AddComboRow("Tamaño/Formato:", out cmbFormato, ReferenceData.Formatos, ref y, lblW, txtW + 80, 
            help: "Código de 2 dígitos:\n01-03: Overlay (requiere plantilla)\n04: PDF A4 ventana izq.\n05: PDF A4 ventana der.\nOBLIGATORIO");
        AddComboRow("Método Envío:", out cmbMetodo, ReferenceData.MetodosEnvio, ref y, lblW, txtW + 80);

        // --- Segmentación ---
        AddHeader("Segmentación y Fechas", ref y);
        AddComboRow("Tipo Destinatario:", out cmbTipoDestino, ReferenceData.Destinos, ref y, lblW, txtW);
        AddComboRow("Indicador Destino:", out cmbIndDestino, ReferenceData.IndicadoresDestino, ref y, lblW, txtW);
        
        string helpFechas = "Formato: AAAAMMDD (8 dígitos).\nEjemplo: 20260115.\nFecha Generación: Creación del fichero.\nFecha Carta: Fecha del documento.\nOBLIGATORIAS";
        AddTextRow("Fecha Generación:", out txtFechaGen, ref y, lblW, 100, maxLength: 8, required: true, help: helpFechas, isDate: true);
        AddTextRow("Fecha Carta:", out txtFechaCarta, ref y, lblW, 100, maxLength: 8, required: true, help: helpFechas, isDate: true);
        
        // --- HOST ---
        AddHeader("Configuración HOST", ref y);
        AddTextRow("Código Entorno:", out txtCodEntorno, ref y, lblW, txtW, maxLength: 8, required: true, 
            help: "Identificador del proceso/entorno (Ej: ABDFN01).\nUso: Para formar nombre del paquete.\nNO va dentro del registro GAWEB.\nOBLIGATORIO");

        // --- Optional Fields ---
        AddHeader("Campos Opcionales GAWEB", ref y);
        AddComboRow("Idioma (ISO):", out cmbIdioma, ReferenceData.Idiomas, ref y, lblW, txtW, help: "Código ISO 639-1 (Pos 86-87).\nEjemplo: ES, EN.\nOpcional.");
        AddComboRow("Vía Reparto:", out cmbViaReparto, ReferenceData.ViasReparto, ref y, lblW, txtW, help: "Código reparto especial (Ej: 01).\nDejar vacío si no aplica.");
        AddComboRow("Copia Papel:", out cmbCopiaPapel, ReferenceData.CopiasPapel, ref y, lblW, txtW, help: "S/N: Copia física.\nX: Bloqueo de impresión.");

        // --- Defaults ---
        AddHeader("Valores por Defecto", ref y);
        AddTextRow("Cod Documento:", out txtCodDoc, ref y, lblW, 100, maxLength: 6, required: true, 
            help: "Identificador de plantilla (Ej: X00054).\n6 caracteres exactos.\nOBLIGATORIO");
        AddTextRow("Cod Oficina:", out txtOficina, ref y, lblW, 100, maxLength: 5, required: true, isNumeric: true,
            help: "Código oficina responsable.\n5 dígitos (Ej: 00152).");
        AddTextRow("Nº Páginas:", out txtPaginas, ref y, lblW, 100, maxLength: 4, required: true, isNumeric: true,
            help: "Páginas del PDF.\n4 dígitos (Ej: 0004).");

        // --- Actions ---
        y += 20;
        // btnMapping removed as requested
        // btnMapping = new Button { Text = "Configurar Mapeo Excel...", Location = new Point(20, y), Width = 200, Height = 30 };
        // btnMapping.Click += (s, e) => OpenMappingDialog();
        // this.Controls.Add(btnMapping);
        y += 50;
    }

    public void LoadPreset(GawebPreset preset)
    {
        _currentPreset = preset;
        if (preset == null) return;

        txtName.Text = preset.Name;
        txtDesc.Text = preset.Description;
        chkActive.Checked = preset.Active;

        SelectByVal(cmbTipoSoporte, preset.TipoSoporte);
        FilterFormatos(); // Update formats list first
        SelectByVal(cmbFormato, preset.FormatoCarta);
        
        SelectByVal(cmbMetodo, preset.ForzarMetodo);
        SelectByVal(cmbIndDestino, preset.IndicadorDestino);
        SelectByVal(cmbTipoDestino, preset.TipoDestino);

        txtFechaGen.Text = preset.FechaGeneracion;
        txtFechaCarta.Text = preset.FechaCarta;
        txtCodEntorno.Text = preset.CodigoEntorno;

        SelectByVal(cmbIdioma, preset.Idioma);
        SelectByVal(cmbViaReparto, preset.ViaReparto);
        SelectByVal(cmbCopiaPapel, preset.CopiaPapel);

        txtCodDoc.Text = preset.CodigoDocumento;
        txtOficina.Text = preset.Oficina;
        txtPaginas.Text = preset.PaginasDefecto.ToString("0000");
    }

    public bool ApplyChanges()
    {
        if (_currentPreset == null) return false;

        // Trigger validation on all controls
        if (!this.ValidateChildren(ValidationConstraints.Enabled))
        {
            MessageBox.Show("Por favor, corrija los errores marcados antes de guardar.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        // Apply
        _currentPreset.Name = txtName.Text;
        _currentPreset.Description = txtDesc.Text;
        _currentPreset.Active = chkActive.Checked;

        _currentPreset.TipoSoporte = GetVal(cmbTipoSoporte);
        _currentPreset.FormatoCarta = GetVal(cmbFormato);
        _currentPreset.ForzarMetodo = GetVal(cmbMetodo);
        _currentPreset.IndicadorDestino = GetVal(cmbIndDestino);
        _currentPreset.TipoDestino = GetVal(cmbTipoDestino);

        _currentPreset.FechaGeneracion = txtFechaGen.Text;
        _currentPreset.FechaCarta = txtFechaCarta.Text;
        _currentPreset.CodigoEntorno = txtCodEntorno.Text;

        _currentPreset.Idioma = GetVal(cmbIdioma);
        _currentPreset.ViaReparto = GetVal(cmbViaReparto);
        _currentPreset.CopiaPapel = GetVal(cmbCopiaPapel);

        _currentPreset.CodigoDocumento = txtCodDoc.Text;
        _currentPreset.Oficina = txtOficina.Text;
        
        if (int.TryParse(txtPaginas.Text, out int pag)) _currentPreset.PaginasDefecto = pag;

        return true;
    }

    // Deprecated internal save
    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (ApplyChanges())
             PresetSaved?.Invoke(this, EventArgs.Empty);
    }

    private void OpenMappingDialog()
    {
         if (_currentPreset == null) return;
         
         using (var dlg = new Forms.MappingForm(_currentPreset))
         {
             dlg.ShowDialog(this);
         }
    }

    // --- Helpers ---

    private void AddHeader(string text, ref int y)
    {
        var lbl = new Label { Text = text, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(20, y), AutoSize = true };
        this.Controls.Add(lbl);
        y += 30;
    }

    private void AddTextRow(string label, out TextBox txt, ref int y, int lblW, int txtW, int maxLength = 0, bool required = false, string help = "", bool isDate = false, bool isNumeric = false)
    {
        var lbl = new Label { Text = label, Location = new Point(20, y + 3), Width = lblW, AutoSize = false };
        txt = new TextBox { Location = new Point(20 + lblW, y), Width = txtW };
        
        if (maxLength > 0) txt.MaxLength = maxLength;
        
        // Add help icon if provided
        AddHelpButton(help, 20 + lblW + txtW + 5, y);

        // Validation Logic
        txt.Validating += (s, e) =>
        {
            var t = (TextBox)s;
            string val = t.Text.Trim();
            if (required && string.IsNullOrEmpty(val))
            {
                e.Cancel = true;
                errorProvider.SetError(t, "Campo obligatorio");
                return;
            }
            if (maxLength > 0 && required && val.Length != maxLength && (isDate || isNumeric || t == txtCodDoc)) // Strict length checks for codes/dates
            {
                // Only strict for fixed width fields, names can be shorter
                e.Cancel = true;
                errorProvider.SetError(t, $"Debe tener exactamente {maxLength} caracteres.");
                return;
            }
            if (isDate)
            {
                if (!System.DateTime.TryParseExact(val, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
                {
                    e.Cancel = true;
                    errorProvider.SetError(t, "Formato fecha inválido (YYYYMMDD).");
                    return;
                }
            }
            if (isNumeric)
            {
                if (!int.TryParse(val, out _))
                {
                     e.Cancel = true;
                     errorProvider.SetError(t, "Debe ser numérico.");
                     return;
                }
            }
            errorProvider.SetError(t, "");
        };

        this.Controls.Add(lbl);
        this.Controls.Add(txt);
        y += 30;
    }

    private void AddComboRow(string label, out ComboBox cmb, List<ReferenceItem> items, ref int y, int lblW, int txtW, string help = "")
    {
        var lbl = new Label { Text = label, Location = new Point(20, y + 3), Width = lblW, AutoSize = false };
        cmb = new ComboBox { Location = new Point(20 + lblW, y), Width = txtW, DropDownStyle = ComboBoxStyle.DropDownList };
        
        cmb.DisplayMember = "Label";
        cmb.ValueMember = "GlobalId";
        // Clone list to avoid binding issues if customized
        foreach(var item in items) cmb.Items.Add(item);

        AddHelpButton(help, 20 + lblW + txtW + 5, y);

        this.Controls.Add(lbl);
        this.Controls.Add(cmb);
        y += 30;
    }

    private void AddHelpButton(string helpText, int x, int y)
    {
        if (string.IsNullOrEmpty(helpText)) return;
        
        var btn = new Button
        {
            Text = "?",
            Size = new Size(25, 23),
            Location = new Point(x, y),
            FlatStyle = FlatStyle.Popup,
            BackColor = Color.LightYellow,
            Font = new Font("Segoe UI", 8, FontStyle.Bold)
        };
        toolTip.SetToolTip(btn, helpText);
        btn.Click += (s, e) => MessageBox.Show(helpText, "Ayuda", MessageBoxButtons.OK, MessageBoxIcon.Information);
        this.Controls.Add(btn);
    }

    private void SelectByVal(ComboBox cmb, string? val)
    {
        if (val == null) val = "";
        foreach (ReferenceItem item in cmb.Items)
        {
            if (item.GlobalId == val)
            {
                cmb.SelectedItem = item;
                return;
            }
        }
    }

    private string GetVal(ComboBox cmb)
    {
        if (cmb.SelectedItem is ReferenceItem item) return item.GlobalId;
        return "";
    }

    private void FilterFormatos()
    {
        string soporte = GetVal(cmbTipoSoporte);
        string filterKey = (soporte == "PDF") ? "PDF" : "OV";

        cmbFormato.Items.Clear();
        foreach (var item in ReferenceData.Formatos)
        {
            if (item.Extra == filterKey) cmbFormato.Items.Add(item);
        }
        if (cmbFormato.Items.Count > 0) cmbFormato.SelectedIndex = 0;
    }
}
