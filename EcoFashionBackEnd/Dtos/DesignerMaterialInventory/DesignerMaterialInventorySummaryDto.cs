namespace EcoFashionBackEnd.Dtos.DesignerMaterialInventory
{
    public class DesignerMaterialInventorySummaryDto
    {
        public int InventoryId { get; set; }
        public int? MaterialId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal PricePerUnit { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string SupplierName { get; set; }
        public int QuantityAvailable { get; set; }
    }

}
