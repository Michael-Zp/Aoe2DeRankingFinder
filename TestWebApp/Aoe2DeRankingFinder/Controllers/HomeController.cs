using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using Aoe2DeRankingFinder.Models;
using System.Threading.Tasks;

namespace Aoe2DeRankingFinder.Controllers
{
    public class HelloWorldController : Controller
    {
        // 
        // GET: /HelloWorld/
        
        public string Index()
        {
            return "This is my default action...";
        }

        // 
        // GET: /HelloWorld/Welcome/ 

        public string Welcome()
        {
            return "This is the Welcome action method...";
        }
        
        // 
        // GET: /HelloWorld/Search/ 

        async public Task<IActionResult> Search(string name)
        {

            SearchModel model = new SearchModel();

            var success = await model.FillPlayers(name);

            ViewData["Message"] = model.Players[0].SteamId;
            ViewData["Name"] = model.Players[0].Name;

            return View();
        }
    }
}