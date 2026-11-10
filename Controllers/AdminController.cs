using Microsoft.AspNetCore.Mvc;
using waranon_csi402.Models;
using waranon_csi402.ViewModels;

namespace waranon_csi402.Controllers
{
    public class AdminController : Controller
    {
        private readonly Csi402dbContext _db;
        public AdminController(Csi402dbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return View();
        }


        public IActionResult UserList(string? status)
        {
            // select * from Users
            var users = (from u in _db.Users select u).ToList();
            if (status != null)
            {
                ViewBag.Status = status;
            }
            return View(users);
        }


        [HttpGet]
        public IActionResult AddUser()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(RegisterViewModel Data)
        {
            if (ModelState.IsValid)
            {
                if (Data.Password != null)
                {
                    // ตรวจสอบ Email ซ้ำ
                    var checkEmail = (from u in _db.Users
                                     where u.Email == Data.Email
                                     select u).FirstOrDefault();

                    if (checkEmail == null)
                    {
                        User newUser = new();
                        newUser.FirstName = Data.FirstName;
                        newUser.LastName = Data.LastName;
                        newUser.Email = Data.Email;
                        newUser.PhoneNumber = Data.PhoneNumber;
                        newUser.Password = Data.Password;
                        newUser.Role = !string.IsNullOrEmpty(Data.Role) ? Data.Role : "Customer";
                        newUser.IsActive = true;
                        newUser.CreatedAt = DateTime.Now;
                        _db.Add(newUser);
                        _db.SaveChanges();
                        return RedirectToAction("UserList", new { status = "เพิ่มผู้ใช้งานสำเร็จ" });
                    }
                    else  
                    {
                        ViewBag.Status = "Email นี้ถูกใช้งานแล้ว";
                        return View(Data);
                    }
                }
                else
                {
                    ViewBag.Status = "Password Null!!";
                    return View(Data);
                }
            }
            return View(Data);
        }

        [HttpGet]
        public IActionResult EditUser(int? UserId)
        {
            var user = (from u in _db.Users where u.UserId == UserId select u).FirstOrDefault();
            if (user == null)
            {
                return RedirectToAction("UserList", new { status = "ไม่พบผู้ใช้งาน" });
            }
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(User data)
        {
            var user = (from u in _db.Users where u.UserId == data.UserId select u).FirstOrDefault();
            if (user != null)
            {
                user.FirstName = data.FirstName;
                user.LastName = data.LastName;
                user.Email = data.Email;
                user.PhoneNumber = data.PhoneNumber;
                user.Role = data.Role;
                if (!string.IsNullOrEmpty(data.Password))
                {
                    user.Password = data.Password;
                }
                _db.SaveChanges();
                return RedirectToAction("UserList", new { status = "แก้ไขข้อมูลสำเร็จ" });
            }
            ViewBag.Status = "เกิดข้อผิดพลาดในการแก้ไข";
            return View(data);
        }

        public IActionResult DeleteUser(int? UserId)
        {
            var user = (from u in _db.Users where u.UserId == UserId select u).FirstOrDefault();
            if (user != null)
            {
                try
                {
                    _db.Users.Remove(user);
                    _db.SaveChanges();
                    return RedirectToAction("UserList", new { status = "ลบสำเร็จ" });
                }
                catch
                {
                    return RedirectToAction("UserList", new { status = "ลบไม่สำเร็จ (มีข้อมูลเชื่อมโยงอยู่ในระบบ)" });
                }
            }
            return RedirectToAction("UserList", new { status = "ลบไม่สำเร็จ (ไม่พบข้อมูล)" });
        }
    }
}
