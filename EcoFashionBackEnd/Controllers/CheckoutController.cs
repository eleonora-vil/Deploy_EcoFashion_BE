using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoFashionBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CheckoutController : ControllerBase
    {
        private readonly CheckoutService _checkoutService;
        private readonly CartService _cartService;
        private readonly OrderPaymentService _orderPaymentService;
        private readonly SettlementService _settlementService;
        private readonly UserAddressService _userAddressService;

        public CheckoutController(CheckoutService checkoutService, CartService cartService, 
            OrderPaymentService orderPaymentService, SettlementService settlementService,
            UserAddressService userAddressService)
        {
            _checkoutService = checkoutService;
            _cartService = cartService;
            _orderPaymentService = orderPaymentService;
            _settlementService = settlementService;
            _userAddressService = userAddressService;
        }

        // Tạo OrderGroup + nhiều Order theo Seller, kèm OrderDetail
        [HttpPost("create-session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            var result = await _checkoutService.CreateSessionAsync(userId, request);
            return Ok(result);
        }

        // Tạo session trực tiếp từ cart hiện tại của user
        [HttpPost("create-session-from-cart")]
        public async Task<IActionResult> CreateSessionFromCart([FromBody] CreateSessionFromCartRequest? request = null)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            // Lấy cart enrich rồi chuyển về CreateSessionRequest
            var cart = await _cartService.GetOrCreateActiveCartAsync(userId);
            if (!cart.Items.Any())
            {
                return BadRequest(new { message = "Giỏ hàng trống." });
            }

            // Get shipping address
            string shippingAddress = "Địa chỉ giao hàng";
            if (request?.AddressId.HasValue == true)
            {
                shippingAddress = await _userAddressService.GetFormattedAddressAsync(request.AddressId.Value, userId);
                if (string.IsNullOrEmpty(shippingAddress))
                {
                    return BadRequest(new { message = "Địa chỉ giao hàng không hợp lệ." });
                }
            }
            else if (!string.IsNullOrEmpty(request?.ShippingAddress))
            {
                shippingAddress = request.ShippingAddress;
            }
            else
            {
                // Use default address if available
                var defaultAddress = await _userAddressService.GetDefaultFormattedAddressAsync(userId);
                if (!string.IsNullOrEmpty(defaultAddress))
                {
                    shippingAddress = defaultAddress;
                }
            }

            var sessionRequest = new CreateSessionRequest
            {
                Items = cart.Items.Select(i => new CartItemDto
                {
                    ItemType = i.ItemType, // Use the actual item type from cart
                    MaterialId = i.MaterialId,
                    ProductId = i.ProductId,
                    DesignId = i.DesignId,
                    Quantity = i.Quantity,
                    UnitPrice = i.CurrentPrice > 0 ? i.CurrentPrice : i.UnitPriceSnapshot,
                    // Let the checkout service determine sellerType and sellerId from the actual item data
                    // This removes the hardcoded "Supplier" limitation
                }).ToList(),
                ShippingAddress = shippingAddress,
                // Truyền idempotency key nếu FE gửi lên
                IdempotencyKey = request?.IdempotencyKey
            };

            var result = await _checkoutService.CreateSessionAsync(userId, sessionRequest);
            return Ok(result);
        }

        [HttpPost("pay-with-wallet")]
        public async Task<IActionResult> PayWithWallet([FromBody] PayWithWalletRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            try
            {
                // Update address if provided
                if (request.AddressId.HasValue)
                {
                    var addressUpdateSuccess = await _checkoutService.UpdateOrderAddressAsync(request.OrderId, userId, request.AddressId.Value);
                    if (!addressUpdateSuccess)
                    {
                        return BadRequest(new { message = "Không thể cập nhật địa chỉ giao hàng." });
                    }
                }

                var success = await _orderPaymentService.PayWithWalletAsync(request.OrderId, userId);
                if (!success)
                {
                    return BadRequest(new { message = "Không thể thanh toán qua ví. Kiểm tra số dư hoặc trạng thái đơn hàng." });
                }

                await _settlementService.CreateSettlementsForOrderAsync(request.OrderId);

                // Clear cart after successful payment
                await _cartService.ClearCartAsync(userId);

                return Ok(new { message = "Thanh toán thành công qua ví.", orderId = request.OrderId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi trong quá trình thanh toán.", error = ex.Message });
            }
        }

        [HttpPost("pay-group-with-wallet")]
        public async Task<IActionResult> PayGroupWithWallet([FromBody] PayGroupWithWalletRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            try
            {
                // Update address for all orders in the group if provided
                if (request.AddressId.HasValue)
                {
                    var addressUpdateSuccess = await _checkoutService.UpdateOrderGroupAddressAsync(request.OrderGroupId, userId, request.AddressId.Value);
                    if (!addressUpdateSuccess)
                    {
                        return BadRequest(new { message = "Không thể cập nhật địa chỉ giao hàng cho nhóm đơn." });
                    }
                }

                var success = await _orderPaymentService.PayGroupWithWalletAsync(request.OrderGroupId, userId);
                if (!success)
                {
                    return BadRequest(new { message = "Không thể thanh toán nhóm đơn qua ví. Kiểm tra số dư hoặc trạng thái đơn hàng." });
                }

                // Clear cart after successful group payment
                await _cartService.ClearCartAsync(userId);

                return Ok(new { message = "Thanh toán nhóm đơn thành công qua ví.", orderGroupId = request.OrderGroupId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi trong quá trình thanh toán nhóm đơn.", error = ex.Message });
            }
        }

        [HttpGet("addresses")]
        public async Task<IActionResult> GetUserAddresses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            try
            {
                var result = await _userAddressService.GetUserAddressesAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi lấy danh sách địa chỉ.", error = ex.Message });
            }
        }

        [HttpPost("create-address")]
        public async Task<IActionResult> CreateAddressDuringCheckout([FromBody] CreateAddressRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            try
            {
                var address = new Entities.UserAddress
                {
                    AddressLine = request.AddressLine,
                    City = request.City,
                    District = request.District,
                    // Lưu SĐT nhận hàng vào cột ZipCode (map ở entity)
                    PersonalPhoneNumber = request.PersonalPhoneNumber,
                    Country = request.Country ?? "Vietnam",
                    IsDefault = request.IsDefault
                };

                var result = await _userAddressService.CreateAddressAsync(userId, address);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi tạo địa chỉ mới.", error = ex.Message });
            }
        }

        [HttpPut("update-address/{addressId}")]
        public async Task<IActionResult> UpdateAddressDuringCheckout(int addressId, [FromBody] UpdateAddressRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            try
            {
                var address = new Entities.UserAddress
                {
                    AddressLine = request.AddressLine,
                    City = request.City,
                    District = request.District,
                    // Lưu SĐT nhận hàng vào cột ZipCode (map ở entity)
                    PersonalPhoneNumber = request.PersonalPhoneNumber,
                    Country = request.Country ?? "Vietnam",
                    IsDefault = request.IsDefault
                };

                var result = await _userAddressService.UpdateAddressAsync(addressId, userId, address);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật địa chỉ.", error = ex.Message });
            }
        }

        [HttpPut("order/{orderId}/address")]
        public async Task<IActionResult> UpdateOrderAddress(int orderId, [FromBody] UpdateOrderAddressRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            try
            {
                bool success = false;

                if (request.AddressId.HasValue)
                {
                    success = await _checkoutService.UpdateOrderAddressAsync(orderId, userId, request.AddressId.Value);
                }
                else if (!string.IsNullOrEmpty(request.ShippingAddress))
                {
                    success = await _checkoutService.UpdateOrderAddressDirectAsync(orderId, userId, request.ShippingAddress);
                }

                if (success)
                {
                    return Ok(new { message = "Cập nhật địa chỉ giao hàng thành công." });
                }
                else
                {
                    return BadRequest(new { message = "Không thể cập nhật địa chỉ. Kiểm tra đơn hàng hoặc địa chỉ." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật địa chỉ đơn hàng.", error = ex.Message });
            }
        }

        [HttpPut("order-group/{orderGroupId}/address")]
        public async Task<IActionResult> UpdateOrderGroupAddress(Guid orderGroupId, [FromBody] UpdateOrderAddressRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            try
            {
                if (!request.AddressId.HasValue)
                {
                    return BadRequest(new { message = "AddressId là bắt buộc để cập nhật nhóm đơn hàng." });
                }

                var success = await _checkoutService.UpdateOrderGroupAddressAsync(orderGroupId, userId, request.AddressId.Value);

                if (success)
                {
                    return Ok(new { message = "Cập nhật địa chỉ giao hàng cho nhóm đơn thành công." });
                }
                else
                {
                    return BadRequest(new { message = "Không thể cập nhật địa chỉ nhóm đơn. Kiểm tra nhóm đơn hàng hoặc địa chỉ." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi khi cập nhật địa chỉ nhóm đơn hàng.", error = ex.Message });
            }
        }
    }
}


