using Microsoft.AspNetCore.Mvc;

namespace waranon_csi402.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult UserList()
        {
            return View();
        }

        public IActionResult AddUser()
        {
            return View();
        }
    }
}
