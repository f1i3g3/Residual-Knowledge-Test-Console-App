using System.Collections.Generic;
using System.Linq;
using System.IO;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;

namespace ConsoleApp.InputFilesParser
{
    class MidCertificationResultsParser
    {
        private readonly CheckingDiscipline discipline;

        public MidCertificationResultsParser(CheckingDiscipline discipline)
        {
            this.discipline = discipline;
        }

        public bool TryParseMidCertificationResults(out List<MidCerificationAssesmentResult> midCerificationResults)
        {
            midCerificationResults = new List<MidCerificationAssesmentResult>();

            if (string.IsNullOrEmpty(discipline.MidCerificationResultsPath))
            {
                return false;
            }

            var fileInfo = new FileInfo(discipline.MidCerificationResultsPath);
            return fileInfo.Extension switch
            {
                "xlsx" => ParseXlsxMidcertification(midCerificationResults),
                "docx" => ParseDocxMidCertification(midCerificationResults),
                _ => false,
            };
        }

        private bool ParseDocxMidCertification(List<MidCerificationAssesmentResult> midCerificationResults)
        {
            var wordDocument = WordprocessingDocument.Open(discipline.MidCerificationResultsPath, false);
            var body = wordDocument.MainDocumentPart.Document.Body;
            var tables = body.Elements<Table>().ToList();

            // извлекаем нужные table rows
            var markRows = new List<TableRow>();
            for (var i = 0; i < tables.Count; ++i)
            {
                if (i % 4 != 1)
                {
                    continue;
                }

                var tableRows = tables[i].Descendants<TableRow>();
                var header = tableRows.FirstOrDefault(tr => tr.InnerText.Contains("Ф.И.О"));
                if (header == null)
                {
                    continue;
                }
                var rows = header.ElementsAfter().Select(e => e as TableRow).Where(r => r != null && !string.IsNullOrEmpty(r.InnerText));
                markRows.AddRange(rows);
            }

            foreach (var row in markRows)
            {
                var cells = row.Descendants<TableCell>().Where(c => !string.IsNullOrEmpty(c.InnerText)).ToList();
                if (cells.Count < 4)
                {
                    continue;
                }
                var studentName = cells[1].InnerText;
                var mark = GetMark(cells[3].InnerText).ToString();
                if (mark == "-1")
                {
                    continue;
                }
                var studentResult = new MidCerificationAssesmentResult(studentName, discipline.Discipline.RussianName, mark);
                midCerificationResults.Add(studentResult);
            }

            return midCerificationResults.Count > 0; ///////////////
        }

        private static int GetMark(string mark)
        {
            return mark.ToLower() switch
            {
                "отлично" => 5,
                "хорошо" => 4,
                "удовлетворительно" => 3,
                "неудовлетворительно" => 2,
                _ => -1,
            };
        }

        private bool ParseXlsxMidcertification(List<MidCerificationAssesmentResult> midCerificationResults) /// xlsx файл какой-то ужасный
        {
            return midCerificationResults.Count > 0;
        }
    }
}
