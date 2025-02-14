﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ContingentParser;
using CurriculumParser;
using ConsoleApp.InputFilesParser;

namespace ConsoleApp
{
	public class Generator
	{
		public static string Generate(DocxCurriculum curriculum, Contingent contingent, List<CheckingDiscipline> checkingDisciplines, List<MarkCriterion> competenceCriterions = null, List<StudentAnswer> studentAnswers = null, List<MidCerificationAssesmentResult> midCertificationResults = null, User user = null)
		{
			try
			{
				var groups = contingent
				.Where(s => s.CurriculumCode == curriculum.CurriculumCode.Replace("/", "\\")) 
				.Select(s => s.GroupInContingent)
				.Distinct()
				.ToList(); // не до конца распарсило

				var disciplines = curriculum.Disciplines.Select(d => d.Implementations[0].Discipline).ToList();

				var discipline = disciplines.First(d => d.Code == "009503");
				var competences = discipline.Implementations[0].Competences.Take(1).ToList();

				var listOfMarks = new List<MarkCriterion>(); // listOfMarkCreations from server?

				user = new User("Кузнецов", "Дмитрий", "Владимирович"); // автор ответов - по идее, нужна авторизация/вносить самому
				var config = new MsFormsParserConfiguration(4, 8, 3, user); // from where?

				var userChoice = new UserChoice(user, curriculum, contingent, checkingDisciplines);
				competenceCriterions = new List<MarkCriterion>
				{
					new MarkCriterion(90, 100, 'A', 5),
					new MarkCriterion(80, 89, 'B', 4),
					new MarkCriterion(70, 79, 'C', 4),
					new MarkCriterion(60, 69, 'D', 3),
					new MarkCriterion(50, 59, 'E', 3),
					new MarkCriterion(0, 49, 'F', 2)
				}; // берутся с сервера?

				midCertificationResults = new List<MidCerificationAssesmentResult>();
				studentAnswers = new List<StudentAnswer>();
				foreach (var d in checkingDisciplines)
				{
					var parser = new ResidualKnowledgeDataParser(d, userChoice.Students);
					var result = parser.Parse();
					midCertificationResults.AddRange(result.MidCerificationResults);
					studentAnswers.AddRange(result.StudentAnswers);
					d.Questions.AddRange(result.Questions);
				}

				/*
				var mcrParser = new MidCertificationResultsParser(d); // нужен ли?
				mcrParser.TryParseMidCertificationResults(out midCertificationResult);  // парсер отдельного файла?
				*/

				var spreadsheetGenerator = new GoogleSpreadsheetGenerator(userChoice, groups, competenceCriterions, studentAnswers, midCertificationResults);
				spreadsheetGenerator.Generate(); // exception point

				return spreadsheetGenerator.spreadsheetId;
			}
			catch
			{
				return null;
			}
		}
	}
}
