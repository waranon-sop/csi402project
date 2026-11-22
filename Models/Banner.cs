using System;

namespace waranon_csi402.Models;

// แบนเนอร์หน้าร้าน (Admin จัดการ) — ตาราง: banners
public partial class Banner
{
    public int BannerId { get; set; }

    public string Title { get; set; } = null!;

    public string? Subtitle { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? LinkUrl { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
