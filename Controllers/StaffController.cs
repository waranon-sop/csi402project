using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using waranon_csi402.Models;
using waranon_csi402.Services;

namespace waranon_csi402.Controllers
{
    public class StaffController : Controller
    {
        private readonly Csi402dbContext _db;

        public StaffController(Csi402dbContext db)
        {
            _db = db;
        }

        // ==========================================
        // Auth Guard Helper — ตรวจสอบสิทธิ์ Staff ครอบคลุมทุก Action
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

            // 2. ถ้าไม่ใช่ Staff หรือ Admin ➔ ส่งกลับหน้าร้าน
            if (role != "Staff" && role != "Admin")
            {
                context.Result = RedirectToAction("Index", "Home");
                return;
            }
        }

        private IActionResult? CheckStaffAuth()
        {
            if (TempData.Peek("Name") == null)
                return RedirectToAction("Login", "Account");
            if (TempData.Peek("Role")?.ToString() != "Staff" && TempData.Peek("Role")?.ToString() != "Admin")
                return RedirectToAction("Index", "Home");
            return null;
        }

        // 1. แดชบอร์ดและรายการคำสั่งซื้อของพนักงาน (Staff Order List)
        // tab: "paid" (รอเตรียมจัดส่ง), "processing" (กำลังแพ็ค), "shipped" (จัดส่งแล้ว), "all" (ทั้งหมดของ Staff)
        public IActionResult Index(string? tab, string? search, string? status)
        {
            ViewBag.Status = status;
            ViewBag.CurrentTab = string.IsNullOrEmpty(tab) ? "paid" : tab.ToLower();
            ViewBag.Search = search;

            // สถิติตัวเลขสำหรับ Badge & Stat Cards
            ViewBag.CountPaid = (from o in _db.Orders where o.Status == "Paid" select o).Count();
            ViewBag.CountProcessing = (from o in _db.Orders where o.Status == "Processing" select o).Count();
            ViewBag.CountShipped = (from o in _db.Orders where o.Status == "Shipped" || o.Status == "Completed" select o).Count();
            ViewBag.CountAllStaff = (from o in _db.Orders
                                     where o.Status == "Paid" || o.Status == "Processing" || o.Status == "Shipped" || o.Status == "Completed"
                                     select o).Count();

            // จำนวน SKU ที่สต็อกใกล้หมด (<= 10)
            ViewBag.CountLowStock = (from v in _db.Productvariants where v.Stock <= 10 select v).Count();

            // Query ข้อมูลออเดอร์ด้วย LINQ Query Syntax
            var query = from o in _db.Orders
                            .Include(o => o.User)
                            .Include(o => o.Address)
                            .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                        select o;

            // กรองสถานะตามแท็บ
            switch (ViewBag.CurrentTab)
            {
                case "paid":
                    query = from o in query where o.Status == "Paid" select o;
                    break;
                case "processing":
                    query = from o in query where o.Status == "Processing" select o;
                    break;
                case "shipped":
                    query = from o in query where o.Status == "Shipped" || o.Status == "Completed" select o;
                    break;
                case "all":
                default:
                    query = from o in query
                            where o.Status == "Paid" || o.Status == "Processing" || o.Status == "Shipped" || o.Status == "Completed"
                            select o;
                    break;
            }

            // ค้นหา
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = from o in query
                        where o.OrderId.ToString().Contains(s)
                              || (o.Address != null && o.Address.ReceiverName.Contains(s))
                              || (o.Address != null && o.Address.Phone.Contains(s))
                              || (o.TrackingNumber != null && o.TrackingNumber.Contains(s))
                              || (o.User != null && (o.User.FirstName.Contains(s) || o.User.LastName.Contains(s)))
                        select o;
            }

            var orders = (from o in query orderby o.OrderedAt descending select o).ToList();
            return View(orders);
        }

        // 2. รายละเอียดคำสั่งซื้อ & รายการสินค้าสำหรับหยิบแพ็ค (Order Detail / Packing Slip)
        public IActionResult OrderDetail(int? id, string? status)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var order = (from o in _db.Orders
                            .Include(o => o.User)
                            .Include(o => o.Address)
                            .Include(o => o.Coupon)
                            .Include(o => o.Promotion)
                            .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                         where o.OrderId == id
                         select o).FirstOrDefault();

            if (order == null)
            {
                return RedirectToAction("Index", new { status = "ไม่พบคำสั่งซื้อที่ระบุ" });
            }

            ViewBag.Status = status;
            return View(order);
        }

        // 3. เริ่มจัดเตรียมสินค้า / แพ็คพัสดุ (Pack Order: Paid -> Processing)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PackOrder(int orderId)
        {
            var order = (from o in _db.Orders where o.OrderId == orderId select o).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index", new { status = "ไม่พบคำสั่งซื้อ" });
            }

            if (order.Status == "Paid")
            {
                order.Status = "Processing";
                order.UpdatedAt = DateTime.Now;

                NotificationHelper.Notify(_db, order.UserId, "กำลังเตรียมจัดส่ง",
                    $"คำสั่งซื้อ #{orderId} อยู่ระหว่างการแพ็คสินค้า");

                _db.SaveChanges();
                return RedirectToAction("OrderDetail", new { id = orderId, status = $"รับออเดอร์ #{orderId} เข้าสู่กระบวนการเตรียมจัดส่ง (Processing) เรียบร้อยแล้ว" });
            }

            return RedirectToAction("OrderDetail", new { id = orderId, status = $"ออเดอร์ #{orderId} อยู่ในสถานะ '{order.Status}' อยู่แล้ว" });
        }

        // 4. บันทึกการจัดส่งสินค้า & กรอก Tracking Number (Ship Order: Processing -> Shipped)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder(int orderId, string trackingNumber, string? courier)
        {
            var order = (from o in _db.Orders where o.OrderId == orderId select o).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index", new { status = "ไม่พบคำสั่งซื้อ" });
            }

            // จัดส่งได้เฉพาะออเดอร์ที่แพ็คแล้ว (Processing) เท่านั้น — ต้องกด "เริ่มแพ็ค" ก่อน
            if (order.Status != "Processing")
            {
                return RedirectToAction("OrderDetail", new { id = orderId, status = $"ไม่สามารถจัดส่งได้เนื่องจากออเดอร์อยู่ในสถานะ '{order.Status}'" });
            }

            if (string.IsNullOrWhiteSpace(trackingNumber))
            {
                return RedirectToAction("OrderDetail", new { id = orderId, status = "กรุณาระบุหมายเลขพัสดุ (Tracking Number) ก่อนยืนยันการจัดส่ง" });
            }

            string fullTracking = string.IsNullOrWhiteSpace(courier)
                ? trackingNumber.Trim()
                : $"{courier.Trim()}: {trackingNumber.Trim()}";

            order.Status = "Shipped";
            order.TrackingNumber = fullTracking;
            order.UpdatedAt = DateTime.Now; // ระบบอัตโนมัติใช้เวลานี้นับ 7 วันก่อนปิดออเดอร์

            NotificationHelper.Notify(_db, order.UserId, "จัดส่งสินค้าแล้ว",
                $"คำสั่งซื้อ #{orderId} จัดส่งแล้ว เลขพัสดุ: {fullTracking}");

            _db.SaveChanges();

            return RedirectToAction("OrderDetail", new { id = orderId, status = $"บันทึกการจัดส่งออเดอร์ #{orderId} สำเร็จแล้ว (เลขพัสดุ: {fullTracking})" });
        }

        // 5. พิมพ์ใบปะหน้าและใบจัดเตรียมสินค้า (Print Packing Slip / Shipping Label)
        public IActionResult PrintSlip(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            var order = (from o in _db.Orders
                            .Include(o => o.User)
                            .Include(o => o.Address)
                            .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                         where o.OrderId == id
                         select o).FirstOrDefault();

            if (order == null)
            {
                return RedirectToAction("Index", new { status = "ไม่พบคำสั่งซื้อ" });
            }

            return View(order);
        }

        // 6. แจ้งเตือนสินค้าใกล้หมดสต็อก & ตรวจสอบคลังสินค้า (Stock Alert & Inventory)
        // filter: "low" (สต็อก <= 10), "out" (หมดสต็อก = 0), "normal" (ปกติ > 10), "all" (SKU ทั้งหมด)
        public IActionResult StockAlert(string? filter, string? search, string? status)
        {
            ViewBag.Status = status;
            ViewBag.CurrentFilter = string.IsNullOrEmpty(filter) ? "low" : filter.ToLower();
            ViewBag.Search = search;

            // สถิติคลังสินค้า
            ViewBag.CountOutOfStock = (from v in _db.Productvariants where v.Stock == 0 select v).Count();
            ViewBag.CountLowStock = (from v in _db.Productvariants where v.Stock > 0 && v.Stock <= 10 select v).Count();
            ViewBag.CountNormalStock = (from v in _db.Productvariants where v.Stock > 10 select v).Count();
            ViewBag.CountTotalVariants = (from v in _db.Productvariants select v).Count();
            ViewBag.TotalInventoryUnits = (from v in _db.Productvariants select (int?)v.Stock).Sum() ?? 0;

            var query = from v in _db.Productvariants
                            .Include(v => v.Product).ThenInclude(p => p.Category)
                        select v;

            switch (ViewBag.CurrentFilter)
            {
                case "out":
                    query = from v in query where v.Stock == 0 select v;
                    break;
                case "low":
                    query = from v in query where v.Stock > 0 && v.Stock <= 10 select v;
                    break;
                case "normal":
                    query = from v in query where v.Stock > 10 select v;
                    break;
                case "all":
                default:
                    break;
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = from v in query
                        where v.Product.ProductName.Contains(s)
                              || v.Color.Contains(s)
                              || v.Size.Contains(s)
                              || v.VariantId.ToString().Contains(s)
                              || (v.Product.Category != null && v.Product.Category.CategoryName.Contains(s))
                        select v;
            }

            // เรียงลำดับสต็อกน้อยสุดขึ้นก่อนเสมอ เพื่อให้เห็นตัวที่ต้องเติมก่อน
            var variants = (from v in query
                            orderby v.Stock ascending, v.Product.ProductName ascending
                            select v).ToList();

            return View(variants);
        }

        // 7. เติมสต็อกสินค้าด่วน (Quick Restock)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult QuickRestock(int variantId, int addStock, string? returnFilter)
        {
            if (addStock <= 0 || addStock > 10000)
            {
                return RedirectToAction("StockAlert", new { filter = returnFilter, status = "จำนวนสต็อกที่ต้องการเติมต้องอยู่ระหว่าง 1 ถึง 10,000 ชิ้น" });
            }

            var variant = (from v in _db.Productvariants
                              .Include(v => v.Product)
                           where v.VariantId == variantId
                           select v).FirstOrDefault();

            if (variant == null)
            {
                return RedirectToAction("StockAlert", new { filter = returnFilter, status = "ไม่พบตัวเลือกสินค้าที่ระบุ" });
            }

            int oldStock = variant.Stock;
            variant.Stock += addStock;
            _db.SaveChanges();

            string productName = variant.Product?.ProductName ?? $"SKU #{variant.VariantId}";
            return RedirectToAction("StockAlert", new
            {
                filter = returnFilter,
                status = $"เติมสต็อก '{productName} ({variant.Color} / {variant.Size})' เพิ่ม +{addStock} ชิ้น เรียบร้อยแล้ว! (สต็อกเดิม {oldStock} ➔ สต็อกใหม่ {variant.Stock} ชิ้น)"
            });
        }
    }
}
