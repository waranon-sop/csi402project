namespace waranon_csi402.ViewModels
{
    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalSales { get; set; }
    }
}

