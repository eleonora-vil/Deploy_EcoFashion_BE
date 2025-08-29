namespace EcoFashionBackEnd.Common.Payloads.Responses
{
    public class CustomerOrderOverviewModel
    {
        public int OrderId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;

        // Overall order progress
        public int TotalSubOrders { get; set; }
        public int ConfirmedSubOrders { get; set; }
        public int ShippedSubOrders { get; set; }
        public int DeliveredSubOrders { get; set; }
        
        public double ConfirmationProgress { get; set; }
        public double ShippingProgress { get; set; }
        public double DeliveryProgress { get; set; }

        // Individual sub-orders
        public List<SubOrderProgressModel> SubOrders { get; set; } = new List<SubOrderProgressModel>();

        // Timeline of events across all sub-orders
        public List<OrderTimelineEvent> Timeline { get; set; } = new List<OrderTimelineEvent>();
    }

    public class SubOrderProgressModel
    {
        public int SubOrderId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string SellerType { get; set; } = string.Empty;
        public string? SellerAvatarUrl { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FulfillmentStatus { get; set; } = string.Empty;
        public string? TrackingNumber { get; set; }
        public string? ShippingCarrier { get; set; }
        public int TotalItems { get; set; }
        public List<SubOrderItemProgressModel> Items { get; set; } = new List<SubOrderItemProgressModel>();
        
        // Timestamps
        public string? ConfirmedAt { get; set; }
        public string? ShippedAt { get; set; }
        public string? DeliveredAt { get; set; }
        public string? EstimatedDeliveryDate { get; set; }
    }

    public class SubOrderItemProgressModel
    {
        public string ItemName { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }

    public class OrderTimelineEvent
    {
        public string EventDate { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? SellerName { get; set; }
        public string? Icon { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}