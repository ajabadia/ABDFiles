using ABDTools.Core.Gaweb.Models;
using System.Text.Json;

namespace LetterConfig.Services;

public static class PresetService
{
    private static readonly string PresetsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ABDTools", "Presets", "GAWEB");

    public static List<GawebPreset> LoadPresets()
    {
        var list = new List<GawebPreset>();
        if (!Directory.Exists(PresetsDir)) return list;

        foreach (var file in Directory.GetFiles(PresetsDir, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<GawebPreset>(json);
                if (preset != null)
                {
                    if (string.IsNullOrEmpty(preset.Id)) preset.Id = Path.GetFileName(file);
                    list.Add(preset);
                }
            }
            catch { /* Ignore bad files */ }
        }
        return list;
    }

    public static void SavePreset(GawebPreset preset)
    {
        if (!Directory.Exists(PresetsDir)) Directory.CreateDirectory(PresetsDir);

        if (string.IsNullOrEmpty(preset.Id))
        {
            preset.Id = $"preset_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.json";
        }
        else if (!preset.Id.EndsWith(".json"))
        {
            preset.Id += ".json";
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(preset, options);
        File.WriteAllText(Path.Combine(PresetsDir, preset.Id), json);
    }

    public static void DeletePreset(string id)
    {
         var path = Path.Combine(PresetsDir, id);
         if (File.Exists(path)) File.Delete(path);
    }
}
