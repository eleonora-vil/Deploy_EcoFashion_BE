namespace EcoFashionBackEnd.Dtos.Cart
{
	public class CartItemDto
	{
		public int CartItemId { get; set; }
		public string ItemType { get; set; } = "material"; // "material" or "product"
		
		// For Material items
		public int? MaterialId { get; set; }
		public string? MaterialName { get; set; }
		public System.Guid? SupplierId { get; set; }
		public string? SupplierName { get; set; }
		
		// For Product items
		public int? ProductId { get; set; }
		public string? ProductName { get; set; }
		public string? SKU { get; set; }
		public string? ColorCode { get; set; }
		public string? SizeName { get; set; }
		public int? DesignId { get; set; }
		public string? DesignName { get; set; }
		public System.Guid? DesignerId { get; set; }
		public string? DesignerName { get; set; }
		
		// Common fields
		public int Quantity { get; set; }
		public decimal UnitPriceSnapshot { get; set; }
		public decimal CurrentPrice { get; set; }
		public string? ImageUrl { get; set; }
		public string? UnitLabel { get; set; }
	}

	public class CartDto
	{
		public int CartId { get; set; }
		public bool IsActive { get; set; }
		public System.DateTime UpdatedAt { get; set; }
		public List<CartItemDto> Items { get; set; } = new();
		public decimal Subtotal => Items.Sum(i => (i.CurrentPrice > 0 ? i.CurrentPrice : i.UnitPriceSnapshot) * i.Quantity);
	}
}


