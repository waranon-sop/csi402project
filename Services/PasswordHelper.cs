using Microsoft.AspNetCore.Identity;
using waranon_csi402.Models;

namespace waranon_csi402.Services
{
    // ==========================================
    // จัดการรหัสผ่านแบบ Hash (ไม่เก็บรหัสผ่านจริงลง DB)
    // ใช้ PasswordHasher ของ ASP.NET Core (PBKDF2)
    // ==========================================
    public static class PasswordHelper
    {
        private static readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

        // แปลงรหัสผ่านเป็น Hash ก่อนบันทึก
        public static string Hash(string password)
        {
            return _hasher.HashPassword(null!, password);
        }

        // รหัสผ่านที่เก็บอยู่เป็น Hash แล้วหรือยัง
        // (Hash ของ PasswordHasher เวอร์ชัน 3 จะขึ้นต้นด้วย "AQAAAA" เสมอ)
        public static bool IsHashed(string storedPassword)
        {
            return storedPassword.StartsWith("AQAAAA");
        }

        // ตรวจรหัสผ่านที่ผู้ใช้กรอก
        // - ถ้า DB เก็บเป็น Hash ➔ ตรวจด้วย PasswordHasher
        // - ถ้า DB ยังเป็นรหัสผ่านแบบเดิม (ข้อมูลเก่า) ➔ เทียบตรงๆ แล้วอัปเกรดเป็น Hash ให้อัตโนมัติ
        //   (ผู้เรียกต้อง SaveChanges เอง)
        public static bool Verify(User user, string password)
        {
            if (IsHashed(user.Password))
            {
                var result = _hasher.VerifyHashedPassword(user, user.Password, password);
                return result != PasswordVerificationResult.Failed;
            }

            if (user.Password == password)
            {
                user.Password = Hash(password); // อัปเกรดข้อมูลเก่าเป็น Hash
                return true;
            }

            return false;
        }
    }
}
