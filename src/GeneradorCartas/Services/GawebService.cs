using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ABDTools.Core.Gaweb.Models;

namespace GeneradorCartas.Services;

public class GawebService
{
    public string GenerateMd5(string text)
    {
        using (var md5 = MD5.Create())
        {
            byte[] bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(text));
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    public string GenerateFileMd5(string filePath)
    {
        using (var md5 = MD5.Create())
        using (var stream = File.OpenRead(filePath))
        {
            byte[] bytes = md5.ComputeHash(stream);
            var sb = new StringBuilder();
            foreach (var b in bytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    public void CreateMetaFiles(string zipPath, string baseName, string outputDir)
    {
        // 1. MD5 of ZIP
        string zipMd5 = GenerateFileMd5(zipPath);
        string md5FileName = Path.Combine(outputDir, baseName + ".MD5");
        File.WriteAllText(md5FileName, zipMd5);
    }
    
    public void ZipDirectory(string sourceDir, string zipPath)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(sourceDir, zipPath, CompressionLevel.Optimal, false);
    }
    
    public string CalculateGawebPdfName(string baseMd5, int sequence)
    {
        // {BaseMD5 (32)}{Sequence (8)} = 40 chars
        // Sequence is 8 digits padded
        return $"{baseMd5}{sequence:D8}";
    }
}
