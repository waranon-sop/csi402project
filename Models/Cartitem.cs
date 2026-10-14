using System;
using System.Collections.Generic;

namespace waranon_csi402.Models;

public partial class Cartitem
{
    public int CartItemId { get; set; }

    public int UserId { get; set; }

    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public DateTime AddedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Productvariant Variant { get; set; } = null!;
}
