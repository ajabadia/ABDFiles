using System;
using System.IO;
using System.Runtime.InteropServices;
// using Word = NetOffice.WordApi; // Removed for late binding

namespace GeneradorCartas.Services;

public class PdfService
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
            // We pass standard args. Using named parameters via dynamic can be tricky, so we use positional for Open 
            // or expected defaults.
            // Open(FileName, [ConfirmConversions], [ReadOnly], [AddToRecentFiles], [PasswordDocument], [PasswordTemplate], [Revert], [WritePasswordDocument], [WritePasswordTemplate], [Format], [Encoding], [Visible], [OpenAndRepair], [DocumentDirection], [NoEncodingDialog], [XMLTransform])
            
            // Safer to let defaults handle most, but we want ReadOnly=true, Visible=false
            // C# dynamic allows named arguments match COM dispatch!
            doc = wordApp.Documents.Open(FileName: inputDocx, ReadOnly: true, Visible: false, AddToRecentFiles: false);

            if (doc != null)
            {
                // ExportAsFixedFormat
                // 17 = wdExportFormatPDF
                // 0 = wdExportOptimizeForPrint
                // 0 = wdExportAllDocument
                // 0 = wdExportDocumentContent
                // 0 = wdExportCreateNoBookmarks
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
            throw new Exception($"Error converting to PDF via Late Binding: {ex.Message}", ex);
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
