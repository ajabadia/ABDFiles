using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using EtlConfig.Controls;
using EtlConfig.Models;
using System.ComponentModel;
using ABDTools.Core.Configuration;
using EtlConfig.Services;

namespace EtlConfig.Forms;

public class MainForm : Form
{
    private MenuStrip menuStrip;
    private SplitContainer splitEditor;
    
    // Global Panels (Groups)
    private GroupBox grpMeta;
    private GroupBox grpTech;
    
    // Global Controls
    private TextBox txtName;
    private TextBox txtVersion;
    private CheckBox chkActive;
    private TextBox txtChunkSize;
    private ComboBox cmbEncoding;
    private TextBox txtRecStart;
    private TextBox txtRecLen;
    private TextBox txtRecDefault;
    private TextBox txtHeaderID;

    // List Editor Controls (Left)
    // private BindingSource _recordTypesBinding; // Removed
    private ListBox lstRecordTypes;
    private Button btnAddType;
    private Button btnDelType;

    // Properties Panel (New)
    private GroupBox grpTypeProps;
    private TextBox txtTypeName;
    private ComboBox cmbTypeBehavior;
    private TextBox txtTypeTrigger;
    private TextBox txtTypeTriggerStart;
    private Button btnHelpWildcard; 
    private TextBox txtTypeRange;

    // Field Editor Controls (Right - Legacy Style)
    private FlowLayoutPanel pnlFieldsContainer; 
    private Button btnAddField;
    
    // Tools
    private Button btnUndo;
    private Button btnCompact;
    private Button btnSort;

    private EtlPreset _currentPreset;
    private Stack<string> _undoStack = new Stack<string>();
    private string _currentFilePath = string.Empty;
    private bool _ignoreChanges = false;

    private readonly ConfigManager<AppConfig> _configManager;
    private readonly EtlPresetStorage _presetStorage;
    private AppConfig _runtimeConfig = new AppConfig(); // defaults

    public MainForm()
    {
        _configManager = new ConfigManager<AppConfig>("EtlConfig");
        _presetStorage = new EtlPresetStorage();

        InitializeComponent();
        InitializeMenu();
        CreateNewPreset();
        
        this.Shown += async (s, e) => await LoadConfigAsync();
    }

    private async Task LoadConfigAsync()
    {
        var res = await _configManager.LoadAsync();
        if (res.IsCorrupted)
        {
             MessageBox.Show($"Configuración corrupta. Se han restaurado los valores por defecto.\n{res.Error?.Message}", "Error Configuración", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        if (res.Exists || res.IsCorrupted) // If exists valid OR corrupted (restored defaults), use value
        {
            _runtimeConfig = res.Value;
        }
    }

    private void InitializeComponent()
    {
        this.Text = "ETL Configurator - ABDFN";
        this.Size = new Size(1100, 750);
        this.DoubleBuffered = true;
        menuStrip = new MenuStrip();
        this.MainMenuStrip = menuStrip;

        var mainLayout = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        this.Controls.Add(mainLayout);

        // --- GLOBAL SETTINGS (GROUPED) ---
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 140 };
        
        // Metadata Group
        grpMeta = new GroupBox { Text = "Metadatos", Location = new Point(0, 0), Size = new Size(350, 130) };
        AddLabel(grpMeta, "Nombre:", 10, 25);
        txtName = new TextBox { Location = new Point(70, 22), Width = 250 };
        AddLabel(grpMeta, "Versión:", 10, 55);
        txtVersion = new TextBox { Location = new Point(70, 52), Width = 80 };
        chkActive = new CheckBox { Text = "Activo", Location = new Point(170, 52), AutoSize = true };
        grpMeta.Controls.AddRange(new Control[] { txtName, txtVersion, chkActive });

        // Tech Group
        grpTech = new GroupBox { Text = "Configuración Técnica", Location = new Point(360, 0), Size = new Size(700, 130) };
        AddLabel(grpTech, "Max Filas:", 10, 25);
        txtChunkSize = new TextBox { Location = new Point(80, 22), Width = 100 };
        AddLabel(grpTech, "Encoding:", 200, 25);
        cmbEncoding = new ComboBox { Location = new Point(270, 22), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbEncoding.Items.AddRange(new object[] { "utf-8", "latin1", "windows-1252" });
        AddLabel(grpTech, "Pos Tipo:", 400, 25);
        txtRecStart = new TextBox { Location = new Point(470, 22), Width = 60 };
        AddLabel(grpTech, "Len Tipo:", 550, 25);
        txtRecLen = new TextBox { Location = new Point(610, 22), Width = 60 };
        AddLabel(grpTech, "Tipo Def:", 10, 60);
        txtRecDefault = new TextBox { Location = new Point(80, 58), Width = 100 };
        AddLabel(grpTech, "Header ID:", 200, 60);
        txtHeaderID = new TextBox { Location = new Point(270, 58), Width = 100 };
        btnUndo = new Button { Text = "Deshacer", Location = new Point(600, 90), Width = 80, BackColor = Color.LightYellow };
        grpTech.Controls.Add(btnUndo);
        grpTech.Controls.AddRange(new Control[] { txtChunkSize, cmbEncoding, txtRecStart, txtRecLen, txtRecDefault, txtHeaderID });

        pnlTop.Controls.Add(grpMeta);
        pnlTop.Controls.Add(grpTech);
        mainLayout.Controls.Add(pnlTop);

        // --- SPLIT CONTAINER ---
        splitEditor = new SplitContainer 
        { 
            Dock = DockStyle.Fill, 
            Orientation = Orientation.Vertical, 
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = 350, // Force minimum width
            SplitterDistance = 400 
        };
        mainLayout.Controls.Add(splitEditor);
        splitEditor.BringToFront(); 

        // LEFT PANEL (Types)
        var pnlLeftTop = new Panel { Dock = DockStyle.Top, Height = 60 };
        var lblTypes = new Label { Text = "Tipos de Registro", Location = new Point(5, 5), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };
        btnAddType = new Button { Text = "Nuevo", Width = 70, Location = new Point(5, 25) };
        btnDelType = new Button { Text = "Eliminar", Width = 90, Location = new Point(80, 25) };
        pnlLeftTop.Controls.AddRange(new Control[] { lblTypes, btnAddType, btnDelType });
        splitEditor.Panel1.Controls.Add(pnlLeftTop);
        
        lstRecordTypes = new ListBox { Dock = DockStyle.Fill, DisplayMember = "DisplayString" };
        splitEditor.Panel1.Controls.Add(lstRecordTypes);
        
        // Z-Order correction: 
        // Dock=Top must be docked FIRST -> SendToBack() (Highest Index)
        // Dock=Fill must be docked LAST -> BringToFront() (Lowest Index/0)
        pnlLeftTop.SendToBack(); 
        lstRecordTypes.BringToFront(); 
        
        // Re-force splitter distance just in case
        splitEditor.SplitterDistance = 400;
        // RIGHT PANEL (Properties + Fields)
        
        // --- 1. Type Properties Group ---
        grpTypeProps = new GroupBox { Text = "Propiedades del Tipo Seleccionado", Dock = DockStyle.Top, Height = 65, Enabled = false };
        var propsInfo = new Label { Text = "Nombre:", Location = new Point(10, 25), AutoSize=true };
        txtTypeName = new TextBox { Location = new Point(60, 22), Width = 140 };
        
        var propsRol = new Label { Text = "Rol:", Location = new Point(205, 25), AutoSize=true };
        cmbTypeBehavior = new ComboBox { Location=new Point(230, 22), Width = 70, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbTypeBehavior.Items.AddRange(new object[] { "DATA", "HEADER", "FOOTER" });
        
        var propsTrig = new Label { Text = "Trigger:", Location = new Point(310, 25), AutoSize=true };
        txtTypeTrigger = new TextBox { Location = new Point(355, 22), Width = 50 };
        
        var propsTrigStart = new Label { Text = "Pos:", Location = new Point(410, 25), AutoSize=true };
        txtTypeTriggerStart = new TextBox { Location = new Point(440, 22), Width = 30, Text = "0" };

        btnHelpWildcard = new Button { Text = "?", Location = new Point(475, 21), Width = 22, Height = 23, BackColor = Color.LightYellow, FlatStyle = FlatStyle.Flat };
        
        var propsRng = new Label { Text = "Rango/Filas:", Location = new Point(505, 25), AutoSize=true };
        txtTypeRange = new TextBox { Location = new Point(550, 22), Width = 60 };
        
        grpTypeProps.Controls.AddRange(new Control[] { propsInfo, txtTypeName, propsRol, cmbTypeBehavior, propsTrig, txtTypeTrigger, propsTrigStart, txtTypeTriggerStart, btnHelpWildcard, propsRng, txtTypeRange });
        splitEditor.Panel2.Controls.Add(grpTypeProps);

        // --- 2. Toolbar ---
        var pnlRightTop = new Panel { Dock = DockStyle.Top, Height = 40 };
        btnAddField = new Button { Text = "    Añadir", Location = new Point(5, 5), Width = 100, BackColor = Color.LightGreen, Image = null, TextAlign = ContentAlignment.MiddleLeft }; 
        btnSort = new Button { Text = "Reordenar", Location = new Point(115, 5), Width = 80 };
        btnCompact = new Button { Text = "Compactar", Location = new Point(205, 5), Width = 80 };
        pnlRightTop.Controls.AddRange(new Control[] { btnAddField, btnSort, btnCompact });
        splitEditor.Panel2.Controls.Add(pnlRightTop);

        // --- 3. Header ---
        var pnlFieldHeader = new TableLayoutPanel 
        { 
            Dock = DockStyle.Top, 
            Height = 35, 
            ColumnCount = 4,
            BackColor = Color.LightGray,
            Padding = new Padding(10, 5, 10, 0)
        };
        pnlFieldHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
        pnlFieldHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        pnlFieldHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        pnlFieldHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        pnlFieldHeader.Controls.Add(new Label { Text = "Nombre del Campo", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Bold) }, 0, 0);
        pnlFieldHeader.Controls.Add(new Label { Text = "Inicio", AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Bold) }, 1, 0);
        pnlFieldHeader.Controls.Add(new Label { Text = "Longitud", AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Bold) }, 2, 0);
        pnlFieldHeader.Controls.Add(new Label { Text = "Borrar", AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font(this.Font, FontStyle.Bold) }, 3, 0);
        splitEditor.Panel2.Controls.Add(pnlFieldHeader);

        // --- 4. Fields ---
        pnlFieldsContainer = new FlowLayoutPanel 
        { 
            Dock = DockStyle.Fill, 
            FlowDirection = FlowDirection.TopDown, 
            WrapContents = false, 
            AutoScroll = true,
            Padding = new Padding(10),
            BackColor = Color.White
        };
        splitEditor.Panel2.Controls.Add(pnlFieldsContainer);
        pnlFieldsContainer.BringToFront(); 

        // Wiring
        btnAddType.Click += (s, e) => AddRecordType();
        btnDelType.Click += (s, e) => DeleteRecordType();
        lstRecordTypes.SelectedIndexChanged += (s, e) => { if(!_ignoreChanges) LoadSelectedTypeFields(); };
        
        btnAddField.Click += (s, e) => AddField();
        btnSort.Click += (s, e) => { Snapshot(); SortFields(); };
        btnCompact.Click += (s, e) => { Snapshot(); CompactFields(); };
        btnUndo.Click += (s, e) => PerformUndo();

        // Property Changed events
        txtTypeName.TextChanged += (s, e) => { if(!_ignoreChanges) UpdateSelectedType(t => t.Name = txtTypeName.Text); };
        txtTypeTrigger.TextChanged += (s, e) => { if(!_ignoreChanges) UpdateSelectedType(t => t.Trigger = txtTypeTrigger.Text); };
        txtTypeTriggerStart.TextChanged += (s, e) => { if(!_ignoreChanges && int.TryParse(txtTypeTriggerStart.Text, out int v)) UpdateSelectedType(t => t.TriggerStart = v); };
        txtTypeRange.TextChanged += (s, e) => { if(!_ignoreChanges) UpdateSelectedType(t => t.Range = txtTypeRange.Text); };
        cmbTypeBehavior.SelectedIndexChanged += (s,e) => { if(!_ignoreChanges) UpdateSelectedType(t => t.Behavior = cmbTypeBehavior.Text); };
        
        btnHelpWildcard.Click += (s, e) => MessageBox.Show(
            "Ayuda de Triggers:\n\n" +
            "• Texto normal: Busca coincidencia exacta (ej: '01', 'ABC').\n" +
            "• '?' : Comodín para CUALQUIER carácter.\n" +
            "• '*' : Comodín para ESPACIO en BLANCO.\n\n" +
            "Ejemplo: '?BC' coincidirá con 'ABC', 'XBC', etc.\n" +
            "Ejemplo: '*BC' coincidirá sólo con ' BC'.", 
            "Ayuda", MessageBoxButtons.OK, MessageBoxIcon.Information);

        pnlFieldsContainer.SizeChanged += (s, e) => 
        {
            pnlFieldsContainer.SuspendLayout();
            foreach(Control c in pnlFieldsContainer.Controls) c.Width = pnlFieldsContainer.ClientSize.Width - 30;
            pnlFieldsContainer.ResumeLayout();
        };

        // Global
        txtName.TextChanged += (s, e) => { if (!_ignoreChanges) _currentPreset.DisplayName = txtName.Text; };
        txtVersion.TextChanged += (s, e) => { if (!_ignoreChanges) _currentPreset.Version = txtVersion.Text; };
        chkActive.CheckedChanged += (s, e) => { if (!_ignoreChanges) _currentPreset.IsActive = chkActive.Checked; };
        
        BindGlobalEvents();
        this.Controls.Add(this.MainMenuStrip);
    }

    private void BindGlobalEvents()
    {
         txtChunkSize.TextChanged += (s,e) => { if(!_ignoreChanges && int.TryParse(txtChunkSize.Text, out int v)) _currentPreset.ChunkSize = v; };
         cmbEncoding.SelectedIndexChanged += (s,e) => { if(!_ignoreChanges) _currentPreset.Encoding = cmbEncoding.Text; };
         txtRecStart.TextChanged += (s,e) => { if(!_ignoreChanges && int.TryParse(txtRecStart.Text, out int v)) _currentPreset.RecordTypeStart = v; };
         txtRecLen.TextChanged += (s,e) => { if(!_ignoreChanges && int.TryParse(txtRecLen.Text, out int v)) _currentPreset.RecordTypeLen = v; };
         txtRecDefault.TextChanged += (s,e) => { if(!_ignoreChanges) _currentPreset.DefaultRecordType = txtRecDefault.Text; };
         txtHeaderID.TextChanged += (s,e) => { if(!_ignoreChanges) _currentPreset.HeaderTypeID = txtHeaderID.Text; };
    }

    private void InitializeMenu()
    {
        var fileMenu = new ToolStripMenuItem("Archivo");
        fileMenu.DropDownItems.Add("Nuevo", null, (s, e) => CreateNewPreset());
        fileMenu.DropDownItems.Add("Abrir...", null, (s, e) => OpenPreset());
        fileMenu.DropDownItems.Add("Guardar", null, (s, e) => SavePreset(false));
        fileMenu.DropDownItems.Add("Guardar Como...", null, (s, e) => SavePreset(true));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Salir", null, (s, e) => Application.Exit());

        var editMenu = new ToolStripMenuItem("Editar");
        editMenu.DropDownItems.Add("Preferencias", null, (s, e) => new PreferencesForm(_runtimeConfig, _configManager).ShowDialog());

        var helpMenu = new ToolStripMenuItem("Ayuda");
        helpMenu.DropDownItems.Add("Acerca de...", null, (s, e) => new AboutForm().ShowDialog());

        menuStrip.Items.Add(fileMenu);
        menuStrip.Items.Add(editMenu);
        menuStrip.Items.Add(helpMenu);
    }

    // --- LOGIC ---
    private void RefreshRecordTypesList()
    {
        // Manual ListBox management for stability
        var selected = lstRecordTypes.SelectedItem as EtlRecordType;
        lstRecordTypes.BeginUpdate();
        lstRecordTypes.Items.Clear();
        
        if (_currentPreset.RecordTypes == null) _currentPreset.RecordTypes = new List<EtlRecordType>();
        
        foreach(var t in _currentPreset.RecordTypes)
        {
            lstRecordTypes.Items.Add(t);
        }
        
        if (selected != null && _currentPreset.RecordTypes.Contains(selected))
            lstRecordTypes.SelectedItem = selected;
        else if (lstRecordTypes.Items.Count > 0)
            lstRecordTypes.SelectedIndex = 0;
            
        lstRecordTypes.EndUpdate();
            
        if (lstRecordTypes.Items.Count == 0)
        {
            grpTypeProps.Enabled = false;
            pnlFieldsContainer.Controls.Clear();
        }
    }
    
    private void AddRecordType()
    {
        Snapshot();
        string input = ShowInputDialog("Trigger (o deje vacío):", "Nuevo Tipo");
        var newType = new EtlRecordType 
        { 
             Name = "Nuevo Tipo " + input, 
             Trigger = input, 
             Behavior = "DATA" 
        };
        _currentPreset.RecordTypes.Add(newType);
        RefreshRecordTypesList();
        lstRecordTypes.SelectedItem = newType;
    }

    private void DeleteRecordType()
    {
        if (lstRecordTypes.SelectedItem == null) return;
        var type = (EtlRecordType)lstRecordTypes.SelectedItem;
        if(MessageBox.Show($"¿Eliminar '{type.Name}'?", "Confirmar", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            Snapshot();
            _currentPreset.RecordTypes.Remove(type);
            RefreshRecordTypesList();
        }
    }

    private void UpdateSelectedType(Action<EtlRecordType> action)
    {
        if (lstRecordTypes.SelectedItem == null) return;
        var t = (EtlRecordType)lstRecordTypes.SelectedItem;
        action(t);
        // Refresh text
        int idx = lstRecordTypes.SelectedIndex;
        if(idx >= 0) 
        {
            lstRecordTypes.Items[idx] = lstRecordTypes.Items[idx]; 
        }
    }

    private void CreateNewPreset()
    {
        _currentPreset = new EtlPreset();
        // Use runtime config
        _currentPreset.ChunkSize = _runtimeConfig.DefaultChunkSize;
        _currentPreset.Encoding = _runtimeConfig.DefaultEncoding;
        
        // Default Type
        _currentPreset.RecordTypes.Add(new EtlRecordType 
        { 
            Name = "Registro Datos", 
            Trigger = "01",
            Behavior = "DATA"
        });

        _currentFilePath = string.Empty;
        BindToUI();
    }

    private void BindToUI()
    {
        _ignoreChanges = true;
        txtName.Text = _currentPreset.DisplayName;
        txtVersion.Text = _currentPreset.Version;
        chkActive.Checked = _currentPreset.IsActive;
        txtChunkSize.Text = _currentPreset.ChunkSize.ToString();
        cmbEncoding.SelectedItem = _currentPreset.Encoding ?? "utf-8";
        txtRecStart.Text = _currentPreset.RecordTypeStart.ToString();
        txtRecLen.Text = _currentPreset.RecordTypeLen.ToString();
        txtRecDefault.Text = _currentPreset.DefaultRecordType;
        txtHeaderID.Text = _currentPreset.HeaderTypeID;
        _ignoreChanges = false;
        RefreshRecordTypesList(); // Will populate manual list
    }
    
    private void LoadSelectedTypeFields()
    {
        pnlFieldsContainer.SuspendLayout();
        pnlFieldsContainer.Controls.Clear();
        
        if (lstRecordTypes.SelectedItem == null) 
        {
            grpTypeProps.Enabled = false;
            pnlFieldsContainer.ResumeLayout();
            return;
        }

        grpTypeProps.Enabled = true;

        var type = (EtlRecordType)lstRecordTypes.SelectedItem;

        bool old = _ignoreChanges;
        _ignoreChanges = true;
        txtTypeName.Text = type.Name;
        txtTypeTrigger.Text = type.Trigger;
        txtTypeTriggerStart.Text = type.TriggerStart.ToString();
        cmbTypeBehavior.SelectedItem = type.Behavior ?? "DATA";
        txtTypeRange.Text = type.Range;
        _ignoreChanges = old;

        var sortedFields = type.Fields.OrderBy(f => f.Start).ToList();
        int currentPos = 0;
        int w = pnlFieldsContainer.ClientSize.Width - 30;

        foreach (var field in sortedFields)
        {
             if (field.Start > currentPos)
             {
                 int gap = field.Start - currentPos;
                 var lblGap = new Label { 
                    Text = $"⚠️ HUECO: {gap}", ForeColor = Color.Orange, Width = w, Height = 25,
                    TextAlign = ContentAlignment.MiddleCenter, Font = new Font(this.Font, FontStyle.Bold), BackColor=Color.Bisque
                 };
                 pnlFieldsContainer.Controls.Add(lblGap);
             }
             else if (field.Start < currentPos)
             {
                 int ov = currentPos - field.Start;
                 var lblErr = new Label { 
                    Text = $"⛔ PISA: {ov}", ForeColor = Color.White, BackColor=Color.Red, Width = w, Height = 25,
                    TextAlign = ContentAlignment.MiddleCenter, Font = new Font(this.Font, FontStyle.Bold)
                 };
                 pnlFieldsContainer.Controls.Add(lblErr);
             }

             var ctrl = new EtlConfig.Controls.FieldControl(field);
             ctrl.Width = w;
             ctrl.DeleteRequested += (s,e) => RemoveField(field);
             pnlFieldsContainer.Controls.Add(ctrl);

             currentPos = field.Start + field.Length;
        }

        pnlFieldsContainer.ResumeLayout();
    }

    private void AddField()
    {
         if (lstRecordTypes.SelectedItem == null) return;
         Snapshot();
         var type = (EtlRecordType)lstRecordTypes.SelectedItem;
         int start = 0;
         if (type.Fields.Count > 0)
         {
             var last = type.Fields.OrderBy(f => f.Start).Last();
             start = last.Start + last.Length;
         }
         type.Fields.Add(new EtlField { Name = "NUEVO", Start = start, Length = 10 });
         LoadSelectedTypeFields();
    }

    private void RemoveField(EtlField f)
    {
         if (lstRecordTypes.SelectedItem == null) return;
         Snapshot();
         var type = (EtlRecordType)lstRecordTypes.SelectedItem;
         type.Fields.Remove(f);
         LoadSelectedTypeFields();
    }

    private void SortFields()
    {
         if (lstRecordTypes.SelectedItem == null) return;
         var type = (EtlRecordType)lstRecordTypes.SelectedItem;
         type.Fields = type.Fields.OrderBy(f => f.Start).ToList();
         LoadSelectedTypeFields();
    }
    
    private void CompactFields()
    {
         if (lstRecordTypes.SelectedItem == null) return;
         var type = (EtlRecordType)lstRecordTypes.SelectedItem;
         var list = type.Fields.OrderBy(f => f.Start).ToList();
         int p = 0;
         foreach(var f in list) { f.Start = p; p += f.Length; }
         type.Fields = list;
         LoadSelectedTypeFields();
    }
    
    // --- Persistence ---
    private void Snapshot()
    {
        try {
            string json = JsonSerializer.Serialize(_currentPreset);
            _undoStack.Push(json);
            if (_undoStack.Count > 50) 
            {
                var list = _undoStack.ToList();
                list.RemoveAt(list.Count - 1); // Remove oldest
                // Stack enumeration is usually LIFO (Top to Bottom). 
                // ToList preserves order? YES. 
                // Rebuilding stack is messy. 
                // Optimization: Just clear if huge or ignore limit for now.
                // Or simplified:
                // This is a naive stack implementation.
            }
        } catch {}
    }

    private void PerformUndo()
    {
        try
        {
            if (_undoStack.Count == 0) return;
            string json = _undoStack.Pop();
            var restored = JsonSerializer.Deserialize<EtlPreset>(json);
            if (restored != null)
            {
                _currentPreset = restored;
                BindToUI();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al deshacer: " + ex.Message);
            _undoStack.Clear(); // Clear corrupted stack
        }
    }

    private void SavePreset(bool forceDialog)
    {
        if (string.IsNullOrEmpty(_currentFilePath) || forceDialog)
        {
            using var sfd = new SaveFileDialog { Filter = "JSON|*.json", InitialDirectory = _runtimeConfig.DefaultSavePath };
            if (sfd.ShowDialog() == DialogResult.OK) _currentFilePath = sfd.FileName;
            else return;
        }

        try
        {
            _presetStorage.SaveToFile(_currentPreset, _currentFilePath);
            MessageBox.Show("Guardado.");
            
            // Update config default path
            _runtimeConfig.DefaultSavePath = Path.GetDirectoryName(_currentFilePath) ?? _runtimeConfig.DefaultSavePath;
            _ = _configManager.SaveAsync(_runtimeConfig);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    
    private void OpenPreset()
    {
        using var ofd = new OpenFileDialog { Filter = "JSON|*.json", InitialDirectory = _runtimeConfig.DefaultSavePath };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            try 
            {
                _currentFilePath = ofd.FileName;
                _currentPreset = _presetStorage.LoadFromFile(_currentFilePath);
                BindToUI();

                // Update config default path
                _runtimeConfig.DefaultSavePath = Path.GetDirectoryName(_currentFilePath) ?? _runtimeConfig.DefaultSavePath;
                _ = _configManager.SaveAsync(_runtimeConfig);
            } 
            catch(Exception ex) 
            { 
                MessageBox.Show("Error al abrir: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); 
            }
        }
    }

    private string ShowInputDialog(string text, string caption)
    {
        Form prompt = new Form()
        {
            Width = 300, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = caption, StartPosition = FormStartPosition.CenterParent
        };
        Label lbl = new Label() { Left = 20, Top = 20, Text = text, AutoSize = true };
        TextBox tb = new TextBox() { Left = 20, Top = 50, Width = 240 };
        Button btn = new Button() { Text = "Ok", Left = 180, Width = 80, Top = 80, DialogResult = DialogResult.OK };
        btn.Click += (s, e) => prompt.Close();
        prompt.Controls.Add(tb); prompt.Controls.Add(btn); prompt.Controls.Add(lbl);
        prompt.AcceptButton = btn;
        return prompt.ShowDialog() == DialogResult.OK ? tb.Text : "";
    }

    private void AddLabel(Control p, string t, int x, int y)
    {
        p.Controls.Add(new Label { Text = t, Location = new Point(x,y), AutoSize = true });
    }
}
