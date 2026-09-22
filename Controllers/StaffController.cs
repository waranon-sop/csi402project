using Microsoft.AspNetCore.Mvc;

namespace waranon_csi402.Controllers
{
    public class StaffController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
