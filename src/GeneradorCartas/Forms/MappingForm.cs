using System.IO;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlWp = DocumentFormat.OpenXml.Wordprocessing;
using GeneradorCartas.Services;

namespace GeneradorCartas.Forms;

/// <summary>
/// Form to map both Word template variables AND GAWEB fields to CSV columns
/// Shows Word variables (W) and GAWEB fields (G) with icons
/// </summary>
public class MappingForm : Form
{
    private readonly string _templatePath;
    private readonly string _dataPath;
    private readonly Dictionary<string, string> _mapping;
    private readonly Dictionary<string, string>? _formOverrides; // Values from main form
    private readonly List<string> _csvColumns = new();
    private readonly List<string> _sampleRow = new();
    private readonly List<string> _wordVariables = new();
    private readonly DataReaderService _dataReader;

    private ListView listMapping = null!;
    
    // All possible GAWEB fields that can be mapped
    private static readonly string[] GawebFields = {
        "Formato", "Lote", "Secuencial", "Pagina",
        "CodDocumento", "Version", "ClaseContrato", "CodContrato",
        "TIREL", "NUREL", "CLALF", "INDOM", "ForzarEnvio", "Idioma",
        "OpAhorroCode", "OpAhorroCuenta", "OpAhorroImporte",
        "FechaCarta", "IndDestino", "ViaReparto", "CopiaPapel",
        "Oficina", "MailFax", "NombrePDF"
    };

    // Fields that can be overridden from main form
    private static readonly Dictionary<string, string> OverrideFieldMap = new()
    {
        { "FechaCarta", "FechaCarta" },
        { "CodDocumento", "CodigoDocumento" },
        { "Oficina", "Oficina" },
        { "Lote", "Lote" }
    };

    public Dictionary<string, string> Mapping => _mapping;

    public MappingForm(string templatePath, string dataPath, Dictionary<string, string> existingMapping, Dictionary<string, string>? formOverrides, DataReaderService dataReader)
    {
        _templatePath = templatePath;
        _dataPath = dataPath;
        _mapping = new Dictionary<string, string>(existingMapping ?? new Dictionary<string, string>());
        _formOverrides = formOverrides;
        _dataReader = dataReader;

        InitializeComponent();
        LoadWordVariables();
        LoadColumns();
        
        // 1. Clean invalid mappings (columns that no longer exist in the data file)
        var invalidKeys = _mapping.Where(kv => !string.IsNullOrEmpty(kv.Value) && !_csvColumns.Contains(kv.Value))
                                  .Select(kv => kv.Key).ToList();
        foreach (var key in invalidKeys) _mapping.Remove(key);

        // 2. Auto-map gaps (always run this to catch unmapped fields that now match)
        AutoMapAll();
        
        PopulateList();
    }

    private void InitializeComponent()
    {
        this.Text = "Mapeo de Variables y Campos";
        this.Size = new Size(850, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(700, 450);

        // Instructions
        Label lblInfo = new Label
        {
            Text = " Leyenda:   [W] = Variable Word   |   [G] = Campo GAWEB   |   [*] = Valor desde pantalla principal",
            Dock = DockStyle.Top,
            Height = 35,
            Padding = new Padding(10),
            BackColor = Color.LightYellow,
            Font = new Font(this.Font, FontStyle.Bold)
        };
        this.Controls.Add(lblInfo);

        // ImageList for icons
        var imgList = new ImageList();
        imgList.ImageSize = new Size(16, 16);
        imgList.ColorDepth = ColorDepth.Depth32Bit;
        imgList.Images.Add("word", CreateIcon(Color.RoyalBlue, "W"));
        imgList.Images.Add("gaweb", CreateIcon(Color.ForestGreen, "G"));
        imgList.Images.Add("flash", CreateIcon(Color.Orange, "*"));
        imgList.Images.Add("empty", CreateIcon(Color.Transparent, ""));

        // ListView
        listMapping = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            SmallImageList = imgList
        };
        listMapping.Columns.Add("", 35); // Icon column
        listMapping.Columns.Add("Campo/Variable", 160);
        listMapping.Columns.Add("Columna CSV/Excel", 200);
        listMapping.Columns.Add("Ejemplo", 200);
        listMapping.Columns.Add("Estado", 120);

        // Double-click to edit
        listMapping.DoubleClick += ListMapping_DoubleClick;
        
        /*
        // Sorting
        var sorter = new MappingSorter();
        listMapping.ListViewItemSorter = sorter;
        listMapping.ColumnClick += (s, e) => 
        {
            sorter.Column = e.Column;
            listMapping.Sort();
        };
        */

        // listMapping added later to ensure correct Docking order (Fill last)

        // Bottom panel
        Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 60, Padding = new Padding(10) };

        Button btnAutoMap = new Button { Text = "Auto-Mapear", Location = new Point(10, 15), Width = 110, Height = 30 };
        btnAutoMap.Click += BtnAutoMap_Click;

        Button btnClear = new Button { Text = "Limpiar Todo", Location = new Point(130, 15), Width = 100, Height = 30 };
        btnClear.Click += (s, e) => 
        { 
            _mapping.Clear(); 
            PopulateList(); 
            MessageBox.Show("Mapeos eliminados.\n(Los valores fijos 'Desde pantalla' se mantienen)", "Limpiar", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        Button btnOK = new Button { Text = "Aceptar", DialogResult = DialogResult.OK, Location = new Point(620, 15), Width = 90, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        Button btnCancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(720, 15), Width = 90, Height = 30, Anchor = AnchorStyles.Top | AnchorStyles.Right };

        pnlBottom.Controls.AddRange(new Control[] { btnAutoMap, btnClear, btnOK, btnCancel });
        // Correct Docking Order:
        // 1. Controls added FIRST are at BOTTOM of Z-Order and docked FIRST.
        // We want Top/Bottom panels to take space first, then Fill takes remaining.
        // So they must be lower in Z-Order (higher index).
        // Current Add order strategy:
        // Add(lblInfo)
        // Add(pnlBottom)
        // Add(listMapping) -> Becomes Index 0 (Top Z). Docked LAST.
        
        this.Controls.Add(pnlBottom);
        this.Controls.Add(listMapping); 
        
        // Fix Layout Z-Order:
        // Docking priority goes to controls at the BOTTOM of the Z-order (Highest Index).
        // So we want pnlBottom and lblInfo to be processed FIRST.
        // listMapping (Fill) should be processed LAST (Top of Z-order, Index 0).
        pnlBottom.SendToBack();
        lblInfo.SendToBack();
        listMapping.BringToFront();
        
        // Custom Sort Removed temporarily
        // listMapping.Sort(); 
    }


    private void LoadWordVariables()
    {
        _wordVariables.Clear();
        if (string.IsNullOrEmpty(_templatePath) || !File.Exists(_templatePath))
            return;

        try
        {
            using var doc = WordprocessingDocument.Open(_templatePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return;

            var regex = new Regex(@"\{\{([^{}]+)\}\}");
            var foundVars = new HashSet<string>();

            // IMPROVED LOGIC: Scan Paragraphs instead of individual Text nodes.
            // Word often splits "{{Variable}}" into multiple runs.
            foreach (var para in body.Descendants<OpenXmlWp.Paragraph>())
            {
                string paraText = para.InnerText;
                var matches = regex.Matches(paraText);
                foreach (Match match in matches)
                {
                    foundVars.Add(match.Groups[1].Value.Trim());
                }
            }

            _wordVariables.AddRange(foundVars.OrderBy(v => v));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al leer variables Word:\n{ex.Message}", "Error Plantilla", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadColumns()
    {
        _csvColumns.Clear();
        _sampleRow.Clear();

        if (string.IsNullOrEmpty(_dataPath) || !File.Exists(_dataPath))
            return;

        try
        {
            var (headers, sample) = _dataReader.ReadHeadersAndSample(_dataPath);
            _csvColumns.AddRange(headers);
            _sampleRow.AddRange(sample);
        }
        catch { }
    }

    private void PopulateList()
    {
        listMapping.Items.Clear();
        listMapping.BeginUpdate();

        // First: Word variables with "W" icon
        foreach (var variable in _wordVariables)
        {
            string key = $"W:{variable}";
            var item = new ListViewItem("", "word"); // Use ImageKey "word"
            item.SubItems.Add(variable);
            item.Tag = key;

            string mappedTo = _mapping.TryGetValue(key, out var val) ? val : "";
            item.SubItems.Add(mappedTo);

            // Sample value
            string sampleValue = GetSampleValue(mappedTo);
            item.SubItems.Add(sampleValue);

            // Status
            SetItemStatus(item, mappedTo);
            
            item.BackColor = Color.LightCyan;
            listMapping.Items.Add(item);
        }

        // Separator
        if (_wordVariables.Count > 0)
        {
            var sep = new ListViewItem("", "empty");
            sep.SubItems.Add("— Campos GAWEB —");
            sep.SubItems.Add("");
            sep.SubItems.Add("");
            sep.SubItems.Add("");
            sep.BackColor = Color.LightGray;
            sep.ForeColor = Color.DarkGray;
            listMapping.Items.Add(sep);
        }

        // Then: GAWEB fields with "G" icon
        // Then: GAWEB fields with "G" icon
        // Initial populate sort handled by Sorter later, we just add items
        foreach (var field in GawebFields)
        {
            string key = $"G:{field}";
            string imageKey = "gaweb";
            
            // Check if this field has an override from main form
            bool hasFormOverride = false;
            string formValue = "";
            if (_formOverrides != null && OverrideFieldMap.TryGetValue(field, out var overrideKey))
            {
                if (_formOverrides.TryGetValue(overrideKey, out var ov) && !string.IsNullOrEmpty(ov))
                {
                    hasFormOverride = true;
                    formValue = ov;
                    imageKey = "flash"; // Use "flash" icon
                }
            }

            var item = new ListViewItem("", imageKey);
            item.SubItems.Add(field);
            item.Tag = key;

            string mappedTo = _mapping.TryGetValue(key, out var val) ? val : "";
            item.SubItems.Add(mappedTo);
            
            // Sample value - show form override if applicable
            string sampleValue = hasFormOverride ? $"⚡ {formValue}" : GetSampleValue(mappedTo);
            item.SubItems.Add(sampleValue);

            // Status with form override indicator
            if (hasFormOverride && string.IsNullOrEmpty(mappedTo))
            {
                item.SubItems.Add("⚡ Desde pantalla");
                item.BackColor = Color.LightGoldenrodYellow;
            }
            else
            {
                SetItemStatus(item, mappedTo);
                if (hasFormOverride)
                {
                    item.BackColor = Color.PaleGoldenrod;
                }
            }

            listMapping.Items.Add(item);
        }

        listMapping.EndUpdate();
    }

    private string GetSampleValue(string columnName)
    {
        if (string.IsNullOrEmpty(columnName)) return "";
        
        int colIndex = _csvColumns.IndexOf(columnName);
        if (colIndex >= 0 && colIndex < _sampleRow.Count)
        {
            string val = _sampleRow[colIndex];
            return val.Length > 35 ? val.Substring(0, 32) + "..." : val;
        }
        return "";
    }

    private void SetItemStatus(ListViewItem item, string mappedTo)
    {
        if (string.IsNullOrEmpty(mappedTo))
        {
            item.SubItems.Add("⏸ Sin mapear");
            if (item.BackColor == Color.Empty || item.BackColor == SystemColors.Window)
                item.BackColor = Color.WhiteSmoke;
        }
        else if (_csvColumns.Contains(mappedTo))
        {
            item.SubItems.Add("✓ Mapeado");
            item.BackColor = Color.LightGreen;
        }
        else
        {
            item.SubItems.Add("⚠ No encontrado");
            item.BackColor = Color.LightPink;
        }
    }

    private void ListMapping_DoubleClick(object? sender, EventArgs e)
    {
        if (listMapping.SelectedItems.Count == 0) return;

        var item = listMapping.SelectedItems[0];
        string? key = item.Tag as string;
        if (string.IsNullOrEmpty(key) || key == "—") return;

        string fieldName = item.SubItems[1].Text;

        // Show column selection dialog
        using var selectForm = new Form
        {
            Text = $"Mapear: {fieldName}",
            Size = new Size(450, 500),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        Label lblInstr = new Label
        {
            Text = $"Seleccione la columna para '{fieldName}':\n(Vacío = usar valor por defecto)",
            Location = new Point(10, 10),
            Size = new Size(410, 35)
        };

        ListBox lstColumns = new ListBox
        {
            Location = new Point(10, 50),
            Size = new Size(410, 330)
        };

        lstColumns.Items.Add("(Sin mapear)");
        foreach (var col in _csvColumns)
        {
            int idx = _csvColumns.IndexOf(col);
            string sample = idx < _sampleRow.Count ? _sampleRow[idx] : "";
            if (sample.Length > 40) sample = sample.Substring(0, 37) + "...";
            lstColumns.Items.Add($"{col}  →  {sample}");
        }

        // Select current mapping
        string current = _mapping.TryGetValue(key, out var v) ? v : "";
        if (string.IsNullOrEmpty(current))
            lstColumns.SelectedIndex = 0;
        else
        {
            for (int i = 1; i < lstColumns.Items.Count; i++)
            {
                if (lstColumns.Items[i].ToString()?.StartsWith(current + "  →") == true)
                {
                    lstColumns.SelectedIndex = i;
                    break;
                }
            }
        }

        Button btnSelect = new Button
        {
            Text = "Seleccionar",
            DialogResult = DialogResult.OK,
            Location = new Point(170, 390),
            Width = 100
        };

        selectForm.Controls.AddRange(new Control[] { lblInstr, lstColumns, btnSelect });
        selectForm.AcceptButton = btnSelect;

        if (selectForm.ShowDialog(this) == DialogResult.OK)
        {
            if (lstColumns.SelectedIndex == 0)
            {
                _mapping.Remove(key);
            }
            else if (lstColumns.SelectedIndex > 0)
            {
                string selected = lstColumns.SelectedItem?.ToString() ?? "";
                int arrowIdx = selected.IndexOf("  →");
                if (arrowIdx > 0)
                    selected = selected.Substring(0, arrowIdx);
                _mapping[key] = selected;
            }
            PopulateList();
        }
    }

    private void AutoMapAll()
    {
        // Helper to normalize string for matching
        string Normalize(string s) => s.Replace("_", "").Replace(" ", "").Replace("-", "").ToLowerInvariant();

        // Auto-map Word variables
        foreach (var variable in _wordVariables)
        {
            string key = $"W:{variable}";
            // Only map if empty or explicit auto-map request
            if (_mapping.ContainsKey(key) && !string.IsNullOrEmpty(_mapping[key])) continue;

            // 1. Exact match (case insensitive)
            var match = _csvColumns.FirstOrDefault(c => 
                c.Equals(variable, StringComparison.OrdinalIgnoreCase));
            
            // 2. Normalized match (ignore case, spaces, underscores)
            if (match == null)
            {
                string normVar = Normalize(variable);
                match = _csvColumns.FirstOrDefault(c => Normalize(c) == normVar);
            }

            // 3. Contains match (if variable name is contained in column name or vice-versa, with high confidence)
            if (match == null)
            {
                string normVar = Normalize(variable);
                match = _csvColumns.FirstOrDefault(c => {
                    string normCol = Normalize(c);
                    return normCol.Contains(normVar) || normVar.Contains(normCol);
                });
            }

            if (match != null)
                _mapping[key] = match;
        }

        // Auto-map GAWEB fields
        foreach (var field in GawebFields)
        {
            string key = $"G:{field}";
            if (_mapping.ContainsKey(key) && !string.IsNullOrEmpty(_mapping[key])) continue;

            // 1. Exact match
            var match = _csvColumns.FirstOrDefault(c => 
                c.Equals(field, StringComparison.OrdinalIgnoreCase));

            // 2. Normalized match
            if (match == null)
            {
                string normField = Normalize(field);
                match = _csvColumns.FirstOrDefault(c => Normalize(c) == normField);
            }

            if (match != null)
                _mapping[key] = match;
        }
    }

    private void BtnAutoMap_Click(object? sender, EventArgs e)
    {
        int before = _mapping.Count;
        AutoMapAll();
        int mapped = _mapping.Count - before;

        PopulateList();
        
        // Force message even if 0
        if (mapped > 0)
        {
             MessageBox.Show($"Auto-mapeados {mapped} campos por coincidencia de nombre.", 
                 "Auto-Mapeo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        else
        {
             MessageBox.Show($"No se encontraron nuevas coincidencias.\n(Verifique que los nombres de cabecera CSV coincidan con las variables)", 
                 "Auto-Mapeo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    
    private Bitmap CreateIcon(Color bgColor, string text)
    {
        Bitmap bmp = new Bitmap(16, 16);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            
            // Background circle/box
            if (!string.IsNullOrEmpty(text))
            {
                using var brush = new SolidBrush(bgColor);
                g.FillRectangle(brush, 0, 0, 15, 15);
                g.DrawRectangle(Pens.Black, 0, 0, 15, 15);
                
                // Text
                using var font = new Font("Segoe UI", 8, FontStyle.Bold);
                var size = g.MeasureString(text, font);
                float x = (16 - size.Width) / 2;
                float y = (16 - size.Height) / 2;
                
                g.DrawString(text, font, Brushes.White, x, y);
            }
        }
        return bmp;
    }
}
