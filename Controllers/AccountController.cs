using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using waranon_csi402.Models;
using waranon_csi402.Services;
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
            // 1. หาผู้ใช้จาก Email
            var checkUser = (from u in _db.Users
                             where u.Email == Data.Email
                             select u).FirstOrDefault();

            // 2. ตรวจรหัสผ่านกับ Hash ใน DB (ถ้าเป็นรหัสผ่านแบบเก่า ระบบจะอัปเกรดเป็น Hash ให้)
            if (checkUser != null && PasswordHelper.Verify(checkUser, Data.Password))
            {
                _db.SaveChanges(); // บันทึก Hash ใหม่ กรณีเพิ่งอัปเกรดจากรหัสผ่านแบบเก่า

                // Login สำเร็จ
                TempData["LoginEmail"] = checkUser.Email;
                TempData["Name"] = checkUser.FirstName + " " + checkUser.LastName;
                TempData["PhoneNumber"] = checkUser.PhoneNumber;
                TempData["Role"] = checkUser.Role;

                // Redirect ตาม Role ของผู้ใช้
                if (checkUser.Role == "Admin")
                    return RedirectToAction("Index", "Admin");
                else if (checkUser.Role == "Staff")
                    return RedirectToAction("Index", "Staff");
                else if (checkUser.Role == "Accounting")
                    return RedirectToAction("Index", "Accounting");
                else
                    return RedirectToAction("Index", "Home");
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
                newUser.Password = PasswordHelper.Hash(Data.Password!); // เก็บเป็น Hash
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

    [HttpGet]
    public IActionResult Logout()
    {
        TempData.Remove("LoginEmail");
        TempData.Remove("Name");
        TempData.Remove("PhoneNumber");
        TempData.Remove("Role");
        return RedirectToAction("Index", "Home");
    }

    // ==========================================
    // การแจ้งเตือนของฉัน (In-app Notifications) — ใช้ได้ทุกบทบาท
    // เปิดหน้านี้แล้ว ระบบจะทำเครื่องหมายว่าอ่านแล้วทั้งหมด
    // ==========================================
    [HttpGet]
    public IActionResult Notifications()
    {
        var user = GetCurrentCustomer();
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        var notifications = (from n in _db.Notifications
                             where n.UserId == user.UserId
                             orderby n.CreatedAt descending
                             select n).Take(50).ToList();

        // ส่งค่า "ยังไม่อ่าน" ไปให้ View ไฮไลต์ก่อน แล้วค่อยทำเครื่องหมายอ่านแล้ว
        ViewBag.UnreadIds = (from n in notifications where !n.IsRead select n.NotificationId).ToList();

        foreach (var n in notifications)
        {
            n.IsRead = true;
        }
        _db.SaveChanges();

        return View(notifications);
    }

    // ==========================================
    // Helper: ดึงข้อมูลผู้ใช้ปัจจุบัน (Current Customer)
    // ==========================================
    private User? GetCurrentCustomer()
    {
        var email = TempData.Peek("LoginEmail") as string;
        if (!string.IsNullOrEmpty(email))
        {
            var user = (from u in _db.Users
                        where u.Email == email
                        select u).FirstOrDefault();
            if (user != null) return user;
        }

        return null;
    }

    // ==========================================
    // 1. หน้าโปรไฟล์ส่วนตัว & สมุดที่อยู่ (Profile & Address Book)
    // tab: "profile" (ข้อมูลส่วนตัว), "address" (สมุดที่อยู่), "password" (เปลี่ยนรหัสผ่าน)
    // ==========================================
    [HttpGet]
    public IActionResult Profile(string? tab, string? status, string? error)
    {
        var user = GetCurrentCustomer();
        if (user == null)
        {
            return RedirectToAction("Login");
        }

        ViewBag.CurrentTab = string.IsNullOrEmpty(tab) ? "profile" : tab.ToLower();
        ViewBag.Status = status;
        ViewBag.Error = error;

        // ดึงรายการที่อยู่ของผู้ใช้คนนี้ เรียงตามที่อยู่เริ่มต้นขึ้นก่อน
        var addresses = (from a in _db.Addresses
                         where a.UserId == user.UserId
                         orderby a.IsDefault descending, a.AddressId descending
                         select a).ToList();
        ViewBag.Addresses = addresses;

        // ดึงจำนวนคำสั่งซื้อของฉัน
        ViewBag.OrderCount = (from o in _db.Orders
                              where o.UserId == user.UserId
                              select o).Count();

        // ดึงจำนวนรายการโปรด (Wishlist)
        ViewBag.WishlistCount = (from w in _db.Wishlists
                                 where w.UserId == user.UserId
                                 select w).Count();

        return View(user);
    }

    // Route ลัดไปแท็บสมุดที่อยู่
    [HttpGet]
    public IActionResult AddressBook(string? status, string? error)
    {
        return RedirectToAction("Profile", new { tab = "address", status, error });
    }

    // ==========================================
    // 2. แก้ไขข้อมูลส่วนตัว (Update Profile)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(string firstName, string lastName, string? phoneNumber)
    {
        var user = GetCurrentCustomer();
        if (user == null) return RedirectToAction("Login");

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return RedirectToAction("Profile", new { tab = "profile", error = "กรุณากรอกชื่อและนามสกุลให้ครบถ้วน" });
        }

        user.FirstName = firstName.Trim();
        user.LastName = lastName.Trim();
        user.PhoneNumber = phoneNumber?.Trim();

        _db.SaveChanges();

        TempData["Name"] = user.FirstName + " " + user.LastName;
        TempData["PhoneNumber"] = user.PhoneNumber;

        return RedirectToAction("Profile", new { tab = "profile", status = "บันทึกข้อมูลส่วนตัวเรียบร้อยแล้ว" });
    }

    // ==========================================
    // 3. เปลี่ยนรหัสผ่าน (Change Password)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        var user = GetCurrentCustomer();
        if (user == null) return RedirectToAction("Login");

        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            return RedirectToAction("Profile", new { tab = "password", error = "กรุณากรอกรหัสผ่านเดิมและรหัสผ่านใหม่" });
        }

        if (!PasswordHelper.Verify(user, oldPassword))
        {
            return RedirectToAction("Profile", new { tab = "password", error = "รหัสผ่านเดิมไม่ถูกต้อง" });
        }

        if (newPassword.Length < 4)
        {
            return RedirectToAction("Profile", new { tab = "password", error = "รหัสผ่านใหม่ต้องมีความยาวอย่างน้อย 4 ตัวอักษร" });
        }

        if (newPassword != confirmPassword)
        {
            return RedirectToAction("Profile", new { tab = "password", error = "รหัสผ่านใหม่และการยืนยันรหัสผ่านไม่ตรงกัน" });
        }

        user.Password = PasswordHelper.Hash(newPassword);
        _db.SaveChanges();

        return RedirectToAction("Profile", new { tab = "password", status = "เปลี่ยนรหัสผ่านสำเร็จเรียบร้อยแล้ว" });
    }

    // ==========================================
    // 4. เพิ่มที่อยู่จัดส่งใหม่ (Add Address)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddAddress(Address data)
    {
        var user = GetCurrentCustomer();
        if (user == null) return RedirectToAction("Login");

        if (string.IsNullOrWhiteSpace(data.ReceiverName) || string.IsNullOrWhiteSpace(data.Phone) || string.IsNullOrWhiteSpace(data.AddressLine))
        {
            return RedirectToAction("Profile", new { tab = "address", error = "กรุณากรอกชื่อผู้รับ เบอร์โทร และที่อยู่ให้ครบถ้วน" });
        }

        var existingAddresses = (from a in _db.Addresses
                                 where a.UserId == user.UserId
                                 select a).ToList();

        bool isFirst = !existingAddresses.Any();

        // ถ้าเลือกให้เป็นที่อยู่เริ่มต้น หรือเป็นที่อยู่แรก ให้ปลด default อันอื่นออก
        if (data.IsDefault || isFirst)
        {
            foreach (var addr in existingAddresses)
            {
                addr.IsDefault = false;
            }
            data.IsDefault = true;
        }

        data.UserId = user.UserId;
        data.ReceiverName = data.ReceiverName.Trim();
        data.Phone = data.Phone.Trim();
        data.AddressLine = data.AddressLine.Trim();
        data.District = string.IsNullOrWhiteSpace(data.District) ? "-" : data.District.Trim();
        data.Province = string.IsNullOrWhiteSpace(data.Province) ? "กรุงเทพมหานคร" : data.Province.Trim();
        data.PostalCode = string.IsNullOrWhiteSpace(data.PostalCode) ? "-" : data.PostalCode.Trim();

        _db.Addresses.Add(data);
        _db.SaveChanges();

        return RedirectToAction("Profile", new { tab = "address", status = "เพิ่มที่อยู่จัดส่งใหม่เรียบร้อยแล้ว" });
    }

    // ==========================================
    // 5. แก้ไขที่อยู่จัดส่ง (Edit Address)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditAddress(Address data)
    {
        var user = GetCurrentCustomer();
        if (user == null) return RedirectToAction("Login");

        var address = (from a in _db.Addresses
                       where a.AddressId == data.AddressId && a.UserId == user.UserId
                       select a).FirstOrDefault();

        if (address == null)
        {
            return RedirectToAction("Profile", new { tab = "address", error = "ไม่พบที่อยู่ที่ต้องการแก้ไข" });
        }

        if (string.IsNullOrWhiteSpace(data.ReceiverName) || string.IsNullOrWhiteSpace(data.Phone) || string.IsNullOrWhiteSpace(data.AddressLine))
        {
            return RedirectToAction("Profile", new { tab = "address", error = "กรุณากรอกข้อมูลที่อยู่ให้ครบถ้วน" });
        }

        // ถ้าตั้งเป็นที่อยู่เริ่มต้น ให้ปลด default ตัวอื่น
        if (data.IsDefault && !address.IsDefault)
        {
            var otherAddresses = (from a in _db.Addresses
                                  where a.UserId == user.UserId && a.AddressId != address.AddressId
                                  select a).ToList();
            foreach (var addr in otherAddresses)
            {
                addr.IsDefault = false;
            }
            address.IsDefault = true;
        }

        address.ReceiverName = data.ReceiverName.Trim();
        address.Phone = data.Phone.Trim();
        address.AddressLine = data.AddressLine.Trim();
        address.District = string.IsNullOrWhiteSpace(data.District) ? "-" : data.District.Trim();
        address.Province = string.IsNullOrWhiteSpace(data.Province) ? "กรุงเทพมหานคร" : data.Province.Trim();
        address.PostalCode = string.IsNullOrWhiteSpace(data.PostalCode) ? "-" : data.PostalCode.Trim();

        _db.SaveChanges();

        return RedirectToAction("Profile", new { tab = "address", status = "แก้ไขที่อยู่จัดส่งเรียบร้อยแล้ว" });
    }

    // ==========================================
    // 6. ลบที่อยู่จัดส่ง (Delete Address)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteAddress(int addressId)
    {
        var user = GetCurrentCustomer();
        if (user == null) return RedirectToAction("Login");

        var address = (from a in _db.Addresses
                       where a.AddressId == addressId && a.UserId == user.UserId
                       select a).FirstOrDefault();

        if (address != null)
        {
            bool wasDefault = address.IsDefault;
            _db.Addresses.Remove(address);
            _db.SaveChanges();

            // ถ้าลบที่อยู่เริ่มต้น ให้ยกที่อยู่ถัดไปเป็น default อัตโนมัติ (ถ้ามี)
            if (wasDefault)
            {
                var nextDefault = (from a in _db.Addresses
                                   where a.UserId == user.UserId
                                   orderby a.AddressId descending
                                   select a).FirstOrDefault();
                if (nextDefault != null)
                {
                    nextDefault.IsDefault = true;
                    _db.SaveChanges();
                }
            }

            return RedirectToAction("Profile", new { tab = "address", status = "ลบที่อยู่จัดส่งเรียบร้อยแล้ว" });
        }

        return RedirectToAction("Profile", new { tab = "address", error = "ไม่พบที่อยู่ที่ต้องการลบ" });
    }

    // ==========================================
    // 7. ตั้งค่าที่อยู่เริ่มต้น (Set Default Address)
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetDefaultAddress(int addressId)
    {
        var user = GetCurrentCustomer();
        if (user == null) return RedirectToAction("Login");

        var allAddresses = (from a in _db.Addresses
                            where a.UserId == user.UserId
                            select a).ToList();

        var target = allAddresses.FirstOrDefault(a => a.AddressId == addressId);
        if (target != null)
        {
            foreach (var addr in allAddresses)
            {
                addr.IsDefault = (addr.AddressId == addressId);
            }
            _db.SaveChanges();

            return RedirectToAction("Profile", new { tab = "address", status = $"ตั้งที่อยู่ของคุณ '{target.ReceiverName}' เป็นที่อยู่เริ่มต้นเรียบร้อยแล้ว" });
        }

        return RedirectToAction("Profile", new { tab = "address", error = "ไม่พบที่อยู่ที่ระบุ" });
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

