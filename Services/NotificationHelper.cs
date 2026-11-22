using System;
using System.Linq;
using waranon_csi402.Models;

namespace waranon_csi402.Services
{
    // ==========================================
    // สร้างการแจ้งเตือนในเว็บ (In-app Notification) ลงตาราง Notifications
    // หมายเหตุ: ฟังก์ชันนี้แค่ Add เข้า DbContext — ผู้เรียกต้อง SaveChanges() เอง
    // ==========================================
    public static class NotificationHelper
    {
        // แจ้งเตือนผู้ใช้ 1 คน
        public static void Notify(Csi402dbContext db, int userId, string title, string message)
        {
            db.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.Now
            });
        }

        // แจ้งเตือนทุกคนในบทบาทที่กำหนด เช่น "Staff", "Accounting"
        public static void NotifyRole(Csi402dbContext db, string role, string title, string message)
        {
            var userIds = (from u in db.Users
                           where u.Role == role
                           select u.UserId).ToList();

            foreach (var id in userIds)
            {
                Notify(db, id, title, message);
            }
        }
    }
}
