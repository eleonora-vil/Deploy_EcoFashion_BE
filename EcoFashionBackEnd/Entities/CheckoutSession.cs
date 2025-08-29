using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoFashionBackEnd.Entities
{
    [Table("CheckoutSessions")]
    public class CheckoutSession
    {
        [Key]
        public Guid CheckoutSessionId { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [EnumDataType(typeof(CheckoutSessionStatus))]
        public CheckoutSessionStatus Status { get; set; } = CheckoutSessionStatus.Active;

        public string? ShippingAddress { get; set; }
        public int? AddressId { get; set; }
        
        // Summary info
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
        public int TotalProviders { get; set; } // Number of different providers

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; }

        public virtual ICollection<CheckoutSessionItem> Items { get; set; } = new List<CheckoutSessionItem>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

    [Table("CheckoutSessionItems")]
    public class CheckoutSessionItem
    {
        [Key]
        public int CheckoutSessionItemId { get; set; }

        [Required]
        public Guid CheckoutSessionId { get; set; }

        [ForeignKey("CheckoutSessionId")]
        public virtual CheckoutSession CheckoutSession { get; set; } = null!;

        // Item reference
        public int? MaterialId { get; set; }
        public int? ProductId { get; set; }

        [ForeignKey("MaterialId")]
        public virtual Material? Material { get; set; }

        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        // Provider info (determined from item)
        public Guid? SupplierId { get; set; }
        public Guid? DesignerId { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderType { get; set; } // "Supplier" or "Designer"

        [ForeignKey("SupplierId")]
        public virtual Supplier? Supplier { get; set; }

        [ForeignKey("DesignerId")]
        public virtual Designer? Designer { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        public decimal TotalPrice => UnitPrice * Quantity;

        [EnumDataType(typeof(OrderDetailType))]
        public OrderDetailType Type { get; set; }

        // Selection state for flexible checkout
        public bool IsSelected { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum CheckoutSessionStatus
    {
        Active,
        Completed,
        Expired,
        Cancelled
    }
}