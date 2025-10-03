namespace EcoFashionBackEnd.Dtos
{
    public class SupplierRevenuePointDto
    {
        public DateTime Date { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class SupplierRevenueAnalyticsDto
    {
        public List<SupplierRevenuePointDto> RevenuePoints { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public string Period { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SupplierRevenueRequestDto
    {
        public string Period { get; set; } = "daily"; // daily, weekly, monthly
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}