using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp
{
    class ProtocolGenerator : IFileGenerator
    {
        private readonly UserChoice userChoice;
        private readonly GoogleSpreadsheetParser.GoogleSpreadsheetParser spreadsheet;

        private readonly string patternPath;
        private readonly string protocolPath;

        private Body body;
        private Table table;

        public ProtocolGenerator(GoogleSpreadsheetParser.GoogleSpreadsheetParser spreadsheet, UserChoice userChoice)
        {
            this.userChoice = userChoice;
            this.spreadsheet = spreadsheet;
            patternPath = "./doc_patterns/protocol.docx";
            protocolPath = "./протокол.docx";
        }

        public void Generate()
        {
            SharedGenerationMethods.CreateDocumentFromPattern(patternPath, protocolPath);
            GenerateProtocolContent();
        }

        private void GenerateProtocolContent()
        {
            using var protocol = WordprocessingDocument.Open(protocolPath, true);
            body = protocol.MainDocumentPart.Document.Body;
            table = body.Descendants<Table>().First();

            InsertTextInfo();
            InsertListOfCheckingCompetences();
            InsertCompetenceCheckResults();
        }

        private void InsertTextInfo()
        {
            // Доля обучающихся, продемонтрировавших сформированность проверяемых компетенций/этапа компетенций -- надо найти, посчитать и вставить 
            // номер УГСН -- хз надо или нет
            var programme = userChoice.Curriculum.Programme;            
            var replacements = new List<(string Replaced, object Replacement)>
            {
                ("день", DateTime.Today.Day), ("месяц", FormatMonth(DateTime.Today.Month)), ("&", DateTime.Today.Year),
                ("&", userChoice.StrangeCode), ("ОП", programme.RussianName), 
                ("код направления", programme.Code), ("ОП", programme.RussianName),
                ("&", userChoice.Course), 
                ("&", spreadsheet.ParticipantsCount), 
                ("&", spreadsheet.ParticipantsPercent), 
                ("&", spreadsheet.QuestionsCount),
                ("ИОФ", userChoice.User.ShortFullNameWithSurnameLast), 
                ("&", spreadsheet.CompetenciesFormationPercentage)
            };

            replacements.ForEach(p => SharedGenerationMethods.FindAndReplaceFirstOccurence(p.Replaced, p.Replacement, body));
        }

        private void InsertListOfCheckingCompetences()
        {
            var competences = userChoice.CheckingDisciplines.SelectMany(d => d.CheckingCompetences).Select(d=>$"{d.Code} — {d.Description}");
            var lastParagraph = body.Descendants<Paragraph>().First(p => p.InnerText.Contains("следующих компетенций"));
            foreach (var c in competences)
            {
                var pPrCompetence = (ParagraphProperties)competenceParagraphPr.CloneNode(true);
                var competenceParagraph = body.InsertAfter(new Paragraph(pPrCompetence), lastParagraph);
                competenceParagraph.AppendChild(new Run(new Text(c)));
                lastParagraph = competenceParagraph;
            }
        }

        private void InsertCompetenceCheckResults()
        {
            //var leftJustificationPr = new ParagraphProperties { Justification = new Justification { Val = JustificationValues.Left } };
            var checkingDisciplines = userChoice.CheckingDisciplines;
            var cellCount = 3 + checkingDisciplines.Max(d => d.CheckingCompetences.Count);
            var counter = 1;
            var rPr = table.Descendants<Run>().First(p => p.InnerText.Contains("№")).RunProperties;
            foreach (var discipline in checkingDisciplines)
            {
                var emptyCellsCount = cellCount - discipline.CheckingCompetences.Count - 3;
                var tr = CreateTableRow(counter, emptyCellsCount, discipline, rPr);
                var lastRow = table.Descendants<TableRow>().Last();
                table.InsertAfter(tr, lastRow);
                ++counter;
            }
        }
         
        private TableRow CreateTableRow(int counter, int emptyCellCount, CheckingDiscipline discipline, RunProperties rPr)
        {
            var cells = new List<TableCell> { SharedGenerationMethods.CreateTableCell($"{counter}", rPr: rPr) };
            foreach (var c in discipline.CheckingCompetences)
            {
                var comptenceParagraph = SharedGenerationMethods.CreateParagraph(c.Code, rPr : rPr);
                var disciplineParagraph = SharedGenerationMethods.CreateParagraph($"({discipline.Discipline.RussianName})", rPr: rPr);
                var cell = new TableCell(comptenceParagraph, disciplineParagraph);
                cells.Add(cell);
            }

            for (var i = 0; i < emptyCellCount; ++i)
            {
                cells.Add(SharedGenerationMethods.CreateTableCell("", rPr: rPr));
            }

            cells.Add(SharedGenerationMethods.CreateTableCell($"{spreadsheet.MidCertificationAverageScore[discipline]}", rPr: rPr));
            cells.Add(SharedGenerationMethods.CreateTableCell($"{spreadsheet.CompetenceAverageScore[discipline]}", rPr: rPr));            
            return new TableRow(cells);
        }

        private static string FormatMonth(int month)
        {
            return month switch
            {
                1 => "января",
                2 => "февраля",
                3 => "марта",
                4 => "апреля",
                5 => "мая",
                6 => "июня",
                7 => "июля",
                8 => "августа",
                9 => "сентября",
                10 => "октября",
                11 => "ноября",
                12 => "декабря",
                _ => "месяц",
            };
        }

        private static readonly ParagraphProperties competenceParagraphPr = new(
            new AdjustRightIndent { Val = false },
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
            new NumberingProperties { NumberingId = new NumberingId { Val = 1 }, NumberingLevelReference = new NumberingLevelReference { Val = 0 } });
    }
}
