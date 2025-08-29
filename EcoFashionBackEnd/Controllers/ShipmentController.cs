using AutoMapper;
using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Common.Payloads.Requests;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EcoFashionBackEnd.Controllers
{
    [Route("api/shipment")]
    [ApiController]
    [Authorize]
    public class ShipmentController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly IMapper _mapper;

        public ShipmentController(OrderService orderService, IMapper mapper)
        {
            _orderService = orderService;
            _mapper = mapper;
        }

        // Get all orders that need shipment management (for sellers)
        [HttpGet("orders")]
        public async Task<IActionResult> GetShipmentOrders()
        {
            // Get orders with PaymentStatus = Paid for shipment management
            var orders = await _orderService.GetAllOrdersAsync();
            var shipmentOrders = orders.Where(o => o.PaymentStatus == "Paid").ToList();
            
            return Ok(ApiResult<IEnumerable<OrderModel>>.Succeed(shipmentOrders));
        }

        // Get orders for specific seller
        [HttpGet("orders/seller/{sellerId}")]
        public async Task<IActionResult> GetSellerShipmentOrders(Guid sellerId)
        {
            var orders = await _orderService.GetOrdersBySellerIdAsync(sellerId);
            return Ok(ApiResult<IEnumerable<OrderModel>>.Succeed(orders));
        }

        // Get specific order shipment details
        [HttpGet("orders/{orderId}")]
        public async Task<IActionResult> GetOrderShipment(int orderId)
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null)
                return NotFound(ApiResult<object>.Fail("Không tìm thấy đơn hàng"));

            return Ok(ApiResult<OrderModel>.Succeed(order));
        }

        // Update shipment status
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateShipmentStatus([FromBody] UpdateShipmentStatusRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _orderService.UpdateFulfillmentStatusAsync(request.OrderId, request.Status);
            if (!success)
                return NotFound(ApiResult<object>.Fail("Không tìm thấy đơn hàng hoặc không thể cập nhật"));

            return Ok(ApiResult<object>.Succeed("Cập nhật trạng thái vận chuyển thành công"));
        }

        // Mark order as shipped
        [HttpPost("{orderId}/ship")]
        public async Task<IActionResult> ShipOrder(int orderId, [FromBody] ShipOrderRequest request)
        {
            var success = await _orderService.MarkOrderShippedAsync(orderId, request);
            if (!success)
                return NotFound(ApiResult<object>.Fail("Không tìm thấy đơn hàng hoặc không thể cập nhật"));

            return Ok(ApiResult<object>.Succeed("Đã cập nhật đơn hàng thành trạng thái đang vận chuyển"));
        }

        // Mark order as delivered (triggers settlement)
        [HttpPost("{orderId}/deliver")]
        public async Task<IActionResult> DeliverOrder(int orderId)
        {
            var success = await _orderService.MarkOrderDeliveredAsync(orderId);
            if (!success)
                return NotFound(ApiResult<object>.Fail("Không tìm thấy đơn hàng hoặc không thể cập nhật"));

            return Ok(ApiResult<object>.Succeed("Đã hoàn thành đơn hàng và xử lý thanh toán"));
        }

        // Get shipment statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetShipmentStatistics()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            var paidOrders = orders.Where(o => o.PaymentStatus == "Paid").ToList();

            var statistics = new
            {
                Total = paidOrders.Count,
                Pending = paidOrders.Count(o => o.FulfillmentStatus == "None" || o.FulfillmentStatus == "Processing"),
                Processing = paidOrders.Count(o => o.FulfillmentStatus == "Processing"),
                Shipped = paidOrders.Count(o => o.FulfillmentStatus == "Shipped"),
                Delivered = paidOrders.Count(o => o.FulfillmentStatus == "Delivered"),
                Cancelled = paidOrders.Count(o => o.FulfillmentStatus == "Canceled")
            };

            return Ok(ApiResult<object>.Succeed(statistics));
        }

        // Demo: Complete order (for testing)
        [HttpPost("{orderId}/complete")]
        public async Task<IActionResult> CompleteOrder(int orderId)
        {
            // This endpoint simulates completing the entire order flow for demo purposes
            var success = await _orderService.MarkOrderDeliveredAsync(orderId);
            if (!success)
                return NotFound(ApiResult<object>.Fail("Không tìm thấy đơn hàng hoặc không thể cập nhật"));

            return Ok(ApiResult<object>.Succeed("Đã hoàn thành đơn hàng (demo)"));
        }
    }
}

// Request DTO for updating shipment status
public class UpdateShipmentStatusRequest
{
    public int OrderId { get; set; }
    public FulfillmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? Location { get; set; }
}