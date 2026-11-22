using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using waranon_csi402.Models;

namespace waranon_csi402.Services
{
    // ==========================================
    // ระบบอัตโนมัติทำงานเบื้องหลัง (Background Automation Service)
    // จัดการเวลาหมดอายุของคำสั่งซื้อ คืนสต็อก และเปลี่ยนสถานะอัตโนมัติตามข้อกำหนด README
    // ==========================================
    public class OrderAutomationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OrderAutomationService> _logger;

        public OrderAutomationService(IServiceScopeFactory scopeFactory, ILogger<OrderAutomationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderAutomationService เริ่มทำงานเบื้องหลังแล้ว...");

            // รันตรวจสอบทุก 1 นาที
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredOrdersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "เกิดข้อผิดพลาดในการประมวลผลคำสั่งซื้ออัตโนมัติ");
                }

                // หน่วงเวลา 1 นาทีก่อนรอบถัดไป
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessExpiredOrdersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Csi402dbContext>();

            DateTime now = DateTime.Now;

            // ==========================================
            // กฎที่ 1: ยกเลิกออเดอร์ที่ไม่แนบสลิปชำระเงิน + คืนสต็อก
            // - ออเดอร์ปกติ (Pending Payment เกิน 24 ชั่วโมง)
            // - ออเดอร์ Flash Sale (โปรโมชั่น P5: Pending Payment เกิน 1 ชั่วโมง)
            // ==========================================
            var pendingOrders = await (from o in db.Orders
                                           .Include(o => o.Promotion)
                                           .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant)
                                       where o.Status == "Pending Payment"
                                       select o).ToListAsync();

            int cancelledPendingCount = 0;
            foreach (var order in pendingOrders)
            {
                // ดูจากโปรโมชั่นที่ออเดอร์ได้รับ (ไม่ผูกกับเลข PromotionID)
                bool isFlashSale = ShopRules.IsFlashSalePromotion(order.Promotion);
                TimeSpan expirationLimit = isFlashSale ? TimeSpan.FromHours(1) : TimeSpan.FromHours(24);

                if (now - order.OrderedAt >= expirationLimit)
                {
                    order.Status = "Cancelled";
                    order.UpdatedAt = now;

                    // คืนสต็อกสินค้ากลับเข้าคลัง
                    foreach (var item in order.Orderitems)
                    {
                        if (item.Variant != null)
                        {
                            item.Variant.Stock += item.Quantity;
                        }
                    }

                    NotificationHelper.Notify(db, order.UserId, "คำสั่งซื้อถูกยกเลิกอัตโนมัติ",
                        $"คำสั่งซื้อ #{order.OrderId} ถูกยกเลิก เนื่องจากไม่ได้แนบสลิปภายใน {(isFlashSale ? "1 ชั่วโมง (Flash Sale)" : "24 ชั่วโมง")}");

                    cancelledPendingCount++;
                }
            }

            // ==========================================
            // กฎที่ 2: สลิปไม่ถูกต้อง (Invalid Slip) ไม่แนบสลิปใหม่เกิน 12 ชั่วโมง ➔ ยกเลิก + คืนสต็อก
            // นับจากเวลาที่ฝ่ายบัญชีกดปฏิเสธ (UpdatedAt)
            // ==========================================
            var invalidOrders = await (from o in db.Orders
                                           .Include(o => o.Orderitems).ThenInclude(oi => oi.Variant)
                                       where o.Status == "Invalid Slip"
                                       select o).ToListAsync();

            int cancelledInvalidCount = 0;
            foreach (var order in invalidOrders)
            {
                if (now - order.UpdatedAt >= TimeSpan.FromHours(12))
                {
                    order.Status = "Cancelled";
                    order.UpdatedAt = now;

                    // คืนสต็อกสินค้ากลับเข้าคลัง
                    foreach (var item in order.Orderitems)
                    {
                        if (item.Variant != null)
                        {
                            item.Variant.Stock += item.Quantity;
                        }
                    }

                    NotificationHelper.Notify(db, order.UserId, "คำสั่งซื้อถูกยกเลิกอัตโนมัติ",
                        $"คำสั่งซื้อ #{order.OrderId} ถูกยกเลิก เนื่องจากไม่ได้แนบสลิปใหม่ภายใน 12 ชั่วโมง");

                    cancelledInvalidCount++;
                }
            }

            // ==========================================
            // กฎที่ 3: จัดส่งแล้ว (Shipped) เกิน 7 วัน ลูกค้าไม่ได้กดยืนยัน ➔ Completed อัตโนมัติ
            // นับจากเวลาที่จัดส่ง (UpdatedAt)
            // ==========================================
            var shippedOrders = await (from o in db.Orders
                                       where o.Status == "Shipped"
                                       select o).ToListAsync();

            int completedShippedCount = 0;
            foreach (var order in shippedOrders)
            {
                if (now - order.UpdatedAt >= TimeSpan.FromDays(7))
                {
                    order.Status = "Completed";
                    order.UpdatedAt = now;

                    NotificationHelper.Notify(db, order.UserId, "คำสั่งซื้อสำเร็จ",
                        $"คำสั่งซื้อ #{order.OrderId} ถูกปิดเป็น 'สำเร็จ' อัตโนมัติหลังจัดส่งครบ 7 วัน คุณสามารถรีวิวสินค้าได้แล้ว");

                    completedShippedCount++;
                }
            }

            // บันทึกการเปลี่ยนแปลงทั้งหมด
            if (cancelledPendingCount > 0 || cancelledInvalidCount > 0 || completedShippedCount > 0)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation(
                    "ประมวลผลคำสั่งซื้ออัตโนมัติสำเร็จ: ยกเลิกไม่ชำระเงิน {Pending} รายการ, ยกเลิกสลิปไม่ถูกต้อง {Invalid} รายการ, สำเร็จอัตโนมัติ {Shipped} รายการ",
                    cancelledPendingCount, cancelledInvalidCount, completedShippedCount);
            }
        }
    }
}
