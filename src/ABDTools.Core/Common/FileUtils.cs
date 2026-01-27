using System.Text.RegularExpressions;

namespace ABDTools.Core.Common;

/// <summary>
/// File and string utilities
/// </summary>
public static partial class FileUtils
{
    /// <summary>
    /// Sanitizes a filename by removing invalid characters
    /// </summary>
    public static string SanitizeFilename(string filename)
    {
        ArgumentException.ThrowIfNullOrEmpty(filename);
        
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", filename.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized.Trim();
    }

    /// <summary>
    /// Formats bytes to human-readable string
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Checks if a file has the .enc extension
    /// </summary>
    public static bool IsEncryptedFile(string path)
    {
        return Path.GetExtension(path).Equals(".enc", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes .enc extension from filename
    /// </summary>
    public static string RemoveEncExtension(string filename)
    {
        if (IsEncryptedFile(filename))
        {
            return Path.GetFileNameWithoutExtension(filename);
        }
        return filename;
    }
}
