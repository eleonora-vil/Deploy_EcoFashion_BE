namespace EcoFashionBackEnd.Dtos
{
    public class CheckoutSessionDto
    {
        public Guid CheckoutSessionId { get; set; }
        public int UserId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ShippingAddress { get; set; }
        public int? AddressId { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
        public int TotalProviders { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public List<CheckoutSessionItemDto> Items { get; set; } = new List<CheckoutSessionItemDto>();
        public List<ProviderGroupDto> ProviderGroups { get; set; } = new List<ProviderGroupDto>();
    }

    public class CheckoutSessionItemDto
    {
        public int CheckoutSessionItemId { get; set; }
        public Guid CheckoutSessionId { get; set; }
        
        // Item info
        public int? MaterialId { get; set; }
        public int? ProductId { get; set; }
        public string? ItemName { get; set; }
        public string? ItemImageUrl { get; set; }
        public string Type { get; set; } = string.Empty;
        
        // Provider info
        public Guid? SupplierId { get; set; }
        public Guid? DesignerId { get; set; }
        public string? ProviderName { get; set; }
        public string? ProviderType { get; set; }
        public string? ProviderAvatarUrl { get; set; }
        
        // Pricing
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        
        // Selection state
        public bool IsSelected { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }

    public class ProviderGroupDto
    {
        public Guid? ProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public string? ProviderAvatarUrl { get; set; }
        public List<CheckoutSessionItemDto> Items { get; set; } = new List<CheckoutSessionItemDto>();
        public decimal GroupSubtotal { get; set; }
        public int GroupItemCount { get; set; }
        public bool CanCheckoutSeparately { get; set; } = true;
    }

    // Request DTOs
    public class CreateCheckoutSessionRequest
    {
        public List<CheckoutSessionItemRequest> Items { get; set; } = new List<CheckoutSessionItemRequest>();
        public string? ShippingAddress { get; set; }
        public int? AddressId { get; set; }
        public int HoldMinutes { get; set; } = 30;
    }

    public class CheckoutSessionItemRequest
    {
        public int? MaterialId { get; set; }
        public int? ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitPrice { get; set; } // Optional, will be fetched if not provided
    }

    public class UpdateCheckoutSelectionRequest
    {
        public List<int> SelectedItemIds { get; set; } = new List<int>();
        public Guid? ProviderIdFilter { get; set; } // If provided, only select items from this provider
        public string? ProviderTypeFilter { get; set; } // "Supplier" or "Designer"
    }

    public class FlexibleCheckoutRequest
    {
        public Guid CheckoutSessionId { get; set; }
        public List<int> SelectedItemIds { get; set; } = new List<int>();
        public string CheckoutMode { get; set; } = "Selected"; // "Selected", "ByProvider", "All"
        public Guid? ProviderIdFilter { get; set; } // For ByProvider mode
        public int? AddressId { get; set; }
        public string? ShippingAddress { get; set; }
    }

    // Controller-specific DTOs
    public class FlexibleCreateSessionFromCartRequest
    {
        public string? ShippingAddress { get; set; }
        public int? AddressId { get; set; }
    }

    public class PaySelectedWithWalletRequest
    {
        public Guid CheckoutSessionId { get; set; }
        public List<int> SelectedItemIds { get; set; } = new List<int>();
        public string CheckoutMode { get; set; } = "Selected"; // "Selected", "ByProvider", "All"
        public Guid? ProviderIdFilter { get; set; }
        public int? AddressId { get; set; }
        public string? ShippingAddress { get; set; }
    }
}