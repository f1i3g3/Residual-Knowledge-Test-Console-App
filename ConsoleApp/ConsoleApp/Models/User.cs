namespace ConsoleApp
{
    public class User
    {
        public User(string userName, string userSurname, string userPatronimic)
        {
            Name = userName;
            Surname = userSurname;
            UserPatronimic = userPatronimic;
            FullName = $"{userSurname} {userName} {userPatronimic}";
            ShortFullNameWithSurnameFirst = $"{userSurname} {userName[0]}.{userPatronimic[0]}.";
            ShortFullNameWithSurnameLast = $"{userName[0]}.{userPatronimic[0]}. {userSurname}";
        }

        public string Name { get; private set; }

        public string Surname { get; private set; }

        public string UserPatronimic { get; private set; }

        public string FullName { get; private set; }

        public string ShortFullNameWithSurnameFirst { get; private set; }

        public string ShortFullNameWithSurnameLast { get; private set; }
    }
}
