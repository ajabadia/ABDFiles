using System.Drawing;
using System.Windows.Forms;
using GeneradorCartas.Services;

namespace GeneradorCartas.Forms;

/// <summary>
/// Form to preview CSV/Excel data in a grid view
/// </summary>
public class DataPreviewForm : Form
{
    private readonly string _dataPath;
    private readonly DataReaderService _dataReader;
    private DataGridView gridData = null!;

    public DataPreviewForm(string dataPath, DataReaderService dataReader)
    {
        _dataPath = dataPath;
        _dataReader = dataReader;
        InitializeComponent();
        LoadData();
    }

    private void InitializeComponent()
    {
        this.Text = $"Vista Previa de Datos - {Path.GetFileName(_dataPath)}";
        this.Size = new Size(900, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.MinimumSize = new Size(400, 300);

        // Info bar
        Label lblInfo = new Label
        {
            Dock = DockStyle.Top,
            Height = 25,
            BackColor = Color.LightSteelBlue,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(5, 0, 0, 0)
        };
        this.Controls.Add(lblInfo);

        // DataGridView
        gridData = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToOrderColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.Fixed3D,
            RowHeadersWidth = 50
        };
        this.Controls.Add(gridData);

        // Bottom panel with close button
        Panel pnlBottom = new Panel { Dock = DockStyle.Bottom, Height = 40 };
        Button btnClose = new Button
        {
            Text = "Cerrar",
            DialogResult = DialogResult.OK,
            Width = 80,
            Height = 28,
            Location = new Point(10, 6)
        };
        Label lblRowCount = new Label
        {
            Name = "lblRowCount",
            Location = new Point(100, 12),
            AutoSize = true
        };
        pnlBottom.Controls.AddRange(new Control[] { btnClose, lblRowCount });
        this.Controls.Add(pnlBottom);

        this.AcceptButton = btnClose;

        // Z-order
        gridData.BringToFront();
    }

    private void LoadData()
    {
        if (string.IsNullOrEmpty(_dataPath) || !File.Exists(_dataPath))
        {
            MessageBox.Show("Archivo no encontrado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var (headers, sampleRow) = _dataReader.ReadHeadersAndSample(_dataPath);
            
            // Limit preview to 1000 rows for performance
            var allData = _dataReader.StreamData(_dataPath).Take(1000).ToList();

            if (headers.Count == 0)
            {
                MessageBox.Show("El archivo está vacío o no se pudo leer.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Create columns
            gridData.Columns.Clear();
            for (int i = 0; i < headers.Count; i++)
            {
                var col = new DataGridViewTextBoxColumn
                {
                    Name = $"col{i}",
                    HeaderText = headers[i],
                    Width = 120,
                    SortMode = DataGridViewColumnSortMode.Automatic
                };
                gridData.Columns.Add(col);
            }

            // Add rows
            foreach (var rowData in allData)
            {
                var row = new object[headers.Count];
                for (int j = 0; j < headers.Count; j++)
                {
                    row[j] = rowData.TryGetValue(headers[j], out var val) ? val : "";
                }
                gridData.Rows.Add(row);
            }

            // Update row count
            var lblRowCount = this.Controls.Find("lblRowCount", true).FirstOrDefault() as Label;
            if (lblRowCount != null)
            {
                lblRowCount.Text = $"{allData.Count} filas mostradas, {headers.Count} columnas";
            }

            // Update title
            string ext = Path.GetExtension(_dataPath).ToUpperInvariant();
            var lblInfo = this.Controls.OfType<Label>().FirstOrDefault(l => l.Dock == DockStyle.Top);
            if (lblInfo != null)
            {
                lblInfo.Text = $"  Archivo: {Path.GetFileName(_dataPath)} | Formato: {ext}";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al leer el archivo:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
