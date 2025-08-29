namespace EcoFashionBackEnd.Common.Payloads.Requests
{
	public class UpsertCartItemRequest
	{
		public string ItemType { get; set; } = "material"; // "material" or "product"
		public int? MaterialId { get; set; }
		public int? ProductId { get; set; }
		public int Quantity { get; set; }
	}

	public class UpsertMaterialCartItemRequest
	{
		public int MaterialId { get; set; }
		public int Quantity { get; set; }
	}

	public class UpsertProductCartItemRequest
	{
		public int ProductId { get; set; }
		public int Quantity { get; set; }
	}

	public class UpdateCartItemQuantityRequest
	{
		public int Quantity { get; set; }
	}
}


