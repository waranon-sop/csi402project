using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using waranon_csi402.Models;
using waranon_csi402.ViewModels;

namespace waranon_csi402.Controllers;

public class AccountController : Controller
{
    private readonly Csi402dbContext _db;
    public AccountController(Csi402dbContext db)
    {
        _db = db;
    }


    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(LoginViewModel Data)
    {
        if (ModelState.IsValid)
        {
            // ตรวจสอบ Email + Password จาก Database
            var checkUser = (from u in _db.Users
                             where u.Email == Data.Email && u.Password == Data.Password
                             select u).FirstOrDefault();

            if (checkUser != null)
            {
                // Login สำเร็จ
                TempData["LoginEmail"] = checkUser.Email;
                TempData["Name"] = checkUser.FirstName + " " + checkUser.LastName;
                TempData["PhoneNumber"] = checkUser.PhoneNumber;
                TempData["Role"] = checkUser.Role;
                return RedirectToAction("UserList", "Admin");
            }
            else
            {
                // Login ไม่สำเร็จ
                ViewBag.Status = "อีเมลหรือรหัสผ่านไม่ถูกต้อง";
                return View(Data);
            }
        }
        return View(Data);
    }


    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Register(RegisterViewModel Data)
    {
        if (ModelState.IsValid)
        {
            
            var checkEmail = (from u in _db.Users
                              where u.Email == Data.Email
                              select u).FirstOrDefault();

            if (checkEmail == null)
            {
                // ยังไม่มี Email นี้ → บันทึกลงฐานข้อมูล
                User newUser = new();
                newUser.FirstName = Data.FirstName;
                newUser.LastName = Data.LastName;
                newUser.Email = Data.Email;
                newUser.PhoneNumber = Data.PhoneNumber;
                newUser.Password = Data.Password;
                newUser.Role = "Customer";
                newUser.IsActive = true;
                newUser.CreatedAt = DateTime.Now;
                _db.Add(newUser);
                _db.SaveChanges();

                ViewBag.Message = "สมัครสมาชิกสำเร็จ กรุณาเข้าสู่ระบบ";
                var loginModel = new LoginViewModel { Email = Data.Email };
                return View("Login", loginModel);
            }
            else
            {
                // มี Email นี้แล้ว
                ViewBag.Status = "Email นี้ถูกใช้งานแล้ว";
                return View(Data);
            }
        }
        return View(Data);
    }


    public IActionResult Lab5(Lab5ViewModel data)
    {
        @ViewBag.Name = "";
        data.LastName = "";
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

