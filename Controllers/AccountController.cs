using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using waranon_csi402.ViewModels;

namespace waranon_csi402.Controllers;

public class AccountController : Controller
{
    public IActionResult Index()
    {
        return View();
    }


    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }


    [HttpPost]
    public IActionResult Login(LoginViewModel Data, string FirstName, string LastName, string PhoneNumber)
    {
        if (ModelState.IsValid)
        {

            ViewBag.LoginEmail = Data.Email;
            ViewBag.LoginPassword = Data.Password;


            ViewBag.Name = FirstName + " " + LastName;
            ViewBag.PhoneNumber = PhoneNumber;

            return View("Index");
        }
        return View(Data);
    }


    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }


    [HttpPost]
    public IActionResult Register(RegisterViewModel Data)
    {
        if (ModelState.IsValid)
        {
            var loginModel = new LoginViewModel { Email = Data.Email };
            ViewBag.Message = "สมัครสมาชิกสำเร็จ กรุณาเข้าสู่ระบบ";
            ViewBag.FirstName = Data.FirstName;
            ViewBag.LastName = Data.LastName;
            ViewBag.PhoneNumber = Data.PhoneNumber;

            return View("Login", loginModel);
        }


        return View(Data);
    }




    public IActionResult Lab5(Lab5ViewModel data)
    {


        data.Name = "Waranon";
        data.LastName = "Soprok";
        data.UserId = "PipeSoMad";
        data.Password = "123456";
        data.Address = "123/456 Moo 7, Soi 8, Bangkok";
        data.Age = 21;
        data.Hight = 177;
        data.Weight = 70;


        return View(data);
    }
    public IActionResult Lab5_2()
    {
        var Data = new List<Lab5ViewModel>
        {
            new Lab5ViewModel{Name = "A", LastName = "B"},
            new Lab5ViewModel{Name = "C", LastName = "D"},

        };
        return View(Data);
    }

    public IActionResult Lab5_3()
    {
        return View();
    }
    [HttpPost]
    public IActionResult Lab5_3(Lab5ViewModel data)
    {
        string Name;

        Name = data.Name + " " + data.LastName;
        ViewBag.Name = Name;
        return RedirectToAction("Lab5", "Account", data);
    }

}
