using System;
using System.IO;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Pdf;

namespace GeneradorCartas.Services;

public class SyncfusionPdfService : IPdfService
{
    public void ConvertDocxToPdf(string inputDocx, string outputPdf)
    {
        if (!File.Exists(inputDocx)) 
            throw new FileNotFoundException("DOCX file not found", inputDocx);

        try
        {
            // Open the document
            using (FileStream inputStream = new FileStream(inputDocx, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (WordDocument wordDocument = new WordDocument(inputStream, FormatType.Docx))
                {
                    // Initialization of DocIORenderer
                    using (DocIORenderer renderer = new DocIORenderer())
                    {
                        // Set specific settings for high fidelity if needed
                        renderer.Settings.EmbedFonts = true;
                        
                        // Convert Word document into PDF document
                        using (PdfDocument pdfDocument = renderer.ConvertToPDF(wordDocument))
                        {
                            // Save the PDF document
                            using (FileStream outputStream = new FileStream(outputPdf, FileMode.Create, FileAccess.Write))
                            {
                                pdfDocument.Save(outputStream);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error converting to PDF via Syncfusion: {ex.Message}", ex);
        }
    }
}
