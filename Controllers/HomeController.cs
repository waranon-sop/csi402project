using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    // GET: หน้าร้าน แสดงแคตตาล็อกสินค้า และตัวกรองหมวดหมู่ / ค้นหา
    public IActionResult Index(int? categoryId, string? search, string? sortBy)
    {
        // 1. ดึงหมวดหมู่ทั้งหมดสำหรับทำแถบตัวกรอง (Category Filter)
        var categories = (from c in _db.Categories select c).ToList();
        ViewBag.Categories = categories;

        // แบนเนอร์หน้าร้านที่ Admin เปิดแสดง
        ViewBag.Banners = (from b in _db.Banners
                           where b.IsActive
                           orderby b.SortOrder, b.BannerId descending
                           select b).ToList();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.Search = search;
        ViewBag.SortBy = sortBy ?? "newest";

        // ดึงคูปองส่วนลดที่เปิดใช้งานสำหรับแสดงบน Banner แนะนำ
        var coupons = (from cp in _db.Coupons
                       where cp.IsActive == true
                       orderby cp.CouponId
                       select cp).Take(3).ToList();
        ViewBag.Coupons = coupons;

        // ดึงโปรโมชั่นอัตโนมัติ
        var promos = (from pr in _db.Promotions
                      where pr.IsActive == true
                      orderby pr.PromotionId
                      select pr).ToList();
        ViewBag.Promotions = promos;

        // คำนวณจำนวนสินค้าในตะกร้าสำหรับ Badge บน Navbar
        var loginEmail = TempData.Peek("LoginEmail") as string;
        int activeUserId = 0;
        if (!string.IsNullOrEmpty(loginEmail))
        {
            var u = (from usr in _db.Users where usr.Email == loginEmail select usr).FirstOrDefault();
            if (u != null) activeUserId = u.UserId;
        }

        if (activeUserId > 0)
        {
            ViewBag.CartCount = (from ci in _db.Cartitems where ci.UserId == activeUserId select ci.Quantity).Sum();
            var userWishlist = (from w in _db.Wishlists where w.UserId == activeUserId select w.ProductId).ToList();
            ViewBag.UserWishlist = new HashSet<int>(userWishlist);
        }
        else
        {
            ViewBag.UserWishlist = new HashSet<int>();
        }

        // 2. ดึงรายการสินค้าเฉพาะที่เปิดขายอยู่ (IsActive == true) เท่านั้น พร้อมหมวดหมู่และตัวเลือกสินค้า (Variants)
        var query = from p in _db.Products.Include(p => p.Category).Include(p => p.Productvariants)
                    where p.IsActive == true
                    select p;

        // กรองตามหมวดหมู่ถ้ามีการเลือก
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            query = from p in query where p.CategoryId == categoryId.Value select p;
        }

        // ค้นหาตามชื่อสินค้าหรือรายละเอียดถ้ามีการพิมพ์ค้นหา
        if (!string.IsNullOrWhiteSpace(search))
        {
            string s = search.Trim();
            query = from p in query
                    where p.ProductName.Contains(s) || (p.Description != null && p.Description.Contains(s))
                    select p;
        }

        // เรียงลำดับตามตัวเลือก (Sorting)
        switch (sortBy)
        {
            case "price_asc":
                query = from p in query orderby p.BasePrice ascending select p;
                break;
            case "price_desc":
                query = from p in query orderby p.BasePrice descending select p;
                break;
            case "name":
                query = from p in query orderby p.ProductName ascending select p;
                break;
            case "newest":
            default:
                query = from p in query orderby p.ProductId descending select p;
                break;
        }

        var products = query.ToList();
        return View(products);
    }

    // GET: ดูรายละเอียดสินค้า (Product Detail)
    public IActionResult ProductDetail(int? id, string? reviewStatus)
    {
        if (id == null)
        {
            return RedirectToAction("Index");
        }

        ViewBag.ReviewStatus = reviewStatus;

        // ค้นหาสินค้าตาม id พร้อมดึง Category และ Productvariants (สี, ไซส์, สต็อก)
        var product = (from p in _db.Products
                                .Include(p => p.Category)
                                .Include(p => p.Productvariants)
                       where p.ProductId == id
                       select p).FirstOrDefault();

        if (product == null || product.IsActive == false)
        {
            return RedirectToAction("Index");
        }

        // ดึงสินค้าอื่นๆ ในหมวดหมู่เดียวกันที่เปิดขายอยู่มาแนะนำ (Related Products)
        var relatedProducts = (from p in _db.Products
                               where p.CategoryId == product.CategoryId && p.ProductId != product.ProductId && p.IsActive == true
                               select p).Take(4).ToList();
        ViewBag.RelatedProducts = relatedProducts;

        // คำนวณจำนวนสินค้าในตะกร้าสำหรับ Badge บน Navbar
        var loginEmail = TempData.Peek("LoginEmail") as string;
        int activeUserId = 0;
        if (!string.IsNullOrEmpty(loginEmail))
        {
            var u = (from usr in _db.Users where usr.Email == loginEmail select usr).FirstOrDefault();
            if (u != null) activeUserId = u.UserId;
        }

        if (activeUserId > 0)
        {
            ViewBag.CartCount = (from ci in _db.Cartitems where ci.UserId == activeUserId select ci.Quantity).Sum();
            var userWishlist = (from w in _db.Wishlists where w.UserId == activeUserId select w.ProductId).ToList();
            ViewBag.UserWishlist = new HashSet<int>(userWishlist);
        }
        else
        {
            ViewBag.UserWishlist = new HashSet<int>();
        }

        // ดึงรีวิวของสินค้านี้
        var reviews = (from r in _db.Reviews.Include(r => r.User)
                       where r.ProductId == id
                       orderby r.CreatedAt descending
                       select r).ToList();
        ViewBag.Reviews = reviews;
        ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => (double)r.Rating) : 5.0;
        ViewBag.TotalReviews = reviews.Count;

        // ตรวจสอบว่าอยู่ใน Wishlist ของผู้ใช้ปัจจุบันหรือไม่
        bool isWishlisted = false;
        if (activeUserId > 0)
        {
            isWishlisted = (from w in _db.Wishlists
                            where w.UserId == activeUserId && w.ProductId == id
                            select w).Any();
        }
        ViewBag.IsWishlisted = isWishlisted;

        // แสดงปุ่มเขียนรีวิวเฉพาะคนที่เคยซื้อและได้รับสินค้าแล้ว
        ViewBag.CanReview = activeUserId > 0 && HasCompletedPurchase(activeUserId, product.ProductId);

        return View(product);
    }

    // POST: เขียนรีวิวสินค้า
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddReview(int productId, int rating, string? comment)
    {
        // 1. ต้องล็อกอินก่อนจึงจะรีวิวได้
        var loginEmail = TempData.Peek("LoginEmail") as string;
        var user = (from usr in _db.Users where usr.Email == loginEmail select usr).FirstOrDefault();
        if (string.IsNullOrEmpty(loginEmail) || user == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // 2. ต้องเคยซื้อสินค้านี้ และออเดอร์นั้นสำเร็จแล้ว (Completed)
        if (!HasCompletedPurchase(user.UserId, productId))
        {
            return RedirectToAction("ProductDetail", new { id = productId, reviewStatus = "รีวิวได้เฉพาะสินค้าที่คุณสั่งซื้อและได้รับสินค้าแล้วเท่านั้น" });
        }

        // 3. รีวิวสินค้าเดิมได้คนละ 1 ครั้ง
        bool alreadyReviewed = (from r in _db.Reviews
                                where r.UserId == user.UserId && r.ProductId == productId
                                select r).Any();
        if (alreadyReviewed)
        {
            return RedirectToAction("ProductDetail", new { id = productId, reviewStatus = "คุณรีวิวสินค้านี้ไปแล้ว" });
        }

        // 4. บันทึกรีวิว
        var newReview = new Review
        {
            ProductId = productId,
            UserId = user.UserId,
            Rating = (sbyte)Math.Clamp(rating, 1, 5),
            Comment = comment?.Trim(),
            CreatedAt = DateTime.Now
        };
        _db.Reviews.Add(newReview);
        _db.SaveChanges();

        return RedirectToAction("ProductDetail", new { id = productId, reviewStatus = "ขอบคุณสำหรับรีวิวครับ" });
    }

    // ==========================================
    // Helper: ตรวจว่าผู้ใช้เคยซื้อสินค้านี้ในออเดอร์ที่สำเร็จแล้ว (Completed) หรือไม่
    // ==========================================
    private bool HasCompletedPurchase(int userId, int productId)
    {
        return (from oi in _db.Orderitems
                where oi.Order.UserId == userId
                      && oi.Order.Status == "Completed"
                      && oi.Variant.ProductId == productId
                select oi).Any();
    }

    // POST: เพิ่ม/ลบ รายการโปรด (Toggle Wishlist)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ToggleWishlist(int productId, string? returnUrl)
    {
        var loginEmail = TempData.Peek("LoginEmail") as string;
        if (string.IsNullOrEmpty(loginEmail))
        {
            return RedirectToAction("Login", "Account");
        }

        var u = (from usr in _db.Users where usr.Email == loginEmail select usr).FirstOrDefault();
        if (u == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int activeUserId = u.UserId;
        var existing = (from w in _db.Wishlists
                        where w.UserId == activeUserId && w.ProductId == productId
                        select w).FirstOrDefault();

        if (existing != null)
        {
            _db.Wishlists.Remove(existing);
        }
        else
        {
            var w = new Wishlist
            {
                UserId = activeUserId,
                ProductId = productId,
                AddedAt = DateTime.Now
            };
            _db.Wishlists.Add(w);
        }
        _db.SaveChanges();

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) && referer.Contains("/Home/Wishlist"))
        {
            return RedirectToAction("Wishlist");
        }

        return RedirectToAction("ProductDetail", new { id = productId });
    }

    // GET: หน้ารายการสินค้าที่ชอบ (Wishlist)
    [HttpGet]
    public IActionResult Wishlist()
    {
        var loginEmail = TempData.Peek("LoginEmail") as string;
        if (string.IsNullOrEmpty(loginEmail))
        {
            return RedirectToAction("Login", "Account");
        }

        var u = (from usr in _db.Users where usr.Email == loginEmail select usr).FirstOrDefault();
        if (u == null)
        {
            return RedirectToAction("Login", "Account");
        }

        int activeUserId = u.UserId;
        var wishlistItems = (from w in _db.Wishlists
                                 .Include(w => w.Product).ThenInclude(p => p.Category)
                             where w.UserId == activeUserId
                             orderby w.AddedAt descending
                             select w).ToList();

        return View(wishlistItems);
    }

    public IActionResult Privacy()
    {
        return View();
    }

}

