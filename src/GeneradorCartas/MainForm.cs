using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GeneradorCartas.Forms;
using GeneradorCartas.Models;
using GeneradorCartas.Services;
using ABDTools.Core.Gaweb.Models;

namespace GeneradorCartas;

public class MainForm : Form
{
    // Services
    private readonly ConfigService _configService;

    // State
    private GenerationConfig _config;
    private string? _configFilePath = null;
    private bool _isDirty = false;
    private CancellationTokenSource? _cts;

    // Menu Items (for enabling/disabling)
    private ToolStripMenuItem _itemSave;
    private ToolStripMenuItem _itemSaveAs;

    // Controls - Files
    private ComboBox cmbPreset;
    private TextBox txtDataFile;
    private TextBox txtTemplateFile;
    private Label lblDataStatus;
    private Label lblTemplateStatus;
    private Label lblPresetName;

    // Controls - Overrides
    private TextBox txtFechaGen;
    private TextBox txtFechaCarta;
    private Label lblFechaGenWarning;
    private Label lblFechaCartaWarning;
    private TextBox txtLote;
    private TextBox txtCodDoc;
    private Label lblCodDocWarning;
    private TextBox txtOficina;
    private Label lblOficinaWarning;

    // Original preset values (for comparison)
    private string? _presetFechaGen;
    private string? _presetFechaCarta;
    private string? _presetCodDoc;
    private string? _presetOficina;
    private bool _presetActive = true;

    // Controls - Range
    private TextBox txtRangeFrom;
    private TextBox txtRangeTo;

    // Controls - Output
    private ComboBox cmbOutputType;

    private TextBox txtOutputDir;

    // Controls - Actions
    private Button btnGenerate;
    private Button btnCancel;
    private Button _btnMapping;
    private ProgressBar progressBar;
    private RichTextBox txtLog;
    private ToolStripStatusLabel lblStatus;
    private ToolTip _warningToolTip = new ToolTip();

    public MainForm()
    {
        _configService = new ConfigService();
        _config = _configService.CreateNew();

        this.Text = "Generador de Cartas v2.0";
        this.Size = new Size(950, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormClosing += MainForm_FormClosing;

        InitializeControls();
        RefreshPresetList();
        UpdateUI();
    }

    private void InitializeControls()
    {
        // === MENU STRIP ===
        MenuStrip menuStrip = new MenuStrip();

        // Archivo
        ToolStripMenuItem menuFile = new ToolStripMenuItem("Archivo");
        ToolStripMenuItem itemNew = new ToolStripMenuItem("Nuevo", null, MnuNew_Click, Keys.Control | Keys.N);
        ToolStripMenuItem itemOpen = new ToolStripMenuItem("Abrir Configuración...", null, MnuOpenConfig_Click, Keys.Control | Keys.O);
        _itemSave = new ToolStripMenuItem("Guardar", null, MnuSave_Click, Keys.Control | Keys.S);
        _itemSaveAs = new ToolStripMenuItem("Guardar como...", null, MnuSaveAs_Click);
        ToolStripMenuItem itemExport = new ToolStripMenuItem("Exportar Paquete (ZIP)...", null, MnuExportPackage_Click);
        ToolStripMenuItem itemImport = new ToolStripMenuItem("Importar Paquete (ZIP)...", null, MnuImportPackage_Click);

        ToolStripMenuItem itemExit = new ToolStripMenuItem("Salir", null, (s, e) => this.Close(), Keys.Alt | Keys.F4);
        menuFile.DropDownItems.AddRange(new ToolStripItem[] { 
            itemNew, itemOpen, 
            new ToolStripSeparator(), 
            _itemSave, _itemSaveAs, 
            new ToolStripSeparator(),
            itemExport, itemImport,
            new ToolStripSeparator(),
            itemExit 
        });

        // Edición
        ToolStripMenuItem menuEdit = new ToolStripMenuItem("Edición");
        ToolStripMenuItem itemCopy = new ToolStripMenuItem("Copiar Log", null, MnuCopy_Click, Keys.Control | Keys.C);
        ToolStripMenuItem itemProps = new ToolStripMenuItem("Propiedades...", null, MnuProperties_Click);
        menuEdit.DropDownItems.AddRange(new ToolStripItem[] { itemCopy, new ToolStripSeparator(), itemProps });

        // Ayuda
        ToolStripMenuItem menuHelp = new ToolStripMenuItem("Ayuda");
        ToolStripMenuItem itemManual = new ToolStripMenuItem("Manual", null, MnuManual_Click, Keys.F1);
        ToolStripMenuItem itemAbout = new ToolStripMenuItem("Acerca de...", null, MnuAbout_Click);
        menuHelp.DropDownItems.AddRange(new ToolStripItem[] { itemManual, new ToolStripSeparator(), itemAbout });

        menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuEdit, menuHelp });
        this.MainMenuStrip = menuStrip;
        this.Controls.Add(menuStrip);

        // === TOP PANEL (Config) ===
        Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 350, Padding = new Padding(15) };

        int y = 15;
        int lblX = 20, txtX = 150, txtW = 500, btnX = 660;

        // -- Preset --
        Label lblPreset = new Label { Text = "Preset GAWEB:", Location = new Point(lblX, y + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
        cmbPreset = new ComboBox { Location = new Point(txtX, y), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbPreset.SelectedIndexChanged += CmbPreset_SelectedIndexChanged;
        Button btnBrowsePreset = new Button { Text = "...", Location = new Point(txtX + 225, y), Width = 30 };
        btnBrowsePreset.Click += BtnBrowsePreset_Click;
        Button btnRefreshPresets = new Button { Text = "R", Location = new Point(txtX + 260, y), Width = 30 }; // R = Refresh
        var ttRefresh = new ToolTip(); ttRefresh.SetToolTip(btnRefreshPresets, "Recargar lista");
        btnRefreshPresets.Click += (s, e) => RefreshPresetList();
        Button btnPresetDetail = new Button { Text = "Detalle", Location = new Point(txtX + 295, y), Width = 75 };
        btnPresetDetail.Click += BtnPresetDetail_Click;
        lblPresetName = new Label { Text = "", Location = new Point(txtX + 380, y + 3), AutoSize = true, ForeColor = Color.DarkBlue, Font = new Font(this.Font, FontStyle.Italic) };
        pnlTop.Controls.AddRange(new Control[] { lblPreset, cmbPreset, btnBrowsePreset, btnRefreshPresets, btnPresetDetail, lblPresetName });
        y += 35;

        // -- Archivo de Datos --
        Label lblData = new Label { Text = "Archivo Datos:", Location = new Point(lblX, y + 3), AutoSize = true };
        txtDataFile = new TextBox { Location = new Point(txtX, y), Width = 450, ReadOnly = true };
        Button btnBrowseData = new Button { Text = "...", Location = new Point(605, y), Width = 40 };
        btnBrowseData.Click += BtnBrowseData_Click;
        Button btnDataDetail = new Button { Text = "Detalle", Location = new Point(650, y), Width = 75 };
        btnDataDetail.Click += BtnDataDetail_Click;
        lblDataStatus = new Label { Text = "", Location = new Point(735, y + 3), AutoSize = true, ForeColor = Color.Green }; // Will use OK/X instead of Check
        pnlTop.Controls.AddRange(new Control[] { lblData, txtDataFile, btnBrowseData, btnDataDetail, lblDataStatus });
        y += 30;

        // -- Plantilla Word --
        Label lblTemplate = new Label { Text = "Plantilla DOCX:", Location = new Point(lblX, y + 3), AutoSize = true };
        txtTemplateFile = new TextBox { Location = new Point(txtX, y), Width = txtW, ReadOnly = true };
        Button btnBrowseTemplate = new Button { Text = "...", Location = new Point(btnX, y), Width = 40 };
        btnBrowseTemplate.Click += BtnBrowseTemplate_Click;
        Button btnTemplateDetail = new Button { Text = "Detalle", Location = new Point(btnX + 45, y), Width = 75 };
        btnTemplateDetail.Click += BtnTemplateDetail_Click;
        lblTemplateStatus = new Label { Text = "", Location = new Point(btnX + 125, y + 3), AutoSize = true, ForeColor = Color.Green };
        pnlTop.Controls.AddRange(new Control[] { lblTemplate, txtTemplateFile, btnBrowseTemplate, btnTemplateDetail, lblTemplateStatus });
        y += 35;

        // -- Separador --
        Label lblSep = new Label { Text = "── Parámetros (sobrescriben preset) ──", Location = new Point(lblX, y), AutoSize = true, ForeColor = Color.Gray };
        pnlTop.Controls.Add(lblSep);
        y += 25;

        // -- Fechas con DatePicker --
        Label lblFechaGen = new Label { Text = "Fecha Generación:", Location = new Point(lblX, y + 3), AutoSize = true };
        txtFechaGen = new TextBox { Location = new Point(txtX, y), Width = 100, MaxLength = 8 };
        txtFechaGen.TextChanged += (s, e) => { _config.Overrides.FechaGeneracion = txtFechaGen.Text; MarkDirty(); UpdateDateWarnings(); };
        Button btnCalGen = new Button { Text = "...", Location = new Point(txtX + 105, y), Width = 30 }; // Calendar
        btnCalGen.Click += (s, e) => ShowDatePicker(txtFechaGen);
        Button btnTodayGen = new Button { Text = "Hoy", Location = new Point(txtX + 140, y), Width = 50 };
        btnTodayGen.Click += (s, e) => { txtFechaGen.Text = DateTime.Now.ToString("yyyyMMdd"); };
        lblFechaGenWarning = new Label { Text = "", Location = new Point(txtX + 195, y + 3), AutoSize = true, ForeColor = Color.DarkOrange };
        var ttGenWarn = new ToolTip();
        ttGenWarn.SetToolTip(lblFechaGenWarning, "Valor diferente al del preset");

        Label lblFechaCarta = new Label { Text = "Fecha Carta:", Location = new Point(360, y + 3), AutoSize = true };
        txtFechaCarta = new TextBox { Location = new Point(460, y), Width = 100, MaxLength = 8 };
        txtFechaCarta.TextChanged += (s, e) => { _config.Overrides.FechaCarta = txtFechaCarta.Text; MarkDirty(); UpdateDateWarnings(); };
        Button btnCalCarta = new Button { Text = "...", Location = new Point(565, y), Width = 30 }; // Calendar
        btnCalCarta.Click += (s, e) => ShowDatePicker(txtFechaCarta);
        Button btnTodayCarta = new Button { Text = "Hoy", Location = new Point(600, y), Width = 50 };
        btnTodayCarta.Click += (s, e) => { txtFechaCarta.Text = DateTime.Now.ToString("yyyyMMdd"); };
        lblFechaCartaWarning = new Label { Text = "", Location = new Point(655, y + 3), AutoSize = true, ForeColor = Color.DarkOrange };
        var ttCartaWarn = new ToolTip();
        ttCartaWarn.SetToolTip(lblFechaCartaWarning, "Valor diferente al del preset");

        pnlTop.Controls.AddRange(new Control[] { lblFechaGen, txtFechaGen, btnCalGen, btnTodayGen, lblFechaGenWarning, lblFechaCarta, txtFechaCarta, btnCalCarta, btnTodayCarta, lblFechaCartaWarning });
        y += 30;

        // -- Lote, CodDoc, Oficina --
        Label lblLote = new Label { Text = "Lote:", Location = new Point(lblX, y + 3), AutoSize = true };
        txtLote = new TextBox { Location = new Point(txtX, y), Width = 60, MaxLength = 4 };
        txtLote.TextChanged += (s, e) => { _config.Overrides.Lote = txtLote.Text; MarkDirty(); };

        Label lblCodDoc = new Label { Text = "Cód. Doc:", Location = new Point(220, y + 3), AutoSize = true };
        txtCodDoc = new TextBox { Location = new Point(290, y), Width = 80, MaxLength = 6 };
        txtCodDoc.TextChanged += (s, e) => { _config.Overrides.CodigoDocumento = txtCodDoc.Text; MarkDirty(); UpdateFieldWarnings(); };
        lblCodDocWarning = new Label { Text = "", Location = new Point(375, y + 3), AutoSize = true, ForeColor = Color.DarkOrange };

        Label lblOficina = new Label { Text = "Oficina:", Location = new Point(400, y + 3), AutoSize = true };
        txtOficina = new TextBox { Location = new Point(460, y), Width = 70, MaxLength = 5 };
        txtOficina.TextChanged += (s, e) => { _config.Overrides.Oficina = txtOficina.Text; MarkDirty(); UpdateFieldWarnings(); };
        lblOficinaWarning = new Label { Text = "", Location = new Point(535, y + 3), AutoSize = true, ForeColor = Color.DarkOrange };

        pnlTop.Controls.AddRange(new Control[] { lblLote, txtLote, lblCodDoc, txtCodDoc, lblCodDocWarning, lblOficina, txtOficina, lblOficinaWarning });
        y += 30;

        // -- Rango --
        Label lblRangeFrom = new Label { Text = "Desde registro:", Location = new Point(lblX, y + 3), AutoSize = true };
        txtRangeFrom = new TextBox { Location = new Point(txtX, y), Width = 60 };
        txtRangeFrom.TextChanged += (s, e) => { _config.RangeFrom = int.TryParse(txtRangeFrom.Text, out int v) ? v : null; MarkDirty(); };

        Label lblRangeTo = new Label { Text = "Hasta:", Location = new Point(230, y + 3), AutoSize = true };
        txtRangeTo = new TextBox { Location = new Point(280, y), Width = 60 };
        txtRangeTo.TextChanged += (s, e) => { _config.RangeTo = int.TryParse(txtRangeTo.Text, out int v) ? v : null; MarkDirty(); };

        // -- Tipo Salida (same line as Range) --
        Label lblOutputType = new Label { Text = "Tipo Salida:", Location = new Point(380, y + 3), AutoSize = true };
        cmbOutputType = new ComboBox { Location = new Point(460, y), Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbOutputType.Items.AddRange(new object[] { "DOCX", "PDF", "PDF + GAWEB" });
        cmbOutputType.SelectedIndex = 2; // Default to PDF+GAWEB
        cmbOutputType.SelectedIndexChanged += (s, e) => { 
            _config.OutputType = cmbOutputType.SelectedIndex switch { 0 => "DOCX", 1 => "PDF", _ => "PDF_GAWEB" }; 
            MarkDirty(); 
        };

        pnlTop.Controls.AddRange(new Control[] { lblRangeFrom, txtRangeFrom, lblRangeTo, txtRangeTo, lblOutputType, cmbOutputType });
        y += 40;

        // -- Output Directory Display --
        // -- Output Directory Display --
        Label lblOutDir = new Label { Text = "Dir. Salida:", Location = new Point(lblX, y + 3), AutoSize = true };
        txtOutputDir = new TextBox { 
            Location = new Point(txtX, y), 
            Width = 450, 
            ReadOnly = true, 
             BackColor = Color.WhiteSmoke
        };
        
        Button btnBrowseOutput = new Button { Text = "...", Location = new Point(605, y), Width = 40 };
        btnBrowseOutput.Click += BtnBrowseOutput_Click;
        
        Button btnOpenOutput = new Button { Text = "Abrir", Location = new Point(650, y), Width = 75 };
        btnOpenOutput.Click += (s, e) => {
             if (!string.IsNullOrEmpty(_config.OutputDirectory) && Directory.Exists(_config.OutputDirectory))
                 System.Diagnostics.Process.Start("explorer.exe", _config.OutputDirectory);
        };
        var ttOut = new ToolTip();
        ttOut.SetToolTip(btnOpenOutput, "Abrir carpeta de salida");

        pnlTop.Controls.AddRange(new Control[] { lblOutDir, txtOutputDir, btnBrowseOutput, btnOpenOutput });
        y += 30;

        // -- Botones de acción --
        btnGenerate = new Button
        {
            Text = "GENERAR",
            Location = new Point(txtX, y),
            Width = 150,
            Height = 40,
            BackColor = Color.LightSkyBlue,
            Font = new Font(this.Font, FontStyle.Bold)
        };
        btnGenerate.Click += BtnGenerate_Click;

        btnCancel = new Button
        {
            Text = "CANCELAR",
            Location = new Point(txtX + 290, y),
            Width = 100,
            Height = 40,
            BackColor = Color.LightCoral,
            Enabled = false
        };
        btnCancel.Click += BtnCancel_Click;

        // Mapeo button (yellow, before Generate)
        _btnMapping = new Button
        {
            Text = "📋 MAPEO...",
            Location = new Point(txtX, y),
            Width = 120,
            Height = 40,
            BackColor = Color.Gold,
            Font = new Font(this.Font, FontStyle.Bold)
        };
        _btnMapping.Click += BtnMapping_Click;

        // Generate button (initially disabled until mapping done)
        btnGenerate = new Button
        {
            Text = "GENERAR",
            Location = new Point(txtX + 130, y),
            Width = 150,
            Height = 40,
            BackColor = Color.LightSkyBlue,
            Font = new Font(this.Font, FontStyle.Bold),
            Enabled = false
        };
        btnGenerate.Click += BtnGenerate_Click;

        pnlTop.Controls.AddRange(new Control[] { _btnMapping, btnGenerate, btnCancel });
        this.Controls.Add(pnlTop);

        // === STATUS STRIP ===
        StatusStrip statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel { Text = "Listo." };
        statusStrip.Items.Add(lblStatus);
        this.Controls.Add(statusStrip);

        // === LOG AREA ===
        Panel pnlFill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 20 };
        txtLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Consolas", 9),
            BorderStyle = BorderStyle.Fixed3D
        };

        pnlFill.Controls.Add(txtLog);
        pnlFill.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
        pnlFill.Controls.Add(progressBar);
        this.Controls.Add(pnlFill);

        // Z-Order
        pnlFill.BringToFront();
        pnlTop.SendToBack();
        statusStrip.SendToBack();
        menuStrip.SendToBack();
        // Set default Lote to current HHmm if empty
        if (string.IsNullOrEmpty(txtLote.Text))
            txtLote.Text = DateTime.Now.ToString("HHmm");
    }


    // === MENU HANDLERS ===


    private void MnuNew_Click(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;
        _config = _configService.CreateNew();
        _configFilePath = null;
        _isDirty = false;
        UpdateUI();
        Log("Nueva configuración creada.");
    }

    private void MnuOpenConfig_Click(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;

        using var ofd = new OpenFileDialog
        {
            Filter = "Configuración GeneradorCartas|*.json",
            Title = "Abrir Configuración"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                _config = _configService.LoadConfig(ofd.FileName);
                _configFilePath = ofd.FileName;
                _isDirty = false;
                UpdateUI();
                Log($"Configuración cargada: {Path.GetFileName(ofd.FileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar configuración:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void MnuSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_configFilePath))
        {
            MnuSaveAs_Click(sender, e);
            return;
        }
        SaveConfig(_configFilePath);
    }

    private void MnuSaveAs_Click(object? sender, EventArgs e)
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Configuración GeneradorCartas|*.json",
            Title = "Guardar Configuración",
            FileName = "mi_configuracion.json"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            SaveConfig(sfd.FileName);
        }
    }

    private void SaveConfig(string path)
    {
        try
        {
            SyncUIToConfig();
            _configService.SaveConfig(_config, path);
            _configFilePath = path;
            _isDirty = false;
            UpdateTitle();
            Log($"Configuración guardada: {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void MnuImportData_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Datos CSV/Excel|*.csv;*.xlsx",
            Title = "Importar Datos"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            _config.DataFilePath = ofd.FileName;
            txtDataFile.Text = ofd.FileName;
            lblDataStatus.Text = "✓";
            MarkDirty();
            ValidateGenerationState();
            Log($"Datos cargados: {Path.GetFileName(ofd.FileName)}");
        }
    }

    private void MnuCopy_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(txtLog.SelectedText))
            Clipboard.SetText(txtLog.SelectedText);
        else if (!string.IsNullOrEmpty(txtLog.Text))
            Clipboard.SetText(txtLog.Text);
    }

    private void MnuProperties_Click(object? sender, EventArgs e)
    {
        using var prop = new PropertiesForm(_config.OutputDirectory, true);
        if (prop.ShowDialog() == DialogResult.OK)
        {
            _config.OutputDirectory = prop.OutputPath;
            MarkDirty();
            Log($"Directorio de salida: {_config.OutputDirectory}");
        }
    }

    private void MnuManual_Click(object? sender, EventArgs e)
    {
        MessageBox.Show(
            "1. Seleccionar Preset GAWEB\n" +
            "2. Importar archivo de datos (CSV/Excel)\n" +
            "3. Seleccionar plantilla DOCX\n" +
            "4. Ajustar parámetros (fechas, lote...)\n" +
            "5. Pulsar GENERAR\n\n" +
            "Archivo > Guardar para reutilizar la configuración.",
            "Manual Rápido");
    }

    private void MnuAbout_Click(object? sender, EventArgs e)
    {
        MessageBox.Show("Generador de Cartas v2.0\nABD Tools Suite\n(c) 2025", "Acerca de");
    }

    // === BUTTON HANDLERS ===

    private void BtnBrowseData_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Filter = "Datos CSV/Excel|*.csv;*.xlsx" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            _config.DataFilePath = ofd.FileName;
            txtDataFile.Text = ofd.FileName;
            lblDataStatus.Text = "✓";
            MarkDirty();
            ValidateGenerationState();
        }
    }

    private void BtnDataDetail_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_config.DataFilePath) || !File.Exists(_config.DataFilePath))
        {
            MessageBox.Show("Seleccione primero un archivo de datos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var previewForm = new Forms.DataPreviewForm(_config.DataFilePath);
        previewForm.ShowDialog(this);
    }

    private void BtnBrowseOutput_Click(object? sender, EventArgs e)
    {
        using var fbd = new FolderBrowserDialog
        {
             Description = "Seleccionar Carpeta de Salida",
             UseDescriptionForTitle = true,
             SelectedPath = !string.IsNullOrEmpty(_config.OutputDirectory) ? Path.GetFullPath(_config.OutputDirectory) : ""
        };
        
        if (fbd.ShowDialog() == DialogResult.OK)
        {
             _config.OutputDirectory = fbd.SelectedPath;
             txtOutputDir.Text = fbd.SelectedPath;
             MarkDirty();
        }
    }

    private void CmbPreset_SelectedIndexChanged(object? sender, EventArgs e)
    {
        _config.PresetPath = cmbPreset.SelectedItem?.ToString() ?? "";
        MarkDirty();
        UpdatePresetNameLabel();
        ValidateGenerationState();
    }

    private void UpdatePresetNameLabel()
    {
        if (string.IsNullOrEmpty(_config.PresetPath))
        {
            lblPresetName.Text = "";
            _presetFechaGen = null;
            _presetFechaCarta = null;
            UpdateDateWarnings();
            return;
        }

        try
        {
            string presetPath = _config.PresetPath;
            if (!Path.IsPathRooted(presetPath))
                presetPath = Path.Combine("presets/gaweb", presetPath);

            if (File.Exists(presetPath))
            {
                var json = File.ReadAllText(presetPath);
                var preset = System.Text.Json.JsonSerializer.Deserialize<ABDTools.Core.Gaweb.Models.GawebPreset>(json);
                lblPresetName.Text = preset?.Name ?? Path.GetFileNameWithoutExtension(presetPath);
                
                // Check if preset is active
                _presetActive = preset?.Active ?? true;
                if (!_presetActive)
                {
                    lblPresetName.Text = "⛔ " + lblPresetName.Text + " (INACTIVO)";
                    lblPresetName.ForeColor = Color.Red;
                }
                else
                {
                    lblPresetName.ForeColor = Color.DarkBlue;
                }
                
                // Load all values from preset
                _presetFechaGen = preset?.FechaGeneracion;
                _presetFechaCarta = preset?.FechaCarta;
                _presetCodDoc = preset?.CodigoDocumento;
                _presetOficina = preset?.Oficina;
                
                // If UI fields are empty, populate with preset values
                if (string.IsNullOrEmpty(txtFechaGen.Text) && !string.IsNullOrEmpty(_presetFechaGen))
                    txtFechaGen.Text = _presetFechaGen;
                if (string.IsNullOrEmpty(txtFechaCarta.Text) && !string.IsNullOrEmpty(_presetFechaCarta))
                    txtFechaCarta.Text = _presetFechaCarta;
                if (string.IsNullOrEmpty(txtCodDoc.Text) && !string.IsNullOrEmpty(_presetCodDoc))
                    txtCodDoc.Text = _presetCodDoc;
                if (string.IsNullOrEmpty(txtOficina.Text) && !string.IsNullOrEmpty(_presetOficina))
                    txtOficina.Text = _presetOficina;
            }
            else
            {
                lblPresetName.Text = Path.GetFileNameWithoutExtension(_config.PresetPath);
                lblPresetName.ForeColor = Color.DarkBlue;
                _presetFechaGen = null;
                _presetFechaCarta = null;
                _presetCodDoc = null;
                _presetOficina = null;
                _presetActive = true;
            }
        }
        catch
        {
            lblPresetName.Text = Path.GetFileNameWithoutExtension(_config.PresetPath);
            lblPresetName.ForeColor = Color.DarkBlue;
            _presetFechaGen = null;
            _presetFechaCarta = null;
            _presetCodDoc = null;
            _presetOficina = null;
            _presetActive = true;
        }
        
        UpdateDateWarnings();
        UpdateFieldWarnings();
    }

    private void UpdateDateWarnings()
    {
        // Show warning if date differs from preset value
        if (!string.IsNullOrEmpty(_presetFechaGen) && txtFechaGen.Text != _presetFechaGen)
        {
            lblFechaGenWarning.Text = "⚠";
            _warningToolTip.SetToolTip(lblFechaGenWarning, $"Valor original del preset: {_presetFechaGen}");
        }
        else
        {
            lblFechaGenWarning.Text = "";
            _warningToolTip.SetToolTip(lblFechaGenWarning, "");
        }

        if (!string.IsNullOrEmpty(_presetFechaCarta) && txtFechaCarta.Text != _presetFechaCarta)
        {
            lblFechaCartaWarning.Text = "⚠";
            _warningToolTip.SetToolTip(lblFechaCartaWarning, $"Valor original del preset: {_presetFechaCarta}");
        }
        else
        {
            lblFechaCartaWarning.Text = "";
            _warningToolTip.SetToolTip(lblFechaCartaWarning, "");
        }
    }

    private void UpdateFieldWarnings()
    {
        // Show warning if field differs from preset value
        if (!string.IsNullOrEmpty(_presetCodDoc) && txtCodDoc.Text != _presetCodDoc)
        {
            lblCodDocWarning.Text = "⚠";
            _warningToolTip.SetToolTip(lblCodDocWarning, $"Valor original del preset: {_presetCodDoc}");
        }
        else
        {
            lblCodDocWarning.Text = "";
            _warningToolTip.SetToolTip(lblCodDocWarning, "");
        }

        if (!string.IsNullOrEmpty(_presetOficina) && txtOficina.Text != _presetOficina)
        {
            lblOficinaWarning.Text = "(!)";
            _warningToolTip.SetToolTip(lblOficinaWarning, $"Valor original del preset: {_presetOficina}");
        }
        else
        {
            lblOficinaWarning.Text = "";
            _warningToolTip.SetToolTip(lblOficinaWarning, "");
        }
    }

    private void BtnBrowseTemplate_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Filter = "Plantillas Word|*.docx" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            _config.TemplatePath = ofd.FileName;
            txtTemplateFile.Text = ofd.FileName;
            lblTemplateStatus.Text = "✓";
            lblTemplateStatus.Text = "✓";
            MarkDirty();
            ValidateGenerationState();
        }
    }

    private void BtnBrowsePreset_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog 
        { 
            Filter = "Preset GAWEB|*.json",
            Title = "Seleccionar Preset GAWEB"
        };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            _config.PresetPath = ofd.FileName;
            // Add to combo if not exists, or just display path
            if (!cmbPreset.Items.Contains(Path.GetFileName(ofd.FileName)))
            {
                cmbPreset.Items.Add(ofd.FileName);
            }
            cmbPreset.SelectedItem = ofd.FileName;
            MarkDirty();
            UpdatePresetNameLabel();
            ValidateGenerationState();
            Log($"Preset cargado: {Path.GetFileName(ofd.FileName)}");
        }
    }

    private void BtnPresetDetail_Click(object? sender, EventArgs e)
    {
        string presetPath = _config.PresetPath;
        
        if (string.IsNullOrEmpty(presetPath))
        {
            MessageBox.Show("Seleccione un preset primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Resolve path if relative
        if (!Path.IsPathRooted(presetPath))
        {
            presetPath = Path.Combine("presets/gaweb", presetPath);
        }

        if (!File.Exists(presetPath))
        {
            MessageBox.Show($"No se encuentra el archivo del preset:\n{presetPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            using var detailForm = new Forms.PresetDetailForm(presetPath);
            detailForm.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar el preset:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnTemplateDetail_Click(object? sender, EventArgs e)
    {
        string path = _config.TemplatePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            MessageBox.Show("Seleccione una plantilla Word válida primero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            var variables = new HashSet<string>();
            // Use regex compatible with MappingForm logic 
            // Note: This simple regex assumes {{tag}} is in one run. 
            // If Word splits it (common), this might fail. But sticking to existing logic for now.
            var regex = new System.Text.RegularExpressions.Regex(@"\{\{([^{}]+)\}\}");

            using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(path, false))
            {
                var body = doc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    // IMPROVED LOGIC: Scan Paragraphs instead of individual Text nodes.
                    // Word often splits "{{Variable}}" into multiple runs (e.g. "{{" + "Variable" + "}}").
                    // By concatenating text within a paragraph, we reconstruct the full string.
                    foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        string paraText = para.InnerText; // InnerText aggregates all text nodes in the paragraph
                        foreach (System.Text.RegularExpressions.Match match in regex.Matches(paraText))
                        {
                            variables.Add(match.Groups[1].Value.Trim());
                        }
                    }
                }
            }

            if (variables.Count > 0)
            {
                var list = string.Join("\n", variables.OrderBy(v => v));
                MessageBox.Show($"Se encontraron {variables.Count} etiquetas:\n\n{list}", "Plantilla", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                 MessageBox.Show("No se detectaron etiquetas con formato {{Variable}} en el documento.\n\nPosibles causas:\n1. El formato no es {{Variable}}.\n2. Word ha partido la etiqueta internamente (Intente guardar como RTF y luego DOCX de nuevo, o reescribir la variable).", 
                     "Aviso - Sin Etiquetas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al leer la plantilla:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void BtnMapping_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_config.TemplatePath) || !File.Exists(_config.TemplatePath))
        {
            MessageBox.Show("Seleccione primero una plantilla DOCX.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (string.IsNullOrEmpty(_config.DataFilePath) || !File.Exists(_config.DataFilePath))
        {
            MessageBox.Show("Seleccione primero un archivo de datos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Prepare form overrides to pass to MappingForm
        var formOverrides = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(txtFechaCarta.Text))
            formOverrides["FechaCarta"] = txtFechaCarta.Text;
        if (!string.IsNullOrWhiteSpace(txtCodDoc.Text))
            formOverrides["CodigoDocumento"] = txtCodDoc.Text;
        if (!string.IsNullOrWhiteSpace(txtOficina.Text))
            formOverrides["Oficina"] = txtOficina.Text;
        if (!string.IsNullOrWhiteSpace(txtLote.Text))
            formOverrides["Lote"] = txtLote.Text;

        using var mappingForm = new Forms.MappingForm(_config.TemplatePath, _config.DataFilePath, _config.VariableMapping, formOverrides);
        if (mappingForm.ShowDialog(this) == DialogResult.OK)
        {
            _config.VariableMapping = mappingForm.Mapping;
            MarkDirty();
            Log($"Mapeo actualizado: {mappingForm.Mapping.Count} variables mapeadas.");

            // Update state (enables Generate if ready)
            ValidateGenerationState();
        }
    }

    private async void BtnGenerate_Click(object? sender, EventArgs e)
    {
        SyncUIToConfig();

        // Check if preset is active
        if (!_presetActive)
        {
            MessageBox.Show("⛔ El preset seleccionado está INACTIVO.\n\nNo se puede continuar con la generación.", 
                "Preset Inactivo", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Check if template has variables
        if (!string.IsNullOrEmpty(_config.TemplatePath) && File.Exists(_config.TemplatePath))
        {
            try
            {
                using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(_config.TemplatePath, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                bool hasVariables = false;
                if (body != null)
                {
                    var regex = new System.Text.RegularExpressions.Regex(@"\{\{([^{}]+)\}\}");
                    // IMPROVED LOGIC: Scan Paragraphs instead of individual Text nodes.
                    foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
                    {
                        if (regex.IsMatch(para.InnerText))
                        {
                            hasVariables = true;
                            break;
                        }
                    }
                }
                if (!hasVariables)
                {
                    MessageBox.Show("⛔ La plantilla Word no contiene variables {{xxx}}.\n\nNo se puede continuar con la generación.", 
                        "Plantilla Sin Variables", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            catch { /* Ignore read errors, validation will catch them */ }
        }

        // Validate
        var errors = _configService.ValidateConfig(_config);
        if (errors.Count > 0)
        {
            MessageBox.Show("Errores de validación:\n\n• " + string.Join("\n• ", errors), 
                "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        btnGenerate.Enabled = false;
        btnCancel.Enabled = true;
        progressBar.Style = ProgressBarStyle.Marquee;
        lblStatus.Text = "Generando...";
        _cts = new CancellationTokenSource();

        try
        {
            await Task.Run(() => RunGeneration(_cts.Token), _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Log("Generación cancelada por el usuario.");
        }
        finally
        {
            btnGenerate.Enabled = true;
            btnCancel.Enabled = false;
            progressBar.Style = ProgressBarStyle.Blocks;
            lblStatus.Text = "Listo.";
            _cts = null;
        }
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    // === GENERATION LOGIC ===

    private void RunGeneration(CancellationToken ct)
    {
        try
        {
            var pdfService = new PdfService();
            var templateService = new TemplateService();
            var gawebService = new GawebService();

            // Load preset with overrides
            GawebPreset preset;
            try
            {
                preset = _configService.LoadPresetWithOverrides(_config);
            }
            catch (Exception ex)
            {
                InvokeLog($"ERROR: No se pudo cargar el preset: {ex.Message}");
                return;
            }

            // Read data file
            string[] lines = File.ReadAllLines(_config.DataFilePath);
            if (lines.Length < 2)
            {
                InvokeLog("ERROR: Archivo de datos vacío o sin cabeceras.");
                return;
            }

            string[] headers = lines[0].Split(';');
            string batchTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string lote = _config.Overrides.Lote ?? "0001";
            string codigoEntorno = preset.CodigoEntorno ?? "ENTORNO";
            string baseMd5 = gawebService.GenerateMd5(batchTimestamp);

            // Create lote folder (Just the Lote number/name)
            string loteFolder = Path.Combine(_config.OutputDirectory, lote);
            Directory.CreateDirectory(loteFolder);
            string tempDir = Path.Combine(loteFolder, $"TEMP_PDF_{batchTimestamp}");
            Directory.CreateDirectory(tempDir);

            var gawebRecords = new List<ABDTools.Core.Gaweb.Models.GawebRecord>();

            int fromRecord = _config.RangeFrom ?? 1;
            int toRecord = _config.RangeTo ?? (lines.Length - 1);
            toRecord = Math.Min(toRecord, lines.Length - 1);

            int total = toRecord - fromRecord + 1;
            int processed = 0;

            InvokeLog($"Procesando {total} registros (del {fromRecord} al {toRecord})...");

            for (int i = fromRecord; i <= toRecord; i++)
            {
                ct.ThrowIfCancellationRequested();

                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string[] values = line.Split(';');
                var rowData = new Dictionary<string, string>();
                for (int h = 0; h < headers.Length && h < values.Length; h++)
                    rowData[headers[h]] = values[h];

                // Apply variable mapping
                var mappedData = new Dictionary<string, string>();
                foreach (var kv in _config.VariableMapping)
                {
                    if (rowData.TryGetValue(kv.Value, out var val))
                        mappedData[kv.Key] = val;
                }
                // If no mapping, use direct column names
                if (mappedData.Count == 0)
                    mappedData = rowData;

                string docName = gawebService.CalculateGawebPdfName(baseMd5, i);
                string finalPdfName = docName + ".pdf";

                // Build GAWEB record
                var rec = new ABDTools.Core.Gaweb.Models.GawebRecord
                {
                    TipoCarta = " ",
                    Formato = preset.FormatoCarta ?? "04",
                    FechaGeneracion = preset.FechaGeneracion ?? DateTime.Now.ToString("yyyyMMdd"),
                    Lote = lote.PadLeft(4, '0').Substring(0, Math.Min(lote.Length, 4)),
                    Secuencial = i.ToString().PadLeft(7, '0'),
                    Pagina = preset.PaginasDefecto.ToString().PadLeft(4, '0'),
                    CodDocumento = preset.CodigoDocumento ?? "X00054",
                    FechaCarta = preset.FechaCarta ?? DateTime.Now.ToString("yyyyMMdd"),
                    IndDestino = preset.IndicadorDestino ?? "0",
                    Idioma = preset.Idioma ?? "  ",
                    Oficina = preset.Oficina ?? "00000",
                    NombrePDF = docName
                };
                gawebRecords.Add(rec);

                string tempDocx = Path.Combine(tempDir, docName + ".docx");
                string tempPdf = Path.Combine(tempDir, finalPdfName);

                try
                {
                    templateService.ProcessTemplate(_config.TemplatePath, tempDocx, mappedData);
                    
                    // Handle output based on OutputType
                    if (_config.OutputType == "DOCX")
                    {
                        // Keep only DOCX - move to output folder
                        string finalDocx = Path.Combine(loteFolder, docName + ".docx");
                        File.Move(tempDocx, finalDocx, true);
                    }
                    else
                    {
                        // PDF or PDF_GAWEB - convert to PDF
                        try 
                        {
                            pdfService.ConvertDocxToPdf(tempDocx, tempPdf);
                            if (File.Exists(tempDocx)) File.Delete(tempDocx);
                        }
                        catch (Exception pdfEx)
                        {
                            // If PDF conversion fails, we MUST log it clearly.
                            // And we probably shouldn't delete the DOCX so the user has something.
                            // But for "PDF_GAWEB" if we keep DOCX, the zip will contain DOCX which confuses the user.
                            // Let's log heavily.
                            InvokeLog($"ERROR AL GENERAR PDF ({docName}): {pdfEx.Message}");
                            // Don't rethrow to avoid stopping the whole batch, but this record failed its primary output.
                        }
                    }

                    processed++;
                    Invoke(() =>
                    {
                        progressBar.Style = ProgressBarStyle.Blocks;
                        progressBar.Value = (int)((float)processed / total * 100);
                    });

                    if (processed % 10 == 0)
                        InvokeLog($"[{processed}/{total}] procesados...");
                }
                catch (Exception ex)
                {
                    InvokeLog($"ERROR registro {i}: {ex.Message}");
                }
            }

            ct.ThrowIfCancellationRequested();

            // Generate GAWEB package only for PDF_GAWEB mode
            if (_config.OutputType == "PDF_GAWEB")
            {
                InvokeLog("Generando paquete GAWEB...");
                string basePackageName = $"COMUNICADOS.PDF.{codigoEntorno}.{batchTimestamp}.{lote}";

                string gawebFile = Path.Combine(loteFolder, basePackageName + ".GAWEB");
                using (var sw = new StreamWriter(gawebFile, false, System.Text.Encoding.UTF8))
                {
                    foreach (var rec in gawebRecords)
                        sw.WriteLine(rec.Serialize());
                }

                string zipFile = Path.Combine(loteFolder, basePackageName + ".ZIP");
                gawebService.ZipDirectory(tempDir, zipFile);
                gawebService.CreateMetaFiles(zipFile, basePackageName, loteFolder);

                Directory.Delete(tempDir, true);
                InvokeLog($"✓ Paquete GAWEB completado: {loteFolder}");
            }
            else if (_config.OutputType == "PDF")
            {
                // Move PDFs from temp to output folder
                foreach (var pdf in Directory.GetFiles(tempDir, "*.pdf"))
                {
                    File.Move(pdf, Path.Combine(loteFolder, Path.GetFileName(pdf)), true);
                }
                Directory.Delete(tempDir, true);
                InvokeLog($"✓ PDFs generados en: {loteFolder}");
            }
            else
            {
                // DOCX mode - temp folder already empty, just clean up
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                InvokeLog($"✓ Documentos DOCX generados en: {loteFolder}");
            }

            Invoke(() => MessageBox.Show("Generación completada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            InvokeLog($"ERROR CRÍTICO: {ex.Message}");
            Invoke(() => MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error));
        }
    }

    // === HELPERS ===

    private void RefreshPresetList()
    {
        cmbPreset.Items.Clear();
        foreach (var preset in _configService.ListPresets())
        {
            cmbPreset.Items.Add(preset);
        }
    }

    private void ShowDatePicker(TextBox target)
    {
        using var mc = new MonthCalendar { MaxSelectionCount = 1 };
        using var form = new Form
        {
            Text = "Seleccionar Fecha",
            Size = new Size(250, 220),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        mc.DateSelected += (s, e) =>
        {
            target.Text = e.Start.ToString("yyyyMMdd");
            form.Close();
        };

        form.Controls.Add(mc);
        form.ShowDialog();
    }

    private void SyncUIToConfig()
    {
        _config.PresetPath = cmbPreset.SelectedItem?.ToString() ?? "";
        _config.DataFilePath = txtDataFile.Text;
        _config.TemplatePath = txtTemplateFile.Text;
        _config.Overrides.FechaGeneracion = string.IsNullOrWhiteSpace(txtFechaGen.Text) ? null : txtFechaGen.Text;
        _config.Overrides.FechaCarta = string.IsNullOrWhiteSpace(txtFechaCarta.Text) ? null : txtFechaCarta.Text;
        _config.Overrides.Lote = string.IsNullOrWhiteSpace(txtLote.Text) ? null : txtLote.Text;
        _config.Overrides.CodigoDocumento = string.IsNullOrWhiteSpace(txtCodDoc.Text) ? null : txtCodDoc.Text;
        _config.Overrides.Oficina = string.IsNullOrWhiteSpace(txtOficina.Text) ? null : txtOficina.Text;
        _config.RangeFrom = int.TryParse(txtRangeFrom.Text, out int rf) ? rf : null;
        _config.RangeTo = int.TryParse(txtRangeTo.Text, out int rt) ? rt : null;
    }

    private void UpdateUI()
    {
        // Robustly set preset selection
        string pPath = _config.PresetPath;
        if (!string.IsNullOrEmpty(pPath))
        {
             // Try exact match
             int idx = cmbPreset.FindStringExact(pPath);
             if (idx == -1) 
             {
                 // Try filename match
                 string pName = Path.GetFileName(pPath);
                 for (int i=0; i < cmbPreset.Items.Count; i++)
                 {
                     string item = cmbPreset.Items[i].ToString() ?? "";
                     if (Path.GetFileName(item).Equals(pName, StringComparison.OrdinalIgnoreCase))
                     {
                         idx = i;
                         break;
                     }
                 }
                 
                 // If still not found, add it (it might be a custom path from a loaded config)
                 if (idx == -1 && File.Exists(pPath)) 
                 {
                      cmbPreset.Items.Add(pPath);
                      idx = cmbPreset.Items.Count - 1;
                 }
             }
             
             if (idx != -1) 
                 cmbPreset.SelectedIndex = idx; 
             else
                 // Just set text or force update labels manually if we can't select in combo
                 UpdatePresetNameLabel(); 
        }
        txtDataFile.Text = _config.DataFilePath;
        txtTemplateFile.Text = _config.TemplatePath;
        txtFechaGen.Text = _config.Overrides.FechaGeneracion ?? "";
        txtFechaCarta.Text = _config.Overrides.FechaCarta ?? "";
        txtLote.Text = _config.Overrides.Lote ?? "";
        txtCodDoc.Text = _config.Overrides.CodigoDocumento ?? "";
        txtOficina.Text = _config.Overrides.Oficina ?? "";
        txtRangeFrom.Text = _config.RangeFrom?.ToString() ?? "";
        txtRangeTo.Text = _config.RangeTo?.ToString() ?? "";

        lblDataStatus.Text = File.Exists(_config.DataFilePath) ? "OK" : "";
        lblTemplateStatus.Text = File.Exists(_config.TemplatePath) ? "OK" : "";
        if (txtOutputDir != null) 
            txtOutputDir.Text = _config.OutputDirectory ?? "output";

        UpdateTitle();
        ValidateGenerationState();
    }

    private void ValidateGenerationState()
    {
        // Resolve paths for checking existence
        string presetPath = _config.PresetPath;
        if (!string.IsNullOrEmpty(presetPath) && !Path.IsPathRooted(presetPath))
            presetPath = Path.Combine("presets/gaweb", presetPath);

        bool hasTemplate = !string.IsNullOrEmpty(_config.TemplatePath) && File.Exists(_config.TemplatePath);
        bool hasData = !string.IsNullOrEmpty(_config.DataFilePath) && File.Exists(_config.DataFilePath);
        bool hasPreset = !string.IsNullOrEmpty(_config.PresetPath) && File.Exists(presetPath);
        
        bool canGenerate = hasTemplate && hasData && hasPreset;
        
        // If the button was disabled, update state.
        // Logic: Enable if all files are present. 
        // We do NOT strictly require checking VariableMapping.Count > 0 because columns might match automatically.
        // However, we do check Validations on Click.
        btnGenerate.Enabled = canGenerate;
        
        // Update Mapping Button visual state
        if (_config.VariableMapping != null && _config.VariableMapping.Count > 0)
        {
             _btnMapping.Text = "✓ MAPEO";
             _btnMapping.BackColor = Color.LightGreen;
        }
        else
        {
             _btnMapping.Text = "📋 MAPEO...";
             _btnMapping.BackColor = Color.Gold;
        }
    }

    private void UpdateTitle()
    {
        string fileName = string.IsNullOrEmpty(_configFilePath) ? "Sin guardar" : Path.GetFileName(_configFilePath);
        string dirty = _isDirty ? "*" : "";
        this.Text = $"Generador de Cartas v2.0 - {fileName}{dirty}";
    }

    private void MarkDirty()
    {
        _isDirty = true;
        UpdateTitle();
    }

    private bool ConfirmDiscardChanges()
    {
        if (!_isDirty) return true;
        var result = MessageBox.Show("Hay cambios sin guardar. ¿Desea continuar?", "Confirmar",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        return result == DialogResult.Yes;
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!ConfirmDiscardChanges())
            e.Cancel = true;
    }

    private void Log(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(new Action<string>(Log), message);
            return;
        }
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        txtLog.ScrollToCaret();
    }

    private void InvokeLog(string message)
    {
        Invoke(() => Log(message));
    }

    private void MnuExportPackage_Click(object? sender, EventArgs e)
    {
        SyncUIToConfig();

        if (string.IsNullOrEmpty(_config.TemplatePath) || !File.Exists(_config.TemplatePath) ||
            string.IsNullOrEmpty(_config.DataFilePath) || !File.Exists(_config.DataFilePath) ||
            string.IsNullOrEmpty(_config.PresetPath))
        {
            MessageBox.Show("Para exportar un paquete completo se requiere:\n- Plantilla DOCX\n- Archivo de datos\n- Preset cargado", 
                "Faltan archivos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var sfd = new SaveFileDialog
        {
            Filter = "Paquete GeneradorCartas|*.zip",
            Title = "Exportar Paquete Completo",
            FileName = "paquete_cartas.zip"
        };

        if (sfd.ShowDialog() == DialogResult.OK)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "GC_Export_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDir);

                // Copy files
                string tplName = Path.GetFileName(_config.TemplatePath);
                string datName = Path.GetFileName(_config.DataFilePath);
                string preName = Path.GetFileName(_config.PresetPath);

                File.Copy(_config.TemplatePath, Path.Combine(tempDir, tplName));
                File.Copy(_config.DataFilePath, Path.Combine(tempDir, datName));
                
                string fullPresetPath = _config.PresetPath;
                if (!Path.IsPathRooted(fullPresetPath))
                    fullPresetPath = Path.Combine("presets/gaweb", fullPresetPath);
                
                if (File.Exists(fullPresetPath))
                    File.Copy(fullPresetPath, Path.Combine(tempDir, preName));

                // Create relative config
                var pkgConfig = new GenerationConfig
                {
                    TemplatePath = tplName,
                    DataFilePath = datName,
                    PresetPath = preName,
                    OutputDirectory = "output",
                    VariableMapping = new Dictionary<string, string>(_config.VariableMapping),
                    Overrides = _config.Overrides,
                    RangeFrom = _config.RangeFrom,
                    RangeTo = _config.RangeTo,
                    OutputType = _config.OutputType
                };

                _configService.SaveConfig(pkgConfig, Path.Combine(tempDir, "config.json"));

                // Zip
                if (File.Exists(sfd.FileName)) File.Delete(sfd.FileName);
                System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, sfd.FileName);

                try { Directory.Delete(tempDir, true); } catch { }
                
                MessageBox.Show($"Paquete exportado correctamente:\n{sfd.FileName}", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar paquete:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void MnuImportPackage_Click(object? sender, EventArgs e)
    {
        if (!ConfirmDiscardChanges()) return;

        using var ofd = new OpenFileDialog
        {
            Filter = "Paquete GeneradorCartas|*.zip",
            Title = "Importar Paquete Completo"
        };

        if (ofd.ShowDialog() == DialogResult.OK)
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Seleccione carpeta destino para descomprimir el paquete",
                ShowNewFolderButton = true
            };

            if (fbd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string targetDir = fbd.SelectedPath;
                    
                    if (Directory.GetFiles(targetDir).Length > 0)
                    {
                        var res = MessageBox.Show("La carpeta destino NO está vacía. ¿Desea crear una subcarpeta con el nombre del paquete?", 
                            "Carpeta no vacía", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (res == DialogResult.Cancel) return;
                        if (res == DialogResult.Yes)
                        {
                            targetDir = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(ofd.FileName));
                            Directory.CreateDirectory(targetDir);
                        }
                    }

                    System.IO.Compression.ZipFile.ExtractToDirectory(ofd.FileName, targetDir, true);

                    string configFile = Path.Combine(targetDir, "config.json");
                    if (File.Exists(configFile))
                    {
                        var newConfig = _configService.LoadConfig(configFile);
                        
                        newConfig.TemplatePath = Path.Combine(targetDir, newConfig.TemplatePath);
                        newConfig.DataFilePath = Path.Combine(targetDir, newConfig.DataFilePath);
                        newConfig.OutputDirectory = Path.Combine(targetDir, "output");
                        
                        string extractedPreset = Path.Combine(targetDir, newConfig.PresetPath);
                        if (File.Exists(extractedPreset))
                            newConfig.PresetPath = extractedPreset;
                        
                        _configFilePath = configFile;
                        _config = newConfig;
                        _isDirty = false;
                        UpdateUI();
                        UpdatePresetNameLabel();
                        Log($"Paquete importado desde: {ofd.FileName}");
                        MessageBox.Show("Paquete importado y cargado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                         MessageBox.Show("No se encontró archivo config.json en el paquete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al importar paquete:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
