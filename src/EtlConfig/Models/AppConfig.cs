using System;
using System.IO;

namespace EtlConfig.Models;

public class AppConfig
{
    public string DefaultSavePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ABD", "Presets", "ETL");
    public string Language { get; set; } = "es-ES";
    public string DefaultEncoding { get; set; } = "utf-8";
    public int DefaultChunkSize { get; set; } = 900000;
}
