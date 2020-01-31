using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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

        public SearchModel()
        {
            Players = new List<Player>();
            ErrorText = "ErrorText";
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
            
            const string DefaultSteamId = "ThisIsNoIDPunk";
            string searchedPlayerSteamId = DefaultSteamId;
            try
            {
                var request = WebRequest.CreateHttp(@"http://steamrep.com/search?q=" + steamName);
                var streamResponse = request.GetResponseAsync();

                Regex steamIdRegex = new Regex(@"\s(\d{17})\s", RegexOptions.Compiled);
                
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
                ErrorText = "Connection to server failed. Can you reach the site www.steamrep.com?";
                success = false;
            }

            if (success)
            {
                try
                {
                    if (searchedPlayerSteamId.Equals(DefaultSteamId))
                    {
                        throw new PlayerNotFoundException(steamName);
                    }

                    dynamic lastMatchJson = await GetJson(@"https://aoe2.net/api/player/lastmatch?game=aoe2de&steam_id=" + searchedPlayerSteamId);
                
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
                    

                    List<Player> tempPlayers = new List<Player>();

                    foreach (var playerTask in tempPlayersTasks)
                    {
                        tempPlayers.Add(await playerTask);
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
            

            Players = outPlayers;

            return success;
        }
    }
}
