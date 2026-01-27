using System.Text.Json.Serialization;

namespace EtlConverter.Models;

public class EtlPreset
{
    public string DisplayName { get; set; } = "Nueva Configuración";
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;

    // Technical Config
    public int ChunkSize { get; set; } = 900000;
    public string Encoding { get; set; } = "utf-8";
    
    // Legacy properties kept for compatibility if needed, but logic moves to RecordTypes list
    public int RecordTypeStart { get; set; } = 0;
    public int RecordTypeLen { get; set; } = 0;
    public string DefaultRecordType { get; set; } = "";
    public string HeaderTypeID { get; set; } = "";

    // NEW: List based logic
    public List<EtlRecordType> RecordTypes { get; set; } = new List<EtlRecordType>();
}

public class EtlRecordType
{
    public string Name { get; set; } = "Nuevo Tipo";
    public string Trigger { get; set; } = ""; // Prefix or Wildcard
    public int TriggerStart { get; set; } = 0; // New: Specific start position
    public string Behavior { get; set; } = "DATA"; // DATA, HEADER, FOOTER
    public string Range { get; set; } = ""; // "1-2" for Header, "1" for Footer
    public List<EtlField> Fields { get; set; } = new List<EtlField>();
}

public class EtlField
{
    public string Name { get; set; } = "NEW_FIELD";
    public int Start { get; set; } = 0;
    public int Length { get; set; } = 10;
}
