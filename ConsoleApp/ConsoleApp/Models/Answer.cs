using System;

namespace ConsoleApp
{
    public class Answer
    {
        public Answer(int number, string answer)
        {
            Number = number;
            AnswerValue = answer;
            Letter = Convert.ToChar(96 + number).ToString();
        }

        public Answer(string answer)
        {
            AnswerValue = answer;
        }

        public int? Number { get; private set; }

        public string Letter { get; private set; }

        public string AnswerValue { get; private set; }
    }
}
