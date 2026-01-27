using System.Globalization;
using ABDTools.Core.Gaweb.Models;

namespace ABDTools.Core.Gaweb;

public class ValidationResult
{
    public int LineNumber { get; set; }
    public bool IsValid { get; set; } = true;
    public List<FieldError> Errors { get; set; } = new List<FieldError>();
    public List<FieldError> Warnings { get; set; } = new List<FieldError>();
    public string RawLine { get; set; } = string.Empty;
    public Dictionary<string, FieldValidation> Fields { get; set; } = new Dictionary<string, FieldValidation>();
}

public class FieldError
{
    public string FieldName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty; // "27-32"
    public string Expected { get; set; } = string.Empty;
    public string Got { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "error" | "warning"
    public string Message { get; set; } = string.Empty;
}

public class FieldValidation
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public string Expected { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public static class GawebValidator
{
    public static List<ValidationResult> ValidateGawebFile(IEnumerable<string> lines)
    {
        var results = new List<ValidationResult>();
        int i = 1;
        foreach (var line in lines)
        {
            results.Add(ValidateRecord(line, i));
            i++;
        }
        return results;
    }

    public static ValidationResult ValidateRecord(string line, int lineNum)
    {
        var result = new ValidationResult
        {
            LineNumber = lineNum,
            IsValid = true,
            RawLine = line
        };

        // Validate total length
        // Validate total length
        // Spec: "Se terminará el registro en la última posición significativa, no se transmitirán los blancos a fin de registro"
        // So line length can be < 251.
        // But header requires up to 251 (NombrePDF ends at 251).
        
        if (line.Length < 212)
        {
            result.IsValid = false;
            result.Errors.Add(new FieldError
            {
                FieldName = "Longitud Invalida",
                Position = $"1-{line.Length}",
                Expected = "Minimum 212 bytes (Header)",
                Got = $"{line.Length} bytes",
                Severity = "error",
                Message = "Longitud insuficiente para contener la cabecera completas."
            });
            return result; 
        }

        // If line is short but valid (trimmed), we pad it virtualy for validation
        if (line.Length < 251)
        {
             // It's technically valid if trimmed. Warning maybe?
             // "no se transmitirán los blancos a fin de registro" -> Valid.
             // We pad for field extraction
             line = line.PadRight(251, ' ');
        }
        else if (line.Length > 251)
        {
            // Check if it is Overlay?
            // "Tamaño/Formato" (Pos 2-3) -> 01, 02, 03 are Overlay.
            // If so, length > 251 is expected.
            // If 04/05 (PDF), length > 251 is strictly suspicious but maybe padding?
            // "Longitud máxima de registro: 10.419" -> So > 251 is DEFINITELY allowed.
        }

        int pos = 0;

        foreach (var fieldDef in GawebConstants.Fields)
        {
            Func<string, string?>? validator = GetValidatorForField(fieldDef);
            ValidateField(result, ref pos, line, fieldDef.Name, fieldDef.Length, validator);
        }

        return result;
    }

    private static Func<string, string?>? GetValidatorForField(GawebFieldDefinition field)
    {
        // 1. Specific rules take precedence
        switch (field.Name)
        {
            case "TipoCarta":
                return val => (val != " ") ? "Debe ser ' ' (Fijo blanco)" : null;
            
            case "Formato":
                return val => (!new[] { "01", "02", "03", "04", "05" }.Contains(val)) ? "Debe ser 01-05" : null;

            case "Lote":
            case "CodDocumento":
            case "NombrePDF":
                return val => string.IsNullOrWhiteSpace(val) ? "No puede estar vacío" : null;
            
            case "FechaGeneracion":
            case "FechaCarta":
                return ValidateDate;

            case "Version":
                return val => (val != "0000") ? "Debe ser '0000' (Fijo ceros)" : null;

            case "DetalleCarga":
                return val => (val != "0000") ? "Debe ser '0000' (Fijo cero)" : null;

            case "ForzarEnvio":
                // Vacío, 1, 3, 4, 5, 8
                return val => (!" 13458".Contains(val)) ? "Valor inválido. Permitidos: ' ' (Vacío), 1, 3, 4, 5, 8" : null;

            case "Idioma":
            case "ViaReparto":
            case "CopiaPapel":
             // "Vacío" in spec usually means Space.
             return val => (!string.IsNullOrWhiteSpace(val)) ? "Debe estar vacío (Espacios)" : null;

            case "IndDestino":
                return val => (val != "0" && val != "O" && val != "7") ? "Debe ser '0', 'O' o '7'" : null;

            case "OpAhorroCode":
                return val => (val != "  " && val != "AH") ? "Debe ser '  ' o 'AH'" : null;
        }

        // 2. Generic rules
        if (field.IsNumeric)
        {
            return val => ValidateNumeric(val, field.Name);
        }

        return null;
    }

    private static void ValidateField(ValidationResult result, ref int pos, string line, string name, int length, Func<string, string?>? validator)
    {
        int start = pos;
        int end = pos + length;
        
        if (end > line.Length) end = line.Length;

        string value = line.Substring(start, end - start);
        string position = $"{start + 1}-{end}";

        var fieldVal = new FieldValidation
        {
            Name = name,
            Value = value,
            IsValid = true
        };

        if (validator != null)
        {
            string? error = validator(value);
            if (error != null)
            {
                fieldVal.IsValid = false;
                fieldVal.Message = error;

                result.IsValid = false;
                result.Errors.Add(new FieldError
                {
                    FieldName = name,
                    Position = position,
                    Expected = "",
                    Got = value,
                    Severity = "error",
                    Message = error
                });
            }
        }

        result.Fields[name] = fieldVal;
        pos = end;
    }

    private static string? ValidateDate(string dateStr)
    {
        if (dateStr.Length != 8) return "debe tener 8 dígitos (YYYYMMDD)";
        
        if (!DateTime.TryParseExact(dateStr, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return "fecha inválida (YYYYMMDD)";
        }
        return null;
    }

    private static string? ValidateNumeric(string value, string fieldName)
    {
        string trimmed = value.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null; // Allow empty/spaces as '0' or null, depending on field semantics, but usually spaces are fine for non-mandatory

        if (!long.TryParse(trimmed, out _))
        {
            return "debe ser numérico";
        }
        return null;
    }
}
