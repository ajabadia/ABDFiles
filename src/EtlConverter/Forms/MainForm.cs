using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using EtlConverter.Models;
using EtlConverter.Services;
using ABDTools.Core.Common;

namespace EtlConverter.Forms;

public class MainForm : Form
{
    // Controls
    private TextBox txtInputFile;
    private Button btnSelectInput;
    private Label lblLinesDetected;
    
    private TextBox txtOutputDir;
    private Button btnSelectOutput;
    
    private ComboBox cmbPresets;
    private Button btnRefreshPresets;
    
    private ComboBox cmbFormat;
    private TextBox txtChunkSize;
    private TextBox txtStartRow;
    private TextBox txtEndRow;
    
    private Button btnProcess;
    private ProgressBar progressBar;
    private TextBox txtLog;
    
    // Logic
    private Dictionary<string, string> _presetsPaths = new();
    private EtlPreset? _selectedPreset;
    private CancellationTokenSource? _cts;
    
    public MainForm()
    {
        InitializeComponent();
        LoadPresets();
        
        txtOutputDir.Text = Directory.GetCurrentDirectory();
    }
    
    private void InitializeComponent()
    {
        this.Text = "ETL Converter - ABDTools";
        this.Size = new Size(800, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); // Try to get icon

        var mainLayout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), RowCount = 5 };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100)); // Files
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));  // Preset
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));  // Options
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Process Btn
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // Log
        
        // --- 1. Files ---
        var grpFiles = new GroupBox { Text = "1. Archivos", Dock = DockStyle.Fill };
        var fileLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 3 };
        fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
        
        txtInputFile = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
        btnSelectInput = new Button { Text = "...", Width = 30 };
        btnSelectInput.Click += (s, e) => SelectInput();
        
        txtOutputDir = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
        btnSelectOutput = new Button { Text = "...", Width = 30 };
        btnSelectOutput.Click += (s, e) => SelectOutput();
        
        fileLayout.Controls.Add(new Label { Text = "Entrada (TXT):", AutoSize = true, Anchor = AnchorStyles.Right }, 0, 0);
        fileLayout.Controls.Add(txtInputFile, 1, 0);
        fileLayout.Controls.Add(btnSelectInput, 2, 0);
        
        fileLayout.Controls.Add(new Label { Text = "Salida:", AutoSize = true, Anchor = AnchorStyles.Right }, 0, 1);
        fileLayout.Controls.Add(txtOutputDir, 1, 1);
        fileLayout.Controls.Add(btnSelectOutput, 2, 1);
        
        grpFiles.Controls.Add(fileLayout);
        mainLayout.Controls.Add(grpFiles, 0, 0);

        // --- 2. Preset ---
        var pnlPreset = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
        pnlPreset.Controls.Add(new Label { Text = "2. Configuración:", AutoSize = true, Padding = new Padding(0,6,0,0), Font = new Font(this.Font, FontStyle.Bold) });
        cmbPresets = new ComboBox { Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
        cmbPresets.SelectedIndexChanged += (s,e) => LoadPresetDetails();
        btnRefreshPresets = new Button { Text = "↻" , Width = 30};
        btnRefreshPresets.Click += (s,e) => LoadPresets();
        
        pnlPreset.Controls.Add(cmbPresets);
        pnlPreset.Controls.Add(btnRefreshPresets);
        mainLayout.Controls.Add(pnlPreset, 0, 1);

        // --- 3. Options ---
        var grpOpts = new GroupBox { Text = "3. Opciones", Dock = DockStyle.Fill };
        var optFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        
        cmbFormat = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
        cmbFormat.Items.AddRange(new object[] { "CSV (Excel)", "JSON" });
        cmbFormat.SelectedIndex = 0;
        
        txtChunkSize = new TextBox { Text = "900000", Width = 80 };
        txtStartRow = new TextBox { Text = "1", Width = 60 };
        txtEndRow = new TextBox { Text = "0", Width = 60 };
        
        optFlow.Controls.Add(new Label { Text = "Formato:", AutoSize = true, Padding = new Padding(0,5,0,0) });
        optFlow.Controls.Add(cmbFormat);
        optFlow.Controls.Add(new Label { Text = "  Max Filas:", AutoSize = true, Padding = new Padding(0,5,0,0) });
        optFlow.Controls.Add(txtChunkSize);
        optFlow.Controls.Add(new Label { Text = "  Fila Ini:", AutoSize = true, Padding = new Padding(0,5,0,0) });
        optFlow.Controls.Add(txtStartRow);
        optFlow.Controls.Add(new Label { Text = "  Fila Fin:", AutoSize = true, Padding = new Padding(0,5,0,0) });
        optFlow.Controls.Add(txtEndRow);
        
        grpOpts.Controls.Add(optFlow);
        mainLayout.Controls.Add(grpOpts, 0, 2);

        // --- 4. Process ---
        btnProcess = new Button { Text = "PROCESAR FICHEROS", Dock = DockStyle.Fill, BackColor = Color.LightBlue, Height = 40, Font = new Font(this.Font, FontStyle.Bold) };
        btnProcess.Click += (s,e) => ToggleProcessing();
        mainLayout.Controls.Add(btnProcess, 0, 3);

        // --- 5. Log ---
        var pnlLog = new Panel { Dock = DockStyle.Fill };
        progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 20 };
        txtLog = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.Black, ForeColor = Color.Lime, Font = new Font("Consolas", 9) };
        pnlLog.Controls.Add(txtLog);
        pnlLog.Controls.Add(progressBar);
        mainLayout.Controls.Add(pnlLog, 0, 4);

        this.Controls.Add(mainLayout);
    }
    
    private void Log(string msg)
    {
        if (InvokeRequired) Invoke(() => Log(msg));
        else 
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\r\n");
            // Scroll to end?
        }
    }

    private void SelectInput()
    {
        using var ofd = new OpenFileDialog { Filter = "Archivos de texto (*.txt)|*.txt|Todos (*.*)|*.*" };
        if(ofd.ShowDialog() == DialogResult.OK)
        {
            txtInputFile.Text = ofd.FileName;
            Log($"Seleccionado: {Path.GetFileName(ofd.FileName)}");
        }
    }

    private void SelectOutput()
    {
        using var fbd = new FolderBrowserDialog();
        if(fbd.ShowDialog() == DialogResult.OK)
        {
            txtOutputDir.Text = fbd.SelectedPath;
        }
    }

    private void LoadPresets()
    {
        cmbPresets.Items.Clear();
        _presetsPaths.Clear();
        
        string presetsDir = Path.Combine(Directory.GetCurrentDirectory(), "presets", "ETL");
        if(Directory.Exists(presetsDir))
        {
            foreach(var f in Directory.GetFiles(presetsDir, "*.json"))
            {
                try {
                    string json = File.ReadAllText(f);
                    var p = JsonSerializer.Deserialize<EtlPreset>(json);
                    if(p != null && p.IsActive)
                    {
                        string name = $"{p.DisplayName} (v{p.Version})";
                        cmbPresets.Items.Add(name);
                        _presetsPaths[name] = f;
                    }
                } catch {}
            }
        }
        
        if(cmbPresets.Items.Count > 0) cmbPresets.SelectedIndex = 0;
        else Log("No se encontraron presets en presets/ETL.");
    }
    
    private void LoadPresetDetails()
    {
        if(cmbPresets.SelectedItem == null) return;
        string key = cmbPresets.SelectedItem.ToString()!;
        if(_presetsPaths.TryGetValue(key, out string? path))
        {
             try {
                string json = File.ReadAllText(path);
                _selectedPreset = JsonSerializer.Deserialize<EtlPreset>(json);
                if(_selectedPreset != null)
                {
                    txtChunkSize.Text = _selectedPreset.ChunkSize.ToString();
                    Log($"Cargado preset: {_selectedPreset.DisplayName}");
                }
             } catch(Exception ex) { Log("Error cargando preset: " + ex.Message); }
        }
    }

    private async void ToggleProcessing()
    {
        if(_cts != null) // Is Running -> Cancel
        {
            _cts.Cancel();
            btnProcess.Text = "CANCELANDO...";
            btnProcess.Enabled = false;
            return;
        }

        // Start
        if(string.IsNullOrEmpty(txtInputFile.Text) || !File.Exists(txtInputFile.Text)) { MessageBox.Show("Seleccione archivo de entrada."); return; }
        if(_selectedPreset == null) { MessageBox.Show("Seleccione preset."); return; }
        
        txtLog.Clear();
        progressBar.Value = 0;
        btnProcess.Text = "CANCELAR PROCESO";
        btnProcess.BackColor = Color.Salmon;
        _cts = new CancellationTokenSource();

        try
        {
            var options = new ProcessorOptions
            {
                InputPath = txtInputFile.Text,
                OutputDir = txtOutputDir.Text,
                Preset = _selectedPreset,
                OutputJson = cmbFormat.SelectedIndex == 1,
                ChunkSize = int.TryParse(txtChunkSize.Text, out int c) ? c : 900000,
                StartRow = int.TryParse(txtStartRow.Text, out int s) ? s : 1,
                EndRow = int.TryParse(txtEndRow.Text, out int e) ? e : 0
            };

            var svc = new ProcessorService();
            svc.OnLog += (s, msg) => Log(msg);
            svc.OnProgress += (s, lines) => Invoke(() => { /* simple indeterminate or count */ });

            await svc.ProcessAsync(options, _cts.Token);
            
            MessageBox.Show("Proceso finalizado correctamente.");
        }
        catch(OperationCanceledException)
        {
            Log("Proceso cancelado por el usuario.");
            MessageBox.Show("Proceso cancelado.");
        }
        catch(Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            MessageBox.Show($"Error: {ex.Message}");
        }
        finally
        {
            _cts = null;
            btnProcess.Text = "PROCESAR FICHEROS";
            btnProcess.BackColor = Color.LightBlue;
            btnProcess.Enabled = true;
        }
    }
}
