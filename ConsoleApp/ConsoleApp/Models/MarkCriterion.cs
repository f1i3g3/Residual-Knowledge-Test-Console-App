namespace ConsoleApp
{
    public class MarkCriterion
    {
        public MarkCriterion(int minScore, 
            int maxScore, char eCTSMark, int fivePointScaleMark)
        {
            MinScore = minScore;
            MaxScore = maxScore;
            ECTSMark = eCTSMark;
            FivePointScaleMark = fivePointScaleMark;
        }

        public int MinScore { get; private set; }

        public int MaxScore { get; private set; }

        public char ECTSMark { get; private set; }

        public int FivePointScaleMark { get; private set; }
    }
}
