using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoFashionBackEnd.Entities
{
	[Table("CartItems")]
	public class CartItem
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int CartItemId { get; set; }

		[ForeignKey("Cart")]
		public int CartId { get; set; }
		public virtual Cart Cart { get; set; } = null!;

		// Type of item: "material" or "product"
		[Required]
		[MaxLength(20)]
		public string ItemType { get; set; } = "material";

		// For Material items
		public int? MaterialId { get; set; }
		[ForeignKey("MaterialId")]
		public virtual Material? Material { get; set; }

		// For Product items  
		public int? ProductId { get; set; }
		[ForeignKey("ProductId")]
		public virtual Product? Product { get; set; }

		public int Quantity { get; set; }
		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitPriceSnapshot { get; set; }

		public DateTime AddedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}


