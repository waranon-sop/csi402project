using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using waranon_csi402.Models;
using waranon_csi402.Services;

namespace waranon_csi402.Controllers
{
    public class AccountingController : Controller
    {
        private readonly Csi402dbContext _db;

        public AccountingController(Csi402dbContext db)
        {
            _db = db;
        }

        // ==========================================
        // Auth Guard Helper — ตรวจสอบสิทธิ์ Accounting ครอบคลุมทุก Action
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

            // 2. ถ้าไม่ใช่ Accounting หรือ Admin ➔ ส่งกลับหน้าร้าน
            if (role != "Accounting" && role != "Admin")
            {
                context.Result = RedirectToAction("Index", "Home");
                return;
            }
        }

        private IActionResult? CheckAccountingAuth()
        {
            if (TempData.Peek("Name") == null)
                return RedirectToAction("Login", "Account");
            if (TempData.Peek("Role")?.ToString() != "Accounting" && TempData.Peek("Role")?.ToString() != "Admin")
                return RedirectToAction("Index", "Home");
            return null;
        }

        // ==========================================
        // 1. แดชบอร์ดและรายการรอตรวจสลิป (Accounting Queue)
        // tab: "verifying" (รอตรวจสอบสลิป), "paid" (อนุมัติแล้ว), "invalid" (สลิปไม่ถูกต้อง), "all" (ทั้งหมด)
        // ==========================================
        public IActionResult Index(string? tab, string? search, string? status)
        {
            ViewBag.Status = status;
            ViewBag.CurrentTab = string.IsNullOrEmpty(tab) ? "verifying" : tab.ToLower();
            ViewBag.Search = search;

            // สถิติตัวเลขสำหรับ Badge & Stat Cards ฝ่ายบัญชี
            ViewBag.CountVerifying = (from o in _db.Orders where o.Status == "Verifying Slip" select o).Count();
            ViewBag.CountPaid = (from o in _db.Orders where o.Status == "Paid" select o).Count();
            ViewBag.CountInvalid = (from o in _db.Orders where o.Status == "Invalid Slip" select o).Count();
            ViewBag.CountPending = (from o in _db.Orders where o.Status == "Pending Payment" select o).Count();

            // คำนวณรายรับรวมทั้งหมดที่ชำระเงินแล้ว
            decimal totalRevenue = 0;
            var paidOrders = (from o in _db.Orders
                              where o.Status == "Paid" || o.Status == "Processing" || o.Status == "Shipped" || o.Status == "Completed"
                              select o.TotalAmount).ToList();
            if (paidOrders.Any())
            {
                totalRevenue = paidOrders.Sum();
            }
            ViewBag.TotalRevenue = totalRevenue;

            // Query ข้อมูลออเดอร์ด้วย LINQ Query Syntax
            var query = from o in _db.Orders
                            .Include(o => o.User)
                            .Include(o => o.Address)
                            .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant).ThenInclude(v => v.Product)
                        select o;

            // กรองสถานะตามแท็บ
            switch (ViewBag.CurrentTab)
            {
                case "verifying":
                    query = from o in query where o.Status == "Verifying Slip" select o;
                    break;
                case "paid":
                    query = from o in query where o.Status == "Paid" select o;
                    break;
                case "invalid":
                    query = from o in query where o.Status == "Invalid Slip" select o;
                    break;
                case "pending":
                    query = from o in query where o.Status == "Pending Payment" select o;
                    break;
                case "refunded":
                    query = from o in query where o.Status == "Refunded" select o;
                    break;
                case "all":
                default:
                    // แสดงทั้งหมดที่มีการทำธุรกรรม
                    break;
            }

            // ค้นหาตาม OrderId, ชื่อลูกค้า, เบอร์โทร
            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = from o in query
                        where o.OrderId.ToString().Contains(s)
                              || (o.Address != null && o.Address.ReceiverName.Contains(s))
                              || (o.Address != null && o.Address.Phone.Contains(s))
                              || (o.User != null && (o.User.FirstName.Contains(s) || o.User.LastName.Contains(s) || o.User.Email.Contains(s)))
                        select o;
            }

            var orders = (from o in query orderby o.UpdatedAt descending select o).ToList();
            return View(orders);
        }

        // ==========================================
        // 2. หน้ารายละเอียดและตรวจสอบสลิปแบบละเอียด (Verify Slip)
        // ==========================================
        [HttpGet]
        public IActionResult VerifySlip(int? id, string? status)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.Status = status;

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
                return RedirectToAction("Index", new { status = "ไม่พบคำสั่งซื้อที่ต้องการ" });
            }

            return View(order);
        }

        // ==========================================
        // 3. อนุมัติสลิปชำระเงิน (Approve Slip)
        // เปลี่ยนสถานะ Verifying Slip ➔ Paid
        // ออเดอร์จะส่งต่อไปยังคิวงานของฝ่ายคลังสินค้า (Staff) ทันที!
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveSlip(int orderId)
        {
            var order = (from o in _db.Orders where o.OrderId == orderId select o).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index", new { status = "ไม่พบคำสั่งซื้อ" });
            }

            // อนุมัติได้เฉพาะออเดอร์ที่อยู่ในสถานะ "รอตรวจสอบสลิป" เท่านั้น
            if (order.Status != "Verifying Slip")
            {
                return RedirectToAction("Index", new { status = $"ไม่สามารถอนุมัติคำสั่งซื้อ #{orderId} ได้ เนื่องจากอยู่ในสถานะ '{order.Status}'" });
            }

            order.Status = "Paid";
            order.UpdatedAt = DateTime.Now;

            // แจ้งลูกค้า + แจ้งพนักงานคลังว่ามีออเดอร์ใหม่รอแพ็ค
            NotificationHelper.Notify(_db, order.UserId, "ชำระเงินเรียบร้อย",
                $"ตรวจสอบสลิปคำสั่งซื้อ #{orderId} เรียบร้อยแล้ว ร้านกำลังเตรียมจัดส่งสินค้า");
            NotificationHelper.NotifyRole(_db, "Staff", "มีออเดอร์ใหม่รอแพ็ค",
                $"คำสั่งซื้อ #{orderId} ชำระเงินแล้ว พร้อมเตรียมจัดส่ง");

            _db.SaveChanges();

            return RedirectToAction("Index", new {
                tab = "paid",
                status = $"อนุมัติสลิปคำสั่งซื้อ #{orderId} ยอดเงิน ฿{order.TotalAmount:N0} เรียบร้อยแล้ว (ส่งต่อไปยังคิวงานฝ่ายคลังสินค้าแล้ว)"
            });
        }

        // ==========================================
        // 4. ปฏิเสธสลิปชำระเงิน (Reject Slip)
        // เปลี่ยนสถานะเป็น Invalid Slip เพื่อให้ลูกค้าแนบใหม่
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectSlip(int orderId, string? reason)
        {
            var order = (from o in _db.Orders where o.OrderId == orderId select o).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index", new { status = "ไม่พบคำสั่งซื้อ" });
            }

            // ปฏิเสธได้เฉพาะออเดอร์ที่อยู่ในสถานะ "รอตรวจสอบสลิป" เท่านั้น
            if (order.Status != "Verifying Slip")
            {
                return RedirectToAction("Index", new { status = $"ไม่สามารถปฏิเสธสลิปคำสั่งซื้อ #{orderId} ได้ เนื่องจากอยู่ในสถานะ '{order.Status}'" });
            }

            order.Status = "Invalid Slip";
            order.UpdatedAt = DateTime.Now; // ระบบอัตโนมัติใช้เวลานี้นับ 12 ชม. ก่อนยกเลิก

            // ส่งเหตุผลให้ลูกค้าผ่านการแจ้งเตือน
            string reasonText = string.IsNullOrWhiteSpace(reason) ? "สลิปไม่ถูกต้อง" : reason.Trim();
            NotificationHelper.Notify(_db, order.UserId, "สลิปไม่ถูกต้อง",
                $"สลิปของคำสั่งซื้อ #{orderId} ไม่ผ่านการตรวจสอบ เนื่องจาก: {reasonText} — กรุณาแนบสลิปใหม่ภายใน 12 ชั่วโมง มิฉะนั้นคำสั่งซื้อจะถูกยกเลิกอัตโนมัติ");

            _db.SaveChanges();

            string msg = string.IsNullOrWhiteSpace(reason)
                ? $"ปฏิเสธสลิปคำสั่งซื้อ #{orderId} แล้ว (ระบบแจ้งให้ลูกค้าแนบสลิปใหม่)"
                : $"ปฏิเสธสลิปคำสั่งซื้อ #{orderId} เนื่องจาก: {reason}";

            return RedirectToAction("Index", new { tab = "invalid", status = msg });
        }

        // ==========================================
        // 4.1 คืนเงิน / คืนสินค้า (Refund Order)
        // Paid / Processing / Shipped / Completed ➔ Refunded
        // คืนสต็อกเฉพาะเมื่อติ๊กว่า "ได้รับสินค้าคืนและตรวจแล้ว"
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RefundOrder(int orderId, string? reason, bool restock)
        {
            var order = (from o in _db.Orders
                             .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant)
                         where o.OrderId == orderId
                         select o).FirstOrDefault();
            if (order == null)
            {
                return RedirectToAction("Index", new { status = "ไม่พบคำสั่งซื้อ" });
            }

            var refundableStatuses = new[] { "Paid", "Processing", "Shipped", "Completed" };
            if (!refundableStatuses.Contains(order.Status))
            {
                return RedirectToAction("VerifySlip", new { id = orderId, status = $"ไม่สามารถคืนเงินได้ เนื่องจากออเดอร์อยู่ในสถานะ '{order.Status}'" });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return RedirectToAction("VerifySlip", new { id = orderId, status = "กรุณาระบุเหตุผลการคืนเงิน" });
            }

            order.Status = "Refunded";
            order.UpdatedAt = DateTime.Now;

            // คืนสต็อกเฉพาะกรณีได้รับสินค้าคืนและตรวจสภาพแล้ว
            if (restock)
            {
                foreach (var item in order.Orderitems)
                {
                    if (item.Variant != null)
                    {
                        item.Variant.Stock += item.Quantity;
                    }
                }
            }

            NotificationHelper.Notify(_db, order.UserId, "คืนเงินเรียบร้อย",
                $"คำสั่งซื้อ #{orderId} ได้รับการคืนเงินจำนวน ฿{order.TotalAmount:N0} แล้ว (เหตุผล: {reason.Trim()})");

            _db.SaveChanges();

            return RedirectToAction("Index", new { tab = "refunded", status = $"คืนเงินคำสั่งซื้อ #{orderId} เรียบร้อยแล้ว" + (restock ? " (คืนสต็อกแล้ว)" : "") });
        }

        // ==========================================
        // 5. รายงานการเงินและรายรับ (Financial Report)
        // ==========================================
        [HttpGet]
        public IActionResult FinancialReport()
        {
            // ดึงออเดอร์ที่ชำระเงินแล้วทั้งหมด
            var completedOrders = (from o in _db.Orders
                                       .Include(o => o.User)
                                       .Include(o => o.Orderitems)
                                   where o.Status == "Paid" || o.Status == "Processing" || o.Status == "Shipped" || o.Status == "Completed"
                                   orderby o.OrderedAt descending
                                   select o).ToList();

            // คำนวณสรุปยอดเงิน
            decimal totalRevenue = 0;
            decimal totalDiscount = 0;
            decimal totalShipping = 0;
            foreach (var o in completedOrders)
            {
                totalRevenue += o.TotalAmount;
                totalDiscount += o.DiscountAmt;
                totalShipping += o.ShippingFee;
            }

            ViewBag.TotalOrders = completedOrders.Count;
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.TotalDiscount = totalDiscount;
            ViewBag.TotalShippingFee = totalShipping;

            return View(completedOrders);
        }
    }
}
