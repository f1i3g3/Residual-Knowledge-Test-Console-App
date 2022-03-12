using System.Collections.Generic;

namespace ConsoleApp
{
    public class Question
    {
        public Question(int number, List<Answer> rightAnswers, List<QuestionMarkCriterion> questionMarkCriteria = null)
        {
            Number = number;
            RightAnswers = rightAnswers;
            PossibleAnswers = new List<Answer>();
            QuestionMarkCriteria = questionMarkCriteria ?? new List<QuestionMarkCriterion>();
        }

        public Question(int number, string formulation, List<Answer> rightAnswers, 
            List<Answer> possibleAnswers, int? maxScore, 
            List<QuestionMarkCriterion> questionMarkCriteria = null)
        {
            Number = number;
            Formulation = formulation;
            RightAnswers = rightAnswers ?? new List<Answer>();
            PossibleAnswers = possibleAnswers ?? new List<Answer>();
            MaxScore = maxScore;
            QuestionMarkCriteria = questionMarkCriteria ?? new List<QuestionMarkCriterion>();
        }

        public int Number { get; private set; }

        public string Formulation { get; private set; } // берется из MS Forms

        public List<Answer> RightAnswers { get; set; } // берется из MS Forms

        public List<Answer> PossibleAnswers { get; private set; } // берется из парсинга pdf

        public int? MaxScore { get; set; } // берется из парсинга pdf

        public List<QuestionMarkCriterion> QuestionMarkCriteria { get; private set; } // неоткуда брать, только если пользователя заставлять -- плохой вариант
    }
}
