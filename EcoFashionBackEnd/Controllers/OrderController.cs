using AutoMapper;
using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Common.Payloads.Requests;
using EcoFashionBackEnd.Common.Payloads.Responses;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System.Security.Claims;

namespace EcoFashionBackEnd.Controllers;
[Route("api/Order")]
[ApiController]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly IMapper _mapper;
    public OrderController(OrderService orderService, IMapper mapper)
    {
        _orderService = orderService;
        _mapper = mapper;
    }
    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllOrders()
    {
        // Admin xem tất cả, các role khác chỉ xem của mình
        var isAdmin = User.IsInRole("admin");
        if (isAdmin)
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(ApiResult<IEnumerable<OrderModel>>.Succeed(orders));
        }
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized(ApiResult<object>.Fail("Không thể xác định người dùng."));
        var myOrders = await _orderService.GetOrdersByUserIdAsync(userId);
        return Ok(ApiResult<IEnumerable<OrderModel>>.Succeed(myOrders));
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
            return NotFound(ApiResult<object>.Fail("Không tìm thấy Order"));
        // Non-admin chỉ xem đơn của mình
        var isAdmin = User.IsInRole("admin");
        if (!isAdmin)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
                return Unauthorized(ApiResult<object>.Fail("Không thể xác định người dùng."));
            if (order.UserId != userId)
                return Forbid();
        }
        return Ok(ApiResult<OrderModel>.Succeed(order));
    }
    [HttpPost("Create")]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized(ApiResult<CreateDesignResponse>.Fail("Không thể xác định người dùng."));
        var orderId = await _orderService.CreateOrderAsync(userId, request);
        return CreatedAtAction(nameof(GetOrderById), new { id = orderId },
            ApiResult<object>.Succeed(new {OrderId = orderId}));
    }
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(int id, [FromForm] UpdateOrderRequest request)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized(ApiResult<CreateDesignResponse>.Fail("Không thể xác định người dùng."));
        var updateSuccess = await _orderService.UpdateOrderAsync(id, request);
        if (updateSuccess)
            return Ok(ApiResult<object>.Succeed("Order đã được cập nhật"));
        return BadRequest("Cập nhật Order thất bại");
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeteleOrder(int id)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return Unauthorized(ApiResult<CreateDesignResponse>.Fail("Không thể xác định người dùng."));
        var result = await _orderService.DeleteOrderAsync(id);
        if (result) return Ok(ApiResult<object>.Succeed($"Order id: {id} đã được xóa"));
        return BadRequest("Xóa Order thất bại");
    }
    [HttpGet("Order-Statuses")]
    public IActionResult GetOrderStatuses()
    {
        var statuses = Enum.GetValues(typeof(OrderStatus))
            .Cast<OrderStatus>()
            .Select(s => new { Value = s, Label = s.ToString() });

        return Ok(statuses);
    }

    // Get orders by seller for shipment management
    [HttpGet("by-seller/{sellerId}")]
    public async Task<IActionResult> GetOrdersBySeller(Guid sellerId)
    {
        var orders = await _orderService.GetOrdersBySellerIdAsync(sellerId);
        return Ok(ApiResult<IEnumerable<OrderModel>>.Succeed(orders));
    }

    // Update fulfillment status
    [HttpPatch("{orderId}/fulfillment-status")]
    public async Task<IActionResult> UpdateFulfillmentStatus(int orderId, [FromBody] UpdateFulfillmentStatusRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await _orderService.UpdateFulfillmentStatusAsync(orderId, request.FulfillmentStatus);
        if (!success)
            return NotFound(ApiResult<object>.Fail("Không tìm thấy đơn hàng hoặc không thể cập nhật"));

        return Ok(ApiResult<object>.Succeed("Cập nhật trạng thái giao hàng thành công"));
    }

    // Mark order as shipped
    [HttpPost("{orderId}/ship")]
    public async Task<IActionResult> MarkOrderShipped(int orderId, [FromBody] ShipOrderRequest request)
    {
        var success = await _orderService.MarkOrderShippedAsync(orderId, request);
        if (!success)
            return NotFound(ApiResult<object>.Fail("Không tìm thấy đơn hàng hoặc không thể cập nhật"));

        return Ok(ApiResult<object>.Succeed("Đã cập nhật đơn hàng thành trạng thái đang vận chuyển"));
    }

    // Mark order as delivered and trigger settlement
    [HttpPost("{orderId}/deliver")]
    public async Task<IActionResult> MarkOrderDelivered(int orderId)
    {
        var success = await _orderService.MarkOrderDeliveredAsync(orderId);
        if (!success)
            return NotFound(ApiResult<object>.Fail("Không tìm thấy đơn hàng hoặc không thể cập nhật"));

        return Ok(ApiResult<object>.Succeed("Đã hoàn thành đơn hàng và xử lý thanh toán"));
    }
    

}