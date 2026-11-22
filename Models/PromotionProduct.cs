namespace waranon_csi402.Models;

// สินค้าที่ร้านกำหนดให้ร่วมโปรโมชั่นจับคู่ 2 ชิ้น (P4)
// ตาราง: promotionproducts (PromotionID + ProductID เป็น Primary Key ร่วม)
public partial class PromotionProduct
{
    public int PromotionId { get; set; }

    public int ProductId { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
