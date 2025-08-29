using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoFashionBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FlexibleCheckoutController : ControllerBase
    {
        private readonly FlexibleCheckoutService _flexibleCheckoutService;
        private readonly OrderPaymentService _orderPaymentService;
        private readonly SettlementService _settlementService;
        private readonly CartService _cartService;

        public FlexibleCheckoutController(
            FlexibleCheckoutService flexibleCheckoutService,
            OrderPaymentService orderPaymentService,
            SettlementService settlementService,
            CartService cartService)
        {
            _flexibleCheckoutService = flexibleCheckoutService;
            _orderPaymentService = orderPaymentService;
            _settlementService = settlementService;
            _cartService = cartService;
        }

        [HttpPost("create-session")]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Cannot identify user." });
            }

            try
            {
                var session = await _flexibleCheckoutService.CreateCheckoutSessionAsync(userId, request);
                return Ok(new { success = true, data = session });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("create-session-from-cart")]
        public async Task<IActionResult> CreateCheckoutSessionFromCart([FromBody] FlexibleCreateSessionFromCartRequest? request = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Cannot identify user." });
            }

            try
            {
                var session = await _flexibleCheckoutService.CreateCheckoutSessionFromCartAsync(
                    userId, 
                    request?.ShippingAddress, 
                    request?.AddressId
                );
                return Ok(new { success = true, data = session });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{checkoutSessionId}")]
        public async Task<IActionResult> GetCheckoutSession(Guid checkoutSessionId)
        {
            try
            {
                var session = await _flexibleCheckoutService.GetCheckoutSessionAsync(checkoutSessionId);
                return Ok(new { success = true, data = session });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{checkoutSessionId}/selection")]
        public async Task<IActionResult> UpdateSelection(Guid checkoutSessionId, [FromBody] UpdateCheckoutSelectionRequest request)
        {
            try
            {
                var success = await _flexibleCheckoutService.UpdateSelectionAsync(checkoutSessionId, request);
                if (success)
                {
                    var updatedSession = await _flexibleCheckoutService.GetCheckoutSessionAsync(checkoutSessionId);
                    return Ok(new { success = true, data = updatedSession });
                }
                return BadRequest(new { message = "Failed to update selection" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("process-flexible-checkout")]
        public async Task<IActionResult> ProcessFlexibleCheckout([FromBody] FlexibleCheckoutRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Cannot identify user." });
            }

            try
            {
                var result = await _flexibleCheckoutService.ProcessFlexibleCheckoutAsync(userId, request);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("pay-selected-with-wallet")]
        public async Task<IActionResult> PaySelectedWithWallet([FromBody] PaySelectedWithWalletRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Cannot identify user." });
            }

            try
            {
                // First process the flexible checkout to create orders
                var checkoutRequest = new FlexibleCheckoutRequest
                {
                    CheckoutSessionId = request.CheckoutSessionId,
                    SelectedItemIds = request.SelectedItemIds,
                    CheckoutMode = request.CheckoutMode,
                    ProviderIdFilter = request.ProviderIdFilter,
                    AddressId = request.AddressId,
                    ShippingAddress = request.ShippingAddress
                };

                var checkoutResult = await _flexibleCheckoutService.ProcessFlexibleCheckoutAsync(userId, checkoutRequest);

                // Then pay for the created orders
                bool paymentSuccess;
                if (checkoutResult.Orders.Count == 1)
                {
                    // Single order payment
                    paymentSuccess = await _orderPaymentService.PayWithWalletAsync(checkoutResult.Orders.First().OrderId, userId);
                }
                else
                {
                    // Group payment
                    paymentSuccess = await _orderPaymentService.PayGroupWithWalletAsync(checkoutResult.OrderGroupId, userId);
                }

                if (!paymentSuccess)
                {
                    return BadRequest(new { message = "Payment failed. Check wallet balance or order status." });
                }

                // Create settlements for paid orders
                foreach (var order in checkoutResult.Orders)
                {
                    await _settlementService.CreateSettlementsForOrderAsync(order.OrderId);
                }

                // Clear cart after successful payment
                await _cartService.ClearCartAsync(userId);

                return Ok(new { 
                    success = true, 
                    message = "Payment successful", 
                    orderGroupId = checkoutResult.OrderGroupId,
                    orders = checkoutResult.Orders 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Payment processing error", error = ex.Message });
            }
        }
    }
}