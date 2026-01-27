using System.Text;

namespace ABDTools.Core.Gaweb.Models;

/// <summary>
/// Represents the actual logical row in the GAWEB file.
/// This struct is used to hold data before serializing to fixed-width string.
/// </summary>
public class GawebRecord
{
    // Constants for field lengths
    public const int RecordLength = 251;

    public string TipoCarta { get; set; } = string.Empty; // 1
    public string Formato { get; set; } = string.Empty; // 2
    public string FechaGeneracion { get; set; } = string.Empty; // 8 (YYYYMMDD)
    public string Lote { get; set; } = string.Empty; // 4
    public string Secuencial { get; set; } = string.Empty; // 7
    public string Pagina { get; set; } = string.Empty; // 4
    public string CodDocumento { get; set; } = string.Empty; // 6
    public int Version { get; set; } // 4 (Default 0)

    // Destino
    public string ClaseContrato { get; set; } = string.Empty; // 2
    public string CodContrato { get; set; } = string.Empty; // 25
    public string TIREL { get; set; } = string.Empty; // 1
    public int NUREL { get; set; } // 3
    public string CLALF { get; set; } = string.Empty; // 15
    public int INDOM { get; set; } // 2

    public string ForzarEnvio { get; set; } = string.Empty; // 1
    public string Idioma { get; set; } = string.Empty; // 2

    // Op Ahorro (Not fully implemented in detailed fields as per Go legacy simplification)
    // We treat these as filler for now, as the logic was simply to pad 48 bytes
    // or use specific defaults.
    
    public string FechaCarta { get; set; } = string.Empty; // 8
    public string IndDestino { get; set; } = string.Empty; // 1 ("0", "O", "7")
    public int DetalleCarga { get; set; } // 4 (0)
    public string ViaReparto { get; set; } = string.Empty; // 2
    public string CopiaPapel { get; set; } = string.Empty; // 1
    public string Oficina { get; set; } = string.Empty; // 5
    public string MailFax { get; set; } = string.Empty; // 50

    public int LongitudContenido { get; set; } // 5 (0 for PDF)
    public string NombrePDF { get; set; } = string.Empty; // 40 (PseudoCODREF)

    /// <summary>
    /// Serializes the record to the 251-byte fixed-width string.
    /// </summary>
    public string Serialize()
    {
        var sb = new StringBuilder(RecordLength);

        // Helper functions
        string PadStr(string s, int len)
        {
            if (s == null) s = string.Empty;
            if (s.Length > len) return s.Substring(0, len);
            return s.PadRight(len); // Left aligned, space padded
        }

        string PadNum(int n, int len)
        {
            var s = n.ToString().PadLeft(len, '0');
            if (s.Length > len) return s.Substring(s.Length - len); // Take rightmost digits
            return s;
        }

        // 1. Tipo Carta (Space)
        sb.Append(PadStr(" ", 1));

        // 2. Formato
        sb.Append(PadStr(Formato, 2));

        // 3. Fecha Generacion
        sb.Append(PadStr(FechaGeneracion, 8));

        // 4. Lote
        sb.Append(PadStr(Lote, 4));

        // 5. Secuencial
        sb.Append(PadStr(Secuencial, 7));

        // 6. Pagina
        sb.Append(PadStr(Pagina, 4));

        // 7. Cod Documento
        sb.Append(PadStr(CodDocumento, 6));

        // 8. Version (Zeros)
        sb.Append(PadNum(Version, 4));

        // 9. Destino
        sb.Append(PadStr(ClaseContrato, 2));
        sb.Append(PadStr(CodContrato, 25));
        sb.Append(PadStr(TIREL, 1));
        sb.Append(PadNum(NUREL, 3));
        sb.Append(PadStr(CLALF, 15));
        sb.Append(PadNum(INDOM, 2));

        // 10. Forzar Envio
        sb.Append(PadStr(ForzarEnvio, 1));

        // 11. Idioma
        sb.Append(PadStr(Idioma, 2));

        // 12. Op Ahorro (48 bytes filler as per Go implementation of non-ahorro default)
        sb.Append(PadStr("", 2));  // AH Code
        sb.Append(PadStr("", 25)); // Account
        sb.Append(PadStr("", 1));  // Sign
        sb.Append(PadNum(0, 13));  // Amount
        sb.Append(PadStr("", 2));  // Currency
        sb.Append(PadStr("", 3));  // ISO
        sb.Append(PadStr("", 2));  // Concept

        // 13. Fecha Carta
        sb.Append(PadStr(FechaCarta, 8));

        // 14. Indicator Destino
        sb.Append(PadStr(IndDestino, 1));

        // 15. Detalle Carga
        sb.Append(PadNum(DetalleCarga, 4));

        // 16. Via Reparto
        sb.Append(PadStr(ViaReparto, 2));

        // 17. Copia Papel
        sb.Append(PadStr(CopiaPapel, 1));

        // 18. Oficina
        sb.Append(PadStr(Oficina, 5));

        // 19. Mail/Fax
        sb.Append(PadStr(MailFax, 50));

        // 20. Longitud Contenido
        sb.Append(PadNum(LongitudContenido, 5));

        // 21. Nombre PDF
        sb.Append(PadStr(NombrePDF, 40));

        return sb.ToString();
    }
}
