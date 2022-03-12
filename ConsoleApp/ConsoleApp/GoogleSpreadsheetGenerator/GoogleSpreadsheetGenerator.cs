using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CurriculumParser;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using System.Threading;
using Google.Apis.Util.Store;

namespace ConsoleApp
{
    /// This version of the Google Sheets API has a limit of 500 requests per 100 seconds per project, 
    /// and 100 requests per 100 seconds per user. Limits for reads and writes are tracked separately. 
    /// There is no daily usage limit.
    partial class GoogleSpreadsheetGenerator
    { 
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };

        static readonly string ApplicationName = "ResidualKnowledgeTestApp";

        private string spreadsheetId;
        private string sheet;

        const string APP_TOKEN_FILE = "../../credentials.json";
        const string USER_TOKEN_FOLDER = "user_token";

        static SheetsService service;

        private bool initializationSuccsess;

        private List<CheckingDiscipline> checkingDisciplines;
        private UserChoice userChoice;
        private List<string> groups;
        private List<MarkCriterion> competenceCriterion;
        private List<StudentAnswer> studentsAnswers;
        private List<MidCerificationAssesmentResult> midCerificationResults;

        private List<string> studentNames;
        private List<string> disciplineCodesNames;
        private List<string> competenceCodes;
        private int studentCount;

        private List<(Discipline Discipline, int Row)> disciplineCriteriaRows;
        private char disciplineCreteriaColumn;

        private int competenceCriteriaRow;
        private char competenceCriteriaColumn;

        private int scoresByDisciplinesStartRow;
        private char scoresByDisciplinesStartColumn;

        private int scoresByQuestionStartRow;
        private int scoresByQuestionEndRow;
        private char scoresByQuestionStartColumn;

        public GoogleSpreadsheetGenerator(UserChoice userChoice, List<string> groups,
            List<MarkCriterion> competenceCriterion, List<StudentAnswer> studentsAnswers,
            List<MidCerificationAssesmentResult> midCerificationResults, string spreadsheetId = null)
        {
            this.checkingDisciplines = userChoice.CheckingDisciplines;
            this.userChoice = userChoice;
            this.studentsAnswers = studentsAnswers;
            this.midCerificationResults = midCerificationResults;
            this.competenceCriterion = competenceCriterion;
            this.groups = groups;
            this.spreadsheetId = spreadsheetId;

            studentNames = userChoice.Students.Select(s => s.FullName).ToList();
            disciplineCodesNames = checkingDisciplines.Select(p => $"[{p.Discipline.Code}] {p.Discipline.RussianName}").ToList();
            competenceCodes = checkingDisciplines.SelectMany(p => p.CheckingCompetences).Select(c => c.Code).ToList();
            studentCount = studentNames.Count;
            disciplineCriteriaRows = new List<(Discipline Discipline, int Row)>();

            UserCredential credential;
            using (var stream = new FileStream(APP_TOKEN_FILE, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(USER_TOKEN_FOLDER, true)).Result;
            }

            service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            initializationSuccsess = InitializeSpreadsheet();
        }

        private bool InitializeSpreadsheet()
        {
            if (string.IsNullOrEmpty(spreadsheetId))
            {
                var spreadsheet = new Spreadsheet()
                {
                    Properties = new SpreadsheetProperties()
                    {
                        Title = $"Проверка остаточных знаний {userChoice.StrangeCode}"
                    }
                };

                try
                {
                    var spreadsheetDocument = service.Spreadsheets.Create(spreadsheet).Execute();
                    spreadsheetId = spreadsheetDocument.SpreadsheetId;
                    sheet = spreadsheetDocument.Sheets.First().Properties.Title;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }

                return true;
            }

            try
            {
                var spreadsheetDocument = service.Spreadsheets.Get(spreadsheetId).Execute();
                spreadsheetId = spreadsheetDocument.SpreadsheetId;
                sheet = spreadsheetDocument.Sheets.First().Properties.Title;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }

            return true;
        }

        private (char Column, int Row) InsertHorizontalNamesWithQuestionNumbers(char column, int row)
        {
            var currentColumn = column;
            foreach (var d in checkingDisciplines)
            {
                InsertValue($"[{d.Discipline.Code}] {d.Discipline.RussianName}", currentColumn, row);
                MergeCells(currentColumn, row, Column.Behind(currentColumn, d.QuestionsCount - 1), row, MergeType.Rows);
                currentColumn = Column.Behind(currentColumn, d.QuestionsCount);
            }
            ++row;

            var questionsCount = checkingDisciplines.Sum(d => d.QuestionsCount);
            var questionNumbers = new List<object>();
            for (var i = 1; i <= questionsCount; ++i)
            {
                questionNumbers.Add(i);
            }
            InsertValues($"{sheet}!{column}{row}", questionNumbers, Dimension.Row);
            FormatCells(column, row, Column.Behind(column, questionsCount - 1), row, CellFormats.BoldFormat);

            return (currentColumn, row + 1);
        }

        private (char Column, int Row) InsertStudentNames(char column, int row)
        {
            InsertValues($"{sheet}!{column}{row}", new List<object>(studentNames), Dimension.Column);
            return (Column.Next(column), row + studentNames.Count);
        }

        private (char Column, int Row) InsertStudentScoresByQuestions(char column, int row)
        {
            scoresByQuestionStartColumn = column;
            scoresByQuestionStartRow = row;
            scoresByQuestionEndRow = row + studentCount;

            var currentColumn = column;
            foreach (var s in userChoice.Students) // дополнение students???
            {
                currentColumn = column;
                foreach (var d in checkingDisciplines)
                {
                    var studentAnswers = studentsAnswers.SingleOrDefault(sa => sa.Discipline == d && sa.Student == s);
                    if (studentAnswers != null)
                    {
                        var scores = studentAnswers.Scores.OrderBy(s => s.Key.Number).Select(s => s.Value).Cast<object>();
                        InsertValues($"{sheet}!{currentColumn}{row}", new List<object>(scores), Dimension.Row);
                    }
                    currentColumn = Column.Behind(currentColumn, d.QuestionsCount);
                }
                ++row;
            }
            return (currentColumn, row);
        }

        // надо доделать
        private (char Column, int Row) InsertParticipantsNumberAnalytics(char column, int row, int scoreStartRow)
        {
            InsertValues($"{sheet}!{column}{row}", new List<object> { Headers.ParticipantsCountHeader, "", "из", "", "%=" }, Dimension.Row);
            /// Стилизация:
            FormatCell('A', row, CellFormats.BoldFormat);
            FormatCell('A', row, CellFormats.RightHorizontalAlignmentFormat);
            FormatCell('C', row, CellFormats.BoldFormat);
            FormatCell('E', row, CellFormats.BoldFormat);
            /// Формулы:
            InsertFormula($"=COUNT(B{scoreStartRow}:B{scoreStartRow + studentCount - 1})", 'B', row);
            InsertFormula($"=COUNTA(A{scoreStartRow}:A{scoreStartRow + studentCount - 1})", 'D', row);  // cont counta????
            InsertFormula($"=ROUND(B{row} * 100 / D{row})", 'F', row);

            return (Column.Behind(column, 6), row + 1); // 5 или 6?
        }

        private (char Column, int Row) InsertBoldHeader(string text, char column, int row)
        {
            InsertValues($"{sheet}!{column}{row}", new List<object> { text }, Dimension.Row);
            FormatCell(column, row, CellFormats.BoldFormat);
            return (Column.Next(column), row + 1);
        }

        private (char Column, int Row) InsertDisciplineEvaluationCriteria(char column, int row)
        {
            foreach (var checkingDiscipline in checkingDisciplines)
            {
                disciplineCriteriaRows.Add((checkingDiscipline.Discipline, row));
                var name = $"[{checkingDiscipline.Discipline.Code}] {checkingDiscipline.Discipline.RussianName}";
                InsertValues($"{sheet}!{column}{row}", new List<object> { name, "min", "max", "ects", "mark" }, Dimension.Row);
                ++row;

                disciplineCreteriaColumn = Column.Next(column);
                foreach (var c in checkingDiscipline.Scale.OrderBy(s => s.ECTSMark))
                {
                    var insertedValues = new List<object> { "", $"{c.MinScore}", $"{c.MaxScore}", $"{c.ECTSMark}", $"{c.FivePointScaleMark}" };
                    InsertValues($"{sheet}!{column}{row}", insertedValues, Dimension.Row);
                    ++row;
                }
                ++row;
            }
            return (Column.Behind(column, 4), row - 1); // 4 or 5?
        }

        /// вставляем названия дисциплин, стилизуем их ... можно ли вставлять названия вместе со стилизацией?
        private (char Column, int Row) InsertVerticalDisciplinesNames(char column, int row)
        {
            InsertValues($"{sheet}!{column}{row}", new List<object>(disciplineCodesNames), Dimension.Row);
            FormatCells(column, row, Column.Behind(column, disciplineCodesNames.Count - 1), row, CellFormats.Minus90DegreeRotateFormat);
            FormatCells(column, row, Column.Behind(column, disciplineCodesNames.Count - 1), row, CellFormats.CenterHorizontalAlightmentFormat);
            return (Column.Behind(column, disciplineCodesNames.Count), row + 1);
        }

        private (char Column, int Row) InsertStudentScoresByDisciplines(char column, int row)
        {
            scoresByDisciplinesStartRow = row;
            scoresByDisciplinesStartColumn = column;

            var startQuestionsColumn = scoresByQuestionStartColumn;
            //var endQuestionsColumn = column;

            var formulaInsertColumn = column;
            var firstFormulaInsertRow = row;
            var lastFormulaInsertRow = row + studentCount - 1;

            var studentScoresByQuestionRow = scoresByQuestionStartRow;

            foreach (var d in checkingDisciplines)
            {
                var questionCount = d.QuestionsCount;
                var endQuestionsColumn = Column.ToChar(Column.ToInt(startQuestionsColumn) + questionCount - 1); // заменить на Behind!!!
                var cell = $"{startQuestionsColumn}{studentScoresByQuestionRow}:{endQuestionsColumn}{studentScoresByQuestionRow}";
                var formula = $"=IF({startQuestionsColumn}{studentScoresByQuestionRow} = \"\", \"\", SUM({cell}))";
                InsertRepeatedFormula(formula, formulaInsertColumn, firstFormulaInsertRow, formulaInsertColumn, lastFormulaInsertRow);

                // conditional formatting
                var disciplineScaleRow = 1 + disciplineCriteriaRows.First(c => c.Discipline.Code == d.Discipline.Code).Row; //////////////////
                InsertScaleBasedConditionalFormating(formulaInsertColumn, firstFormulaInsertRow, formulaInsertColumn, lastFormulaInsertRow, disciplineCreteriaColumn, disciplineScaleRow);

                formulaInsertColumn = Column.Next(formulaInsertColumn);
                startQuestionsColumn = Column.Next(endQuestionsColumn);
            }

            return (formulaInsertColumn, row + studentCount);
        }

        private (char Column, int Row) InsertStudentsCompetenceMarks(char column, int row)
        {
            var scoresByDisciplinesColumn = column;
            foreach (var discipline in checkingDisciplines)
            {
                var cell = $"{scoresByDisciplinesColumn}{scoresByDisciplinesStartRow}";
                var criteriaColumn = 'B';//////////////////////////////////////////////////////////
                var criteriaRow = 1 + disciplineCriteriaRows.First(c => c.Discipline.Code == discipline.Discipline.Code).Row;
                var scoreToMarkFormula = $"=IFS({cell} = \"\", \"\", " +
                    $"{cell} >= ${criteriaColumn}${criteriaRow}, \"5 (A)\", " +
                    $"{cell} >= ${criteriaColumn}${criteriaRow + 1}, \"4 (B)\", " +
                    $"{cell} >= ${criteriaColumn}${criteriaRow + 2}, \"4 (C)\", " +
                    $"{cell} >= ${criteriaColumn}${criteriaRow + 3}, \"3 (D)\", " +
                    $"{cell} >= ${criteriaColumn}${criteriaRow + 4}, \"3 (E)\", " +
                    $"{cell} >= ${criteriaColumn}${criteriaRow + 5}, \"2 (F)\")";

                InsertRepeatedFormula(scoreToMarkFormula, column, row, column, row + studentCount);
                column = Column.Next(column);
                scoresByDisciplinesColumn = Column.Next(scoresByDisciplinesColumn);
            }
            return (column, row + studentCount); // 4 or 5?
        }

        private (char Column, int Row) InsertMarksAnalyticsByDiscipline(char column, int row, int firstStudentRow, int lastStudentRow)
        {
            var inserted = new List<object> 
            { 
                Headers.MarkFiveCountHeader, 
                Headers.MarkFourCountHeader,
                Headers.MarkThreeCountHeader, 
                Headers.MarkTwoCountHeader, 
                Headers.AverageScoreHeader 
            };
            InsertValues($"{sheet}!{column}{row}", inserted, Dimension.Column);
            FormatCells(column, row, column, row + inserted.Count - 1, CellFormats.BoldFormat);
            FormatCells(column, row, column, row + inserted.Count - 1, CellFormats.RightHorizontalAlignmentFormat);

            var (nextFreeColumn, nextFreeRow) = InsertEachMarkTypeCount(column, row, firstStudentRow, lastStudentRow);

            var marksStartColumn = Column.Next(column);
            var marksEndColumn = Column.Behind(marksStartColumn, disciplineCodesNames.Count - 1);
            (column, _) = InsertGeneralCompetenceMarksAnalytics(nextFreeColumn, row - 1, marksStartColumn, marksEndColumn);
            return (column, nextFreeRow);
        }

        private (char Column, int Row) InsertEachMarkTypeCount(char column, int row, int firstStudentRow, int lastStudentRow)
        {
            var insertColumn = Column.Next(column);
            foreach (var discipline in checkingDisciplines)
            {
                var range = $"{insertColumn}{firstStudentRow}:{insertColumn}{lastStudentRow}";
                var criteriaRow = disciplineCriteriaRows.First(c => c.Discipline.Code == discipline.Discipline.Code).Row;
                InsertFormula($"=COUNTIF({range}, \"5*\") + COUNTIF({range}, \"5\")", insertColumn, row); // пятёрки
                InsertFormula($"=COUNTIF({range}, \"4*\") + COUNTIF({range}, \"4\")", insertColumn, row + 1); // четвёрки
                InsertFormula($"=COUNTIF({range}, \"3*\") + COUNTIF({range}, \"3\")", insertColumn, row + 2); // тройки
                InsertFormula($"=COUNTIF({range}, \"2*\") + COUNTIF({range}, \"2\")", insertColumn, row + 3); // двойки
                insertColumn = Column.Next(insertColumn);
            }

            var startColumn = Column.Next(column);
            var formula = $"=ROUND((5 * {startColumn}{row} + 4 * {startColumn}{row + 1} + " +
                $"3 * {startColumn}{row + 2} + 2 * {startColumn}{row + 3}) / " +
                $"SUM({startColumn}{row}:{startColumn}{row + 3}); 2)";

            InsertRepeatedFormula(formula, startColumn, row + 4, Column.Prev(insertColumn), row + 4);

            return (insertColumn, row + 5);
        }

        private (char Column, int Row) InsertGeneralCompetenceMarksAnalytics(char column, int row, char marksStartColumn, char marksEndColumn)
        {
            InsertValue("Итого", column, row);
            /// rotate "Итого"
            FormatCell(column, row, CellFormats.Minus90DegreeRotateFormat);
            FormatCell(column, row, CellFormats.CenterHorizontalAlightmentFormat);
            FormatCell(column, row, CellFormats.BoldFormat);
            /// merge for "Итого"
            MergeCells(column, row - 1, column, row, MergeType.Columns);

            ++row;
            /// итого, кол-во оценок каждого типа
            var formula = $"=SUM({marksStartColumn}{row}:{marksEndColumn}{row})";
            InsertRepeatedFormula(formula, column, row, column, row + 3);

            return (Column.Next(column), row + 4);
        }

        private (char Column, int Row) InsertCompetenceEvaluationCriteria(char column, int row)
        {
            var inserted = new List<object> 
            { 
                Headers.CompetenceAssessmentCriteriaHeader, 
                Headers.MinHeader, 
                Headers.MaxHeader, 
                Headers.EctsHeader, 
                Headers.MarkHeader 
            };
            InsertValues($"{sheet}!{column}{row}", inserted, Dimension.Row);
            FormatCell(column, row, CellFormats.BoldFormat);
            ++row;

            competenceCriteriaRow = row;
            competenceCriteriaColumn = Column.Next(column);

            foreach (var c in competenceCriterion.OrderBy(s => s.ECTSMark))
            {
                var insertedValues = new List<object> { "", $"{c.MinScore}", $"{c.MaxScore}", $"{c.ECTSMark}", $"{c.FivePointScaleMark}" };
                InsertValues($"{sheet}!{column}{row}", insertedValues, Dimension.Row);
                ++row;
            }

            return (Column.Behind(column, inserted.Count), row);
        }

        private (char Column, int Row) InsertCompetenceHeaders(char column, int row)
        {
            InsertValues($"{sheet}!{column}{row}", new List<object>(competenceCodes), Dimension.Row);
            FormatCells(column, row, Column.Behind(column, competenceCodes.Count - 1), row, CellFormats.Minus90DegreeRotateFormat);
            FormatCells(column, row, Column.Behind(column, competenceCodes.Count - 1), row, CellFormats.CenterHorizontalAlightmentFormat);

            return (Column.Behind(column, competenceCodes.Count), row + 1);
        }

        private (char Column, int Row) InsertPercentageOfStudentsCompetenceFormation(char column, int row)
        {
            var endColumn = Column.Behind(column, competenceCodes.Count - 1);
            var endRow = row + studentCount - 1;
            InsertScaleBasedConditionalFormating(column, row, endColumn, endRow, competenceCriteriaColumn, competenceCriteriaRow);

            var currentColumn = column;
            foreach (var code in competenceCodes)
            {
                var disciplineCode = checkingDisciplines.First(c => c.CheckingCompetences.Select(cc => cc.Code).Contains(code)).Discipline.Code;
                var criteriaRow = 1 + disciplineCriteriaRows.First(c => c.Discipline.Code == disciplineCode).Row;
                var maxScoreCell = $"$C${criteriaRow}";
                var cell = $"{currentColumn}{scoresByDisciplinesStartRow}";
                var formul = $"=IF({cell} = \"\", \"\", ROUND({cell} / {maxScoreCell} * 100))";
                InsertRepeatedFormula(formul, currentColumn, row, currentColumn, row + studentCount - 1); // !!!!!!! studentCount
                currentColumn = Column.Next(currentColumn);
            }
            return (currentColumn, endRow + 1);
        }

        private (char Column, int Row) InsertSheetByGroupsInfo(char column, int row)
        {
            var values = new List<object> { Headers.SheetDocumentsHeader }; groups.Add(groups[0]);
            foreach (var group in groups)
            {
                values.Add(group);
                values.Add("");
            }
            InsertValues($"{sheet}!{column}{row}", values, Dimension.Row);
            FormatCell(column, row, CellFormats.BoldFormat);

            var leftMergeColummn = Column.Next(column);
            for (var i = 0; i < groups.Count; ++i)
            {
                for (var j = 0; j < disciplineCodesNames.Count + 1; ++j)
                {
                    MergeCells(leftMergeColummn, row + j, Column.Next(leftMergeColummn), row + j, MergeType.Rows);
                }
                leftMergeColummn = Column.Behind(leftMergeColummn, 2);
            }
            ++row;

            //вставка дисциплин для которых нужны ведомости
            InsertValues($"{sheet}!A{row}", new List<object>(disciplineCodesNames), Dimension.Column);

            return (leftMergeColummn, row + disciplineCodesNames.Count);
        }

        private (char clmn, int currentRow) InsertStudentsMidCerificationMarks(char column, int row)
        {
            if (midCerificationResults.Count == 0)
            {
                return (column, row);
            }

            var results = studentNames.Select(n =>
            {
                var midResult = midCerificationResults.Where(r => r.Student.Contains(n));
                return (n, midResult);
            });

            foreach (var (studentName, midResult) in results)
            {
                if (!midResult.Any())
                {
                    ++row;
                    continue;
                }

                var marks = new List<string>();
                foreach (var d in checkingDisciplines)
                {
                    var res = midResult
                        .FirstOrDefault(r => (r.Student.Contains(studentName) || studentName.Contains(r.Student))
                            && (d.Discipline.RussianName.Contains(r.Discipline) || r.Discipline.Contains(d.Discipline.RussianName)));

                    var mark = res == null ? "" : res.MidtermCertificationMark;
                    marks.Add(mark);
                }
                InsertValues($"{sheet}!{column}{row}", new List<object>(marks), Dimension.Row);
                ++row;
            }

            return (Column.Behind(column, checkingDisciplines.Count), row);
        }

        private void InsertScaleBasedConditionalFormating(char startColumn, int startRow, char endColumn, int endRow,
            char criteriaStartColumn, int criteriaStartRow)
        {
            var gridRange = new GridRange
            {
                StartColumnIndex = Column.ToInt(startColumn) - 1,
                StartRowIndex = startRow - 1,
                EndColumnIndex = Column.ToInt(endColumn),
                EndRowIndex = endRow,
            };

            var gridRanges = new List<GridRange>
            {
                gridRange
            };

            var startCell = $"{startColumn}{startRow}";
            var clmn = criteriaStartColumn;
            var nxtClmn = Column.Next(clmn);
            var row = criteriaStartRow;

            var yellow = new CellFormat { BackgroundColor = Colors.Yellow };
            var green = new CellFormat { BackgroundColor = Colors.Green };
            var orange = new CellFormat { BackgroundColor = Colors.Orange };
            var red = new CellFormat { BackgroundColor = Colors.Red };

            AddConditionalFormatRuleBasedOnCustomFormula(gridRanges, $"=AND({startCell}>=${clmn}${row};{startCell}<=${nxtClmn}${row})", green);
            AddConditionalFormatRuleBasedOnCustomFormula(gridRanges, $"=AND({startCell}>=${clmn}${row + 2};{startCell}<=${nxtClmn}${row + 1})", yellow);
            AddConditionalFormatRuleBasedOnCustomFormula(gridRanges, $"=AND({startCell}>=${clmn}${row + 4};{startCell}<=${nxtClmn}${row + 3})", orange);
            AddConditionalFormatRuleBasedOnCustomFormula(gridRanges, $"=AND({startCell}>=${clmn}${row + 5};{startCell}<=${nxtClmn}${row + 5})", red);
        }

        private void ApplyInitialStyles(int questionsCount, int engagedRows)
        {
            AutoResizeCells(Dimension.Column, 0, 1);

            FormatCells('B', 1, Column.Behind('B', questionsCount), engagedRows, CellFormats.CenterHorizontalAlightmentFormat);

            AutoResizeCells(Dimension.Column, 0, 1);

            ResizeCells(Dimension.Column, 1, questionsCount, 35);
        }

        public void Generate()
        {
            if (!initializationSuccsess)
            {
                Console.WriteLine("Initialization failed");
                return;
            }

            var currentRow = 1; // 1-based
            currentRow = InsertHorizontalNamesWithQuestionNumbers('B', currentRow).Row;

            // student scores by questions + analytics block
            var scoresByQuestionStartRow = currentRow;
            var clmn = InsertStudentNames('A', scoresByQuestionStartRow).Column;
            currentRow = 1 + InsertStudentScoresByQuestions(clmn, scoresByQuestionStartRow).Row;
            currentRow = 1 + InsertParticipantsNumberAnalytics('A', currentRow, scoresByQuestionStartRow).Row;

            var questionsCount = checkingDisciplines.Sum(d => d.QuestionsCount);
            var engagedRows = studentNames.Count * 5 + disciplineCodesNames.Count * 9 + 50;
            ApplyInitialStyles(1 + questionsCount, engagedRows); // или final????

            // disciplines evaluation criteria block
            currentRow = InsertBoldHeader(Headers.DisciplineAssessmentCriteriaHeader, 'A', currentRow).Row;
            currentRow = 1 + InsertDisciplineEvaluationCriteria('A', currentRow).Row;

            // student scores by disciplines block
            (clmn, currentRow) = InsertBoldHeader(Headers.ScoresByDisciplinesHeader, 'A', currentRow);
            currentRow = InsertVerticalDisciplinesNames(clmn, currentRow).Row;
            clmn = InsertStudentNames('A', currentRow).Column;
            currentRow = 1 + InsertStudentScoresByDisciplines(clmn, currentRow).Row;

            // student marks (5-scale + ects) + analytics by discipline based on test ('disciplines evaluation criteria' and 'student scores by disciplines' blocks)
            (clmn, currentRow) = InsertBoldHeader(Headers.EctsAndFiveScaleMarksHeader, 'A', currentRow);
            currentRow = InsertVerticalDisciplinesNames(clmn, currentRow).Row;
            var firstStudentRow = currentRow;
            var lastStudentRow = currentRow + studentCount - 1;
            clmn = InsertStudentNames('A', firstStudentRow).Column;
            (clmn, currentRow) = InsertStudentsCompetenceMarks(clmn, firstStudentRow);
            currentRow = 1 + InsertMarksAnalyticsByDiscipline('A', currentRow, firstStudentRow, lastStudentRow).Row;

            // competence evaluation criteria block
            currentRow = 1 + InsertCompetenceEvaluationCriteria('A', currentRow).Row;

            // student percent of competence formation by competences
            (clmn, currentRow) = InsertBoldHeader(Headers.CompetencesFormationHeader, 'A', currentRow);
            currentRow = InsertCompetenceHeaders(clmn, currentRow).Row;
            clmn = InsertStudentNames('A', currentRow).Column;
            currentRow = 1 + InsertPercentageOfStudentsCompetenceFormation(clmn, currentRow).Row;

            currentRow = 1 + InsertSheetByGroupsInfo('A', currentRow).Row;

            // student 5-scale marks + analytics by discipline based on midterm certification 
            (clmn, currentRow) = InsertBoldHeader(Headers.MidtermCerificationMarksHeader, 'A', currentRow);
            currentRow = InsertVerticalDisciplinesNames(clmn, currentRow).Row;
            firstStudentRow = currentRow;
            lastStudentRow = currentRow + studentCount - 1;
            (clmn, _) = InsertStudentNames('A', currentRow);
            (clmn, currentRow) = InsertStudentsMidCerificationMarks(clmn, currentRow);
            currentRow = 1 + InsertMarksAnalyticsByDiscipline('A', currentRow, firstStudentRow, lastStudentRow).Row;

            SendBatchUpdateValuesRequest();
            SendBatchUpdateSpreadsheetRequest();
        }
    }
}
