namespace WebApp.Models
{
    public class Player
    {
        public string Name { get; set; }
        public string SteamId { get; set; }
        public string UnrankedRating { get; set; }
        public string DMRating { get; set; }
        public string TeamDMRating { get; set; }
        public string RMRating { get; set; }
        public string TeamRMRating { get; set; }

        public Player(string name, string steamId, string unrankedRating, string dMRating, string teamDMRating, string rMRating, string teamRMRating)
        {
            Name = name;
            SteamId = steamId;
            UnrankedRating = unrankedRating;
            DMRating = dMRating;
            TeamDMRating = teamDMRating;
            RMRating = rMRating;
            TeamRMRating = teamRMRating;
        }

        public Player()
        {
            Name = "NameName";
            SteamId = "1234";
            UnrankedRating = "-";
            DMRating = "-";
            TeamDMRating = "-";
            RMRating = "-";
            TeamRMRating = "-";
        }
    }
}
