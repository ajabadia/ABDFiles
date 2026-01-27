using EtlConfig.Models;

namespace EtlConfig.Controls;

public class FieldControl : UserControl
{
    private TextBox txtName;
    private TextBox txtStart;
    private TextBox txtLength;
    private Button btnDelete;

    private EtlField _field;
    
    // Events
    public event EventHandler? DataChanged;
    public event EventHandler? DeleteRequested;

    public FieldControl(EtlField field)
    {
        _field = field;
        InitializeComponent();
        BindData();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(480, 35); // Slightly compact
        this.Padding = new Padding(0, 0, 0, 1); // Bottom margin feeling
        this.BackColor = Color.WhiteSmoke; 
        
        var layout = new TableLayoutPanel();
        layout.Dock = DockStyle.Fill;
        layout.ColumnCount = 4;
        layout.RowCount = 1;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F)); // Name
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Start
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Length
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Delete
        
        // Remove individual labels for cleaner "Grid" look
        txtName = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3) };
        txtStart = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3), TextAlign = HorizontalAlignment.Center };
        txtLength = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3), TextAlign = HorizontalAlignment.Center };

        btnDelete = new Button { Text = "❌", ForeColor = Color.Red, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Margin = new Padding(1) };
        btnDelete.FlatAppearance.BorderSize = 0;
        btnDelete.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);

        layout.Controls.Add(txtName, 0, 0);
        layout.Controls.Add(txtStart, 1, 0);
        layout.Controls.Add(txtLength, 2, 0);
        layout.Controls.Add(btnDelete, 3, 0);

        this.Controls.Add(layout);
        
        // Wire events
        txtName.TextChanged += OnValueChanged;
        txtStart.TextChanged += OnValueChanged;
        txtLength.TextChanged += OnValueChanged;
    }

    // Helper removed as we don't need labels per row anymore
    
    private void BindData()
    {
        txtName.Text = _field.Name;
        txtStart.Text = _field.Start.ToString();
        txtLength.Text = _field.Length.ToString();
        
        // Tooltips
        var tip = new ToolTip();
        tip.SetToolTip(txtName, "Nombre del Campo");
        tip.SetToolTip(txtStart, "Posición Inicio");
        tip.SetToolTip(txtLength, "Longitud");
    }

    private bool _ignoreEvents = false;

    private void OnValueChanged(object? sender, EventArgs e)
    {
        if (_ignoreEvents) return;
        
        _field.Name = txtName.Text;
        if (int.TryParse(txtStart.Text, out int s)) _field.Start = s;
        if (int.TryParse(txtLength.Text, out int l)) _field.Length = l;

        DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
