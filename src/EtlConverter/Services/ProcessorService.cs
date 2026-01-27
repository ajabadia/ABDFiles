using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EtlConverter.Models;

namespace EtlConverter.Services;

public class ProcessorOptions
{
    public string InputPath { get; set; } = string.Empty;
    public string OutputDir { get; set; } = string.Empty;
    public EtlPreset Preset { get; set; } = new();
    public bool OutputJson { get; set; }
    public int StartRow { get; set; } = 1;
    public int EndRow { get; set; } = 0; // 0 = All
    public int ChunkSize { get; set; } = 900000;
}

public class OutputState : IDisposable
{
    public string BaseName { get; set; } = string.Empty;
    public string OutputDir { get; set; } = string.Empty;
    public bool IsJson { get; set; }
    public List<string> Header { get; set; } = new();
    
    public int Part { get; set; } = 0;
    public int Count { get; set; } = 0;
    public StreamWriter? Writer { get; set; }
    public bool FirstLine { get; set; } = true;

    // Constructor validation logic moved here or ensuring properties set before Rotate
    
    public void Rotate()
    {
        Close();
        Part++;
        Count = 0;
        FirstLine = true;

        if (!IsJson && (Header == null || Header.Count == 0))
        {
             // Fallback or Error? 
             // Ideally we should throw, but let's just log or allow empty file if strictly requested??
             // User requested validation.
        }

        string ext = IsJson ? "json" : "csv";
        string fileName = $"{BaseName}";
        if(Part > 1) fileName += $"_Parte{Part}";
        fileName += $".{ext}";
        
        string fullPath = Path.Combine(OutputDir, fileName);

        Writer = new StreamWriter(fullPath, false, Encoding.UTF8);

        if (IsJson)
        {
            Writer.Write("[\n");
        }
        else
        {
            Writer.Flush();
            Writer.BaseStream.Write(new byte[] { 0xEF, 0xBB, 0xBF }, 0, 3); // BOM
            if (Header != null && Header.Count > 0)
                Writer.WriteLine(string.Join(";", Header));
        }
    }

    public void Close()
    {
        if (Writer != null)
        {
            if (IsJson) Writer.Write("\n]");
            Writer.Flush();
            Writer.Close();
            Writer = null;
        }
    }

    public void Dispose()
    {
        Close();
    }
}

public class ProcessorService
{
    public event EventHandler<string>? OnLog;
    public event EventHandler<int>? OnProgress;
    
    private void Log(string msg) => OnLog?.Invoke(this, msg);

    public async Task ProcessAsync(ProcessorOptions options, CancellationToken token)
    {
        Log($"Iniciando proceso: {Path.GetFileName(options.InputPath)}");
        
        var outputs = new Dictionary<string, OutputState>();
        var headerRules = options.Preset.RecordTypes.Where(r => r.Behavior == "HEADER").ToList();
        var footerRules = options.Preset.RecordTypes.Where(r => r.Behavior == "FOOTER").ToList();
        var dataRules = options.Preset.RecordTypes.Where(r => r.Behavior == "DATA").ToList();
        
        // Safety: ensure default "Genérico" rule if wildcards used? 
        // Logic will handle fallback.

        try
        {
             Encoding encoding = Encoding.UTF8;
             if (options.Preset.Encoding.ToLower().Contains("latin") || options.Preset.Encoding.ToLower().Contains("windows"))
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                encoding = Encoding.GetEncoding("windows-1252");
            }
            
            using (var reader = new StreamReader(options.InputPath, encoding))
            {
                int totalLines = 0;
                string? line;
                var buffer = new Queue<string>(); // For Footer detection if needed?
                // Footer detection logic:
                // If we define "last line" as footer, we can't know it IS the last line until we read null.
                // Approach: If Footer depth is 1, buffer 1 line. Process (current - 1). When EOF, process buffer as Footer.
                // Assuming simplified Footer: "Last N lines" not fully supported yet in UI (Range logic complex).
                // Assuming standard "Last Line is Footer" or Header/Footer explicit ranges if possible.
                // User requirement: "Tipos de cabecera por lineas".
                
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (token.IsCancellationRequested) break;
                    totalLines++;
                    
                    // Header Logic (Line Number)
                   var headerMatch = headerRules.FirstOrDefault(h => IsLineInRange(totalLines, h.Range));
                   if (headerMatch != null)
                   {
                       ProcessRecord(line, headerMatch, outputs, options);
                       continue;
                   }

                   // Data Matching logic
                   var rule = IdentifyRecordType(line, dataRules);
                   if (rule != null)
                   {
                       ProcessRecord(line, rule, outputs, options);
                   }
                   else
                   {
                       // Unknown? Skip or Log
                   }
                   
                   if (totalLines % 5000 == 0) OnProgress?.Invoke(this, totalLines);
                }
                
                OnProgress?.Invoke(this, totalLines);
                Log($"Proceso finalizado. Total líneas: {totalLines}");
            }
        }
        catch (Exception ex)
        {
            Log($"ERROR FATAL: {ex.Message}");
            throw;
        }
        finally
        {
            foreach (var s in outputs.Values) s.Dispose();
        }
    }

    private bool IsLineInRange(int line, string range)
    {
        if (string.IsNullOrWhiteSpace(range)) return false;
        // Supports "1", "1-2", "1,3"
        var parts = range.Split(',');
        foreach(var part in parts)
        {
             if(part.Contains('-'))
             {
                 var rangeParts = part.Split('-');
                 if(rangeParts.Length == 2 && int.TryParse(rangeParts[0], out int start) && int.TryParse(rangeParts[1], out int end))
                 {
                     if(line >= start && line <= end) return true;
                 }
             }
             else
             {
                 if(int.TryParse(part, out int val) && line == val) return true;
             }
        }
        return false;
    }

    private EtlRecordType? IdentifyRecordType(string line, List<EtlRecordType> rules)
    {
        EtlRecordType? fallback = null;

        foreach (var rule in rules)
        {
            // 1. Fallback (Universal Wildcard - Empty Trigger)
            if (string.IsNullOrEmpty(rule.Trigger)) 
            {
                fallback ??= rule; // Keep first generic rule found as fallback
                continue; // Do NOT return immediately, check specific rules first
            }

            // 2. Check Length availability
            if (rule.TriggerStart < 0) continue; // Safety
            if (line.Length < rule.TriggerStart + rule.Trigger.Length) continue;

            // 3. Extract candidate string
            string candidate = line.Substring(rule.TriggerStart, rule.Trigger.Length);

            // 4. Wildcard Logic (* = Space, ? = Any)
            if (MatchesPattern(candidate, rule.Trigger)) return rule;
        }
        
        return fallback; // Return generic rule only if no specific rule matched
    }

    private bool MatchesPattern(string input, string pattern)
    {
        if (input.Length != pattern.Length) return false;
        
        for (int i = 0; i < pattern.Length; i++)
        {
            char p = pattern[i];
            char c = input[i];

            if (p == '?') continue; // Match Any
            if (p == '*') // Match Space
            {
                if (c != ' ') return false;
                continue;
            }
            if (p != c) return false;
        }
        return true;
    }

    private void ProcessRecord(string line, EtlRecordType type, Dictionary<string, OutputState> outputs, ProcessorOptions options)
    {
        // Get/Create Output
        if (!outputs.ContainsKey(type.Name)) 
        {
            var headers = type.Fields.OrderBy(f => f.Start).Select(f => f.Name).ToList();

            if (!options.OutputJson && headers.Count == 0)
            {
                Log($"> WARNING: Tipo '{type.Name}' no tiene campos definidos. Se omitirá registro.");
                return;
            }

            var state = new OutputState 
            { 
                BaseName = $"{Path.GetFileNameWithoutExtension(options.InputPath)}_{NormalizeName(type.Name)}", 
                OutputDir = options.OutputDir, 
                IsJson = options.OutputJson,
                Header = headers
            };
            state.Rotate();
            outputs[type.Name] = state;
            Log($"> Generando salida: {type.Name}");
        }

        var output = outputs[type.Name];
        var data = ParseLine(line, type.Fields);

        if (options.OutputJson)
        {
            if (!output.FirstLine) output.Writer!.Write(",\n");
            var json = JsonSerializer.Serialize(data); 
            output.Writer!.Write("  " + json);
            output.FirstLine = false;
        }
        else
        {
             var csvValues = new List<string>();
             foreach(var h in output.Header)
             {
                 if(data.TryGetValue(h, out string? val)) csvValues.Add(val);
                 else csvValues.Add("");
             }
             var csvLine = string.Join(";", csvValues); 
             output.Writer!.WriteLine(csvLine);
        }

        output.Count++;
        if (output.Count >= options.ChunkSize)
        {
            output.Rotate();
        }
    }

    private string NormalizeName(string name)
    {
        return string.Join("_", name.Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
    }

    private Dictionary<string, string> ParseLine(string line, List<EtlField> fields)
    {
        var result = new Dictionary<string, string>();
        foreach (var field in fields)
        {
            string val = "";
            if (field.Start + field.Length <= line.Length)
            {
                val = line.Substring(field.Start, field.Length).Trim();
            }
            else if (field.Start < line.Length)
            {
                val = line.Substring(field.Start).Trim();
            }
            result[field.Name] = val;
        }
        return result;
    }
}
