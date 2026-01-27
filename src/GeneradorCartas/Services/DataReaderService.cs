using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace GeneradorCartas.Services;

/// <summary>
/// Service to read data from CSV or Excel files with streaming support
/// </summary>
public class DataReaderService
{
    private readonly CsvConfiguration _csvConfig;

    public DataReaderService()
    {
        _csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";",
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null
        };
    }

    /// <summary>
    /// Read headers and sample row from data file (CSV or Excel)
    /// </summary>
    public (List<string> Headers, List<string> SampleRow) ReadHeadersAndSample(string filePath)
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
    /// Streams all data from file as an enumerable of dictionaries
    /// </summary>
    public IEnumerable<Dictionary<string, string>> StreamData(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            yield break;

        string ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ext == ".xlsx")
        {
            foreach (var row in ReadExcelDataStreaming(filePath))
                yield return row;
        }
        else
        {
            foreach (var row in ReadCsvDataStreaming(filePath))
                yield return row;
        }
    }

    private (List<string> Headers, List<string> SampleRow) ReadCsvHeadersAndSample(string filePath)
    {
        try
        {
            using var reader = new StreamReader(filePath);
            using var csv = new CsvReader(reader, _csvConfig);
            
            if (!csv.Read() || !csv.ReadHeader())
                return (new List<string>(), new List<string>());

            var headers = csv.HeaderRecord?.ToList() ?? new List<string>();
            
            var sample = new List<string>();
            if (csv.Read())
            {
                for (int i = 0; i < headers.Count; i++)
                {
                    sample.Add(csv.GetField(i) ?? "");
                }
            }

            return (headers, sample);
        }
        catch
        {
            return (new List<string>(), new List<string>());
        }
    }

    private IEnumerable<Dictionary<string, string>> ReadCsvDataStreaming(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, _csvConfig);
        
        if (!csv.Read() || !csv.ReadHeader())
            yield break;

        var headers = csv.HeaderRecord;
        if (headers == null) yield break;

        while (csv.Read())
        {
            var row = new Dictionary<string, string>();
            foreach (var header in headers)
            {
                row[header] = csv.GetField(header) ?? "";
            }
            yield return row;
        }
    }

    private (List<string> Headers, List<string> SampleRow) ReadExcelHeadersAndSample(string filePath)
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

    private IEnumerable<Dictionary<string, string>> ReadExcelDataStreaming(string filePath)
    {
        using var doc = SpreadsheetDocument.Open(filePath, false);
        var workbookPart = doc.WorkbookPart;
        if (workbookPart == null) yield break;

        var sheet = workbookPart.Workbook.Sheets?.GetFirstChild<Sheet>();
        if (sheet == null) yield break;

        var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!);
        var sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();
        if (sheetData == null) yield break;

        var rowsEnum = sheetData.Elements<Row>().GetEnumerator();
        if (!rowsEnum.MoveNext()) yield break;

        var headers = GetRowValues(rowsEnum.Current, workbookPart);

        while (rowsEnum.MoveNext())
        {
            var values = GetRowValues(rowsEnum.Current, workbookPart);
            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Count && j < values.Count; j++)
            {
                row[headers[j]] = values[j];
            }
            yield return row;
        }
    }

    private List<string> GetRowValues(Row row, WorkbookPart workbookPart)
    {
        var values = new List<string>();
        var stringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

        foreach (var cell in row.Elements<Cell>())
        {
            string value = cell.CellValue?.Text ?? "";

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
