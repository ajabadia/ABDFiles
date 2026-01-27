using System.Text.Json.Serialization;

namespace ABDTools.Core.Gaweb.Models;

/// <summary>
/// Represents a saved configuration for generating GAWEB files.
/// Users create these presets in the UI to define common settings.
/// </summary>
public class GawebPreset
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    // Configuration Fields

    [JsonPropertyName("tipo_soporte")]
    public string TipoSoporte { get; set; } = string.Empty; // "OV" or "PDF"

    [JsonPropertyName("formato_carta")]
    public string FormatoCarta { get; set; } = string.Empty; // e.g., "04"

    [JsonPropertyName("forzar_metodo")]
    public string ForzarMetodo { get; set; } = string.Empty; // e.g., "1", "4"

    [JsonPropertyName("indicador_destino")]
    public string IndicadorDestino { get; set; } = string.Empty; // "0", "O", "7"

    [JsonPropertyName("tipo_destino")]
    public string TipoDestino { get; set; } = string.Empty; // "CL", "CT", "CC"

    [JsonPropertyName("idioma")]
    public string Idioma { get; set; } = string.Empty; // e.g., "ES", "  "

    [JsonPropertyName("via_reparto")]
    public string ViaReparto { get; set; } = string.Empty; // e.g., "  "

    [JsonPropertyName("copia_papel")]
    public string CopiaPapel { get; set; } = string.Empty; // e.g., " ", "X"

    // Fixed Data (Overrideable)

    [JsonPropertyName("fecha_generacion")]
    public string FechaGeneracion { get; set; } = string.Empty; // YYYYMMDD

    [JsonPropertyName("fecha_carta")]
    public string FechaCarta { get; set; } = string.Empty; // YYYYMMDD

    [JsonPropertyName("codigo_entorno")]
    public string CodigoEntorno { get; set; } = string.Empty; // HOST environment code (e.g., ABDFN01)

    [JsonPropertyName("codigo_documento")]
    public string CodigoDocumento { get; set; } = string.Empty; // Default Template ID (e.g. "X00054")

    [JsonPropertyName("oficina")]
    public string Oficina { get; set; } = string.Empty; // Default Office Code

    [JsonPropertyName("paginas_defecto")]
    public int PaginasDefecto { get; set; } // Default page count

    // Dynamic Mapping: GAWEB Field Name -> Excel Column Header
    [JsonPropertyName("mapping")]
    public Dictionary<string, string> Mapping { get; set; } = new Dictionary<string, string>();
}
