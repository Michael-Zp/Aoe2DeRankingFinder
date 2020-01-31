using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Net;

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

        ObservableCollection<Player> players = new ObservableCollection<Player>()
            {
                new Player("1 island 3 ranges", "123456789", "-90", "-90", "-90", "-90", "-90"),
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
            HttpWebRequest request = WebRequest.CreateHttp(url);
            HttpWebResponse response = (HttpWebResponse)(await request.GetResponseAsync());
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new HttpResourceNotFoundException("");
                }

                //Yes this is filthy, but it outputs standard error messages and I don´t know how else to access this method
                HttpResponseMessage msg = new HttpResponseMessage
                {
                    StatusCode = response.StatusCode
                };
                msg.EnsureSuccessStatusCode();
            }

            var responseStream = response.GetResponseStream();
            
            dynamic json = JsonConvert.DeserializeObject((new StreamReader(responseStream)).ReadToEnd());
            
            return json;
        }

        async private Task<Player> GetPlayer(string steamId, string name)
        {
            Player currentPlayer = new Player
            {
                Name = name,
                SteamId = steamId
            };

            if(currentPlayer.SteamId != null)
            {
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
            }
            else
            {
                currentPlayer.Name = currentPlayer.Name + "*";
            }
            return currentPlayer;
        }

        private class PlayerNotFoundException : Exception
        {
            public PlayerNotFoundException(string playerName) : base("Could not finde a steam user with the name: " + playerName)
            { }
        }

        private void Measure(Stopwatch sw, string label)
        {
            Console.WriteLine(label + ":\t" + sw.ElapsedMilliseconds);
            sw.Restart();
        }

        async private void btnSearchPlayer_Click(object sender, RoutedEventArgs e)
        {
            tbError.Text = "";
            pbSearch.Value = 0;


            string steamName = PlayerName.Text;

            const string DefaultSteamId = "ThisIsNoIDPunk";
            string searchedPlayerSteamId = DefaultSteamId;
            try
            {
                var request = WebRequest.CreateHttp(@"http://steamrep.com/search?q=" + steamName);
                var streamResponse = request.GetResponseAsync();

                Regex steamIdRegex = new Regex(@"\s\d{17}\s", RegexOptions.Compiled);

                pbSearch.Value = 15;

                var response = await streamResponse;

                StreamReader sr = new StreamReader(response.GetResponseStream());


                do
                {
                    var line = sr.ReadLine();

                    var match = steamIdRegex.Match(line);

                    if (match.Success)
                    {
                        searchedPlayerSteamId = match.Value.Trim();
                        break;
                    }
                }
                while (!sr.EndOfStream);
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
                foreach (dynamic player in playersOfLastMatch.Children())
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


            pbSearch.Value = 0;
        }

    }
}
