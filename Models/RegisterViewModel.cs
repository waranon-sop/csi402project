using System.ComponentModel.DataAnnotations;

namespace waranon_csi402_5008_Assessment4.Models;

public class RegisterViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อ")]
    [Display(Name = "ชื่อ")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกนามสกุล")]
    [Display(Name = "นามสกุล")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกเบอร์โทรศัพท์")]
    [Phone(ErrorMessage = "รูปแบบเบอร์โทรศัพท์ไม่ถูกต้อง")]
    [Display(Name = "เบอร์โทรศัพท์")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [StringLength(100, ErrorMessage = "รหัสผ่านต้องมีอย่างน้อย {2} ตัวอักษร", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณายืนยันรหัสผ่าน")]
    [DataType(DataType.Password)]
    [Display(Name = "ยืนยันรหัสผ่าน")]
    [Compare("Password", ErrorMessage = "รหัสผ่านไม่ตรงกัน")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "ยอมรับเงื่อนไขการใช้งาน")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "กรุณายอมรับเงื่อนไขการใช้งาน")]
    public bool AgreeTerms { get; set; }
}
