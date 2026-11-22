using Microsoft.EntityFrameworkCore;

namespace waranon_csi402.Models;

// ==========================================
// ตารางที่เพิ่มเองภายหลัง (ไม่ได้มาจากการ Scaffold)
// แยกไฟล์ไว้ เพื่อไม่ให้หายเวลา Scaffold ไฟล์ Csi402dbContext.cs ใหม่
// ==========================================
public partial class Csi402dbContext
{
    public virtual DbSet<PromotionProduct> PromotionProducts { get; set; }

    public virtual DbSet<Banner> Banners { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PromotionProduct>(entity =>
        {
            entity.HasKey(e => new { e.PromotionId, e.ProductId }).HasName("PRIMARY");
            entity.ToTable("promotionproducts");

            entity.Property(e => e.PromotionId).HasColumnName("PromotionID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");

            entity.HasOne(d => d.Promotion).WithMany()
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.Product).WithMany()
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Banner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("PRIMARY");
            entity.ToTable("banners");

            entity.Property(e => e.BannerId).HasColumnName("BannerID");
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Subtitle).HasMaxLength(300);
            entity.Property(e => e.ImageUrl).HasMaxLength(500).HasColumnName("ImageURL");
            entity.Property(e => e.LinkUrl).HasMaxLength(500).HasColumnName("LinkURL");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
        });
    }
}
