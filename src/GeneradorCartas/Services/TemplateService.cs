using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace GeneradorCartas.Services;

public class TemplateService
{
    /// <summary>
    /// Opens a template, replaces {{VARIABLES}} with values from the dictionary, and saves to outputPath.
    /// </summary>
    public void ProcessTemplate(string templatePath, string outputPath, Dictionary<string, string> values)
    {
        if (!File.Exists(templatePath)) throw new FileNotFoundException("Template not found", templatePath);
        
        // Copy template to output path first
        File.Copy(templatePath, outputPath, true);

        // Open the copy
        using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(outputPath, true))
        {
            string docText = null;
            using (StreamReader sr = new StreamReader(wordDoc.MainDocumentPart.GetStream()))
            {
                docText = sr.ReadToEnd();
            }

            // Improved Replacement Logic: Handle split runs (e.g. {{ + VAR + }})
            // Strategy: For each paragraph containing '{{', consolidate text, replace, and rewrite.
            
            var body = wordDoc.MainDocumentPart.Document.Body;
            foreach (var para in body.Descendants<Paragraph>())
            {
                string text = para.InnerText;
                if (text.Contains("{{") && text.Contains("}}"))
                {
                    // This paragraph has variables. Cycle through all replacements.
                    string originalText = text;
                    bool modified = false;

                    foreach (var kvp in values)
                    {
                        string placeholder = "{{" + kvp.Key + "}}";
                        if (text.Contains(placeholder))
                        {
                            text = text.Replace(placeholder, kvp.Value ?? "");
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        // 1. Capture properties from the first run (to preserve font/style)
                        var firstRun = para.Descendants<Run>().FirstOrDefault();
                        RunProperties rPr = firstRun?.RunProperties?.CloneNode(true) as RunProperties;

                        // 2. Remove all existing content (Runs) from paragraph
                        para.RemoveAllChildren<Run>();

                        // 3. Create new Run with replaced text
                        var newRun = new Run();
                        if (rPr != null) newRun.AppendChild(rPr);
                        newRun.AppendChild(new Text(text));

                        // 4. Append back to paragraph
                        para.AppendChild(newRun);
                    }
                }
            }

            wordDoc.MainDocumentPart.Document.Save();
        }
    }
}
