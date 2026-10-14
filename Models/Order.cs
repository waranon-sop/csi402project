using System;
using System.Collections.Generic;

namespace waranon_csi402.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public int AddressId { get; set; }

    public decimal Subtotal { get; set; }

    public decimal ShippingFee { get; set; }

    public decimal DiscountAmt { get; set; }

    public decimal TotalAmount { get; set; }

    public int? PromotionId { get; set; }

    public int? CouponId { get; set; }

    public string Status { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string? TrackingNumber { get; set; }

    public string? SlipImageUrl { get; set; }

    public DateTime OrderedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual Coupon? Coupon { get; set; }

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();

    public virtual Promotion? Promotion { get; set; }

    public virtual User User { get; set; } = null!;
}
