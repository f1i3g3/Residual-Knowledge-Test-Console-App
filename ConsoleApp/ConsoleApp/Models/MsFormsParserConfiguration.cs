namespace ConsoleApp
{
    public class MsFormsParserConfiguration
    {
        /// <summary>
        /// 0-based index of column, that contains author of answers surname and name
        /// Author can be as student as creator of test
        /// </summary>
        public int StudentNameIndex { get; private set; }

        /// <summary>
        /// 0-based index of column, where first answer occurs
        /// </summary>
        public int StartQuestionIndex { get; private set; }

        /// <summary>
        /// Number of columns that contain information about particular question.
        /// Shift is needed to know how to many columns skip to get to column with answer to the next question
        /// </summary>
        public int Shift { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public User User { get; private set; }

        public MsFormsParserConfiguration(int studentNameIndex, int startQuestionIndex, int shift, User user)
        {
            StudentNameIndex = studentNameIndex;
            StartQuestionIndex = startQuestionIndex;
            Shift = shift;
            User = user;
        }
    }
}
