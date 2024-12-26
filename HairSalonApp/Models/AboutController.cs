using Microsoft.AspNetCore.Mvc;

namespace HairSalonApp.Models
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
