using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Dtos.Auth;
using EcoFashionBackEnd.Dtos.User;
using EcoFashionBackEnd.Exceptions;
using EcoFashionBackEnd.Helpers;
using EcoFashionBackEnd.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EcoFashionBackEnd.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly IEmailService _emailService;

        public UserController(UserService userService, IEmailService emailService)
        {
            _userService = userService;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResult<object>.Fail("Dữ liệu không hợp lệ."));

            try
            {
                // Gọi service để xác thực user
                var result = await _userService.LoginAsync(request);
                return Ok(ApiResult<AuthResponse>.Succeed(result));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ApiResult<object>.Fail(ex.Message));
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ApiResult<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                // Log exception
                return StatusCode(500, ApiResult<object>.Fail(ex.Message));
            }
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResult<object>.Fail("Dữ liệu không hợp lệ."));

            try
            {
                var result = await _userService.SignupAsync(request);
                return Ok(ApiResult<SignupResponse>.Succeed(result));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ApiResult<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail(ex.Message));
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResult<object>.Fail("Dữ liệu không hợp lệ."));

            try
            {
                var result = await _userService.VerifyOTPAsync(request);
                if (result)
                {
                    return Ok(ApiResult<object>.Succeed("Xác thực OTP thành công."));
                }
                return BadRequest(ApiResult<object>.Fail("Xác thực OTP thất bại."));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ApiResult<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail(ex.Message));
            }
        }

        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOTP([FromBody] ResendOTPRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResult<object>.Fail("Dữ liệu không hợp lệ."));

            try
            {
                var result = await _userService.ResendOTPAsync(request.Email);
                if (result)
                {
                    return Ok(ApiResult<object>.Succeed("Mã OTP đã được gửi lại."));
                }
                return BadRequest(ApiResult<object>.Fail("Không thể gửi lại mã OTP."));
            }
            catch (BadRequestException ex)
            {
                return BadRequest(ApiResult<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail(ex.Message));
            }
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetCurrentUserProfile()
        {
            try
            {
                // Lấy userId từ claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(ApiResult<object>.Fail("Không thể xác định user."));
                }

                var userProfile = await _userService.GetUserProfileAsync(userId);
                if (userProfile == null)
                {
                    return NotFound(ApiResult<object>.Fail("Không tìm thấy thông tin user."));
                }

                return Ok(ApiResult<object>.Succeed(userProfile));
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(ApiResult<object>.Fail(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail(ex.Message));
            }
        }
        [HttpGet("test-email")]
        public async Task<IActionResult> TestEmail()
        {
            var mailData = new MailData()
            {
                EmailToId = "vinhthitheokaka@gmail.com",
                EmailToName = "Tester",
                EmailSubject = "Railway Test",
                EmailBody = "<h2>This is a test from Railway backend</h2>"

            };

            var result = await _emailService.SendEmailAsync(mailData);
            return Ok(new { success = result });
        }

    }
}
