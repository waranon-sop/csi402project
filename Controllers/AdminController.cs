using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using waranon_csi402.Models;
using waranon_csi402.Services;
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

        // ==========================================
        // Auth Guard Helper — ตรวจสอบสิทธิ์ Admin ครอบคลุมทุก Action
        // ==========================================
        public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            var name = TempData.Peek("Name");
            var role = TempData.Peek("Role")?.ToString();

            // 1. ถ้ายังไม่ได้เข้าสู่ระบบ ➔ ส่งไปหน้า Login
            if (name == null)
            {
                context.Result = RedirectToAction("Login", "Account");
                return;
            }

            // 2. ถ้าไม่ใช่ Admin ➔ ส่งกลับหน้าร้าน
            if (role != "Admin")
            {
                context.Result = RedirectToAction("Index", "Home");
                return;
            }
        }

        private IActionResult? CheckAdminAuth()
        {
            if (TempData.Peek("Name") == null)
                return RedirectToAction("Login", "Account");
            if (TempData.Peek("Role")?.ToString() != "Admin")
                return RedirectToAction("Index", "Home");
            return null;
        }

        public IActionResult Index()
        {
            // ดึงสถิติภาพรวมสำหรับ Dashboard
            ViewBag.TotalUsers = (from u in _db.Users select u).Count();
            ViewBag.TotalProducts = (from p in _db.Products select p).Count();
            ViewBag.TotalStock = (from v in _db.Productvariants select (int?)v.Stock).Sum() ?? 0;
            ViewBag.TotalVariants = (from v in _db.Productvariants select v).Count();
            ViewBag.TotalCategories = (from c in _db.Categories select c).Count();
            ViewBag.TotalOrders = (from o in _db.Orders select o).Count();

            // ดึงสินค้าล่าสุด 5 รายการ
            var recentProducts = (from p in _db.Products.Include(p => p.Category)
                                  orderby p.ProductId descending
                                  select p).Take(5).ToList();
            ViewBag.RecentProducts = recentProducts;

            // ดึงคำสั่งซื้อล่าสุด 5 รายการ
            var recentOrders = (from o in _db.Orders
                                    .Include(o => o.User)
                                    .Include(o => o.Address)
                                orderby o.OrderedAt descending
                                select o).Take(5).ToList();
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        // ======กนะ====================================
        // ส่วนที่ 1: จัดการผู้ใช้งาน (User Management)
        // ==========================================

        public IActionResult UserList(string? status)
        {
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
                        newUser.Password = PasswordHelper.Hash(Data.Password); // เก็บเป็น Hash
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
                // ตรวจสอบอีเมลซ้ำกับผู้ใช้งานอื่น
                var emailExists = (from u in _db.Users
                                   where u.Email == data.Email && u.UserId != data.UserId
                                   select u).Any();
                if (emailExists)
                {
                    ViewBag.Status = "อีเมลนี้ถูกใช้งานแล้วโดยผู้ใช้อื่นในระบบ";
                    return View(data);
                }

                user.FirstName = data.FirstName;
                user.LastName = data.LastName;
                user.Email = data.Email;
                user.PhoneNumber = data.PhoneNumber;
                user.Role = data.Role;
                // เปลี่ยนรหัสผ่านเฉพาะเมื่อกรอกรหัสใหม่ (เก็บเป็น Hash)
                if (!string.IsNullOrEmpty(data.Password))
                {
                    user.Password = PasswordHelper.Hash(data.Password);
                }
                _db.SaveChanges();
                return RedirectToAction("UserList", new { status = "แก้ไขข้อมูลสำเร็จ" });
            }
            ViewBag.Status = "เกิดข้อผิดพลาดในการแก้ไข";
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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


        // ==========================================
        // ส่วนที่ 2: จัดการหมวดหมู่ (Category CRUD)
        // ==========================================

        public IActionResult CategoryList(string? status)
        {
            // ดึงหมวดหมู่ทั้งหมด พร้อมจำนวนสินค้าในหมวดหมู่นั้น
            var categories = (from c in _db.Categories.Include(c => c.Products)
                              select c).ToList();

            if (status != null)
            {
                ViewBag.Status = status;
            }
            return View(categories);
        }

        [HttpGet]
        public IActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCategory(Category data)
        {
            if (ModelState.IsValid)
            {
                // ตรวจสอบชื่อหมวดหมู่ซ้ำ
                var checkName = (from c in _db.Categories
                                 where c.CategoryName.Trim().ToLower() == data.CategoryName.Trim().ToLower()
                                 select c).FirstOrDefault();

                if (checkName == null)
                {
                    Category newCat = new()
                    {
                        CategoryName = data.CategoryName.Trim(),
                        Description = data.Description
                    };
                    _db.Categories.Add(newCat);
                    _db.SaveChanges();
                    return RedirectToAction("CategoryList", new { status = "เพิ่มหมวดหมู่สำเร็จ" });
                }
                else
                {
                    ViewBag.Status = "ชื่อหมวดหมู่นี้มีอยู่ในระบบแล้ว";
                    return View(data);
                }
            }
            return View(data);
        }

        [HttpGet]
        public IActionResult EditCategory(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("CategoryList");
            }

            var category = (from c in _db.Categories
                            where c.CategoryId == id
                            select c).FirstOrDefault();

            if (category == null)
            {
                return RedirectToAction("CategoryList", new { status = "ไม่พบหมวดหมู่ที่ต้องการแก้ไข" });
            }

            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCategory(Category data)
        {
            if (ModelState.IsValid)
            {
                var category = (from c in _db.Categories
                                where c.CategoryId == data.CategoryId
                                select c).FirstOrDefault();

                if (category != null)
                {
                    // เช็คชื่อซ้ำกับรายการอื่น
                    var checkDup = (from c in _db.Categories
                                    where c.CategoryName.Trim().ToLower() == data.CategoryName.Trim().ToLower()
                                          && c.CategoryId != data.CategoryId
                                    select c).FirstOrDefault();

                    if (checkDup != null)
                    {
                        ViewBag.Status = "ชื่อหมวดหมู่นี้ซ้ำกับรายการอื่น";
                        return View(data);
                    }

                    category.CategoryName = data.CategoryName.Trim();
                    category.Description = data.Description;
                    _db.SaveChanges();
                    return RedirectToAction("CategoryList", new { status = "แก้ไขหมวดหมู่สำเร็จ" });
                }
                else
                {
                    return RedirectToAction("CategoryList", new { status = "ไม่พบหมวดหมู่ในระบบ" });
                }
            }
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategory(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("CategoryList");
            }

            var category = (from c in _db.Categories
                            where c.CategoryId == id
                            select c).FirstOrDefault();

            if (category != null)
            {
                // ตรวจสอบว่ามีสินค้าใช้งานหมวดหมู่นี้อยู่หรือไม่
                var hasProducts = (from p in _db.Products
                                   where p.CategoryId == id
                                   select p).Any();

                if (hasProducts)
                {
                    return RedirectToAction("CategoryList", new { status = "ไม่สามารถลบได้ เนื่องจากมีสินค้าอยู่ในหมวดหมู่นี้" });
                }

                try
                {
                    _db.Categories.Remove(category);
                    _db.SaveChanges();
                    return RedirectToAction("CategoryList", new { status = "ลบหมวดหมู่สำเร็จ" });
                }
                catch
                {
                    return RedirectToAction("CategoryList", new { status = "เกิดข้อผิดพลาดในการลบข้อมูล" });
                }
            }

            return RedirectToAction("CategoryList", new { status = "ไม่พบข้อมูลหมวดหมู่" });
        }


        // ==========================================
        // ส่วนที่ 3: จัดการสินค้า (Product CRUD)
        // ==========================================

        public IActionResult ProductList(int? categoryId, string? status)
        {
            // ดึง Categories เพื่อทำ Dropdown ตัวกรอง
            ViewBag.Categories = (from c in _db.Categories select c).ToList();
            ViewBag.SelectedCategory = categoryId;

            var query = from p in _db.Products.Include(p => p.Category).Include(p => p.Productvariants)
                        select p;

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = from p in query
                        where p.CategoryId == categoryId.Value
                        select p;
            }

            var products = (from p in query
                            orderby p.ProductId descending
                            select p).ToList();

            if (status != null)
            {
                ViewBag.Status = status;
            }

            return View(products);
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            ViewBag.Categories = (from c in _db.Categories select c).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddProduct(Product data, string? defaultColor, string? defaultSize, int? defaultStock)
        {
            ViewBag.Categories = (from c in _db.Categories select c).ToList();

            // ตัดการ validate Navigation property 'Category' ที่ไม่ได้ส่งมาจากฟอร์ม
            ModelState.Remove("Category");

            if (string.IsNullOrWhiteSpace(data.ProductName))
            {
                ViewBag.Status = "กรุณาระบุชื่อสินค้า";
                return View(data);
            }

            data.CreatedAt = DateTime.Now;
            data.IsActive = (data.IsActive == true);

            _db.Products.Add(data);
            _db.SaveChanges();

            // สร้าง SKU เริ่มต้นทันที เพื่อให้สินค้าสามารถเลือกซื้อที่หน้าร้านได้เลย
            var initialColor = !string.IsNullOrWhiteSpace(defaultColor) ? defaultColor.Trim() : "สีพื้นฐาน";
            var initialSize = !string.IsNullOrWhiteSpace(defaultSize) ? defaultSize.Trim() : "Free Size";
            var initialStock = defaultStock ?? 10;

            Productvariant initialSku = new()
            {
                ProductId = data.ProductId,
                Color = initialColor,
                Size = initialSize,
                Stock = initialStock,
                ExtraPrice = 0,
                ImageUrl = data.ImageUrl
            };
            _db.Productvariants.Add(initialSku);
            _db.SaveChanges();

            return RedirectToAction("ProductList", new { status = "เพิ่มสินค้าและสร้าง SKU เริ่มต้นสำเร็จ" });
        }

        [HttpGet]
        public IActionResult EditProduct(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("ProductList");
            }

            var product = (from p in _db.Products
                           where p.ProductId == id
                           select p).FirstOrDefault();

            if (product == null)
            {
                return RedirectToAction("ProductList", new { status = "ไม่พบสินค้าที่ต้องการแก้ไข" });
            }

            ViewBag.Categories = (from c in _db.Categories select c).ToList();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProduct(Product data)
        {
            ViewBag.Categories = (from c in _db.Categories select c).ToList();

            // ตัดการ validate Navigation property 'Category' ที่ไม่ได้ส่งมาจากฟอร์ม
            ModelState.Remove("Category");

            if (string.IsNullOrWhiteSpace(data.ProductName))
            {
                ViewBag.Status = "กรุณาระบุชื่อสินค้า";
                return View(data);
            }

            var product = (from p in _db.Products
                           where p.ProductId == data.ProductId
                           select p).FirstOrDefault();

            if (product != null)
            {
                product.ProductName = data.ProductName.Trim();
                product.CategoryId = data.CategoryId;
                product.BasePrice = data.BasePrice;
                product.Description = data.Description;
                product.ImageUrl = data.ImageUrl;
                // บันทึกสถานะเปิดขาย/ปิดการขายจริง
                product.IsActive = (data.IsActive == true);

                _db.SaveChanges();
                string statusMsg = product.IsActive == true ? "แก้ไขข้อมูลสำเร็จ (เปิดขาย)" : "แก้ไขข้อมูลสำเร็จ (ปิดการขาย - ซ่อนจากหน้าร้านแล้ว)";
                return RedirectToAction("ProductList", new { status = statusMsg });
            }
            else
            {
                return RedirectToAction("ProductList", new { status = "ไม่พบสินค้าในระบบ" });
            }
        }

        // ปุ่มลัด 1 คลิกสำหรับสลับสถานะ เปิดขาย/ปิดการขาย จากหน้าตารางสินค้า
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleProductStatus(int id)
        {
            var product = (from p in _db.Products where p.ProductId == id select p).FirstOrDefault();
            if (product != null)
            {
                bool newStatus = !(product.IsActive == true);
                product.IsActive = newStatus;
                _db.SaveChanges();

                string statusText = newStatus ? "เปิดขายแล้ว (แสดงที่หน้าร้าน)" : "ปิดการขายแล้ว (ซ่อนจากหน้าร้านแล้ว)";
                return RedirectToAction("ProductList", new { status = $"สินค้า '{product.ProductName}' {statusText}" });
            }

            return RedirectToAction("ProductList", new { status = "ไม่พบสินค้าที่ต้องการเปลี่ยนสถานะ" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteProduct(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("ProductList");
            }

            var product = (from p in _db.Products.Include(p => p.Productvariants)
                           where p.ProductId == id
                           select p).FirstOrDefault();

            if (product != null)
            {
                // ตรวจสอบว่าสินค้ามีประวัติใน Orderitems หรือไม่
                var variantIds = (from v in product.Productvariants select v.VariantId).ToList();
                var hasOrders = (from oi in _db.Orderitems
                                 where variantIds.Contains(oi.VariantId)
                                 select oi).Any();

                if (hasOrders)
                {
                    // ถ้ามีออเดอร์แล้ว ไม่ให้ลบ hard delete ให้ soft delete (ปิดการใช้งาน) แทน
                    product.IsActive = false;
                    _db.SaveChanges();
                    return RedirectToAction("ProductList", new { status = "สินค้านี้มีประวัติการสั่งซื้อแล้ว ระบบได้ทำการ 'ปิดการขาย' แทนการลบเพื่อรักษาประวัติข้อมูล" });
                }

                try
                {
                    // ลบ Variants ที่เกี่ยวข้องก่อน แล้วค่อยลบ Product
                    if (product.Productvariants.Any())
                    {
                        _db.Productvariants.RemoveRange(product.Productvariants);
                    }

                    _db.Products.Remove(product);
                    _db.SaveChanges();
                    return RedirectToAction("ProductList", new { status = "ลบสินค้าและ SKU ทั้งหมดสำเร็จ" });
                }
                catch
                {
                    return RedirectToAction("ProductList", new { status = "เกิดข้อผิดพลาดในการลบสินค้า" });
                }
            }

            return RedirectToAction("ProductList", new { status = "ไม่พบข้อมูลสินค้า" });
        }


        // ==========================================
        // ส่วนที่ 4: จัดการตัวเลือกสินค้า (SKU / Product Variants)
        // ==========================================

        public IActionResult ProductVariantList(int? productId, string? status)
        {
            return RedirectToAction("ManageVariants", new { productId = productId, status = status });
        }

        [HttpGet]
        public IActionResult ManageVariants(int? productId, string? status)
        {
            if (productId == null)
            {
                return RedirectToAction("ProductList");
            }

            var product = (from p in _db.Products
                                      .Include(p => p.Category)
                                      .Include(p => p.Productvariants)
                           where p.ProductId == productId
                           select p).FirstOrDefault();

            if (product == null)
            {
                return RedirectToAction("ProductList", new { status = "ไม่พบสินค้า" });
            }

            if (status != null)
            {
                ViewBag.Status = status;
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddVariant(int productId, string color, string size, int stock, decimal extraPrice, string? imageUrl)
        {
            var product = (from p in _db.Products where p.ProductId == productId select p).FirstOrDefault();
            if (product == null)
            {
                return RedirectToAction("ProductList", new { status = "ไม่พบสินค้า" });
            }

            color = color?.Trim() ?? "สีพื้นฐาน";
            size = size?.Trim() ?? "Free Size";

            // ตรวจสอบคู่ สี + ไซส์ ซ้ำ
            var checkExist = (from v in _db.Productvariants
                              where v.ProductId == productId && v.Color.ToLower() == color.ToLower() && v.Size.ToLower() == size.ToLower()
                              select v).FirstOrDefault();

            if (checkExist != null)
            {
                return RedirectToAction("ManageVariants", new { productId = productId, status = $"มี SKU สี '{color}' ไซส์ '{size}' อยู่แล้วในระบบ" });
            }

            Productvariant newVariant = new()
            {
                ProductId = productId,
                Color = color,
                Size = size,
                Stock = stock >= 0 ? stock : 0,
                ExtraPrice = extraPrice >= 0 ? extraPrice : 0,
                ImageUrl = !string.IsNullOrWhiteSpace(imageUrl) ? imageUrl.Trim() : product.ImageUrl
            };

            _db.Productvariants.Add(newVariant);
            _db.SaveChanges();

            return RedirectToAction("ManageVariants", new { productId = productId, status = "เพิ่มตัวเลือกสินค้า (SKU) สำเร็จ" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditVariant(int variantId, int productId, string color, string size, int stock, decimal extraPrice, string? imageUrl)
        {
            var variant = (from v in _db.Productvariants
                           where v.VariantId == variantId && v.ProductId == productId
                           select v).FirstOrDefault();

            if (variant != null)
            {
                color = color?.Trim() ?? variant.Color;
                size = size?.Trim() ?? variant.Size;

                // ตรวจสอบคู่ สี + ไซส์ ซ้ำกับ variant อื่น
                var checkDup = (from v in _db.Productvariants
                                where v.ProductId == productId 
                                      && v.VariantId != variantId 
                                      && v.Color.ToLower() == color.ToLower() 
                                      && v.Size.ToLower() == size.ToLower()
                                select v).FirstOrDefault();

                if (checkDup != null)
                {
                    return RedirectToAction("ManageVariants", new { productId = productId, status = $"ไม่สามารถเปลี่ยนเป็น สี '{color}' ไซส์ '{size}' ได้เนื่องจากซ้ำกับ SKU อื่น" });
                }

                variant.Color = color;
                variant.Size = size;
                variant.Stock = stock >= 0 ? stock : 0;
                variant.ExtraPrice = extraPrice >= 0 ? extraPrice : 0;
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    variant.ImageUrl = imageUrl.Trim();
                }

                _db.SaveChanges();
                return RedirectToAction("ManageVariants", new { productId = productId, status = "อัปเดตข้อมูล SKU สำเร็จ" });
            }

            return RedirectToAction("ManageVariants", new { productId = productId, status = "ไม่พบตัวเลือกสินค้าที่ต้องการแก้ไข" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteVariant(int variantId, int productId)
        {
            var variant = (from v in _db.Productvariants
                           where v.VariantId == variantId && v.ProductId == productId
                           select v).FirstOrDefault();

            if (variant != null)
            {
                // ตรวจสอบว่า variant มีคำสั่งซื้ออ้างอิงอยู่หรือไม่
                var hasOrders = (from oi in _db.Orderitems
                                 where oi.VariantId == variantId
                                 select oi).Any();

                if (hasOrders)
                {
                    return RedirectToAction("ManageVariants", new { productId = productId, status = "ไม่สามารถลบ SKU นี้ได้เนื่องจากมีคำสั่งซื้อที่อ้างอิงอยู่ (แนะนำให้ปรับสต็อกเป็น 0 แทน)" });
                }

                try
                {
                    _db.Productvariants.Remove(variant);
                    _db.SaveChanges();
                    return RedirectToAction("ManageVariants", new { productId = productId, status = "ลบ SKU สำเร็จ" });
                }
                catch
                {
                    return RedirectToAction("ManageVariants", new { productId = productId, status = "เกิดข้อผิดพลาดในการลบ SKU" });
                }
            }

            return RedirectToAction("ManageVariants", new { productId = productId, status = "ไม่พบตัวเลือกสินค้า" });
        }


        // ==========================================
        // ส่วนที่ 5: จัดการโปรโมชั่นและคูปอง (Promotions & Coupons)
        // ==========================================

        public IActionResult PromotionList(string? status, string? tab)
        {
            var promotions = (from p in _db.Promotions.Include(p => p.Category)
                              orderby p.PromotionId
                              select p).ToList();

            var coupons = (from c in _db.Coupons
                           orderby c.CouponId
                           select c).ToList();

            ViewBag.Coupons = coupons;
            ViewBag.Categories = (from c in _db.Categories select c).ToList();
            ViewBag.ActiveTab = tab ?? "promotions";

            if (status != null)
            {
                ViewBag.Status = status;
            }

            return View(promotions);
        }

        // --- ส่วนโปรโมชั่นอัตโนมัติ (Promotions) ---

        // ส่งข้อมูลสำหรับ dropdown หมวดหมู่ และรายการสินค้าโปรจับคู่ (P4) ให้ View
        private void LoadPromotionFormData(List<int> selectedProductIds)
        {
            ViewBag.Categories = (from c in _db.Categories select c).ToList();
            ViewBag.Products = (from p in _db.Products orderby p.ProductName select p).ToList();
            ViewBag.SelectedProductIds = selectedProductIds;
        }

        // บันทึกรายการสินค้าโปรจับคู่ (P4): ลบของเดิมทั้งหมด แล้วใส่ตามที่เลือกใหม่
        private void SavePairProducts(int promotionId, int[]? productIds)
        {
            var oldItems = (from pp in _db.PromotionProducts
                            where pp.PromotionId == promotionId
                            select pp).ToList();
            _db.PromotionProducts.RemoveRange(oldItems);

            if (productIds != null)
            {
                foreach (int productId in productIds.Distinct())
                {
                    _db.PromotionProducts.Add(new PromotionProduct { PromotionId = promotionId, ProductId = productId });
                }
            }

            _db.SaveChanges();
        }

        [HttpGet]
        public IActionResult AddPromotion()
        {
            LoadPromotionFormData(new List<int>());
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddPromotion(Promotion data, int[]? pairProductIds)
        {
            LoadPromotionFormData((pairProductIds ?? new int[0]).ToList());
            ModelState.Remove("Category");

            if (string.IsNullOrWhiteSpace(data.PromotionName))
            {
                ViewBag.Status = "กรุณาระบุชื่อโปรโมชั่น";
                return View(data);
            }

            data.PromotionName = data.PromotionName.Trim();
            // MySQL ENUM('Percent','Fixed')
            data.DiscountType = (!string.IsNullOrEmpty(data.DiscountType) && data.DiscountType.Equals("Fixed", StringComparison.OrdinalIgnoreCase)) 
                ? "Fixed" 
                : "Percent";
            data.IsActive = (data.IsActive == true);

            _db.Promotions.Add(data);
            _db.SaveChanges(); // เพื่อให้ได้ PromotionId ก่อนบันทึกสินค้าโปรจับคู่

            SavePairProducts(data.PromotionId, pairProductIds);

            return RedirectToAction("PromotionList", new { tab = "promotions", status = "เพิ่มโปรโมชั่นใหม่สำเร็จ" });
        }

        [HttpGet]
        public IActionResult EditPromotion(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("PromotionList");
            }

            var promo = (from p in _db.Promotions where p.PromotionId == id select p).FirstOrDefault();
            if (promo == null)
            {
                return RedirectToAction("PromotionList", new { status = "ไม่พบโปรโมชั่นที่ต้องการแก้ไข" });
            }

            var selectedIds = (from pp in _db.PromotionProducts
                               where pp.PromotionId == promo.PromotionId
                               select pp.ProductId).ToList();
            LoadPromotionFormData(selectedIds);
            return View(promo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditPromotion(Promotion data, int[]? pairProductIds)
        {
            LoadPromotionFormData((pairProductIds ?? new int[0]).ToList());
            ModelState.Remove("Category");

            if (string.IsNullOrWhiteSpace(data.PromotionName))
            {
                ViewBag.Status = "กรุณาระบุชื่อโปรโมชั่น";
                return View(data);
            }

            var promo = (from p in _db.Promotions where p.PromotionId == data.PromotionId select p).FirstOrDefault();
            if (promo != null)
            {
                promo.PromotionName = data.PromotionName.Trim();
                promo.DiscountType = (!string.IsNullOrEmpty(data.DiscountType) && data.DiscountType.Equals("Fixed", StringComparison.OrdinalIgnoreCase)) 
                    ? "Fixed" 
                    : "Percent";
                promo.DiscountValue = data.DiscountValue;
                promo.MinOrderAmt = data.MinOrderAmt;
                promo.CategoryId = data.CategoryId;
                promo.StartDate = data.StartDate;
                promo.EndDate = data.EndDate;
                promo.IsActive = (data.IsActive == true);

                _db.SaveChanges();
                SavePairProducts(promo.PromotionId, pairProductIds);

                return RedirectToAction("PromotionList", new { tab = "promotions", status = "แก้ไขข้อมูลโปรโมชั่นสำเร็จ" });
            }

            return RedirectToAction("PromotionList", new { tab = "promotions", status = "ไม่พบโปรโมชั่นในระบบ" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TogglePromotionStatus(int id)
        {
            var promo = (from p in _db.Promotions where p.PromotionId == id select p).FirstOrDefault();
            if (promo != null)
            {
                bool newStatus = !(promo.IsActive == true);
                promo.IsActive = newStatus;
                _db.SaveChanges();

                string statusText = newStatus ? "เปิดใช้งานแล้ว" : "ปิดใช้งานแล้ว";
                return RedirectToAction("PromotionList", new { tab = "promotions", status = $"โปรโมชั่น '{promo.PromotionName}' {statusText}" });
            }

            return RedirectToAction("PromotionList", new { tab = "promotions", status = "ไม่พบโปรโมชั่น" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePromotion(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("PromotionList", new { tab = "promotions" });
            }

            var promo = (from p in _db.Promotions where p.PromotionId == id select p).FirstOrDefault();
            if (promo != null)
            {
                try
                {
                    _db.Promotions.Remove(promo);
                    _db.SaveChanges();
                    return RedirectToAction("PromotionList", new { tab = "promotions", status = "ลบโปรโมชั่นสำเร็จ" });
                }
                catch
                {
                    return RedirectToAction("PromotionList", new { tab = "promotions", status = "เกิดข้อผิดพลาดในการลบโปรโมชั่น (อาจมีออเดอร์อ้างอิงอยู่ แนะนำให้ปิดใช้งานแทน)" });
                }
            }

            return RedirectToAction("PromotionList", new { tab = "promotions", status = "ไม่พบโปรโมชั่น" });
        }

        // --- ส่วนโค้ดส่วนลด (Coupons) ---

        [HttpGet]
        public IActionResult AddCoupon()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCoupon(Coupon data)
        {
            if (string.IsNullOrWhiteSpace(data.CouponCode))
            {
                ViewBag.Status = "กรุณาระบุรหัสคูปอง";
                return View(data);
            }

            var code = data.CouponCode.Trim().ToUpper();

            // เช็คโค้ดซ้ำ
            var checkDup = (from c in _db.Coupons where c.CouponCode == code select c).FirstOrDefault();
            if (checkDup != null)
            {
                ViewBag.Status = $"รหัสคูปอง '{code}' มีอยู่ในระบบแล้ว";
                return View(data);
            }

            data.CouponCode = code;
            // MySQL ENUM('Percent','Fixed')
            data.DiscountType = (!string.IsNullOrEmpty(data.DiscountType) && data.DiscountType.Equals("Fixed", StringComparison.OrdinalIgnoreCase)) 
                ? "Fixed" 
                : "Percent";
            data.UsedCount = 0;
            data.IsActive = (data.IsActive == true);

            _db.Coupons.Add(data);
            _db.SaveChanges();

            return RedirectToAction("PromotionList", new { tab = "coupons", status = $"เพิ่มคูปองส่วนลด '{code}' สำเร็จ" });
        }

        [HttpGet]
        public IActionResult EditCoupon(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("PromotionList", new { tab = "coupons" });
            }

            var coupon = (from c in _db.Coupons where c.CouponId == id select c).FirstOrDefault();
            if (coupon == null)
            {
                return RedirectToAction("PromotionList", new { tab = "coupons", status = "ไม่พบคูปองที่ต้องการแก้ไข" });
            }

            return View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCoupon(Coupon data)
        {
            if (string.IsNullOrWhiteSpace(data.CouponCode))
            {
                ViewBag.Status = "กรุณาระบุรหัสคูปอง";
                return View(data);
            }

            var code = data.CouponCode.Trim().ToUpper();

            var coupon = (from c in _db.Coupons where c.CouponId == data.CouponId select c).FirstOrDefault();
            if (coupon != null)
            {
                // ตรวจสอบชื่อซ้ำกับคูปองอื่น
                var checkDup = (from c in _db.Coupons 
                                where c.CouponCode == code && c.CouponId != data.CouponId 
                                select c).FirstOrDefault();

                if (checkDup != null)
                {
                    ViewBag.Status = $"รหัสคูปอง '{code}' ซ้ำกับคูปองอื่น";
                    return View(data);
                }

                coupon.CouponCode = code;
                coupon.DiscountType = (!string.IsNullOrEmpty(data.DiscountType) && data.DiscountType.Equals("Fixed", StringComparison.OrdinalIgnoreCase)) 
                    ? "Fixed" 
                    : "Percent";
                coupon.DiscountValue = data.DiscountValue;
                coupon.MinOrderAmt = data.MinOrderAmt;
                coupon.UsageLimit = data.UsageLimit;
                coupon.ExpiresAt = data.ExpiresAt;
                coupon.IsActive = (data.IsActive == true);

                _db.SaveChanges();
                return RedirectToAction("PromotionList", new { tab = "coupons", status = "แก้ไขข้อมูลคูปองสำเร็จ" });
            }

            return RedirectToAction("PromotionList", new { tab = "coupons", status = "ไม่พบคูปองในระบบ" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleCouponStatus(int id)
        {
            var coupon = (from c in _db.Coupons where c.CouponId == id select c).FirstOrDefault();
            if (coupon != null)
            {
                bool newStatus = !(coupon.IsActive == true);
                coupon.IsActive = newStatus;
                _db.SaveChanges();

                string statusText = newStatus ? "เปิดใช้งานแล้ว" : "ปิดใช้งานแล้ว";
                return RedirectToAction("PromotionList", new { tab = "coupons", status = $"คูปอง '{coupon.CouponCode}' {statusText}" });
            }

            return RedirectToAction("PromotionList", new { tab = "coupons", status = "ไม่พบคูปอง" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCoupon(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("PromotionList", new { tab = "coupons" });
            }

            var coupon = (from c in _db.Coupons where c.CouponId == id select c).FirstOrDefault();
            if (coupon != null)
            {
                try
                {
                    _db.Coupons.Remove(coupon);
                    _db.SaveChanges();
                    return RedirectToAction("PromotionList", new { tab = "coupons", status = "ลบคูปองสำเร็จ" });
                }
                catch
                {
                    return RedirectToAction("PromotionList", new { tab = "coupons", status = "เกิดข้อผิดพลาดในการลบคูปอง (อาจมีออเดอร์อ้างอิงอยู่ แนะนำให้ปิดใช้งานแทน)" });
                }
            }

            return RedirectToAction("PromotionList", new { tab = "coupons", status = "ไม่พบคูปอง" });
        }


        // ==========================================
        // ส่วนจัดการคำสั่งซื้อ (Orders)
        // ==========================================

        // แสดงรายการคำสั่งซื้อทั้งหมด
        public IActionResult OrderList(string? status, string? search)
        {
            // ดึงข้อมูลคำสั่งซื้อ พร้อมข้อมูลลูกค้าและที่อยู่
            var orders = (from o in _db.Orders
                              .Include(o => o.User)
                              .Include(o => o.Address)
                              .Include(o => o.Orderitems)
                          orderby o.OrderedAt descending
                          select o).ToList();

            // กรองตามสถานะที่เลือก
            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                orders = (from o in orders
                          where o.Status == status
                          select o).ToList();
            }

            // ค้นหาตามเลขออเดอร์ หรือชื่อลูกค้า หรืออีเมล
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                orders = (from o in orders
                          where o.OrderId.ToString().Contains(search)
                                || (o.User != null && (o.User.FirstName.ToLower().Contains(search) || o.User.LastName.ToLower().Contains(search) || o.User.Email.ToLower().Contains(search)))
                                || (o.Address != null && o.Address.ReceiverName.ToLower().Contains(search))
                          select o).ToList();
            }

            ViewBag.SelectedStatus = status ?? "All";
            ViewBag.Search = search;

            // นับจำนวนออเดอร์แต่ละสถานะสำหรับแสดงบนปุ่ม
            ViewBag.CountAll = (from o in _db.Orders select o).Count();
            ViewBag.CountPending = (from o in _db.Orders where o.Status == "Pending Payment" select o).Count();
            ViewBag.CountVerifying = (from o in _db.Orders where o.Status == "Verifying Slip" select o).Count();
            ViewBag.CountPaid = (from o in _db.Orders where o.Status == "Paid" select o).Count();
            ViewBag.CountProcessing = (from o in _db.Orders where o.Status == "Processing" select o).Count();
            ViewBag.CountShipped = (from o in _db.Orders where o.Status == "Shipped" select o).Count();
            ViewBag.CountCompleted = (from o in _db.Orders where o.Status == "Completed" select o).Count();
            ViewBag.CountCancelled = (from o in _db.Orders where o.Status == "Cancelled" select o).Count();

            return View(orders);
        }

        // ดูรายละเอียดคำสั่งซื้อ
        public IActionResult AdminOrderDetail(int? id, string? status)
        {
            if (id == null)
            {
                return RedirectToAction("OrderList");
            }

            // ดึงข้อมูลออเดอร์ตาม id
            var order = (from o in _db.Orders
                             .Include(o => o.User)
                             .Include(o => o.Address)
                             .Include(o => o.Promotion)
                             .Include(o => o.Coupon)
                             .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                         where o.OrderId == id
                         select o).FirstOrDefault();

            if (order == null)
            {
                return RedirectToAction("OrderList", new { status = "ไม่พบคำสั่งซื้อนี้" });
            }

            if (status != null)
            {
                ViewBag.Status = status;
            }

            // ส่งรายการสถานะที่เปลี่ยนไปได้ ให้ View แสดงเฉพาะตัวเลือกที่ถูกต้อง
            ViewBag.AllowedStatuses = GetAllowedNextStatuses(order.Status);

            return View(order);
        }

        // ==========================================
        // กฎการเปลี่ยนสถานะออเดอร์ (State Transition) ตาม agent.md
        // คืนค่ารายการสถานะถัดไปที่อนุญาต ถ้าเป็นลิสต์ว่าง = ออเดอร์สิ้นสุดแล้ว
        // ==========================================
        private static string[] GetAllowedNextStatuses(string currentStatus)
        {
            switch (currentStatus)
            {
                case "Pending Payment":
                    return new[] { "Verifying Slip", "Cancelled" };
                case "Verifying Slip":
                    return new[] { "Paid", "Invalid Slip" };
                case "Invalid Slip":
                    return new[] { "Verifying Slip", "Cancelled" };
                case "Paid":
                    return new[] { "Processing", "Refunded" };
                case "Processing":
                    return new[] { "Shipped", "Refunded" };
                case "Shipped":
                    return new[] { "Completed", "Refunded" };
                case "Completed":
                    return new[] { "Refunded" };
                default:
                    // Cancelled, Refunded หรือสถานะที่ไม่รู้จัก ➔ เปลี่ยนต่อไม่ได้
                    return new string[0];
            }
        }

        // อัปเดตสถานะคำสั่งซื้อ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderStatus(int orderId, string newStatus, string? trackingNumber)
        {
            var order = (from o in _db.Orders
                             .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant)
                         where o.OrderId == orderId
                         select o).FirstOrDefault();

            if (order != null)
            {
                string oldStatus = order.Status;
                string[] allowedStatuses = GetAllowedNextStatuses(oldStatus);

                // ถ้าออเดอร์จบไปแล้ว จะไม่ให้แก้ไข
                if (allowedStatuses.Length == 0)
                {
                    return RedirectToAction("AdminOrderDetail", new { id = orderId, status = "ออเดอร์นี้สิ้นสุดแล้ว ไม่สามารถแก้ไขได้" });
                }

                // ต้องเปลี่ยนไปยังสถานะที่อยู่ในตาราง State Transition เท่านั้น
                if (!allowedStatuses.Contains(newStatus))
                {
                    return RedirectToAction("AdminOrderDetail", new { id = orderId, status = $"ไม่สามารถเปลี่ยนสถานะจาก '{oldStatus}' เป็น '{newStatus}' ได้" });
                }

                // ถ้าเปลี่ยนเป็น จัดส่งแล้ว ต้องใส่เลขพัสดุ
                if (newStatus == "Shipped" && string.IsNullOrEmpty(trackingNumber) && string.IsNullOrEmpty(order.TrackingNumber))
                {
                    return RedirectToAction("AdminOrderDetail", new { id = orderId, status = "กรุณากรอกเลขพัสดุก่อนเปลี่ยนสถานะเป็นจัดส่งแล้ว" });
                }

                // อัปเดตสถานะและเลขพัสดุ
                order.Status = newStatus;
                if (!string.IsNullOrEmpty(trackingNumber))
                {
                    order.TrackingNumber = trackingNumber.Trim();
                }
                order.UpdatedAt = DateTime.Now;

                // ถ้ากดยกเลิกออเดอร์ ให้คืนสต็อกสินค้า
                if (newStatus == "Cancelled" && oldStatus != "Cancelled")
                {
                    foreach (var item in order.Orderitems)
                    {
                        if (item.Variant != null)
                        {
                            item.Variant.Stock = item.Variant.Stock + item.Quantity;
                        }
                    }
                }

                // แจ้งลูกค้าว่าสถานะเปลี่ยน
                NotificationHelper.Notify(_db, order.UserId, "สถานะคำสั่งซื้ออัปเดต",
                    $"คำสั่งซื้อ #{orderId} เปลี่ยนสถานะเป็น '{newStatus}' โดยผู้ดูแลระบบ");

                _db.SaveChanges();
                return RedirectToAction("AdminOrderDetail", new { id = orderId, status = "อัปเดตสถานะเรียบร้อยแล้ว" });
            }

            return RedirectToAction("OrderList", new { status = "ไม่พบคำสั่งซื้อ" });
        }

        // --- หน้ารายงานยอดขายและสถิติภาพรวม (Sales Report) ---
        public IActionResult SalesReport()
        {
            // ดึงออเดอร์ทั้งหมด
            var allOrders = (from o in _db.Orders
                                 .Include(o => o.User)
                                 .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                             select o).ToList();

            // ออเดอร์ที่ชำระเงินแล้ว ถือเป็นยอดขายจริง (Paid, Processing, Shipped, Completed)
            var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Completed" };
            var paidOrders = (from o in allOrders
                              where paidStatuses.Contains(o.Status)
                              select o).ToList();

            // ยอดขายรวมสุทธิ
            decimal totalRevenue = (from o in paidOrders select o.TotalAmount).Sum();
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.PaidOrdersCount = paidOrders.Count;

            // ยอดขายวันนี้
            var today = DateTime.Today;
            decimal todayRevenue = (from o in paidOrders
                                    where o.OrderedAt.Date == today
                                    select o.TotalAmount).Sum();
            ViewBag.TodayRevenue = todayRevenue;

            // ยอดขายเดือนนี้
            decimal monthRevenue = (from o in paidOrders
                                    where o.OrderedAt.Month == today.Month && o.OrderedAt.Year == today.Year
                                    select o.TotalAmount).Sum();
            ViewBag.MonthRevenue = monthRevenue;

            // ค่าเฉลี่ยต่อออเดอร์ (AOV)
            decimal avgOrderValue = paidOrders.Any() ? totalRevenue / paidOrders.Count : 0;
            ViewBag.AvgOrderValue = avgOrderValue;

            // ยอดรวมส่วนลดที่ลูกค้าได้รับทั้งหมด
            decimal totalDiscounts = (from o in paidOrders select o.DiscountAmt).Sum();
            ViewBag.TotalDiscounts = totalDiscounts;

            // สินค้าขายดี 5 อันดับแรก
            var topProducts = (from o in paidOrders
                               from oi in o.Orderitems
                               where oi.Variant != null && oi.Variant.Product != null
                               group oi by new { oi.Variant.ProductId, oi.Variant.Product.ProductName, oi.Variant.Product.ImageUrl } into g
                               orderby g.Sum(x => x.Quantity) descending
                               select new TopProductViewModel
                               {
                                   ProductId = g.Key.ProductId,
                                   ProductName = g.Key.ProductName,
                                   ImageUrl = g.Key.ImageUrl,
                                   TotalQuantity = g.Sum(x => x.Quantity),
                                   TotalSales = g.Sum(x => x.Quantity * x.UnitPrice)
                               }).Take(5).ToList();

            ViewBag.TopProducts = topProducts;

            // รายการคำสั่งซื้อล่าสุด 10 รายการ
            var recentOrders = (from o in allOrders
                                orderby o.OrderedAt descending
                                select o).Take(10).ToList();
            ViewBag.RecentOrders = recentOrders;

            return View();
        }

        // ==========================================
        // ส่วนที่ 7: จัดการแบนเนอร์หน้าร้าน (Banners)
        // ==========================================

        // หน้ารายการแบนเนอร์ + ฟอร์มเพิ่มแบนเนอร์ใหม่
        [HttpGet]
        public IActionResult BannerList(string? status)
        {
            ViewBag.Status = status;

            var banners = (from b in _db.Banners
                           orderby b.SortOrder, b.BannerId descending
                           select b).ToList();
            return View(banners);
        }

        // เพิ่มแบนเนอร์ (อัปโหลดรูป หรือใส่ URL รูป)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddBanner(string title, string? subtitle, string? linkUrl, int sortOrder, string? imageUrl, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return RedirectToAction("BannerList", new { status = "กรุณาระบุหัวข้อแบนเนอร์" });
            }

            // ลิงก์ต้องเป็นหน้าในเว็บ (/...) หรือ http(s) เท่านั้น กันลิงก์แบบ javascript:
            string? safeLink = linkUrl?.Trim();
            if (!string.IsNullOrEmpty(safeLink) && !safeLink.StartsWith("/") && !safeLink.StartsWith("http://") && !safeLink.StartsWith("https://"))
            {
                return RedirectToAction("BannerList", new { status = "ลิงก์ต้องขึ้นต้นด้วย / หรือ http(s)://" });
            }

            string finalImageUrl = imageUrl?.Trim() ?? "";

            // ถ้าอัปโหลดไฟล์รูปมา ให้ใช้ไฟล์แทน URL
            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                string ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExt.Contains(ext) || !imageFile.ContentType.StartsWith("image/"))
                {
                    return RedirectToAction("BannerList", new { status = "กรุณาอัปโหลดไฟล์รูปภาพ (.jpg, .png, .webp) เท่านั้น" });
                }
                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    return RedirectToAction("BannerList", new { status = "ขนาดไฟล์รูปต้องไม่เกิน 5 MB" });
                }

                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "banners");
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = $"banner_{Guid.NewGuid()}{ext}";
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }
                finalImageUrl = $"/images/banners/{fileName}";
            }

            if (string.IsNullOrEmpty(finalImageUrl))
            {
                return RedirectToAction("BannerList", new { status = "กรุณาอัปโหลดรูปหรือใส่ URL รูปแบนเนอร์" });
            }

            var banner = new Banner
            {
                Title = title.Trim(),
                Subtitle = subtitle?.Trim(),
                LinkUrl = safeLink,
                ImageUrl = finalImageUrl,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            _db.Banners.Add(banner);
            _db.SaveChanges();

            return RedirectToAction("BannerList", new { status = "เพิ่มแบนเนอร์สำเร็จ" });
        }

        // เปิด/ปิดการแสดงแบนเนอร์
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleBanner(int id)
        {
            var banner = (from b in _db.Banners where b.BannerId == id select b).FirstOrDefault();
            if (banner == null)
            {
                return RedirectToAction("BannerList", new { status = "ไม่พบแบนเนอร์" });
            }

            banner.IsActive = !banner.IsActive;
            _db.SaveChanges();

            return RedirectToAction("BannerList", new { status = banner.IsActive ? "เปิดแสดงแบนเนอร์แล้ว" : "ซ่อนแบนเนอร์แล้ว" });
        }

        // ลบแบนเนอร์
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteBanner(int id)
        {
            var banner = (from b in _db.Banners where b.BannerId == id select b).FirstOrDefault();
            if (banner == null)
            {
                return RedirectToAction("BannerList", new { status = "ไม่พบแบนเนอร์" });
            }

            _db.Banners.Remove(banner);
            _db.SaveChanges();

            return RedirectToAction("BannerList", new { status = "ลบแบนเนอร์สำเร็จ" });
        }
    }
}
