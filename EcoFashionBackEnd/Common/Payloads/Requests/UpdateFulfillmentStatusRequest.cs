using System.ComponentModel.DataAnnotations;
using EcoFashionBackEnd.Entities;

namespace EcoFashionBackEnd.Common.Payloads.Requests
{
    public class UpdateFulfillmentStatusRequest
    {
        [Required]
        [EnumDataType(typeof(FulfillmentStatus))]
        public FulfillmentStatus FulfillmentStatus { get; set; }
        
        public string? Notes { get; set; }
        public string? Location { get; set; }
    }
}