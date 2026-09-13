using Microsoft.AspNetCore.Mvc;

namespace waranon_csi402_5008_Assessment4.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
