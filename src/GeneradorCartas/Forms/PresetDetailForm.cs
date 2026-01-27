using System.Drawing;
using System.Windows.Forms;
using System.Text.Json;
using ABDTools.Core.Gaweb.Models;

namespace GeneradorCartas.Forms;

/// <summary>
/// Form to display preset details with validation indicators
/// </summary>
public class PresetDetailForm : Form
{
    private readonly GawebPreset _preset;
    private readonly List<(Label lbl, TextBox txt, Label status)> _fields = new();

    public PresetDetailForm(GawebPreset preset)
    {
        _preset = preset;
        InitializeComponent();
        LoadData();
        ValidateAll();
    }

    public PresetDetailForm(string presetPath)
    {
        if (!File.Exists(presetPath))
            throw new FileNotFoundException("Preset not found", presetPath);

        string json = File.ReadAllText(presetPath);
        _preset = JsonSerializer.Deserialize<GawebPreset>(json) ?? new GawebPreset();
        
        InitializeComponent();
        LoadData();
        ValidateAll();
    }

    private void InitializeComponent()
    {
        this.Text = $"Detalle del Preset: {_preset.Name}";
        this.Size = new Size(600, 550);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;

        Panel pnlMain = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(20) };

        int y = 10;
        int lblW = 150, txtW = 300, statusX = 480;

        // Title
        Label lblTitle = new Label 
        { 
            Text = "Configuración del Preset GAWEB", 
            Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold),
            Location = new Point(20, y), 
            AutoSize = true 
        };
        pnlMain.Controls.Add(lblTitle);
        y += 35;

        // Identificación
        AddSectionHeader(pnlMain, "Identificación", ref y);
        AddField(pnlMain, "Nombre:", _preset.Name, ref y, "Requerido");
        AddField(pnlMain, "Descripción:", _preset.Description, ref y);
        AddField(pnlMain, "Activo:", _preset.Active ? "Sí" : "No", ref y);
        y += 10;

        // Configuración Técnica
        AddSectionHeader(pnlMain, "Configuración Técnica", ref y);
        AddField(pnlMain, "Tipo Soporte:", _preset.TipoSoporte, ref y, "OV o PDF");
        AddField(pnlMain, "Formato Carta:", _preset.FormatoCarta, ref y, "2 chars, requerido");
        AddField(pnlMain, "Forzar Método:", _preset.ForzarMetodo, ref y);
        AddField(pnlMain, "Ind. Destino:", _preset.IndicadorDestino, ref y, "0, O o 7");
        AddField(pnlMain, "Tipo Destino:", _preset.TipoDestino, ref y);
        y += 10;

        // Datos HOST
        AddSectionHeader(pnlMain, "Datos HOST", ref y);
        AddField(pnlMain, "Código Entorno:", _preset.CodigoEntorno, ref y, "Max 8 chars, requerido");
        AddField(pnlMain, "Código Documento:", _preset.CodigoDocumento, ref y, "6 chars exactos, requerido");
        y += 10;

        // Valores por Defecto
        AddSectionHeader(pnlMain, "Valores por Defecto", ref y);
        AddField(pnlMain, "Fecha Generación:", _preset.FechaGeneracion, ref y, "YYYYMMDD");
        AddField(pnlMain, "Fecha Carta:", _preset.FechaCarta, ref y, "YYYYMMDD");
        AddField(pnlMain, "Oficina:", _preset.Oficina, ref y, "5 chars");
        AddField(pnlMain, "Páginas:", _preset.PaginasDefecto.ToString(), ref y, "1-9999");
        AddField(pnlMain, "Idioma:", _preset.Idioma, ref y, "2 chars ISO");
        AddField(pnlMain, "Vía Reparto:", _preset.ViaReparto, ref y);
        AddField(pnlMain, "Copia Papel:", _preset.CopiaPapel, ref y, "S, N o X");

        // Mapping count
        y += 10;
        AddSectionHeader(pnlMain, "Mapeo de Campos", ref y);
        int mappingCount = _preset.Mapping?.Count ?? 0;
        AddField(pnlMain, "Campos mapeados:", mappingCount.ToString(), ref y);

        this.Controls.Add(pnlMain);

        // Close button
        Button btnClose = new Button 
        { 
            Text = "Cerrar", 
            DialogResult = DialogResult.OK,
            Dock = DockStyle.Bottom,
            Height = 35
        };
        this.Controls.Add(btnClose);
        this.AcceptButton = btnClose;
    }

    private void AddSectionHeader(Panel panel, string text, ref int y)
    {
        Label lbl = new Label
        {
            Text = text,
            Font = new Font(this.Font, FontStyle.Bold),
            ForeColor = Color.DarkBlue,
            Location = new Point(20, y),
            AutoSize = true
        };
        panel.Controls.Add(lbl);
        y += 25;
    }

    private void AddField(Panel panel, string label, string value, ref int y, string? rule = null)
    {
        Label lbl = new Label { Text = label, Location = new Point(30, y + 3), Width = 130, TextAlign = ContentAlignment.MiddleRight };
        TextBox txt = new TextBox 
        { 
            Text = value ?? "", 
            Location = new Point(165, y), 
            Width = 280, 
            ReadOnly = true,
            BackColor = Color.White
        };
        Label status = new Label 
        { 
            Text = "OK", // Check
            ForeColor = Color.Green, 
            Location = new Point(455, y + 3), 
            AutoSize = true 
        };

        // Store rule in Tag for validation
        txt.Tag = rule;

        panel.Controls.AddRange(new Control[] { lbl, txt, status });
        _fields.Add((lbl, txt, status));
        y += 28;
    }

    private void LoadData()
    {
        // Data already loaded in AddField calls during InitializeComponent
    }

    private void ValidateAll()
    {
        foreach (var (lbl, txt, status) in _fields)
        {
            string fieldName = lbl.Text.TrimEnd(':');
            string value = txt.Text;
            string? rule = txt.Tag as string;
            string? error = ValidateField(fieldName, value, rule);

            if (error != null)
            {
                status.Text = "ERR"; // X
                status.ForeColor = Color.Red;
                txt.BackColor = Color.MistyRose;
                
                // Add tooltip
                var tt = new ToolTip();
                tt.SetToolTip(status, error);
                tt.SetToolTip(txt, error);
            }
            else
            {
                status.Text = "OK"; // v
                status.ForeColor = Color.Green;
                txt.BackColor = Color.White;
            }
        }
    }

    private string? ValidateField(string fieldName, string value, string? rule)
    {
        // Required fields
        if (fieldName == "Nombre" && string.IsNullOrWhiteSpace(value))
            return "El nombre es obligatorio";

        if (fieldName == "Formato Carta")
        {
            if (string.IsNullOrWhiteSpace(value))
                return "El formato es obligatorio";
            if (value.Length != 2)
                return "Debe tener exactamente 2 caracteres";
        }

        if (fieldName == "Código Entorno")
        {
            if (string.IsNullOrWhiteSpace(value))
                return "El código de entorno es obligatorio";
            if (value.Length > 8)
                return "Máximo 8 caracteres";
        }

        if (fieldName == "Código Documento")
        {
            if (string.IsNullOrWhiteSpace(value))
                return "El código de documento es obligatorio";
            if (value.Length != 6)
                return "Debe tener exactamente 6 caracteres";
        }

        // Date fields
        if ((fieldName == "Fecha Generación" || fieldName == "Fecha Carta") && !string.IsNullOrEmpty(value))
        {
            if (value.Length != 8)
                return "Debe tener formato YYYYMMDD (8 dígitos)";
            if (!DateTime.TryParseExact(value, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _))
                return "Fecha no válida";
        }

        // Oficina
        if (fieldName == "Oficina" && !string.IsNullOrEmpty(value))
        {
            if (value.Length != 5)
                return "Debe tener exactamente 5 caracteres";
            if (!value.All(char.IsDigit))
                return "Solo dígitos numéricos";
        }

        // Páginas
        if (fieldName == "Páginas" && !string.IsNullOrEmpty(value))
        {
            if (!int.TryParse(value, out int pag) || pag < 1 || pag > 9999)
                return "Debe ser un número entre 1 y 9999";
        }

        // Tipo Soporte
        if (fieldName == "Tipo Soporte" && !string.IsNullOrEmpty(value))
        {
            if (value != "OV" && value != "PDF")
                return "Debe ser 'OV' o 'PDF'";
        }

        // Indicador Destino
        if (fieldName == "Ind. Destino" && !string.IsNullOrEmpty(value))
        {
            if (value != "0" && value != "O" && value != "7")
                return "Debe ser '0', 'O' o '7'";
        }

        return null; // Valid
    }
}
