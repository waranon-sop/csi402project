using System;
using System.Collections.Generic;

namespace waranon_csi402.Models;

public partial class Productvariant
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string Color { get; set; } = null!;

    public string Size { get; set; } = null!;

    public int Stock { get; set; }

    public decimal ExtraPrice { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Cartitem> Cartitems { get; set; } = new List<Cartitem>();

    public virtual ICollection<Orderitem> Orderitems { get; set; } = new List<Orderitem>();

    public virtual Product Product { get; set; } = null!;
}
