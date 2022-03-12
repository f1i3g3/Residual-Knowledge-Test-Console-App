using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using static Google.Apis.Sheets.v4.SpreadsheetsResource.ValuesResource;

namespace ConsoleApp.GoogleSpreadsheetParser
{
    // ТУТ НАДО ПЕРЕДЕЛАТЬ СВЯЗЬ С ГУГЛ-ТАБЛИЦЕЙ

    // Надо добавить парсинг процента сформированности компетенций
    class GoogleSpreadsheetParser
    {
        // info from user, нужно, т.к. для каждой дисциплины добавляется Scale 
        private List<CheckingDiscipline> checkingDisciplines;

        // sheet
        public List<DisciplineAssesmentResult> StudentsMarks { get; private set; }

        public Dictionary<int, string> NumberOfCompetenceMarkType { get; private set; }
        
        public Dictionary<int, string> NumberOfMidCertificationMarkType { get; private set; }

        // protocol
        public int ParticipantsCount { get; private set; }

        public int ParticipantsPercent { get; private set; }
        
        public int QuestionsCount { get; private set; } // мб стоит переделать

        public int CompetenciesFormationPercentage { get; private set; } // надо добавить парсинг данного показателя

        public Dictionary<CheckingDiscipline, string> CompetenceAverageScore { get; private set; }

        public Dictionary<CheckingDiscipline, string> MidCertificationAverageScore { get; private set; }

        // residual fund generator
        public List<MarkCriterion> DisciplineMarkCriteria { get; private set; }

        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "ResidualKnowledgeTestApp";
        static readonly string SpreadsheetId = "";
        static readonly string sheet = "";
        static SheetsService service;

        private bool rowInitializationSuccess = true;

        private IList<IList<object>> rows;

        public GoogleSpreadsheetParser(List<CheckingDiscipline> checkingDisciplines)
        {
            // info from user
            this.checkingDisciplines = checkingDisciplines;
            
            // sheet
            StudentsMarks = new List<DisciplineAssesmentResult>();
            NumberOfCompetenceMarkType = new Dictionary<int, string>();
            NumberOfMidCertificationMarkType = new Dictionary<int, string>();
            
            // protocol
            CompetenceAverageScore = new Dictionary<CheckingDiscipline, string>();
            MidCertificationAverageScore = new Dictionary<CheckingDiscipline, string>();

            GoogleCredential credential;
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName
            });

            InitializeRows();
            
            if (rowInitializationSuccess)
            {
                Parse();
            }
        }

        private void InitializeRows()
        {
            var range = $"{sheet}!A:Z";/////////////////////////////////////
            var request = service.Spreadsheets.Values.Get(SpreadsheetId, range);
            
            rows = SendGetValuesRequest(request);
            if (rows == null || rows.Count == 0)
            {
                rowInitializationSuccess = false;
            }
        }

        private IList<IList<object>> SendGetValuesRequest(GetRequest request)
        {
            var cntr = 0;
            while (true)
            {
                try
                {
                    var response = request.Execute();
                    return response.Values;
                }
                catch (GoogleApiException e) when (e.Error.Errors.FirstOrDefault()?.Reason == "notImplemented")
                {
                    continue;
                }
                catch (GoogleApiException e) when (e.Error.Errors.FirstOrDefault()?.Reason == "limitExceeded")
                {
                    if (cntr > 10000 * 12)
                    {
                        return null;
                    }
                    Thread.Sleep(10000);
                    cntr += 10000;
                }
                catch (GoogleApiException e)
                {
                    foreach (var error in e.Error.Errors)
                    {
                        Console.WriteLine($"Reason: {error.Reason}. Message: {error.Message}");
                    }
                    return null;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
        }

        private static IList<IList<object>> CutOutDataByHeaders(IList<IList<object>> rows, string topInclusiveRowText, string bottomInclusiveRowText = null)
        {
            var firstIndex = rows.IndexOf(rows.FirstOrDefault(r => r.Contains(topInclusiveRowText)));
            var lastIndex = bottomInclusiveRowText != null
                ? rows.IndexOf(rows.FirstOrDefault(r => r.Contains(bottomInclusiveRowText)))
                : rows.Count;

            var toTake = lastIndex - firstIndex + 1;
            return rows.Skip(firstIndex).Take(toTake).ToList();
        }

        private void Parse()
        {
            // "Число участников:" {} "из" {} "%=" {}
            var participantsData = CutOutDataByHeaders(rows, Headers.ParticipantsCountHeader).FirstOrDefault().Cast<string>().ToList(); // ввести константы для header'ов
            ParticipantsCount = int.Parse(participantsData[1]);
            ParticipantsPercent = int.Parse(participantsData[5]);
            QuestionsCount = checkingDisciplines.Sum(d => d.QuestionsCount); // мб стоит вырезать из гугл-таблицы а не считать на случай, если пользователь ошибся?

            // sheet
            ParseNumberOfCompetenceMarkType();
            ParseNumberOfMidCertificationMarkType();
            ParseStudentsMarks();

            // protocol
            ParseCompetenceAverageScore();
            ParseMidCertificationAverageScore();

            // residual fund
            ParseDisciplineMarkCriteria();
        }

        // sheet
        private void ParseNumberOfCompetenceMarkType()
        {
            var competenceAssessmentRows = CutOutDataByHeaders(rows, Headers.EctsAndFiveScaleMarksHeader).ToList(); // не переместить критерии так
            var competenceAnalyticsRows = CutOutDataByHeaders(competenceAssessmentRows, Headers.MarkFiveCountHeader, Headers.MarkTwoCountHeader)
                .Select(r => r.Skip(3).First()) // first
                .Cast<string>()
                .ToList();

            NumberOfCompetenceMarkType = GetNumberOfEachMarkType(competenceAnalyticsRows);
        }

        private void ParseNumberOfMidCertificationMarkType()
        {
            var midtermCertificationRows = CutOutDataByHeaders(rows, Headers.MidtermCerificationMarksHeader).ToList();
            var midCertificationAnalyticsRows = CutOutDataByHeaders(midtermCertificationRows, Headers.MarkFiveCountHeader, Headers.MarkTwoCountHeader)
                .Select(r => r.Skip(3).First()) // first
                .Cast<string>()
                .ToList();

            NumberOfMidCertificationMarkType = GetNumberOfEachMarkType(midCertificationAnalyticsRows);
        }

        private static Dictionary<int, string> GetNumberOfEachMarkType(List<string> cells)
        {
            var res = new Dictionary<int, string>(); // mark count
            var mark = 5;
            foreach (var cell in cells)
            {
                res.Add(mark, cell);
                --mark;
            }
            return res;
        }

        private void ParseStudentsMarks()
        {
            var competenceAssessmentRows = CutOutDataByHeaders(rows, Headers.EctsAndFiveScaleMarksHeader).ToList();
            var studentCompetenceAssessmentRows = CutOutDataByHeaders(competenceAssessmentRows, Headers.EctsAndFiveScaleMarksHeader, Headers.MarkFiveCountHeader)
                .Skip(1).SkipLast(1)
                .Select(r => r.Cast<string>().ToList())
                .ToList();

            var competenceMarks = GetMarksFromRows(studentCompetenceAssessmentRows).OrderBy(i => i.Student).ToList();

            var midtermCertificationResultRows = CutOutDataByHeaders(rows, Headers.MidtermCerificationMarksHeader);
            var studentMidCertificationAssessmentsRows = CutOutDataByHeaders(midtermCertificationResultRows, Headers.MidtermCerificationMarksHeader, Headers.MarkFiveCountHeader)
                .Skip(1).SkipLast(1)
                .Select(r => r.Cast<string>().ToList())
                .ToList();

            var midtermCertificationMarks = GetMarksFromRows(studentMidCertificationAssessmentsRows).OrderBy(i => i.Student).ToList();

            for (var i = 0; i < competenceMarks.Count; ++i)
            {
                var (student, disciplineCompetenceMarks) = competenceMarks[i];
                var studentComptetenceMarks = disciplineCompetenceMarks.OrderBy(e => e.Discipline);

                (string Student, List<(string Discipline, string Mark)> disciplineMidtermMarks) = midtermCertificationMarks[i];
                var studentMidtermMarks = disciplineMidtermMarks.OrderBy(e => e.Discipline);

                var studentMarks = studentComptetenceMarks.Zip(studentMidtermMarks, (comp, mid) => new DisciplineAssesmentResult(student, comp.Discipline, comp.Mark, mid.Mark));
                StudentsMarks.AddRange(studentMarks);
            }
        }

        private static List<(string Student, List<(string Discipline, string Mark)> DisciplineMark)> GetMarksFromRows(List<List<string>> rows)
        {
            var disciplines = rows[0].Where(c => c != "").ToList();
            rows = rows.Skip(1).Where(r => r.Count > 0).ToList();
            var marksByStudent = new List<(string Student, List<(string Discipline, string Mark)>)>();
            foreach (var row in rows)
            {
                // здесь неоч нехорошо
                var student = row[0];
                var marks = new List<(string Discipline, string Mark)>();
                for (var i = 0; i < disciplines.Count; ++i)
                {
                    if (row.Count - 1 <= i)
                    {
                        continue;
                    }
                    var disciplineName = disciplines[i];
                    var mark = row[i + 1];
                    if (mark != "")
                    {
                        marks.Add((disciplineName, mark));
                    }
                }
                marksByStudent.Add((student, marks));
            }
            return marksByStudent;
        }

        // protocol
        private void ParseCompetenceAverageScore()
        {
            var competenceAssessmentRows = CutOutDataByHeaders(rows, Headers.EctsAndFiveScaleMarksHeader).ToList();
            var competenceAnalyticsData = CutOutDataByHeaders(competenceAssessmentRows, Headers.AverageScoreHeader)
                .FirstOrDefault()
                .Cast<string>()
                .Skip(1)
                .ToList();

            for (var i = 0; i < checkingDisciplines.Count; ++i)
            {
                CompetenceAverageScore.Add(checkingDisciplines[i], competenceAnalyticsData[i]);
            }
        }

        private void ParseMidCertificationAverageScore()
        {
            var midtermCertificationResultRows = CutOutDataByHeaders(rows, Headers.MidtermCerificationMarksHeader);
            var midCertificationAnalyticsData = CutOutDataByHeaders(midtermCertificationResultRows, Headers.AverageScoreHeader)
                .FirstOrDefault().
                Cast<string>().
                Skip(1).
                ToList();

            for (var i = 0; i < checkingDisciplines.Count; ++i)
            {
                MidCertificationAverageScore.Add(checkingDisciplines[i], midCertificationAnalyticsData[i]);
            }
        }

        // residual fund
        private void ParseDisciplineMarkCriteria()// тут нужны всякие проверки, TryParse и тп. И не только здесь, а везде
        {
            foreach (var d in checkingDisciplines)
            {
                var block = CutOutDataByHeaders(rows, Headers.DisciplineAssessmentCriteriaHeader).Skip(1).ToList();
                var disciplineCriteriaRows = CutOutDataByHeaders(block, $"[{d.Discipline.Code}] {d.Discipline.RussianName}").Skip(1).Take(6);
                var min = disciplineCriteriaRows.Select(r => r[1]).Cast<string>().ToList(); // ну вообще-то int
                var max = disciplineCriteriaRows.Select(r => r[2]).Cast<string>().ToList(); // ну вообще-то int
                var ects = 'A';
                for (var i = 0; i < min.Count; ++i) 
                {
                    var minScore = int.Parse(min[i]);
                    var maxScore = int.Parse(max[i]);
                    var markCriterion = new MarkCriterion(minScore, maxScore, ects, GetFivePointScaleMarkByECTSMark(ects));
                    d.Scale.Add(markCriterion);
                    //DisciplineMarkCriteria.Add(markCriterion);/////////////
                    ects = (char)(ects + 1);
                } 
            }
        }

        private static int GetFivePointScaleMarkByECTSMark(char mark)
        {
            return mark switch
            {
                'A' => 5,
                'B' => 4,
                'C' => 4,
                'D' => 3,
                'E' => 3,
                'F' => 2,
                _ => -1,
            };
        }
    }
}
