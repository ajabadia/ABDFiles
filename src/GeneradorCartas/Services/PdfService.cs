using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GeneradorCartas.Services;

/// <summary>
/// Legacy PDF Service using Microsoft Word Interop (Late Binding)
/// </summary>
public class PdfService : IPdfService
{
    public void ConvertDocxToPdf(string inputDocx, string outputPdf)
    {
        if (!File.Exists(inputDocx)) throw new FileNotFoundException("DOCX file not found", inputDocx);

        // Use Late Binding (Reflection) to avoid version mismatch and DLL dependnecy hell.
        // This requires Word to be installed on the machine.
        Type wordType = Type.GetTypeFromProgID("Word.Application", true);
        if (wordType == null) throw new Exception("Word.Application not found on this machine.");

        dynamic wordApp = Activator.CreateInstance(wordType);
        dynamic doc = null;

        try
        {
            wordApp.Visible = false;
            wordApp.ScreenUpdating = false;
            // 0 = wdAlertsNone
            wordApp.DisplayAlerts = 0; 

            // Open document: Open(FileName, ConfirmConversions, ReadOnly, ...)
            doc = wordApp.Documents.Open(FileName: inputDocx, ReadOnly: true, Visible: false, AddToRecentFiles: false);

            if (doc != null)
            {
                // ExportAsFixedFormat
                // 17 = wdExportFormatPDF
                doc.ExportAsFixedFormat(
                    OutputFileName: outputPdf, 
                    ExportFormat: 17, 
                    OpenAfterExport: false, 
                    OptimizeFor: 0, 
                    Range: 0, 
                    Item: 0, 
                    IncludeDocProps: true, 
                    KeepIRM: true, 
                    CreateBookmarks: 0
                );
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error converting to PDF via Word Interop: {ex.Message}", ex);
        }
        finally
        {
            if (doc != null)
            {
                try 
                { 
                    // Close(SaveChanges: 0 [wdDoNotSaveChanges])
                    doc.Close(SaveChanges: 0); 
                    Marshal.ReleaseComObject(doc);
                } 
                catch { }
            }
            if (wordApp != null)
            {
                try 
                { 
                    // Quit(SaveChanges: 0)
                    wordApp.Quit(SaveChanges: 0); 
                    Marshal.ReleaseComObject(wordApp);
                } 
                catch { }
            }
        }
    }
}
