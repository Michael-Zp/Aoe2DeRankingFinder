using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Aoe2DeRanking
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>


    public partial class MainWindow : Window
    {

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

        ObservableCollection<Player> players = new ObservableCollection<Player>()
            {
                new Player("1 island 3 ranges", "123456789", -90, -90, -90, -90, -90),
            };

        public MainWindow()
        {
            InitializeComponent();
            
            MyDataGrid.ItemsSource = players;

            tbError.Text = "";
        }

        private class HttpResourceNotFoundException : Exception
        {
            public HttpResourceNotFoundException(string s) : base(s)
            { }
        }

        async private Task<object> GetJson(string url)
        {
            HttpClient client = new HttpClient();
            HttpResponseMessage responseBody = await client.GetAsync(url);

            if(!responseBody.IsSuccessStatusCode)
            {
                if(responseBody.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new HttpResourceNotFoundException("");
                }

                responseBody.EnsureSuccessStatusCode();
            }

            client.Dispose();

            return JsonConvert.DeserializeObject(await responseBody.Content.ReadAsStringAsync());
        }

        async private Task<Player> GetPlayer(string steamId, string name)
        {
            Player currentPlayer = new Player
            {
                Name = name,
                SteamId = steamId
            };

            var unrankedTask = GetJson(@"https://aoe2.net/api/leaderboard?game=aoe2de&leaderboard_id=0&steam_id=" + currentPlayer.SteamId);
            var DMTask = GetJson(@"https://aoe2.net/api/leaderboard?game=aoe2de&leaderboard_id=1&steam_id=" + currentPlayer.SteamId);
            var teamDMTask = GetJson(@"https://aoe2.net/api/leaderboard?game=aoe2de&leaderboard_id=2&steam_id=" + currentPlayer.SteamId);
            var RMTask = GetJson(@"https://aoe2.net/api/leaderboard?game=aoe2de&leaderboard_id=3&steam_id=" + currentPlayer.SteamId);
            var teamRMTask = GetJson(@"https://aoe2.net/api/leaderboard?game=aoe2de&leaderboard_id=4&steam_id=" + currentPlayer.SteamId);

            dynamic unranked = await unrankedTask;
            dynamic DM = await DMTask;
            dynamic teamDM = await teamDMTask;
            dynamic RM = await RMTask;
            dynamic teamRM = await teamRMTask;


            if (unranked.count > 0)
                currentPlayer.UnrankedRating = unranked.leaderboard[0].rating;

            if (DM.count > 0)
                currentPlayer.DMRating = DM.leaderboard[0].rating;

            if (teamDM.count > 0)
                currentPlayer.TeamDMRating = teamDM.leaderboard[0].rating;

            if (RM.count > 0)
                currentPlayer.RMRating = RM.leaderboard[0].rating;

            if (teamRM.count > 0)
                currentPlayer.TeamRMRating = teamRM.leaderboard[0].rating;

            return currentPlayer;
        }

        private class PlayerNotFoundException : Exception
        {
            public PlayerNotFoundException(string playerName) : base("Could not finde a steam user with the name: " + playerName)
            { }
        }

        async private void btnSearchPlayer_Click(object sender, RoutedEventArgs e)
        {
            tbError.Text = "";
            pbSearch.Value = 0;

            HttpClient client = new HttpClient();

            string steamName = PlayerName.Text;

            const string DefaultSteamId = "ThisIsNoIDPunk";
            string searchedPlayerSteamId = DefaultSteamId;
            try
            {
                string responseBody = await client.GetStringAsync(@"http://steamrep.com/search?q=" + steamName);
                Regex steamIdRegex = new Regex(@"\d{17}", RegexOptions.Compiled);

                pbSearch.Value = 15;

                foreach (string line in responseBody.Split('\n'))
                {
                    if (line.Contains("steamID64: https"))
                    {
                        foreach (var urlPart in line.Split('/'))
                        {
                            if (steamIdRegex.IsMatch(urlPart))
                            {
                                searchedPlayerSteamId = urlPart;
                            }
                        }

                    }
                }
            }
            catch (HttpRequestException)
            {
                tbError.Text = "Connection to server failed. Can you reach the site www.steamrep.com?";
            }

            pbSearch.Value = 20;

            try
            { 
                if (searchedPlayerSteamId.Equals(DefaultSteamId))
                {
                    throw new PlayerNotFoundException(steamName);
                }
                
                dynamic lastMatchJson = await GetJson(@"https://aoe2.net/api/player/lastmatch?game=aoe2de&steam_id=" + searchedPlayerSteamId);

                pbSearch.Value = 35;

                dynamic playersOfLastMatch = lastMatchJson.last_match.players;

               

                int count = 0;
                foreach(dynamic player in playersOfLastMatch.Children())
                {
                    count++;
                }

                Task<Player>[] tempPlayersTasks = new Task<Player>[count];

                int i = 0;
                foreach (dynamic player in playersOfLastMatch.Children())
                {
                    tempPlayersTasks[i] = GetPlayer((string)player.steam_id, (string)player.name);
                    i++;
                }

                pbSearch.Value = 50;

                List<Player> tempPlayers = new List<Player>();

                foreach (var playerTask in tempPlayersTasks)
                {
                    tempPlayers.Add(await playerTask);

                    pbSearch.Value = pbSearch.Value + 1.0 / count * 50.0;
                }

                players.Clear();

                foreach (var player in tempPlayers)
                {
                    players.Add(player);
                }
            }
            catch (HttpResourceNotFoundException)
            {
                tbError.Text = "The user " + steamName + " doesn´t seem to have an AOE2 DE account.";
            }
            catch (PlayerNotFoundException ex)
            {
                tbError.Text = ex.Message;
            }
            catch (HttpRequestException ex)
            {
                tbError.Text = "Request to aoe2.net failed. " + ex.Message;
            }
            

            client.Dispose();
            pbSearch.Value = 0;
        }
        
    }
}
