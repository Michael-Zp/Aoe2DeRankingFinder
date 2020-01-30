public class Player
{
    public string Name { get; set; }
    public string SteamId { get; set; }
    public int UnrankedRating { get; set; }
    public int DMRating { get; set; }
    public int TeamDMRating { get; set; }
    public int RMRating { get; set; }
    public int TeamRMRating { get; set; }

    public Player(string name, string steamId, int unrankedRating, int dMRating, int teamDMRating, int rMRating, int teamRMRating)
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
        UnrankedRating = -1;
        DMRating = -1;
        TeamDMRating = -1;
        RMRating = -1;
        TeamRMRating = -1;
    }
}