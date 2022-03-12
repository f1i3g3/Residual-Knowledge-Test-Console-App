using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Linq;

namespace ConsoleApp
{
    static class SharedGenerationMethods
    {
        public static TableCell CreateTableCell(string text, ParagraphProperties pPr = null, RunProperties rPr = null)
            => pPr == null
                ? new(new Paragraph(CreateRun(text, rPr)))
                : new(new Paragraph(CreateRun(text, rPr)) { ParagraphProperties = (ParagraphProperties)pPr.CloneNode(true) });

        public static Paragraph CreateParagraph(string text, ParagraphProperties pPr = null, RunProperties rPr = null)
            => pPr == null
                ? new(CreateRun(text, rPr))
                : new(CreateRun(text, rPr)) { ParagraphProperties = (ParagraphProperties)pPr.CloneNode(true) };

        private static Run CreateRun(string text, RunProperties rPr = null)
            => rPr == null
                    ? new(new Text(text))
                    : new(new Text(text)) { RunProperties = (RunProperties)rPr.CloneNode(true) };

        public static void FindAndReplaceFirstOccurence(string replaced, object replacement, OpenXmlElement element)
        {
            var text = element.Descendants<Text>().First(t => t.InnerText.Contains(replaced));
            text.Text = text.Text.Replace(replaced, replacement.ToString());
        }

        public static void CreateDocumentFromPattern(string patternPath, string newDocumentPath)
        {
            using var pattern = WordprocessingDocument.Open(patternPath, false);
            using var sheetDocument = WordprocessingDocument.Create(newDocumentPath, WordprocessingDocumentType.Document);
            foreach (var part in pattern.Parts)
            {
                sheetDocument.AddPart(part.OpenXmlPart, part.RelationshipId);
            }
        }
    }
}
