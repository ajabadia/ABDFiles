using System.Text.Json.Serialization;

namespace GeneradorCartas.Models;

/// <summary>
/// Configuration for a letter generation session.
/// This is saved/loaded from JSON files via File > Open/Save.
/// </summary>
public class GenerationConfig
{
    /// <summary>
    /// Type identifier for JSON validation. Must be "GeneradorCartas.GenerationConfig"
    /// </summary>
    [JsonPropertyName("$type")]
    public string Type { get; set; } = "GeneradorCartas.GenerationConfig";

    /// <summary>
    /// Schema version for future compatibility
    /// </summary>
    [JsonPropertyName("$version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Path to the base GAWEB preset JSON file (relative or absolute)
    /// </summary>
    [JsonPropertyName("presetPath")]
    public string PresetPath { get; set; } = string.Empty;

    /// <summary>
    /// Values that override the base preset
    /// </summary>
    [JsonPropertyName("overrides")]
    public ConfigOverrides Overrides { get; set; } = new ConfigOverrides();

    /// <summary>
    /// Path to the CSV/Excel data file
    /// </summary>
    [JsonPropertyName("dataFilePath")]
    public string DataFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the Word template (.docx)
    /// </summary>
    [JsonPropertyName("templatePath")]
    public string TemplatePath { get; set; } = string.Empty;

    /// <summary>
    /// Mapping of DOCX variables to CSV column names.
    /// Key: Variable name in template (without braces)
    /// Value: Column name in CSV/Excel
    /// </summary>
    [JsonPropertyName("variableMapping")]
    public Dictionary<string, string> VariableMapping { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Output directory for generated files
    /// </summary>
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = string.Empty;

    /// <summary>
    /// First record to process (1-based, inclusive). Null = start from beginning.
    /// </summary>
    [JsonPropertyName("rangeFrom")]
    public int? RangeFrom { get; set; }

    /// <summary>
    /// Last record to process (1-based, inclusive). Null = process until end.
    /// </summary>
    [JsonPropertyName("rangeTo")]
    public int? RangeTo { get; set; }

    /// <summary>
    /// Output type: "DOCX", "PDF", or "PDF_GAWEB"
    /// </summary>
    [JsonPropertyName("outputType")]
    public string OutputType { get; set; } = "PDF_GAWEB";

    /// <summary>
    /// PDF library to use: "Syncfusion" or "Word"
    /// </summary>
    [JsonPropertyName("pdfLibrary")]
    public string PdfLibrary { get; set; } = "Syncfusion";

    /// <summary>
    /// Community License Key for Syncfusion to avoid watermarks
    /// </summary>
    [JsonPropertyName("syncfusionLicenseKey")]
    public string? SyncfusionLicenseKey { get; set; }

    /// <summary>
    /// Validates the $type field to ensure this is the correct JSON type
    /// </summary>
    public bool IsValidType() => Type == "GeneradorCartas.GenerationConfig";
}

/// <summary>
/// Override values for preset fields
/// </summary>
public class ConfigOverrides
{
    [JsonPropertyName("fechaGeneracion")]
    public string? FechaGeneracion { get; set; }

    [JsonPropertyName("fechaCarta")]
    public string? FechaCarta { get; set; }

    [JsonPropertyName("lote")]
    public string? Lote { get; set; }

    [JsonPropertyName("codigoDocumento")]
    public string? CodigoDocumento { get; set; }

    [JsonPropertyName("oficina")]
    public string? Oficina { get; set; }

    [JsonPropertyName("codigoEntorno")]
    public string? CodigoEntorno { get; set; }

    [JsonPropertyName("idioma")]
    public string? Idioma { get; set; }

    [JsonPropertyName("paginasDefecto")]
    public int? PaginasDefecto { get; set; }
}
