using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using GeneradorCartas.Models;
using ABDTools.Core.Gaweb.Models;
using ABDTools.Core.Logging;

namespace GeneradorCartas.Services;

public class GenerationEngine
{
    private readonly ConfigService _configService;
    private readonly IPdfService _pdfService;
    private readonly TemplateService _templateService;
    private readonly GawebService _gawebService;
    private readonly DataReaderService _dataReader;

    public GenerationEngine(
        ConfigService configService,
        IPdfService pdfService,
        TemplateService templateService,
        GawebService gawebService,
        DataReaderService dataReader)
    {
        _configService = configService;
        _pdfService = pdfService;
        _templateService = templateService;
        _gawebService = gawebService;
        _dataReader = dataReader;
    }

    public void Run(GenerationConfig config, IGenerationProgress progress, CancellationToken ct)
    {
        Logger.Info($"Iniciando generación de cartas. Template: {config.TemplatePath}, Data: {config.DataFilePath}");
        try
        {
            // Load preset with overrides
            GawebPreset preset;
            try
            {
                preset = _configService.LoadPresetWithOverrides(config);
            }
            catch (Exception ex)
            {
                progress.ReportLog($"ERROR: No se pudo cargar el preset: {ex.Message}");
                return;
            }

            // Start streaming data
            var dataStream = _dataReader.StreamData(config.DataFilePath);
            
            string batchTimestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string lote = config.Overrides.Lote ?? "0001";
            string codigoEntorno = preset.CodigoEntorno ?? "ENTORNO";
            string baseMd5 = _gawebService.GenerateMd5(batchTimestamp);

            // Create lote folder
            string loteFolder = Path.Combine(config.OutputDirectory, lote);
            Directory.CreateDirectory(loteFolder);
            string tempDir = Path.Combine(loteFolder, $"TEMP_PDF_{batchTimestamp}");
            Directory.CreateDirectory(tempDir);

            var gawebRecords = new List<GawebRecord>();

            int fromRecord = config.RangeFrom ?? 1;
            int toRecord = config.RangeTo ?? int.MaxValue;
            
            // Note: In streaming mode with headers being line 0, 
            // the first data row is index 1.
            
            int processed = 0;
            int recordIndex = 0;

            progress.ReportLog($"Iniciando generación (Rango: {fromRecord} a {(toRecord == int.MaxValue ? "Fin" : toRecord.ToString())})...");

            foreach (var rowData in dataStream)
            {
                recordIndex++; // 1-based index (header was skipped by streamer)
                
                if (recordIndex < fromRecord) continue;
                if (recordIndex > toRecord) break;

                ct.ThrowIfCancellationRequested();

                // Apply variable mapping
                var mappedData = new Dictionary<string, string>();
                foreach (var kv in config.VariableMapping)
                {
                    if (rowData.TryGetValue(kv.Value, out var val))
                        mappedData[kv.Key] = val;
                }
                
                // If no mapping, use direct column names
                if (mappedData.Count == 0)
                    mappedData = rowData;

                string docName = _gawebService.CalculateGawebPdfName(baseMd5, recordIndex);
                string finalPdfName = docName + ".pdf";

                // Build GAWEB record
                var rec = new GawebRecord
                {
                    TipoCarta = " ",
                    Formato = preset.FormatoCarta ?? "04",
                    FechaGeneracion = preset.FechaGeneracion ?? DateTime.Now.ToString("yyyyMMdd"),
                    Lote = lote.PadLeft(4, '0').Substring(0, Math.Min(lote.Length, 4)),
                    Secuencial = recordIndex.ToString().PadLeft(7, '0'),
                    Pagina = preset.PaginasDefecto.ToString().PadLeft(4, '0'),
                    CodDocumento = preset.CodigoDocumento ?? "X00054",
                    FechaCarta = preset.FechaCarta ?? DateTime.Now.ToString("yyyyMMdd"),
                    IndDestino = preset.IndicadorDestino ?? "0",
                    Idioma = preset.Idioma ?? "  ",
                    Oficina = preset.Oficina ?? "00000",
                    NombrePDF = docName
                };
                gawebRecords.Add(rec);

                string tempDocx = Path.Combine(tempDir, docName + ".docx");
                string tempPdf = Path.Combine(tempDir, finalPdfName);

                try
                {
                    _templateService.ProcessTemplate(config.TemplatePath, tempDocx, mappedData);
                    
                    if (config.OutputType == "DOCX")
                    {
                        string finalDocx = Path.Combine(loteFolder, docName + ".docx");
                        File.Move(tempDocx, finalDocx, true);
                    }
                    else
                    {
                        try 
                        {
                            _pdfService.ConvertDocxToPdf(tempDocx, tempPdf);
                            if (File.Exists(tempDocx)) File.Delete(tempDocx);
                        }
                        catch (Exception pdfEx)
                        {
                            Logger.Error($"Error al generar PDF para {docName}", pdfEx);
                            progress.ReportLog($"ERROR AL GENERAR PDF ({docName}): {pdfEx.Message}");
                        }
                    }

                    processed++;
                    progress.ReportProgress(processed, 0, $"Procesado {processed} registros");

                    if (processed % 10 == 0)
                    {
                        Logger.Info($"Progreso: {processed} registros procesados.");
                        progress.ReportLog($"[{processed}] registros completados...");
                    }
                }
                catch (Exception ex)
                {
                    progress.ReportLog($"ERROR registro {recordIndex}: {ex.Message}");
                }
            }

            ct.ThrowIfCancellationRequested();

            if (config.OutputType == "PDF_GAWEB")
            {
                progress.ReportLog("Generando paquete GAWEB...");
                string basePackageName = $"COMUNICADOS.PDF.{codigoEntorno}.{batchTimestamp}.{lote}";

                string gawebFile = Path.Combine(loteFolder, basePackageName + ".GAWEB");
                using (var sw = new StreamWriter(gawebFile, false, System.Text.Encoding.UTF8))
                {
                    foreach (var rec in gawebRecords)
                        sw.WriteLine(rec.Serialize());
                }

                string zipFile = Path.Combine(loteFolder, basePackageName + ".ZIP");
                _gawebService.ZipDirectory(tempDir, zipFile);
                _gawebService.CreateMetaFiles(zipFile, basePackageName, loteFolder);

                Directory.Delete(tempDir, true);
                progress.ReportLog($"✓ Paquete GAWEB completado: {loteFolder}");
            }
            else if (config.OutputType == "PDF")
            {
                foreach (var pdf in Directory.GetFiles(tempDir, "*.pdf"))
                {
                    File.Move(pdf, Path.Combine(loteFolder, Path.GetFileName(pdf)), true);
                }
                Directory.Delete(tempDir, true);
                Logger.Info($"Generación PDF finalizada con éxito en {loteFolder}");
                progress.ReportLog($"✓ PDFs generados en: {loteFolder}");
            }
            else
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
                Logger.Info($"Generación DOCX finalizada con éxito en {loteFolder}");
                progress.ReportLog($"✓ Documentos DOCX generados en: {loteFolder}");
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Warn("Generación cancelada por el usuario.");
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error("Error crítico durante la generación", ex);
            progress.ReportLog($"ERROR CRÍTICO: {ex.Message}");
            throw; 
        }
    }
}
