using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebApp.Models
{
    public class SearchModel
    {
        public List<Player> Players { get; set; }
        public string ErrorText { get; set; }
        public int ProgressBarValue { get; set; }

        public SearchModel()
        {
            Players = new List<Player>();
            ErrorText = "ErrorText";
            ProgressBarValue = 50;
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

            if (!responseBody.IsSuccessStatusCode)
            {
                if (responseBody.StatusCode == System.Net.HttpStatusCode.NotFound)
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

        async public Task<bool> FillPlayers(string steamName)
        {
            ErrorText = "";

            bool success = true;

            List<Player> outPlayers = new List<Player>();

            HttpClient client = new HttpClient();
            
            string DefaultSteamId = "ThisIsNoIDPunk";
            string searchedPlayerSteamId = DefaultSteamId;
            try
            {
                string url = @"http://steamrep.com/search?q=" + steamName;

                string responseBody = await client.GetStringAsync(url);
                Regex steamIdRegex = new Regex(@"\d{17}", RegexOptions.Compiled);
                
                ProgressBarValue = 15;

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
                ErrorText = "Connection to server failed. Can you reach the site www.steamrep.com?";
                success = false;
            }

            if(success)
            {
                ProgressBarValue = 20;

                try
                {
                    if (searchedPlayerSteamId.Equals(DefaultSteamId))
                    {
                        throw new PlayerNotFoundException(steamName);
                    }

                    dynamic lastMatchJson = await GetJson(@"https://aoe2.net/api/player/lastmatch?game=aoe2de&steam_id=" + searchedPlayerSteamId);

                    ProgressBarValue = 35;

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

                    ProgressBarValue = 50;

                    List<Player> tempPlayers = new List<Player>();

                    foreach (var playerTask in tempPlayersTasks)
                    {
                        tempPlayers.Add(await playerTask);

                        ProgressBarValue = ProgressBarValue + (int)(1.0 / count * 50.0);
                    }

                    outPlayers.Clear();

                    foreach (var player in tempPlayers)
                    {
                        outPlayers.Add(player);
                    }
                }
                catch (HttpResourceNotFoundException)
                {
                    ErrorText = "The user " + steamName + " doesn´t seem to have an AOE2 DE account.";
                    success = false;
                }
                catch (PlayerNotFoundException ex)
                {
                    ErrorText = ex.Message;
                    success = false;
                }
                catch (HttpRequestException ex)
                {
                    ErrorText = "Request to aoe2.net failed. " + ex.Message;
                    success = false;
                }
                catch (Exception ex)
                {
                    ErrorText = ex.Message;
                    success = false;
                }
            }
            

            client.Dispose();
            ProgressBarValue = 0;

            Players = outPlayers;

            return success;
        }
    }
}
