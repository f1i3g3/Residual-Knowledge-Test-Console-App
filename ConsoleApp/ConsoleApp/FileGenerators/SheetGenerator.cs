using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp
{
    /// <summary>
    ///  Генератор ведомости
    /// </summary>
    partial class SheetGenerator : IFileGenerator
    {
        private readonly UserChoice userChoice;
        private readonly GoogleSpreadsheetParser.GoogleSpreadsheetParser spreadsheet;
        private readonly string sheetDocumentPath;
        private readonly string patternPath;

        private Body body;
        private Table table;

        public SheetGenerator(GoogleSpreadsheetParser.GoogleSpreadsheetParser spreadsheet, UserChoice userChoice)
        {
            this.userChoice = userChoice;
            this.spreadsheet = spreadsheet;
            sheetDocumentPath = "./ведомость.docx";
            patternPath = "./doc_patterns/sheet.docx";
        }

        public void Generate()
        {
            SharedGenerationMethods.CreateDocumentFromPattern(patternPath, sheetDocumentPath);
            GenerateSheetDocumentContent();
        }

        private void GenerateSheetDocumentContent()
        {
            using var sheetDocument = WordprocessingDocument.Open(sheetDocumentPath, true);
            body = sheetDocument.MainDocumentPart.Document.Body;
            table = body.Descendants<Table>().First();

            InsertStudentMarks(2);
            InsertAnalytics();
            InsertTextInfo();
        }

        private void InsertStudentMarks(int course)
        {
            var leftJustificationPr = new ParagraphProperties { Justification = new Justification { Val = JustificationValues.Left } };
            var counter = 1;
            var marksByStudent = spreadsheet.StudentsMarks.OrderBy(m => m.Student).GroupBy(m => m.Student);
            foreach (var group in marksByStudent)
            {
                foreach (var disciplineMarks in group.OrderBy(d => d.Discipline))
                {
                    var competenceAssessmentMark = FormatCompetenceAssessmentMark(disciplineMarks.CompetenceAssessmentMark);
                    var midtermCertificationMark = FormatMidtermCertificationMark(disciplineMarks.MidtermCertificationMark);
                    var tr = new TableRow(
                        SharedGenerationMethods.CreateTableCell($"{counter}"),
                        SharedGenerationMethods.CreateTableCell($"{group.Key}", leftJustificationPr),
                        SharedGenerationMethods.CreateTableCell($"{course}"),
                        SharedGenerationMethods.CreateTableCell($"{disciplineMarks.Discipline}"),
                        SharedGenerationMethods.CreateTableCell($"{competenceAssessmentMark}"),
                        SharedGenerationMethods.CreateTableCell($"{midtermCertificationMark}"));
                    var lastRow = table.Descendants<TableRow>().Last();
                    table.InsertBefore(tr, lastRow);
                    ++counter;
                }
            }
        }

        private void InsertAnalytics() // нельзя ли сократить в 2 раза?
        {
            var last = spreadsheet.NumberOfMidCertificationMarkType.OrderByDescending(p => p.Key).Last();
            var runs = spreadsheet.NumberOfMidCertificationMarkType.OrderByDescending(p => p.Key)
                .SkipLast(1)
                .Select(p => new Run(new Text($"«{p.Key}» - {p.Value}"), new Break()))
                .Append(new Run(new Text($"«{last.Key}» - {last.Value}")));

            table.Descendants<TableRow>().Last()
                .Descendants<TableCell>().Last()
                .Descendants<Paragraph>().First()
                .Append(runs);

            last = spreadsheet.NumberOfCompetenceMarkType.OrderByDescending(p => p.Key).Last();
            runs = spreadsheet.NumberOfCompetenceMarkType.OrderByDescending(p => p.Key)
                .SkipLast(1)
                .Select(p => new Run(new Text($"«{p.Key}» - {p.Value}"), new Break()))
                .Append(new Run(new Text($"«{last.Key}» - {last.Value}")));

            table.Descendants<TableRow>().Last()
                .Descendants<TableCell>().Last()
                .PreviousSibling<TableCell>()
                .Descendants<Paragraph>().First()
                .Append(runs);
        }

        private void InsertTextInfo()
        {
            var month = DateTime.Now.Date.Month < 10 ? $"0{DateTime.Now.Date.Month}" : $"{DateTime.Now.Date.Month}";
            var day = DateTime.Now.Date.Day < 10 ? $"0{DateTime.Now.Date.Day}" : $"{DateTime.Now.Date.Day}";
            var date = $"{day}.{month}.{DateTime.Now.Date.Year}";

            var replacements = new List<(string Replaced, object Replacement)>
            {
                ("код", userChoice.Curriculum.Programme.Code), 
                ("Наименование", userChoice.Curriculum.Programme.RussianName), 
                ("уровень", userChoice.Curriculum.Programme.LevelOfEducation), 
                ("фамилия", userChoice.User.ShortFullNameWithSurnameFirst), 
                ("00.00.0000", date)
            };

            replacements.ForEach(p => SharedGenerationMethods.FindAndReplaceFirstOccurence(p.Replaced, p.Replacement, body)); 
        }

        private static string FormatCompetenceAssessmentMark(string competenceAssessmentMark)
        {
            if (competenceAssessmentMark == "")
            {
                return "неявка";
            }

            return competenceAssessmentMark[0] switch
            {
                '5' => competenceAssessmentMark.Replace("5", "отлично"),
                '4' => competenceAssessmentMark.Replace("4", "хорошо"),
                '3' => competenceAssessmentMark.Replace("3", "удовлетворительно"),
                '2' => competenceAssessmentMark.Replace("2", "неудовлетворительно"),
                _ => "",
            };
        }

        private static string FormatMidtermCertificationMark(string midtermCertificationMark)
        {
            return midtermCertificationMark switch
            {
                "5" => "отлично",
                "4" => "хорошо",
                "3" => "удовлетворительно",
                "2" => "неудовлетворительно",
                _ => "неявка",
            };
        }
    }
}
