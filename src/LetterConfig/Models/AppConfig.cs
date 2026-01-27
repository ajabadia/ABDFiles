using System;
using System.IO;

namespace LetterConfig.Models;

public class AppConfig
{
    public string DefaultSavePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ABD", "Presets");
    public string DefaultLanguage { get; set; } = "ES";
}
