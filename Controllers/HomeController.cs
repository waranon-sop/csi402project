using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using waranon_csi402.Models;
using waranon_csi402.ViewModels;

namespace waranon_csi402.Controllers;

public class HomeController : Controller
{
    private readonly Csi402dbContext _db;
    public HomeController(Csi402dbContext db)
    {
        _db = db;
    }


    [HttpGet]
    public IActionResult Lab9()
    {
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Lab9(Labstudent data)
    {
        if (ModelState.IsValid)
        {
            if (data.StdPassword != null)
            {
                var checkID = (from a in _db.Labstudents where a.StdId == data.StdId select a).FirstOrDefault();
                if (checkID == null)
                {
                    Labstudent s = new();
                    s.StdId = data.StdId;
                    s.StdPassword = data.StdPassword;
                    s.StdName = data.StdName;
                    s.StdLastname = data.StdLastname;
                    _db.Add(s);
                    _db.SaveChanges();
                    return View(data);
                }
                else  // มีอยู่แล้ว → แจ้ง error
                {
                    ViewBag.Status = "username ซ้ำ";
                    return View(data);
                }
            }
            else
            {
                ViewBag.Status = "Password Null!!";
                return View(data);
            }
        }
        return View(data);
    }
    public IActionResult Lab9_1(string? status)
    {

        var user = (from l in _db.Labstudents select l).ToList();
        if (status != null)
        {
            ViewBag.Status = status;
        }
        return View(user);
    }

    public IActionResult Lab10(string? StdId)
    {
        var User = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        return View(User);
    }


    [HttpPost]

    public IActionResult Lab10(Labstudent data)
    {
        var User = (from s in _db.Labstudents where s.StdId == data.StdId select s).FirstOrDefault();
        User.StdPassword = data.StdPassword;
        User.StdName = data.StdName;
        User.StdLastname = data.StdLastname;
        _db.SaveChanges();
        return View(data);
    }
    
    public IActionResult Lab10_1(string? StdId)
    {
        var check = (from s in _db.Labstudents where s.StdId == StdId select s).FirstOrDefault();
        if (check != null)
        {
            _db.Remove(check);
            _db.SaveChanges();
            return RedirectToAction("Lab9_1", "Home", new { status = "ลบสำเร็จ" });
        }
        else
        {
            return RedirectToAction("Lab9_1", "Home", new { status = "ลบไม่สำเร็จ" });
        }
    }
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

}

