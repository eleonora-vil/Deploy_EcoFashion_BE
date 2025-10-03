using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcoFashionBackEnd.Services;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Common;

namespace EcoFashionBackEnd.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminAnalyticsController : ControllerBase
    {
        private readonly AdminAnalyticsService _adminAnalyticsService;
        private readonly UserRegistrationAnalyticsService _userRegistrationAnalyticsService;
        private readonly DashboardStatsService _dashboardStatsService;

        public AdminAnalyticsController(
            AdminAnalyticsService adminAnalyticsService,
            UserRegistrationAnalyticsService userRegistrationAnalyticsService,
            DashboardStatsService dashboardStatsService)
        {
            _adminAnalyticsService = adminAnalyticsService;
            _userRegistrationAnalyticsService = userRegistrationAnalyticsService;
            _dashboardStatsService = dashboardStatsService;
        }

        /// <summary>
        /// Admin: Get system revenue analytics from commissions
        /// </summary>
        [HttpGet("revenue")]
        public async Task<IActionResult> GetRevenueAnalytics([FromQuery] AdminRevenueRequestDto request)
        {
            try
            {
                var analytics = await _adminAnalyticsService.GetAdminRevenueAnalyticsAsync(request);
                return Ok(ApiResult<AdminRevenueAnalyticsDto>.Succeed(analytics));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Lỗi khi lấy dữ liệu analytics: {ex.Message}"));
            }
        }

        /// <summary>
        /// Admin: Get user registration analytics by month
        /// </summary>
        [HttpGet("user-registrations")]
        public async Task<IActionResult> GetUserRegistrationAnalytics([FromQuery] UserRegistrationRequestDto request)
        {
            try
            {
                var analytics = await _userRegistrationAnalyticsService.GetUserRegistrationAnalyticsAsync(request);
                return Ok(ApiResult<UserRegistrationAnalyticsDto>.Succeed(analytics));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Lỗi khi lấy dữ liệu analytics: {ex.Message}"));
            }
        }

        /// <summary>
        /// Admin: Get dashboard statistics
        /// </summary>
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _dashboardStatsService.GetDashboardStatsAsync();
                return Ok(ApiResult<DashboardStatsDto>.Succeed(stats));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Lỗi khi lấy dữ liệu thống kê: {ex.Message}"));
            }
        }
    }
}
