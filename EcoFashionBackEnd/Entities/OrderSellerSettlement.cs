using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoFashionBackEnd.Entities
{
    [Table("OrderSellerSettlements")]
    public class OrderSellerSettlement
    {
        [Key]
        public Guid SettlementId { get; set; } = Guid.NewGuid();

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } = null!;

        // The destination seller user who will receive payout
        [Required]
        public int SellerUserId { get; set; }

        // e.g., "Supplier" or "Designer"
        [MaxLength(20)]
        public string? SellerType { get; set; }

        // Amounts
        public decimal GrossAmount { get; set; }
        public decimal CommissionRate { get; set; } // e.g. 0.10 for 10%
        public decimal CommissionAmount { get; set; }
        public decimal NetAmount { get; set; }

        [EnumDataType(typeof(SettlementStatus))]
        public SettlementStatus Status { get; set; } = SettlementStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReleasedAt { get; set; }
    }

    public enum SettlementStatus
    {
        Pending,
        Released,
        Cancelled
    }
}


