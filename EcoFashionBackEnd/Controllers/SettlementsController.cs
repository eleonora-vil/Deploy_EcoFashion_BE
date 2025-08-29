using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoFashionBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettlementsController : ControllerBase
    {
        private readonly SettlementService _settlementService;
        private readonly OrderService _orderService;

        public SettlementsController(SettlementService settlementService, OrderService orderService)
        {
            _settlementService = settlementService;
            _orderService = orderService;
        }

        [HttpPost("release")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ReleasePayouts([FromQuery] int? orderId, [FromQuery] Guid? orderGroupId)
        {
            try
            {
                if (orderId.HasValue)
                {
                    await _settlementService.ReleasePayoutsForOrderAsync(orderId.Value);
                    
                    await _orderService.UpdateFulfillmentStatusAsync(orderId.Value, FulfillmentStatus.Delivered);
                    
                    return Ok(new { message = "Đã chi trả thành công cho đơn hàng.", orderId = orderId.Value });
                }
                else if (orderGroupId.HasValue)
                {
                    await _settlementService.ReleasePayoutsForGroupAsync(orderGroupId.Value);
                    return Ok(new { message = "Đã chi trả thành công cho nhóm đơn hàng.", orderGroupId = orderGroupId.Value });
                }
                else
                {
                    return BadRequest(new { message = "Cần cung cấp orderId hoặc orderGroupId." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi trong quá trình chi trả.", error = ex.Message });
            }
        }

        [HttpPost("demo-complete-order")]
        public async Task<IActionResult> DemoCompleteOrder([FromQuery] int orderId)
        {
            try
            {
                await _settlementService.ReleasePayoutsForOrderAsync(orderId);
                
                await _orderService.UpdateFulfillmentStatusAsync(orderId, FulfillmentStatus.Delivered);
                
                return Ok(new { message = "Demo: Đơn hàng đã hoàn thành và chi trả.", orderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi trong quá trình demo hoàn thành đơn hàng.", error = ex.Message });
            }
        }

        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetSettlementsForOrder(int orderId)
        {
            try
            {
                var settlements = await _settlementService.GetSettlementsForOrderAsync(orderId);
                return Ok(settlements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin chi trả.", error = ex.Message });
            }
        }

        [HttpGet("seller/pending")]
        public async Task<IActionResult> GetPendingSettlementsForSeller()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            try
            {
                var settlements = await _settlementService.GetPendingSettlementsForSellerAsync(userId);
                return Ok(settlements);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy thông tin chi trả chưa hoàn thành.", error = ex.Message });
            }
        }
    }
}