using Microsoft.AspNetCore.Mvc;
using System.Text.Encodings.Web;
using WebApp.Models;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
    public class SearchController : Controller
    {

        // 
        // GET: / 

        async public Task<IActionResult> Index(string name)
        {
            SearchModel model = new SearchModel();

            if(string.IsNullOrEmpty(name))
            {
                ViewData["Error"] = "";
                return View(model.Players);
            }


            var success = await model.FillPlayers(name);

            if(success)
            {
                ViewData["Message"] = model.Players[0].SteamId;
                ViewData["Name"] = model.Players[0].Name;
                ViewData["Players"] = model.Players;
            }
            ViewData["Error"] = model.ErrorText;

            return View(model.Players);
        }
    }
}