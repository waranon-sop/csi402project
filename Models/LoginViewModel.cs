using System.ComponentModel.DataAnnotations;

namespace waranon_csi402_5008_Assessment4.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "กรุณากรอกอีเมล")]
    [EmailAddress(ErrorMessage = "รูปแบบอีเมลไม่ถูกต้อง")]
    [Display(Name = "อีเมล")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน")]
    [DataType(DataType.Password)]
    [Display(Name = "รหัสผ่าน")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "จดจำฉันไว้")]
    public bool RememberMe { get; set; }
}
