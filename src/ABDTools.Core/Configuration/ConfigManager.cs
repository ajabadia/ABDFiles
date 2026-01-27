using System.Text.Json;
using System.Text.Json.Serialization;

namespace ABDTools.Core.Configuration;

public record ConfigResult<T> where T : class, new()
{
    public T Value { get; init; } = new();
    public bool Exists { get; init; }
    public bool IsCorrupted { get; init; }
    public Exception? Error { get; init; }
}

/// <summary>
/// Application configuration manager
/// </summary>
public class ConfigManager<T> where T : class, new()
{
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConfigManager(string appName, string configFileName = "config.json")
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appDataPath, "ABDTools", appName);
        Directory.CreateDirectory(appFolder);
        
        _configPath = Path.Combine(appFolder, configFileName);
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Gets the configuration file path
    /// </summary>
    public string ConfigPath => _configPath;

    /// <summary>
    /// Loads configuration from disk
    /// </summary>
    public async Task<ConfigResult<T>> LoadAsync()
    {
        var result = new ConfigResult<T>();

        if (!File.Exists(_configPath))
        {
            return result; // Exists = false, Value = new T()
        }

        result = result with { Exists = true };

        try
        {
            var json = await File.ReadAllTextAsync(_configPath).ConfigureAwait(false);
            var value = JsonSerializer.Deserialize<T>(json, _jsonOptions) ?? new T();
            return result with { Value = value };
        }
        catch (Exception ex)
        {
            // Opcional: backup del fichero roto
            try
            {
                var backupPath = _configPath + ".bak";
                if (File.Exists(_configPath))
                {
                    File.Copy(_configPath, backupPath, overwrite: true);
                }
            }
            catch
            {
                // Ignorar errores de backup
            }

            return result with
            {
                Value = new T(),
                IsCorrupted = true,
                Error = ex
            };
        }
    }

    /// <summary>
    /// Saves configuration to disk
    /// </summary>
    public async Task SaveAsync(T config)
    {
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(_configPath, json).ConfigureAwait(false);
    }
}
