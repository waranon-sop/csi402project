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

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel model, string FirstName, string LastName, string PhoneNumber)
    {
        if (ModelState.IsValid)
        {
            // ใช้ ViewBag ส่งค่าไปแสดงที่หน้า Index
            ViewBag.LoginEmail = model.Email;
            ViewBag.LoginPassword = model.Password; 
            
            // นำข้อมูลที่ซ่อนไว้จากหน้า Login ส่งต่อไปให้หน้า Index
            ViewBag.Name = FirstName + " " + LastName;
            ViewBag.PhoneNumber = PhoneNumber;

            return View("Index");
        }

        // ถ้า Login ไม่สำเร็จ กลับไปหน้า Login พร้อมแสดง error
        return View(model);
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            // ส่งต่อข้อมูลทั้งหมดไปที่หน้า Login เพื่อไม่ให้ข้อมูลหาย
            var loginModel = new LoginViewModel { Email = model.Email };
            ViewBag.Message = "สมัครสมาชิกสำเร็จ กรุณาเข้าสู่ระบบ";
            ViewBag.FirstName = model.FirstName;
            ViewBag.LastName = model.LastName;
            ViewBag.PhoneNumber = model.PhoneNumber;
            
            return View("Login", loginModel);
        }

       
        return View(model);
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
