using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using iTextSharp.text.pdf;
using System.Text;
using iTextSharp.text.pdf.parser;

namespace ConsoleApp.InputFilesParser
{
    class FormWithQuestionsAndPossibleAnswersParser
    {
        private string path;

        public FormWithQuestionsAndPossibleAnswersParser(string path)
        {
            this.path = path;
        }

        /// <summary>
        /// Uses txt form
        /// </summary>
        /// <returns></returns>
        public bool TryParseQuestionsAndPossibleAnswers(out List<(int QuestionNumber, string Formulation, List<Answer> PossibleAnswers, int MaxScore)> questions)
        {
            questions = new List<(int QuestionNumber, string Formulation, List<Answer> PossibleAnswers, int MaxScore)>();
            var content = "";
            var fileInfo = new FileInfo(path);
            switch (fileInfo.Extension)
            {
                case ".txt":
                    using (var reader = new StreamReader(path))
                    {
                        content = reader.ReadToEnd();
                    }
                    break;
                case ".pdf":
                    using (var reader = new PdfReader(path))
                    {
                        var text = new StringBuilder();
                        for (int i = 1; i <= reader.NumberOfPages; i++)
                        {
                            var its = new SimpleTextExtractionStrategy();
                            var thePage = PdfTextExtractor.GetTextFromPage(reader, i, its);
                            var theLines = thePage.Split('\n');
                            foreach (var theLine in theLines)
                            {
                                text.AppendLine(theLine);
                            }
                        }
                        content = text.ToString();
                    }
                    break;
                default:
                    return false;
            }

            var regex = new Regex(@"(.+)\r\n\(Баллов: (\d+)\)\r\n(\d+)\.\r\n(?(\d+/\d+/\d{4})(?:\r\n|$)|(.+)(?:\r\n|$)(?!\(Баллов: (\d+)\)))*");
            var matches = regex.Matches(content);
            foreach (Match match in matches)
            {
                var formulation = match.Groups[1].Value;
                var maxScore = int.Parse(match.Groups[2].Value);
                var questionNumber = int.Parse(match.Groups[3].Value);
                var possibleAnswers = GetPossibleAnswers(match.Groups[4].Captures.Select(c => c.Value).ToList());
                var question = (questionNumber, formulation, possibleAnswers, maxScore);
                questions.Add(question);
            }
            return questions.Count > 0;
        }

        private static List<Answer> GetPossibleAnswers(IList<string> answers)
        {
            var possibleAnswers = new List<Answer>();
            for (var i = 0; i < answers.Count; ++i)
            {
                var answer = new Answer(i + 1, answers[i]);
                possibleAnswers.Add(answer);
            }
            return possibleAnswers;
        }
    }
}
