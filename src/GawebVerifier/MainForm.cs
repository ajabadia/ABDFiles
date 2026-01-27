using System;
using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using ABDTools.Core.Gaweb;
using ABDTools.Core.Gaweb.Models;

namespace GawebVerifier;

public class MainForm : Form
{
    private TextBox txtFilePath;
    private Button btnValidate;
    private DataGridView gridErrors;
    private DataGridView gridData;
    private TabControl tabControl;
    private ToolStripStatusLabel lblStatus;
    private Label lblSummary;
    private string _defaultExportPath = string.Empty;

    public MainForm()
    {
        this.Text = "Verificador GAWEB - v1.0";
        this.Size = new Size(1000, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        
        // Initialize Default Export Path to MyDocuments
        _defaultExportPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        InitializeControls();
    }

    private void InitializeControls()
    {
        // 1. Menu Strip
        MenuStrip menuStrip = new MenuStrip();
        
        // Archivo
        ToolStripMenuItem menuFile = new ToolStripMenuItem("Archivo");
        ToolStripMenuItem itemOpen = new ToolStripMenuItem("Abrir...", null, MnuOpen_Click, Keys.Control | Keys.O);
        ToolStripMenuItem itemExport = new ToolStripMenuItem("Exportar CSV...", null, MnuExport_Click, Keys.Control | Keys.E);
        ToolStripMenuItem itemExit = new ToolStripMenuItem("Salir", null, (s, e) => this.Close(), Keys.Alt | Keys.F4);
        menuFile.DropDownItems.AddRange(new ToolStripItem[] { itemOpen, itemExport, new ToolStripSeparator(), itemExit });

        // Edición
        ToolStripMenuItem menuEdit = new ToolStripMenuItem("Edición");
        ToolStripMenuItem itemCopy = new ToolStripMenuItem("Copiar", null, MnuCopy_Click, Keys.Control | Keys.C);
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

        // 2. Top Panel (Simplified - No Browse/Export buttons)
        Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(10) };
        
        Label lblFile = new Label { Text = "Archivo GAWEB:", Location = new Point(10, 25), AutoSize = true };
        // FIX: Enabled = false
        txtFilePath = new TextBox { Location = new Point(140, 22), Width = 550, Enabled = false, BackColor = Color.WhiteSmoke };
        
        btnValidate = new Button 
        { 
            Text = "VALIDAR", 
            Location = new Point(710, 15), 
            Width = 250, 
            Height = 40, 
            Font = new Font(this.Font, FontStyle.Bold), 
            BackColor = Color.LightGreen 
        };
        btnValidate.Click += BtnValidate_Click;


        lblSummary = new Label { Text = "Listo.", Location = new Point(140, 50), AutoSize = true, Font = new Font(this.Font, FontStyle.Bold) };

        pnlTop.Controls.Add(lblFile);
        pnlTop.Controls.Add(txtFilePath);
        pnlTop.Controls.Add(btnValidate); 
        pnlTop.Controls.Add(lblSummary);
        
        this.Controls.Add(pnlTop);

        // 3. Status Strip
        StatusStrip statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel { Text = "Esperando archivo..." };
        statusStrip.Items.Add(lblStatus);
        this.Controls.Add(statusStrip);

        // 4. TabControl & Grids
        tabControl = new TabControl { Dock = DockStyle.Fill };

        // Tab 1: Datos (The "Table" user requested)
        var tabData = new TabPage("Datos del Fichero");
        gridData = new DataGridView 
        { 
            Dock = DockStyle.Fill, 
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None, // Fixed width helps readability for many columns
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true,
            RowHeadersVisible = true
        };
        // Add Line Number column first
        gridData.Columns.Add("Line", "Línea");
        gridData.Columns[0].Width = 50;
        gridData.Columns[0].Frozen = true;

        // Dynamically add columns from GawebConstants
        foreach (var field in GawebConstants.Fields)
        {
            gridData.Columns.Add(field.Name, field.Name);
        }

        tabData.Controls.Add(gridData);
        tabControl.TabPages.Add(tabData);

        // Tab 2: Errores (The existing error grid)
        var tabErrors = new TabPage("Errores y Advertencias");
        gridErrors = new DataGridView 
        { 
            Dock = DockStyle.Fill, 
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AllowUserToAddRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = true
        };
        
        gridErrors.Columns.Add("Line", "Línea");
        gridErrors.Columns.Add("Field", "Campo");
        gridErrors.Columns.Add("Pos", "Posición");
        gridErrors.Columns.Add("Severity", "Gravedad");
        gridErrors.Columns.Add("Message", "Mensaje");
        gridErrors.Columns.Add("Value", "Valor");
        
        gridErrors.Columns[0].Width = 60; // Line
        gridErrors.Columns[2].Width = 80; // Pos
        gridErrors.Columns[3].Width = 80; // Severity
        
        tabErrors.Controls.Add(gridErrors);
        tabControl.TabPages.Add(tabErrors);


        this.Controls.Add(tabControl);
        
        // Fix Z-Order
        menuStrip.SendToBack();
        statusStrip.SendToBack();
        pnlTop.SendToBack();
        tabControl.BringToFront();

        this.Controls.SetChildIndex(menuStrip, 3);
        this.Controls.SetChildIndex(statusStrip, 2);
        this.Controls.SetChildIndex(pnlTop, 1);
        this.Controls.SetChildIndex(tabControl, 0); // Front
    }

    // --- Menu Handlers ---

    private void MnuOpen_Click(object sender, EventArgs e)
    {
        using (OpenFileDialog ofd = new OpenFileDialog())
        {
            ofd.Filter = "Archivos GAWEB (*.gaweb;*.txt;*.dat)|*.gaweb;*.txt;*.dat|Todos los archivos (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = ofd.FileName;
                lblStatus.Text = "Archivo cargado. Pulse Validar.";
                gridErrors.Rows.Clear();
                gridData.Rows.Clear();
            }
        }
    }

    private void MnuExport_Click(object sender, EventArgs e)
    {
        if (tabControl.SelectedTab == tabControl.TabPages[0]) // Data Tab
        {
             if (gridData.Rows.Count == 0) { MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        }
        else // Errors Tab
        {
             if (gridErrors.Rows.Count == 0) { MessageBox.Show("No hay errores para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        }

        using (SaveFileDialog sfd = new SaveFileDialog())
        {
            sfd.Filter = "CSV Report|*.csv";
            sfd.InitialDirectory = _defaultExportPath;
            if (!string.IsNullOrEmpty(txtFilePath.Text))
            {
                 sfd.FileName = $"Reporte_Validacion_{Path.GetFileNameWithoutExtension(txtFilePath.Text)}_{DateTime.Now:yyyyMMddHHmmss}.csv";
            }
            else
            {
                sfd.FileName = "Reporte_Validacion.csv";
            }

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(sfd.FileName, false, Encoding.UTF8))
                    {
                        if (tabControl.SelectedTab == tabControl.TabPages[0])
                        {
                            // Export Data Grid usually has many columns, we create header
                            string header = "";
                            foreach(DataGridViewColumn col in gridData.Columns) header += col.HeaderText + ";";
                            sw.WriteLine(header.TrimEnd(';'));

                            foreach (DataGridViewRow row in gridData.Rows)
                            {
                                if (row.IsNewRow) continue;
                                string line = "";
                                foreach (DataGridViewCell cell in row.Cells) line += (cell.Value?.ToString() ?? "") + ";";
                                sw.WriteLine(line.TrimEnd(';'));
                            }
                        }
                        else
                        {
                            sw.WriteLine("Linea;Campo;Posicion;Gravedad;Mensaje;Valor");
                            foreach (DataGridViewRow row in gridErrors.Rows)
                            {
                                if (row.IsNewRow) continue;
                                var cells = row.Cells;
                                sw.WriteLine($"{cells[0].Value};{cells[1].Value};{cells[2].Value};{cells[3].Value};{cells[4].Value};{cells[5].Value}");
                            }
                        }
                    }
                    MessageBox.Show("Informe exportado correctamente.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    private void MnuCopy_Click(object sender, EventArgs e)
    {
        try
        {
            var grid = tabControl.SelectedTab.Controls[0] as DataGridView;
            if (grid.GetCellCount(DataGridViewElementStates.Selected) > 0)
            {
                Clipboard.SetDataObject(grid.GetClipboardContent());
                lblStatus.Text = "Selección copiada al portapapeles.";
            }
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            lblStatus.Text = "Error al copiar al portapapeles.";
        }
    }

    private void MnuProperties_Click(object sender, EventArgs e)
    {
        using (Form propForm = new Form())
        {
            propForm.Text = "Propiedades";
            propForm.Size = new Size(500, 180);
            propForm.StartPosition = FormStartPosition.CenterParent;
            propForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            propForm.MaximizeBox = false;
            propForm.MinimizeBox = false;

            Label lblPath = new Label { Text = "Ruta por defecto Exportación:", Location = new Point(20, 20), AutoSize = true };
            TextBox txtPath = new TextBox { Text = _defaultExportPath, Location = new Point(20, 45), Width = 350, ReadOnly = true };
            Button btnBrowseProp = new Button { Text = "...", Location = new Point(380, 43), Width = 40 };
            
            btnBrowseProp.Click += (s, args) => 
            {
                 using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                 {
                     fbd.SelectedPath = txtPath.Text;
                     if (fbd.ShowDialog() == DialogResult.OK)
                     {
                         txtPath.Text = fbd.SelectedPath;
                     }
                 }
            };

            Button btnOk = new Button { Text = "Aceptar", Location = new Point(280, 90), Width = 80, DialogResult = DialogResult.OK };
            Button btnCancel = new Button { Text = "Cancelar", Location = new Point(370, 90), Width = 80, DialogResult = DialogResult.Cancel };
            
            propForm.Controls.AddRange(new Control[] { lblPath, txtPath, btnBrowseProp, btnOk, btnCancel });
            propForm.AcceptButton = btnOk;
            propForm.CancelButton = btnCancel;

            if (propForm.ShowDialog(this) == DialogResult.OK)
            {
                _defaultExportPath = txtPath.Text;
                MessageBox.Show("Configuración guardada.", "Propiedades", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    private void MnuManual_Click(object sender, EventArgs e)
    {
        MessageBox.Show("Manual de Usuario:\n\n1. Use Archivo > Abrir para cargar un fichero GAWEB.\n2. Pulse VALIDAR para chequear el formato (251 bytes).\n3. Revise los errores en la tabla.\n4. Use Archivo > Exportar CSV para guardar el informe.", "Manual de Ayuda", MessageBoxButtons.OK, MessageBoxIcon.Question);
    }

    private void MnuAbout_Click(object sender, EventArgs e)
    {
        MessageBox.Show("GawebVerifier v1.0\n\nHerramienta de verificación de ficheros de intercambio GAWEB.\n\nDesarrollado por ABD.", "Acerca de", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // --- Main Actions ---

    private void BtnValidate_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtFilePath.Text) || !File.Exists(txtFilePath.Text))
        {
            MessageBox.Show("Seleccione un archivo válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            this.Cursor = Cursors.WaitCursor;
            lblStatus.Text = "Validando...";
            gridErrors.Rows.Clear();
            gridData.Rows.Clear();

            Encoding encoding = Encoding.Default; 
            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); encoding = Encoding.GetEncoding(1252); } catch {}

            string[] linesArray = File.ReadAllLines(txtFilePath.Text, encoding);
            List<string> linesList = new List<string>(linesArray);

            List<ABDTools.Core.Gaweb.ValidationResult> results = GawebValidator.ValidateGawebFile(linesList);
            
            int errorCount = 0;
            int warningCount = 0;

            foreach (var res in results)
            {
                // Populate Data Grid
                var dataRow = new List<object>();
                dataRow.Add(res.LineNumber);
                
                // Get fields in order defined by grid columns (which match GawebConstants.Fields)
                // Skip index 0 (Line)
                foreach (var fieldDef in GawebConstants.Fields)
                {
                    if (res.Fields.ContainsKey(fieldDef.Name))
                    {
                        dataRow.Add(res.Fields[fieldDef.Name].Value);
                    }
                    else
                    {
                        dataRow.Add("");
                    }
                }
                
                int dataRowIdx = gridData.Rows.Add(dataRow.ToArray());
                if (!res.IsValid)
                {
                    gridData.Rows[dataRowIdx].DefaultCellStyle.BackColor = Color.MistyRose;
                }

                // Populate Errors Grid
                if (!res.IsValid)
                {
                    foreach (var err in res.Errors)
                    {
                        var rowIdx = gridErrors.Rows.Add(res.LineNumber, err.FieldName, err.Position, "ERROR", err.Message, err.Got);
                        gridErrors.Rows[rowIdx].DefaultCellStyle.BackColor = Color.MistyRose;
                        errorCount++;
                    }
                    foreach (var warn in res.Warnings)
                    {
                         var rowIdx = gridErrors.Rows.Add(res.LineNumber, warn.FieldName, warn.Position, "WARN", warn.Message, warn.Got);
                         gridErrors.Rows[rowIdx].DefaultCellStyle.BackColor = Color.LightYellow;
                         warningCount++;
                    }
                }
            }

            lblSummary.Text = $"Total Líneas: {linesArray.Length} | Errores: {errorCount} | Advertencias: {warningCount}";
            lblStatus.Text = errorCount == 0 ? "Validación Correcta." : "Validación finalizada con errores.";
            
            if (errorCount == 0)
            {
                MessageBox.Show("¡Archivo Válido! No se encontraron errores.", "Validación Correcta", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            // Auto switch to Errors tab if errors exist
            if (errorCount > 0) tabControl.SelectedTab = tabControl.TabPages[1];
            else tabControl.SelectedTab = tabControl.TabPages[0];
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al procesar: {ex.Message}", "Excepción", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            this.Cursor = Cursors.Default;
        }
    }
}
