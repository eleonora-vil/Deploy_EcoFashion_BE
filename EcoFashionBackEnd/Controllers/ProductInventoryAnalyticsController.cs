using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Dtos.Warehouse;
using EcoFashionBackEnd.Services;
using Microsoft.AspNetCore.Mvc;

namespace EcoFashionBackEnd.Controllers
{
    [Route("api/analytics/product-inventory")]
    [ApiController]
    public class ProductInventoryAnalyticsController : ControllerBase
    {
        private readonly ProductInventoryAnalyticsService _analyticsService;

        public ProductInventoryAnalyticsController(ProductInventoryAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        /// <summary>
        /// Get product inventory summary statistics
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] Guid? designerId = null)
        {
            try
            {
                var summary = await _analyticsService.GetSummaryAsync(from, to, designerId);
                return Ok(ApiResult<ProductSummaryDto>.Succeed(summary));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Error getting product summary: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get low stock product items
        /// </summary>
        [HttpGet("low-stock")]
        public async Task<IActionResult> GetLowStock([FromQuery] int limit = 20, [FromQuery] Guid? designerId = null)
        {
            try
            {
                var lowStockItems = await _analyticsService.GetLowStockItemsAsync(limit, designerId);
                return Ok(ApiResult<List<ProductLowStockItemDto>>.Succeed(lowStockItems));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Error getting low stock items: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get product inventory transactions
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] DateTime? from = null, 
            [FromQuery] DateTime? to = null, 
            [FromQuery] Guid? designerId = null,
            [FromQuery] int? productId = null,
            [FromQuery] int limit = 50)
        {
            try
            {
                var transactions = await _analyticsService.GetTransactionsAsync(from, to, designerId, productId, limit);
                return Ok(ApiResult<List<ProductTransactionDto>>.Succeed(transactions));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Error getting transactions: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get production activity data for charts
        /// </summary>
        [HttpGet("production-activity")]
        public async Task<IActionResult> GetProductionActivity(
            [FromQuery] DateTime? from = null, 
            [FromQuery] DateTime? to = null, 
            [FromQuery] Guid? designerId = null)
        {
            try
            {
                var activity = await _analyticsService.GetProductionActivityAsync(from, to, designerId);
                return Ok(ApiResult<List<ProductActivityPointDto>>.Succeed(activity));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Error getting production activity: {ex.Message}"));
            }
        }

        /// <summary>
        /// Get design popularity based on sales data
        /// </summary>
        [HttpGet("design-popularity")]
        public async Task<IActionResult> GetDesignPopularity(
            [FromQuery] DateTime? from = null, 
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var popularity = await _analyticsService.GetDesignPopularityAsync(from, to);
                return Ok(ApiResult<List<ProductActivityPointDto>>.Succeed(popularity));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResult<object>.Fail($"Error getting design popularity: {ex.Message}"));
            }
        }
    }
}