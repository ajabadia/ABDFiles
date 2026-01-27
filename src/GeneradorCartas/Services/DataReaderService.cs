using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace GeneradorCartas.Services;

/// <summary>
/// Service to read data from CSV or Excel files
/// </summary>
public static class DataReaderService
{
    /// <summary>
    /// Read headers and sample row from data file (CSV or Excel)
    /// </summary>
    public static (List<string> Headers, List<string> SampleRow) ReadHeadersAndSample(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return (new List<string>(), new List<string>());

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".xlsx")
            return ReadExcelHeadersAndSample(filePath);
        else
            return ReadCsvHeadersAndSample(filePath);
    }

    /// <summary>
    /// Read all data from file as list of dictionaries (column -> value)
    /// </summary>
    public static List<Dictionary<string, string>> ReadAllData(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return new List<Dictionary<string, string>>();

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".xlsx")
            return ReadExcelData(filePath);
        else
            return ReadCsvData(filePath);
    }

    private static (List<string> Headers, List<string> SampleRow) ReadCsvHeadersAndSample(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            string? headerLine = reader.ReadLine();
            string? sampleLine = reader.ReadLine();

            var headers = headerLine?.Split(';').Select(c => c.Trim()).ToList() ?? new List<string>();
            var sample = sampleLine?.Split(';').Select(c => c.Trim()).ToList() ?? new List<string>();

            return (headers, sample);
        }
        catch
        {
            return (new List<string>(), new List<string>());
        }
    }

    private static (List<string> Headers, List<string> SampleRow) ReadExcelHeadersAndSample(string filePath)
    {
        try
        {
            using var doc = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null) return (new List<string>(), new List<string>());

            var sheet = workbookPart.Workbook.Sheets?.GetFirstChild<Sheet>();
            if (sheet == null) return (new List<string>(), new List<string>());

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null) return (new List<string>(), new List<string>());

            var rows = sheetData.Elements<Row>().Take(2).ToList();
            
            var headers = new List<string>();
            var sample = new List<string>();

            if (rows.Count > 0)
                headers = GetRowValues(rows[0], workbookPart);
            if (rows.Count > 1)
                sample = GetRowValues(rows[1], workbookPart);

            return (headers, sample);
        }
        catch
        {
            return (new List<string>(), new List<string>());
        }
    }

    private static List<Dictionary<string, string>> ReadCsvData(string filePath)
    {
        var result = new List<Dictionary<string, string>>();
        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length < 2) return result;

            var headers = lines[0].Split(';').Select(h => h.Trim()).ToArray();

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var values = lines[i].Split(';');
                var row = new Dictionary<string, string>();
                for (int j = 0; j < headers.Length && j < values.Length; j++)
                {
                    row[headers[j]] = values[j].Trim();
                }
                result.Add(row);
            }
        }
        catch { }
        return result;
    }

    private static List<Dictionary<string, string>> ReadExcelData(string filePath)
    {
        var result = new List<Dictionary<string, string>>();
        try
        {
            using var doc = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = doc.WorkbookPart;
            if (workbookPart == null) return result;

            var sheet = workbookPart.Workbook.Sheets?.GetFirstChild<Sheet>();
            if (sheet == null) return result;

            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
            var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
            if (sheetData == null) return result;

            var rows = sheetData.Elements<Row>().ToList();
            if (rows.Count < 2) return result;

            var headers = GetRowValues(rows[0], workbookPart);

            for (int i = 1; i < rows.Count; i++)
            {
                var values = GetRowValues(rows[i], workbookPart);
                var row = new Dictionary<string, string>();
                for (int j = 0; j < headers.Count && j < values.Count; j++)
                {
                    row[headers[j]] = values[j];
                }
                result.Add(row);
            }
        }
        catch { }
        return result;
    }

    private static List<string> GetRowValues(Row row, WorkbookPart workbookPart)
    {
        var values = new List<string>();
        var stringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

        foreach (var cell in row.Elements<Cell>())
        {
            string value = cell.CellValue?.Text ?? "";

            // If cell references shared string table
            if (cell.DataType?.Value == CellValues.SharedString && stringTable != null)
            {
                if (int.TryParse(value, out int idx))
                {
                    value = stringTable.ElementAt(idx).InnerText;
                }
            }

            values.Add(value);
        }

        return values;
    }
}
