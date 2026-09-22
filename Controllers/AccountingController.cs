using Microsoft.AspNetCore.Mvc;

namespace waranon_csi402.Controllers
{
    public class AccountingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
