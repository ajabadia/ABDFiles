namespace LetterConfig.Services;

public class ReferenceItem
{
    public string GlobalId { get; set; } = "";
    public string Label { get; set; } = "";
    public string Extra { get; set; } = "";

    public override string ToString() => Label;
}

public static class ReferenceData
{
    // Idiomas (ISO 639-1)
    public static readonly List<ReferenceItem> Idiomas = new()
    {
        new() { GlobalId = "  ", Label = "Vacío (Por defecto)" },
        new() { GlobalId = "ES", Label = "ES - Español" },
        new() { GlobalId = "EN", Label = "EN - Inglés" },
        new() { GlobalId = "FR", Label = "FR - Francés" },
        new() { GlobalId = "DE", Label = "DE - Alemán" },
        new() { GlobalId = "IT", Label = "IT - Italiano" },
        new() { GlobalId = "PT", Label = "PT - Portugués" },
        new() { GlobalId = "CA", Label = "CA - Catalán" },
        new() { GlobalId = "EU", Label = "EU - Euskera" },
        new() { GlobalId = "GL", Label = "GL - Gallego" },
        new() { GlobalId = "RU", Label = "RU - Ruso" },
        new() { GlobalId = "ZH", Label = "ZH - Chino" }
    };

    // ViasReparto
    public static readonly List<ReferenceItem> ViasReparto = new()
    {
        new() { GlobalId = "  ", Label = "Vacío (Estándar)" },
        new() { GlobalId = "01", Label = "01 - Reparto Especial 1" },
        new() { GlobalId = "02", Label = "02 - Reparto Especial 2" }
    };

    // CopiaPapel
    public static readonly List<ReferenceItem> CopiasPapel = new()
    {
        new() { GlobalId = " ", Label = "Vacío (No indicado)" },
        new() { GlobalId = "S", Label = "S - Sí" },
        new() { GlobalId = "N", Label = "N - No" },
        new() { GlobalId = "X", Label = "X - No Imprimir" }
    };
    
    // Soportes
    public static readonly List<ReferenceItem> Soportes = new()
    {
        new() { GlobalId = "OV", Label = "OV - Overlay", Extra = "OV" },
        new() { GlobalId = "PDF", Label = "PDF - Digital", Extra = "PDF" }
    };

    // Formatos (Hardcoded fallback from legacy)
    public static readonly List<ReferenceItem> Formatos = new()
    {
        new() { GlobalId = "01", Label = "01 - Overlay, tercio (Din A6)", Extra = "OV" },
        new() { GlobalId = "02", Label = "02 - Overlay, Din A4 sobre americano ventana pequeña", Extra = "OV" },
        new() { GlobalId = "03", Label = "03 - Overlay, Din A4 Duplex sobre americano ventana grande", Extra = "OV" },
        new() { GlobalId = "04", Label = "04 - PDF A4 ventana Izquierda sobre americano ventana grande", Extra = "PDF" },
        new() { GlobalId = "05", Label = "05 - PDF A4 ventana derecha sobre C5", Extra = "PDF" }
    };

    // Destinos
    public static readonly List<ReferenceItem> Destinos = new()
    {
        new() { GlobalId = "CL", Label = "Cliente" },
        new() { GlobalId = "CT", Label = "Contrato" },
        new() { GlobalId = "CC", Label = "Cuenta Corriente" }
    };

    // IndicadoresDestino
    public static readonly List<ReferenceItem> IndicadoresDestino = new()
    {
        new() { GlobalId = "0", Label = "Clientes" },
        new() { GlobalId = "7", Label = "Central" },
        new() { GlobalId = "O", Label = "Oficinas" }
    };

    // MetodosEnvio
    public static readonly List<ReferenceItem> MetodosEnvio = new()
    {
        new() { GlobalId = " ", Label = "Canal elegido por el cliente" },
        new() { GlobalId = "1", Label = "Papel al cliente" },
        new() { GlobalId = "3", Label = "Por FAX" },
        new() { GlobalId = "4", Label = "Por Correo Electrónico" },
        new() { GlobalId = "5", Label = "Al Buzón Electrónico (solo cargar en la WEB)" },
        new() { GlobalId = "8", Label = "No enviar, solo cargar en WEB" }
    };
}
