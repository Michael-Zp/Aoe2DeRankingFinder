using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
    public class SearchController : Controller
    {

        // 
        // GET: / 

        async private Task<IActionResult> Search(string name)
        {
            SearchModel model = new SearchModel();

            if (string.IsNullOrEmpty(name))
            {
                ViewData["Error"] = "";
                return View(model.Players);
            }
            
            var success = await model.FillPlayers(name);

            if (success)
            {
                ViewData["Message"] = model.Players[0].SteamId;
                ViewData["Players"] = model.Players;
            }
            ViewData["Name"] = name;
            ViewData["Error"] = model.ErrorText;

            return View(model.Players);
        }

        async public Task<IActionResult> Index(string name)
        {
            return await Search(name);
        }
    }
}