using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GeneradorCartas.Forms;
using GeneradorCartas.Models;
using GeneradorCartas.Services;
using ABDTools.Core.Gaweb.Models;

namespace GeneradorCartas;

public class MainForm : Form, IGenerationProgress
{
    // Services
    private readonly ConfigService _configService;
    private readonly GenerationEngine _engine;

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
        var dataReader = new DataReaderService();
        _engine = new GenerationEngine(
            _configService, 
            new SyncfusionPdfService(), 
            new TemplateService(), 
            new GawebService(),
            dataReader
        );
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
        // We use a high panel to house the GroupBoxes
        Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 480, Padding = new Padding(15) };
        this.Size = new Size(1000, 900);
        this.MinimumSize = new Size(950, 800);

        int lblX = 20, txtX = 150, txtW = 500;

        // --- PASO 1: ARCHIVOS Y PRESETS ---
        GroupBox grpStep1 = new GroupBox { 
            Text = " Paso 1: Selección de Archivos y Presets ", 
            Bounds = new Rectangle(15, 10, 920, 130),
            Font = new Font(this.Font, FontStyle.Bold)
        };
        grpStep1.Font = new Font(this.Font.FontFamily, 9, FontStyle.Bold); // Slighty larger for header
        
        int y1 = 30;
        Label lblPreset = new Label { Text = "Preset GAWEB:", Location = new Point(lblX, y1 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        cmbPreset = new ComboBox { Location = new Point(txtX, y1), Width = 300, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(this.Font, FontStyle.Regular) };
        cmbPreset.SelectedIndexChanged += CmbPreset_SelectedIndexChanged;
        
        // Vertical alignment fix: Button at y1-2 to center with ComboBox
        Button btnBrowsePreset = new Button { Text = "📂", Location = new Point(txtX + 305, y1 - 2), Width = 40, Height = 28, Font = new Font("Segoe UI Emoji", 10) };
        btnBrowsePreset.Click += BtnBrowsePreset_Click;
        _warningToolTip.SetToolTip(btnBrowsePreset, "Explorar archivo de preset...");

        Button btnRefreshPresets = new Button { Text = "↻", Location = new Point(txtX + 350, y1 - 2), Width = 40, Height = 28, Font = new Font("Segoe UI Symbol", 12) };
        btnRefreshPresets.Click += (s, e) => RefreshPresetList();
        _warningToolTip.SetToolTip(btnRefreshPresets, "Recargar lista de presets");

        Button btnPresetDetail = new Button { Text = "Ver Detalle", Location = new Point(txtX + 395, y1 - 2), Width = 90, Height = 28 };
        btnPresetDetail.Click += BtnPresetDetail_Click;
        lblPresetName = new Label { Text = "", Location = new Point(txtX + 495, y1 + 5), AutoSize = true, ForeColor = Color.DarkBlue, Font = new Font(this.Font, FontStyle.Italic) };
        grpStep1.Controls.AddRange(new Control[] { lblPreset, cmbPreset, btnBrowsePreset, btnRefreshPresets, btnPresetDetail, lblPresetName });

        y1 += 35;
        Label lblData = new Label { Text = "Archivo Datos:", Location = new Point(lblX, y1 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtDataFile = new TextBox { Location = new Point(txtX, y1), Width = 550, ReadOnly = true, Font = new Font(this.Font, FontStyle.Regular) };
        Button btnBrowseData = new Button { Text = "Explorar...", Location = new Point(txtX + 555, y1), Width = 85 };
        btnBrowseData.Click += BtnBrowseData_Click;
        Button btnDataDetail = new Button { Text = "Vista Previa", Location = new Point(txtX + 645, y1), Width = 90 };
        btnDataDetail.Click += BtnDataDetail_Click;
        lblDataStatus = new Label { Text = "", Location = new Point(txtX + 740, y1 + 3), AutoSize = true, ForeColor = Color.Green };
        grpStep1.Controls.AddRange(new Control[] { lblData, txtDataFile, btnBrowseData, btnDataDetail, lblDataStatus });

        y1 += 35;
        Label lblTemplate = new Label { Text = "Plantilla DOCX:", Location = new Point(lblX, y1 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtTemplateFile = new TextBox { Location = new Point(txtX, y1), Width = 550, ReadOnly = true, Font = new Font(this.Font, FontStyle.Regular) };
        Button btnBrowseTemplate = new Button { Text = "Explorar...", Location = new Point(txtX + 555, y1), Width = 85 };
        btnBrowseTemplate.Click += BtnBrowseTemplate_Click;
        Button btnTemplateDetail = new Button { Text = "Etiquetas", Location = new Point(txtX + 645, y1), Width = 90 };
        btnTemplateDetail.Click += BtnTemplateDetail_Click;
        lblTemplateStatus = new Label { Text = "", Location = new Point(txtX + 740, y1 + 3), AutoSize = true, ForeColor = Color.Green };
        grpStep1.Controls.AddRange(new Control[] { lblTemplate, txtTemplateFile, btnBrowseTemplate, btnTemplateDetail, lblTemplateStatus });
        pnlTop.Controls.Add(grpStep1);


        // --- PASO 2: PARÁMETROS DEL LOTE ---
        GroupBox grpStep2 = new GroupBox { 
            Text = " Paso 2: Parámetros del Lote (Sobrescriben Preset) ", 
            Bounds = new Rectangle(15, 145, 920, 110),
            Font = new Font(this.Font, FontStyle.Bold)
        };
        int yRow1 = 30;
        int yRow2 = 65;
        int step2X = 15;
        
        // Row 1: Dates
        Label lblFechaGen = new Label { Text = "F. Generación:", Location = new Point(step2X, yRow1 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtFechaGen = new TextBox { Location = new Point(step2X + 105, yRow1), Width = 100, MaxLength = 8, Font = new Font(this.Font, FontStyle.Regular) };
        txtFechaGen.TextChanged += (s, e) => { _config.Overrides.FechaGeneracion = txtFechaGen.Text; MarkDirty(); UpdateDateWarnings(); };
        Button btnCalGen = new Button { Text = "📅", Location = new Point(step2X + 210, yRow1 - 1), Width = 35, Height = 26, Font = new Font("Segoe UI Emoji", 10) };
        btnCalGen.Click += (s, e) => ShowDatePicker(txtFechaGen);
        Button btnTodayGen = new Button { Text = "Hoy", Location = new Point(step2X + 250, yRow1 - 1), Width = 45, Height = 26 };
        btnTodayGen.Click += (s, e) => { txtFechaGen.Text = DateTime.Now.ToString("yyyyMMdd"); };
        lblFechaGenWarning = new Label { Text = "", Location = new Point(step2X + 300, yRow1 + 3), AutoSize = true, ForeColor = Color.DarkOrange };

        Label lblFechaCarta = new Label { Text = "F. Carta:", Location = new Point(step2X + 370, yRow1 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtFechaCarta = new TextBox { Location = new Point(step2X + 440, yRow1), Width = 100, MaxLength = 8, Font = new Font(this.Font, FontStyle.Regular) };
        txtFechaCarta.TextChanged += (s, e) => { _config.Overrides.FechaCarta = txtFechaCarta.Text; MarkDirty(); UpdateDateWarnings(); };
        Button btnCalCarta = new Button { Text = "📅", Location = new Point(step2X + 545, yRow1 - 1), Width = 35, Height = 26, Font = new Font("Segoe UI Emoji", 10) };
        btnCalCarta.Click += (s, e) => ShowDatePicker(txtFechaCarta);
        Button btnTodayCarta = new Button { Text = "Hoy", Location = new Point(step2X + 585, yRow1 - 1), Width = 45, Height = 26 };
        btnTodayCarta.Click += (s, e) => { txtFechaCarta.Text = DateTime.Now.ToString("yyyyMMdd"); };
        lblFechaCartaWarning = new Label { Text = "", Location = new Point(step2X + 635, yRow1 + 3), AutoSize = true, ForeColor = Color.DarkOrange };

        // Row 2: Others
        Label lblLote = new Label { Text = "Lote:", Location = new Point(step2X, yRow2 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtLote = new TextBox { Location = new Point(step2X + 105, yRow2), Width = 70, MaxLength = 4, Font = new Font(this.Font, FontStyle.Regular) };
        txtLote.TextChanged += (s, e) => { _config.Overrides.Lote = txtLote.Text; MarkDirty(); };

        Label lblCodDoc = new Label { Text = "Cód. Documento:", Location = new Point(step2X + 200, yRow2 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtCodDoc = new TextBox { Location = new Point(step2X + 325, yRow2), Width = 100, MaxLength = 6, Font = new Font(this.Font, FontStyle.Regular) };
        txtCodDoc.TextChanged += (s, e) => { _config.Overrides.CodigoDocumento = txtCodDoc.Text; MarkDirty(); UpdateFieldWarnings(); };
        lblCodDocWarning = new Label { Text = "", Location = new Point(step2X + 430, yRow2 + 3), AutoSize = true, ForeColor = Color.DarkOrange };

        Label lblOficina = new Label { Text = "Oficina:", Location = new Point(step2X + 500, yRow2 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtOficina = new TextBox { Location = new Point(step2X + 570, yRow2), Width = 70, MaxLength = 5, Font = new Font(this.Font, FontStyle.Regular) };
        txtOficina.TextChanged += (s, e) => { _config.Overrides.Oficina = txtOficina.Text; MarkDirty(); UpdateFieldWarnings(); };
        lblOficinaWarning = new Label { Text = "", Location = new Point(step2X + 645, yRow2 + 3), AutoSize = true, ForeColor = Color.DarkOrange };

        grpStep2.Controls.AddRange(new Control[] { 
            lblFechaGen, txtFechaGen, btnCalGen, btnTodayGen, lblFechaGenWarning,
            lblFechaCarta, txtFechaCarta, btnCalCarta, btnTodayCarta, lblFechaCartaWarning,
            lblLote, txtLote, lblCodDoc, txtCodDoc, lblCodDocWarning, 
            lblOficina, txtOficina, lblOficinaWarning 
        });
        pnlTop.Controls.Add(grpStep2);


        // --- PASO 3: SELECCIÓN DE REGISTROS Y SALIDA ---
        GroupBox grpStep3 = new GroupBox { 
            Text = " Paso 3: Selección de Registros y Salida ", 
            Bounds = new Rectangle(15, 265, 920, 110),
            Font = new Font(this.Font, FontStyle.Bold)
        };
        int y3 = 30;
        Label lblRangeFrom = new Label { Text = "Desde Registro:", Location = new Point(lblX, y3 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtRangeFrom = new TextBox { Location = new Point(txtX, y3), Width = 80, Font = new Font(this.Font, FontStyle.Regular) };
        txtRangeFrom.TextChanged += (s, e) => { _config.RangeFrom = int.TryParse(txtRangeFrom.Text, out int v) ? v : null; MarkDirty(); };

        Label lblRangeTo = new Label { Text = "Hasta:", Location = new Point(250, y3 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtRangeTo = new TextBox { Location = new Point(300, y3), Width = 80, Font = new Font(this.Font, FontStyle.Regular) };
        txtRangeTo.TextChanged += (s, e) => { _config.RangeTo = int.TryParse(txtRangeTo.Text, out int v) ? v : null; MarkDirty(); };

        Label lblOutputType = new Label { Text = "Tipo Salida:", Location = new Point(500, y3 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        cmbOutputType = new ComboBox { Location = new Point(600, y3), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(this.Font, FontStyle.Regular) };
        cmbOutputType.Items.AddRange(new object[] { "DOCX", "PDF", "PDF + GAWEB" });
        cmbOutputType.SelectedIndexChanged += (s, e) => { 
            _config.OutputType = cmbOutputType.SelectedIndex switch { 0 => "DOCX", 1 => "PDF", _ => "PDF_GAWEB" }; 
            MarkDirty(); 
        };

        y3 += 35;
        Label lblOutDir = new Label { Text = "Dir. Salida:", Location = new Point(lblX, y3 + 3), AutoSize = true, Font = new Font(this.Font, FontStyle.Regular) };
        txtOutputDir = new TextBox { Location = new Point(txtX, y3), Width = 550, ReadOnly = true, BackColor = Color.WhiteSmoke, Font = new Font(this.Font, FontStyle.Regular) };
        Button btnBrowseOutput = new Button { Text = "Cambiar...", Location = new Point(txtX + 555, y3), Width = 85 };
        btnBrowseOutput.Click += BtnBrowseOutput_Click;
        Button btnOpenOutput = new Button { Text = "Abrir Carpeta", Location = new Point(txtX + 645, y3), Width = 95 };
        btnOpenOutput.Click += (s, e) => {
             if (!string.IsNullOrEmpty(_config.OutputDirectory) && Directory.Exists(_config.OutputDirectory))
                 System.Diagnostics.Process.Start("explorer.exe", _config.OutputDirectory);
        };
        grpStep3.Controls.AddRange(new Control[] { lblRangeFrom, txtRangeFrom, lblRangeTo, txtRangeTo, lblOutputType, cmbOutputType, lblOutDir, txtOutputDir, btnBrowseOutput, btnOpenOutput });
        pnlTop.Controls.Add(grpStep3);


        // --- PASO 4: ACCIONES PRINCIPALES ---
        _btnMapping = new Button {
            Text = "📋 CONFIGURAR MAPEO",
            Location = new Point(15, 400),
            Width = 240,
            Height = 60,
            BackColor = Color.Gold,
            Font = new Font(this.Font.FontFamily, 11, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnMapping.Click += BtnMapping_Click;

        btnGenerate = new Button {
            Text = "🚀 INICIAR GENERACIÓN",
            Location = new Point(270, 400),
            Width = 400,
            Height = 60,
            BackColor = Color.LightSkyBlue,
            Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
            Enabled = false,
            Cursor = Cursors.Hand
        };
        btnGenerate.Click += BtnGenerate_Click;

        btnCancel = new Button { // Renamed to btnStop in instruction, but keeping original name as per instruction to only make specified changes.
            Text = "DETENER",
            Location = new Point(685, 410), // Adjusted Y
            Width = 140, // Adjusted Width
            Height = 60,
            BackColor = Color.LightCoral,
            Font = new Font(this.Font.FontFamily, 11, FontStyle.Bold), // Adjusted Font
            Enabled = false,
            Cursor = Cursors.Hand
        };
        btnCancel.Click += BtnCancel_Click;

        pnlTop.Controls.AddRange(new Control[] { _btnMapping, btnGenerate, btnCancel });

        this.Controls.Add(pnlTop);

        // === STATUS STRIP ===
        StatusStrip statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel { Text = "Listo." };
        statusStrip.Items.Add(lblStatus);
        this.Controls.Add(statusStrip);

        // === LOG AREA ===
        Panel pnlFill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15) };
        progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 25 };
        txtLog = new RichTextBox {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            BackColor = Color.White,
            Font = new Font("Consolas", 10),
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
        using var prop = new PropertiesForm(_config.OutputDirectory, _config.OutputType == "PDF_GAWEB", _config.SyncfusionLicenseKey ?? "", _config.PdfLibrary);
        if (prop.ShowDialog() == DialogResult.OK)
        {
            _config.OutputDirectory = prop.OutputPath;
            _config.SyncfusionLicenseKey = prop.SyncfusionKey;
            _config.PdfLibrary = prop.PdfLibrary;
            if (prop.GawebMode) 
                _config.OutputType = "PDF_GAWEB";
            else if (_config.OutputType == "PDF_GAWEB")
                _config.OutputType = "PDF"; // Fallback if GAWEB was unchecked

            MarkDirty();
            Log($"Configuración actualizada.");
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

        var dataReader = new DataReaderService();
        using var previewForm = new Forms.DataPreviewForm(_config.DataFilePath, dataReader);
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

        var dataReader = new DataReaderService();
        using var mappingForm = new Forms.MappingForm(_config.TemplatePath, _config.DataFilePath, _config.VariableMapping, formOverrides, dataReader);
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
            // Register Syncfusion License if present
            if (!string.IsNullOrWhiteSpace(_config.SyncfusionLicenseKey))
            {
                ConfigService.RegisterLicense(_config.SyncfusionLicenseKey);
            }

            // Choose PDF service based on config
            IPdfService pdfService = _config.PdfLibrary == "Word" 
                ? new PdfService() 
                : new SyncfusionPdfService();

            // Re-instantiate engine with current services (Poor man's DI/Service Locator)
            var engine = new GenerationEngine(_configService, pdfService, new TemplateService(), new GawebService(), new DataReaderService());

            await Task.Run(() => engine.Run(_config, this, _cts.Token), _cts.Token);
            MessageBox.Show("Generación completada.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            Log("Generación cancelada por el usuario.");
        }
        catch (Exception ex)
        {
            Log($"ERROR CRÍTICO: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    // === IGenerationProgress Implementation ===

    public void ReportProgress(int current, int total, string message)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => ReportProgress(current, total, message)));
            return;
        }
        progressBar.Style = ProgressBarStyle.Blocks;
        progressBar.Value = (int)((float)current / total * 100);
        lblStatus.Text = message;
    }

    public void ReportLog(string message)
    {
        InvokeLog(message);
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        _cts?.Cancel();
    }

    // === GENERATION LOGIC MOVED TO GenerationEngine ===

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
