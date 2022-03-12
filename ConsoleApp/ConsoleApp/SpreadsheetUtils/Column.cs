namespace ConsoleApp
{
    public static class Column
    {
        public static int ToInt(char c) => c - 'A' + 1;

        public static char ToChar(int i) => (char)('A' + i - 1); // сделать private

        public static char Prev(char c) => ToChar(ToInt(c) - 1);
       
        public static char Next(char c) => ToChar(ToInt(c) + 1);

        public static char Behind(char c, int i) => ToChar(ToInt(c) + i); 
    }
}
