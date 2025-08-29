using System.ComponentModel.DataAnnotations;

namespace EcoFashionBackEnd.Common.Payloads.Requests
{
    public class ShipOrderRequest
    {
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }
        public string? Notes { get; set; }
        public DateTime? EstimatedDelivery { get; set; }
    }
}