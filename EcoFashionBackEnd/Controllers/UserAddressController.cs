using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Entities;
using EcoFashionBackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoFashionBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserAddressController : ControllerBase
    {
        private readonly UserAddressService _userAddressService;

        public UserAddressController(UserAddressService userAddressService)
        {
            _userAddressService = userAddressService;
        }

        /// <summary>
        /// Get all addresses for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserAddresses()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            var result = await _userAddressService.GetUserAddressesAsync(userId.Value);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get a specific address by ID
        /// </summary>
        [HttpGet("{addressId}")]
        public async Task<IActionResult> GetAddress(int addressId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            var result = await _userAddressService.GetAddressByIdAsync(addressId, userId.Value);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get the default address for the current user
        /// </summary>
        [HttpGet("default")]
        public async Task<IActionResult> GetDefaultAddress()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            var result = await _userAddressService.GetDefaultAddressAsync(userId.Value);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Create a new address
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddressRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var address = new UserAddress
            {
                AddressLine = request.AddressLine,
                City = request.City,
                District = request.District,
                PersonalPhoneNumber = request.PersonalPhoneNumber,
                Country = request.Country ?? "Vietnam",
                IsDefault = request.IsDefault
            };

            var result = await _userAddressService.CreateAddressAsync(userId.Value, address);
            
            if (result.Success)
                return CreatedAtAction(nameof(GetAddress), new { addressId = result.Result?.AddressId }, result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Update an existing address
        /// </summary>
        [HttpPut("{addressId}")]
        public async Task<IActionResult> UpdateAddress(int addressId, [FromBody] UpdateAddressRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var address = new UserAddress
            {
                AddressLine = request.AddressLine,
                City = request.City,
                District = request.District,
                PersonalPhoneNumber = request.PersonalPhoneNumber,
                Country = request.Country ?? "Vietnam",
                IsDefault = request.IsDefault
            };

            var result = await _userAddressService.UpdateAddressAsync(addressId, userId.Value, address);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Delete an address
        /// </summary>
        [HttpDelete("{addressId}")]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            var result = await _userAddressService.DeleteAddressAsync(addressId, userId.Value);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Set an address as default
        /// </summary>
        [HttpPut("{addressId}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            var result = await _userAddressService.SetDefaultAddressAsync(addressId, userId.Value);
            
            if (result.Success)
                return Ok(result);
            
            return BadRequest(result);
        }

        /// <summary>
        /// Get formatted address string
        /// </summary>
        [HttpGet("{addressId}/formatted")]
        public async Task<IActionResult> GetFormattedAddress(int addressId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            var formattedAddress = await _userAddressService.GetFormattedAddressAsync(addressId, userId.Value);
            
            return Ok(new { formattedAddress });
        }

        /// <summary>
        /// Get default formatted address string
        /// </summary>
        [HttpGet("default/formatted")]
        public async Task<IActionResult> GetDefaultFormattedAddress()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Unable to determine user ID");

            var formattedAddress = await _userAddressService.GetDefaultFormattedAddressAsync(userId.Value);
            
            return Ok(new { formattedAddress });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
                return userId;
            
            return null;
        }
    }

    // DTOs for requests
    public class CreateAddressRequest
    {
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        // Số điện thoại nhận hàng (thay cho ZipCode)
        public string? PersonalPhoneNumber { get; set; }
        public string? Country { get; set; }
        public bool IsDefault { get; set; } = false;
    }

    public class UpdateAddressRequest
    {
        public string? AddressLine { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        // Số điện thoại nhận hàng (thay cho ZipCode)
        public string? PersonalPhoneNumber { get; set; }
        public string? Country { get; set; }
        public bool IsDefault { get; set; } = false;
    }
}