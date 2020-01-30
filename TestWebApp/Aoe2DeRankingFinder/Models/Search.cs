using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aoe2DeRankingFinder.Models
{
    public class SearchModel
    {
        public List<Player> Players {get; set;}
        public string ErrorText { get; set; }
        public int ProgressBarValue { get; set; }

        public SearchModel()
        {
            Players = new List<Player>();
            ErrorText = "ErrorText";
            ProgressBarValue = 50;
        }

        async public Task<bool> FillPlayers(string steamName)
        {
            List<Player> outPlayers = new List<Player>();

            HttpClient client = new HttpClient();

            System.Diagnostics.Debug.WriteLine("HI");

            string DefaultSteamId = "ThisIsNoIDPunk";
            string searchedPlayerSteamId = DefaultSteamId;
            try
            {
                string responseBody = await client.GetStringAsync(@"http://steamrep.com/search?q=" + steamName);
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
            }

            outPlayers.Add(new Player());
            outPlayers[0].Name = steamName;
            outPlayers[0].SteamId = searchedPlayerSteamId;

            Players = outPlayers;

            return true;
        }
    }
}
