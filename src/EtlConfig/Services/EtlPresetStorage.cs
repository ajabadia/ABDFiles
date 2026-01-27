using System.Text.Json;
using System.Text.Json.Serialization;
using ABDTools.Core.Logging;
using ABDTools.Core.Common;
using EtlConfig.Models; // Ensure namespace correct

namespace EtlConfig.Services;

public sealed class EtlPresetStorage
{
    private readonly string _folder;
    private readonly JsonSerializerOptions _options;

    public EtlPresetStorage()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _folder = Path.Combine(appData, "ABDTools", "EtlConfig", "Presets");
        Directory.CreateDirectory(_folder);

        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public string GetPresetPath(string id)
        => Path.Combine(_folder, $"{FileUtils.SanitizeFilename(id)}.json");

    public EtlPreset Load(string id)
    {
        var path = GetPresetPath(id);
        if (!File.Exists(path))
            throw new FileNotFoundException("No se ha encontrado el preset.", path);

        try
        {
            var json = File.ReadAllText(path);
            var preset = JsonSerializer.Deserialize<EtlPreset>(json, _options);
            if (preset == null)
                throw new InvalidDataException("El preset está vacío o tiene un formato no válido.");

            return preset;
        }
        catch (Exception ex)
        {
            Logger.Error($"Error al cargar preset '{id}'", ex);

            // Backup del fichero roto
            try
            {
                var bak = path + ".bak";
                File.Copy(path, bak, overwrite: true);
            }
            catch
            {
                // ignorar
            }

            throw;
        }
    }

    public void Save(EtlPreset preset)
    {
        if (string.IsNullOrWhiteSpace(preset.Id))
            throw new InvalidOperationException("El preset debe tener Id.");

        var path = GetPresetPath(preset.Id);

        var json = JsonSerializer.Serialize(preset, _options);
        try
        {
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error al guardar preset '{preset.Id}'", ex);
            throw;
        }
    }

    public IEnumerable<EtlPreset> LoadAll()
    {
        var list = new List<EtlPreset>();
        
        if (!Directory.Exists(_folder)) return list;

        foreach (var file in Directory.EnumerateFiles(_folder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<EtlPreset>(json, _options);
                if (preset != null)
                    list.Add(preset);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Preset inválido ignorado: {Path.GetFileName(file)}");
                Logger.Error("Detalle del error al leer preset", ex);

                // Opcional: backup del corrupto
                try
                {
                    var bak = file + ".bak";
                    File.Copy(file, bak, overwrite: true);
                }
                catch
                {
                    // ignorar
                }
            }
        }

        return list;
    }

    // --- File Path Based Methods (For Open/Save As Dialogs) ---

    public EtlPreset LoadFromFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Fichero no encontrado", path);
        try
        {
             var json = File.ReadAllText(path);
             var preset = JsonSerializer.Deserialize<EtlPreset>(json, _options);
             return preset ?? throw new InvalidDataException("Formato inválido/vacío");
        }
        catch (Exception ex)
        {
             Logger.Error($"Error cargando fichero {path}", ex);
             // User requested specific error behavior or backup?
             throw; // Re-throw to let UI handle message
        }
    }

    public void SaveToFile(EtlPreset preset, string path)
    {
        try
        {
             var json = JsonSerializer.Serialize(preset, _options);
             File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
             Logger.Error($"Error guardando fichero {path}", ex);
             throw;
        }
    }

    public void Delete(string id)
    {
        var path = GetPresetPath(id);
        if (!File.Exists(path)) return;

        try
        {
            File.Delete(path);
        }
        catch (Exception ex)
        {
            Logger.Error($"No se ha podido borrar el preset '{id}'", ex);
            throw;
        }
    }
}
