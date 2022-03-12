using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleApp
{
    class ResidualKnowledgeFundGenrator : IFileGenerator
    {
        private readonly UserChoice userChoice;

        private readonly string patternPart1Path;
        private readonly string patternPart2Path;
        private readonly string RKF_Path;

        public ResidualKnowledgeFundGenrator(UserChoice userChoice)
        {
            this.userChoice = userChoice;

            patternPart1Path = "./doc_patterns/RKF-part1.docx";
            patternPart2Path = "./doc_patterns/RKF-part2.docx";
            RKF_Path = "./ФОС.docx";
        }

        public void Generate()
        {
            CreateDocumentFromPattern(RKF_Path, patternPart1Path);
            GeneratePart1Content();
            GeneratePart2Content();
        }

        private static void CreateDocumentFromPattern(string toCreate, string patternPath)
        {
            using var pattern = WordprocessingDocument.Open(patternPath, false);
            using var sheetDocument = WordprocessingDocument.Create(toCreate, WordprocessingDocumentType.Document);
            foreach (var part in pattern.Parts)
            {
                sheetDocument.AddPart(part.OpenXmlPart, part.RelationshipId);
            }
        }

        private void GeneratePart1Content(string progammeCode = "-1111",
            string programmeName = "Матобес", string unknownCode = "666666")
        {
            using var part1 = WordprocessingDocument.Open(RKF_Path, true);
            var body = part1.MainDocumentPart.Document.Body;

            SharedGenerationMethods.FindAndReplaceFirstOccurence("&", $"{progammeCode} {programmeName}", body);
            SharedGenerationMethods.FindAndReplaceFirstOccurence("&", $"{unknownCode} {programmeName}", body);
            SharedGenerationMethods.FindAndReplaceFirstOccurence("&", DateTime.Today.Year, body);

            var table1 = body.Descendants<Table>().First();
            var competences = userChoice.CheckingDisciplines.SelectMany(d => d.CheckingCompetences).OrderBy(c => c.Code).ToList();
            foreach (var c in competences)
            {
                var row = new TableRow(SharedGenerationMethods.CreateTableCell($"{c.Code}"), SharedGenerationMethods.CreateTableCell($"{c.Description}"));
                var lastRow = table1.Descendants<TableRow>().Last();
                table1.InsertAfter(row, lastRow);
            }

            var table2 = body.Descendants<Table>().Skip(1).First(); // possible throw exception
            foreach (var d in userChoice.CheckingDisciplines)
            {
                var c = d.CheckingCompetences.Aggregate("", (acc, c) => acc += $"{c.Code}, ");
                c = c.Remove(c.Length - 2);
                var row = new TableRow(SharedGenerationMethods.CreateTableCell($"{d.Discipline.RussianName}"), SharedGenerationMethods.CreateTableCell($"{c}"));
                var lastRow = table2.Descendants<TableRow>().Last();
                table2.InsertAfter(row, lastRow);
            }
        }
       
        private void FindAndReplaceAllOccurences(string replaced, object replacement, OpenXmlElement element)
        {
            var texts = element.Descendants<Text>().Where(t => t.InnerText.Contains(replaced)).ToList();
            texts.ForEach(t => t.Text = t.Text.Replace(replaced, replacement.ToString()));
        }

        private Paragraph GetRightAnswersParagraph(Question q)
        {
            var ra = q.RightAnswers.Select(a => 
            {
                var id = q.PossibleAnswers.FirstOrDefault(pa => pa.AnswerValue == a.AnswerValue);
                return id != null ? a.AnswerValue : $"{a.Letter}. {a.AnswerValue}";
            }).ToList();
            var runs = new List<OpenXmlElement>();
            for (var i = 0; i < ra.Count; ++i)
            {
                var a = ra[i];
                runs.Add(new Text(a));
                if (i != ra.Count - 1)
                {
                    runs.Add(new Break());
                }
            }
            return new Paragraph(new Run(runs));
        }

        private void GeneratePart2Content()
        {
            var disciplineNumber = 0;
            foreach (var d in userChoice.CheckingDisciplines)
            {
                var temp = $"{d.Discipline.Code} {d.Discipline.RussianName} ФОС.docx";
                CreateDocumentFromPattern(temp, patternPart2Path);
                using (var document = WordprocessingDocument.Open(temp, true))
                {
                    var body = document.MainDocumentPart.Document.Body;
                    SharedGenerationMethods.FindAndReplaceFirstOccurence("&", $"{d.Discipline.RussianName}", body);
                    InsertQuestionsAndAnswerOptions(body, d, disciplineNumber);
                    InsertRightAnswers(body, d);
                    InsertQuestionScoreCriteria(body, d); 
                    InsertDisciplineMarkCriteria(body, d);
                }
                AppendDocumentContentToAnotherDocument(RKF_Path, temp, disciplineNumber);
                ++disciplineNumber;
                File.Delete(temp);
            }
        }

        private void InsertQuestionsAndAnswerOptions(Body body, CheckingDiscipline d, int disciplineNumber)
        {
            var lastParagraph = body.Descendants<Paragraph>().First(p => p.InnerText.Contains("Вопросы"));
            foreach (var q in d.Questions)
            {
                var pPrQuestion = (ParagraphProperties)questionParagraphPr(disciplineNumber).CloneNode(true);
                var questionParagraph = body.InsertAfter(new Paragraph(pPrQuestion), lastParagraph);
                questionParagraph.AppendChild(new Run(new Text(q.Formulation)));
                lastParagraph = questionParagraph;
                foreach (var possibleAnswer in q.PossibleAnswers)
                {
                    var pPrAnswer = (ParagraphProperties)answerParagraphPr(disciplineNumber).CloneNode(true);
                    var answerParagraph = body.InsertAfter(new Paragraph(pPrAnswer), lastParagraph); // тут неоч красиво, потом переделать
                    answerParagraph.AppendChild(new Run(new Text(possibleAnswer.AnswerValue)));
                    lastParagraph = answerParagraph;
                }
            }
        }

        private void InsertRightAnswers(Body body, CheckingDiscipline d)
        {
            var answerTable = body.Descendants<Table>().First();
            if (d.Questions.Any(q => q.RightAnswers != null && q.RightAnswers.Count > 0))
            {
                foreach (var q in d.Questions)
                {
                    var rightAnswers = GetRightAnswersParagraph(q);
                    var row = new TableRow(SharedGenerationMethods.CreateTableCell($"{q.Number}"), new TableCell(rightAnswers));
                    var lastRow = answerTable.Descendants<TableRow>().Last();
                    answerTable.InsertAfter(row, lastRow);
                }
            }
        }

        private void InsertQuestionScoreCriteria(Body body, CheckingDiscipline d)
        {
            SharedGenerationMethods.FindAndReplaceFirstOccurence("&", $"{FormatNumber(d.QuestionsCount)}", body);
            var copiedTable = body.Descendants<Table>().TakeLast(2).First(); // possible exception
            OpenXmlElement nodeToInsertAfter = copiedTable;
            foreach (var q in d.Questions)
            {
                if (q.Number == 1)
                {
                    continue;
                }
                var newParagraph = SharedGenerationMethods.CreateParagraph($"Вопрос номер {q.Number} оценивается по следующей шкале:");
                var questionCriteria = (Table)copiedTable.CloneNode(true);
                FindAndReplaceAllOccurences("0", q.MaxScore, questionCriteria);
                body.InsertAfter(newParagraph, nodeToInsertAfter);
                body.InsertAfter(questionCriteria, newParagraph);
                nodeToInsertAfter = questionCriteria;
            }
            FindAndReplaceAllOccurences("0", d.Questions.First().MaxScore, copiedTable); // possible exception
        }

        private void InsertDisciplineMarkCriteria(Body body, CheckingDiscipline d)
        {
            var ects = 'A';
            var tblrows = body.Descendants<Table>().Last().Descendants<TableRow>().Skip(1).ToList();
            foreach (var row in tblrows)
            {
                var cell = row.Descendants<TableCell>().First();
                var textElement = cell.Descendants<Text>().First();
                var criterion = d.Scale.First(c => c.ECTSMark == ects);
                textElement.Text = textElement.InnerText.Contains("менее")
                    ? $"менее {d.Scale.First(c => c.ECTSMark == Column.Prev(criterion.ECTSMark)).MaxScore}"
                    : criterion.MinScore == criterion.MaxScore
                        ? $"{criterion.MinScore}"
                        : $"{criterion.MinScore} - {criterion.MaxScore}";

                ects = Column.Next(ects);
            }
        }

        private static void AppendDocumentContentToAnotherDocument(string destinationPath, string sourcePath, int id)
        {   
            using var destination = WordprocessingDocument.Open(destinationPath, true);
            var altChunkId = $"AltChunkId{id}";
            var mainPart = destination.MainDocumentPart;
            var chunk = mainPart.AddAlternativeFormatImportPart(AlternativeFormatImportPartType.WordprocessingML, altChunkId);

            using (var fileStream = File.Open(sourcePath, FileMode.Open))
                chunk.FeedData(fileStream);

            var altChunk = new AltChunk
            {
                Id = altChunkId
            };
            mainPart.Document
                .Body
                .InsertAfter(altChunk, mainPart.Document.Body.Elements<Paragraph>().Last());
            mainPart.Document.Save();
        }

        private static string FormatNumber(int num)
        {
            var map = new Dictionary<int, string>
            {
                { 1, "один" },
                { 2, "два" },
                { 3, "три" },
                { 4, "четыре" },
                { 5, "пять" },
                { 6, "шесть" },
                { 7, "семь" },
                { 8, "восемь" },
                { 9, "девять" },
                { 10, "десять" },
                { 11, "одиннадцать" },
                { 12, "двенадцать" },
                { 13, "тринадцать" },
                { 14, "четырнадцать" },
                { 15, "пятнадцать" },
                { 16, "шеснадцать" },
                { 17, "семнадцать" },
                { 18, "восемнадцать" },
                { 19, "девятнадцать" },
                { 20, "двадцать" }
            };
            if (map.ContainsKey(num))
            {
                return map[num];
            }
            return num.ToString();
        }

        private static ParagraphProperties questionParagraphPr(int disciplineNumber) => new ParagraphProperties(
            new AdjustRightIndent { Val = false },
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
            new NumberingProperties { NumberingId = new NumberingId { Val = 5 + disciplineNumber }, NumberingLevelReference = new NumberingLevelReference { Val = 0 } });

        private static ParagraphProperties answerParagraphPr(int disciplineNumber) => new ParagraphProperties(
            new AdjustRightIndent { Val = false },
            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
            new NumberingProperties { NumberingId = new NumberingId { Val = 5 + disciplineNumber }, NumberingLevelReference = new NumberingLevelReference { Val = 1 } });
    }
}
