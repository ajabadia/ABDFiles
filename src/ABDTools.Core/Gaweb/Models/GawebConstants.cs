using System.Collections.Generic;

namespace ABDTools.Core.Gaweb.Models;

public record GawebFieldDefinition(string Name, int Length, bool IsNumeric = false);

public static class GawebConstants
{
    public const int RecordLength = 251;

    public static readonly IReadOnlyList<GawebFieldDefinition> Fields = new List<GawebFieldDefinition>
    {
        new("TipoCarta", 1),
        new("Formato", 2),
        new("FechaGeneracion", 8, true),
        new("Lote", 4),
        new("Secuencial", 7, true),
        new("Pagina", 4, true),
        new("CodDocumento", 6),
        new("Version", 4, true),
        
        // Destino (Total 48)
        new("ClaseContrato", 2),
        new("CodContrato", 25),
        new("TIREL", 1),
        new("NUREL", 3, true),
        new("CLALF", 15),
        new("INDOM", 2, true),
        
        new("ForzarEnvio", 1),
        new("Idioma", 2),
        
        // Op Ahorro (Total 48)
        new("OpAhorroCode", 2),
        new("OpAhorroCuenta", 25),
        new("OpAhorroSign", 1),
        new("OpAhorroAmount", 13, true),
        new("OpAhorroCurrency", 2),
        new("OpAhorroISO", 3),
        new("OpAhorroConcept", 2),

        new("FechaCarta", 8, true),
        new("IndDestino", 1),
        new("DetalleCarga", 4, true),
        new("ViaReparto", 2),
        new("CopiaPapel", 1),
        new("Oficina", 5),
        new("MailFax", 50),
        new("LongitudContenido", 5, true),
        new("NombrePDF", 40)
    };
}
