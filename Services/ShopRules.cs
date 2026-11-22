using System;
using waranon_csi402.Models;

namespace waranon_csi402.Services
{
    // ==========================================
    // กฎกลางของร้านที่ใช้ร่วมกันหลายที่ (Cart, ระบบอัตโนมัติ)
    // ==========================================
    public static class ShopRules
    {
        // เวลาปัจจุบันตามเขตเวลาไทย (Asia/Bangkok, UTC+7)
        // ไม่ขึ้นกับว่าเซิร์ฟเวอร์ตั้งเวลาเป็นโซนไหน
        public static DateTime ThaiNow()
        {
            string zoneId = OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Bangkok";
            TimeZoneInfo thaiZone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, thaiZone);
        }

        // โปรโมชั่นนี้คือ Flash Sale (P5) หรือไม่ — ดูจากชื่อโปรโมชั่น
        public static bool IsFlashSalePromotion(Promotion? promo)
        {
            if (promo == null) return false;
            return promo.PromotionName.Contains("Flash Sale", StringComparison.OrdinalIgnoreCase);
        }

        // อยู่ในช่วง Flash Sale หรือไม่: วันที่ 1 ของเดือน เวลา 00:01 – 02:00 น. (เวลาไทย)
        public static bool IsInFlashSaleWindow(DateTime thaiNow)
        {
            if (thaiNow.Day != 1) return false;

            TimeSpan time = thaiNow.TimeOfDay;
            TimeSpan start = new TimeSpan(0, 1, 0);   // 00:01
            TimeSpan end = new TimeSpan(2, 1, 0);     // ก่อน 02:01 (รวมนาทีที่ 02:00)
            return time >= start && time < end;
        }
    }
}
