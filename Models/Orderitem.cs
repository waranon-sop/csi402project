using System;
using System.Collections.Generic;

namespace waranon_csi402.Models;

public partial class Orderitem
{
    public int OrderItemId { get; set; }

    public int OrderId { get; set; }

    public int VariantId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Productvariant Variant { get; set; } = null!;
}
