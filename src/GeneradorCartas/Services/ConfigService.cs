using System.Globalization;
using System.Text.Json;
using ABDTools.Core.Gaweb.Models;
using GeneradorCartas.Models;

namespace GeneradorCartas.Services;

public class ConfigService
{
    private readonly string _presetsBasePath;
    
    public ConfigService(string presetsBasePath = "presets/gaweb")
    {
        _presetsBasePath = presetsBasePath;
    }

    /// <summary>
    /// Registers the Syncfusion license key. 
    /// Should be called on application startup.
    /// </summary>
    public static void RegisterLicense(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(key);
        }
    }

    /// <summary>
    /// Loads a GenerationConfig from a JSON file, validating its type
    /// </summary>
    public GenerationConfig LoadConfig(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Configuration file not found", path);

        string json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<GenerationConfig>(json);

        if (config == null)
            throw new InvalidDataException("Failed to parse configuration file");

        if (!config.IsValidType())
            throw new InvalidDataException($"Invalid configuration type. Expected 'GeneradorCartas.GenerationConfig', got '{config.Type}'");

        return config;
    }

    /// <summary>
    /// Saves a GenerationConfig to a JSON file
    /// </summary>
    public void SaveConfig(GenerationConfig config, string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        string json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Creates a new empty configuration with defaults
    /// </summary>
    public GenerationConfig CreateNew()
    {
        return new GenerationConfig
        {
            OutputDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "CartasGeneradas")
        };
    }

    /// <summary>
    /// Lists available GAWEB presets from the presets folder
    /// </summary>
    public List<string> ListPresets()
    {
        var presets = new List<string>();
        
        if (Directory.Exists(_presetsBasePath))
        {
            foreach (var file in Directory.GetFiles(_presetsBasePath, "*.json"))
            {
                presets.Add(Path.GetFileName(file));
            }
        }

        return presets;
    }

    /// <summary>
    /// Loads a GAWEB preset and applies overrides from the config
    /// </summary>
    public GawebPreset LoadPresetWithOverrides(GenerationConfig config)
    {
        string presetPath = config.PresetPath;
        
        // Resolve relative paths
        if (!Path.IsPathRooted(presetPath))
        {
            presetPath = Path.Combine(_presetsBasePath, presetPath);
        }

        if (!File.Exists(presetPath))
            throw new FileNotFoundException("Preset file not found", presetPath);

        string json = File.ReadAllText(presetPath);
        var preset = JsonSerializer.Deserialize<GawebPreset>(json);

        if (preset == null)
            throw new InvalidDataException("Failed to parse preset file");

        // Apply overrides
        var overrides = config.Overrides;
        
        if (!string.IsNullOrEmpty(overrides.FechaGeneracion))
            preset.FechaGeneracion = overrides.FechaGeneracion;
        
        if (!string.IsNullOrEmpty(overrides.FechaCarta))
            preset.FechaCarta = overrides.FechaCarta;
        
        // Note: Lote is handled at runtime, not stored in preset
        
        if (!string.IsNullOrEmpty(overrides.CodigoDocumento))
            preset.CodigoDocumento = overrides.CodigoDocumento;
        
        if (!string.IsNullOrEmpty(overrides.Oficina))
            preset.Oficina = overrides.Oficina;
        
        if (!string.IsNullOrEmpty(overrides.CodigoEntorno))
            preset.CodigoEntorno = overrides.CodigoEntorno;
        
        if (!string.IsNullOrEmpty(overrides.Idioma))
            preset.Idioma = overrides.Idioma;
        
        if (overrides.PaginasDefecto.HasValue)
            preset.PaginasDefecto = overrides.PaginasDefecto.Value;

        return preset;
    }

    /// <summary>
    /// Validates a GenerationConfig before generation
    /// </summary>
    public List<string> ValidateConfig(GenerationConfig config)
    {
        var errors = new List<string>();

        // Required files
        if (string.IsNullOrWhiteSpace(config.DataFilePath))
            errors.Add("Archivo de datos es requerido");
        else if (!File.Exists(config.DataFilePath))
            errors.Add($"Archivo de datos no encontrado: {config.DataFilePath}");

        if (string.IsNullOrWhiteSpace(config.TemplatePath))
            errors.Add("Plantilla DOCX es requerida");
        else if (!File.Exists(config.TemplatePath))
            errors.Add($"Plantilla no encontrada: {config.TemplatePath}");

        if (string.IsNullOrWhiteSpace(config.PresetPath))
            errors.Add("Preset GAWEB es requerido");

        // Validate date overrides if present
        if (!string.IsNullOrEmpty(config.Overrides.FechaGeneracion))
        {
            if (!IsValidDate(config.Overrides.FechaGeneracion))
                errors.Add("Fecha Generación inválida (formato: YYYYMMDD)");
        }

        if (!string.IsNullOrEmpty(config.Overrides.FechaCarta))
        {
            if (!IsValidDate(config.Overrides.FechaCarta))
                errors.Add("Fecha Carta inválida (formato: YYYYMMDD)");
        }

        // Validate field lengths
        if (!string.IsNullOrEmpty(config.Overrides.CodigoDocumento) && config.Overrides.CodigoDocumento.Length != 6)
            errors.Add("Código Documento debe tener exactamente 6 caracteres");

        if (!string.IsNullOrEmpty(config.Overrides.Oficina) && config.Overrides.Oficina.Length != 5)
            errors.Add("Oficina debe tener exactamente 5 caracteres");

        if (!string.IsNullOrEmpty(config.Overrides.Lote) && config.Overrides.Lote.Length > 4)
            errors.Add("Lote no puede exceder 4 caracteres");

        // Range validation
        if (config.RangeFrom.HasValue && config.RangeTo.HasValue)
        {
            if (config.RangeFrom > config.RangeTo)
                errors.Add("'Desde' no puede ser mayor que 'Hasta'");
        }

        return errors;
    }

    private bool IsValidDate(string dateStr)
    {
        if (string.IsNullOrEmpty(dateStr) || dateStr.Length != 8)
            return false;

        return DateTime.TryParseExact(dateStr, "yyyyMMdd", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    /// <summary>
    /// Converts a DateTime to YYYYMMDD string format
    /// </summary>
    public static string DateToString(DateTime date) => date.ToString("yyyyMMdd");

    /// <summary>
    /// Parses a YYYYMMDD string to DateTime
    /// </summary>
    public static DateTime? StringToDate(string dateStr)
    {
        if (DateTime.TryParseExact(dateStr, "yyyyMMdd", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
        {
            return result;
        }
        return null;
    }
}
