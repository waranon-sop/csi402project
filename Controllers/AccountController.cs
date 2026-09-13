using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using waranon_csi402_5008_Assessment4.Models;

namespace waranon_csi402_5008_Assessment4.Controllers;

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
    public IActionResult Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            // TODO: ตรวจสอบข้อมูล Login กับฐานข้อมูล
            // ตัวอย่าง: ถ้า Login สำเร็จ redirect ไปหน้าแรก
            return RedirectToAction("Index", "Home");
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
            // TODO: บันทึกข้อมูลสมาชิกลงฐานข้อมูล
            // ตัวอย่าง: ถ้าสมัครสำเร็จ redirect ไปหน้า Login
            return RedirectToAction("Login");
        }

        // ถ้าสมัครไม่สำเร็จ กลับไปหน้า Register พร้อมแสดง error
        return View(model);
    }
}
