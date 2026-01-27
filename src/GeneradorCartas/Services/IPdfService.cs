using System;

namespace GeneradorCartas.Services;

public interface IPdfService
{
    void ConvertDocxToPdf(string inputDocx, string outputPdf);
}
