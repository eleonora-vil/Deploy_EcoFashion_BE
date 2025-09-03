using AutoMapper;
using EcoFashionBackEnd.Common;
using EcoFashionBackEnd.Common.Payloads.Requests;
using EcoFashionBackEnd.Common.Payloads.Responses;
using EcoFashionBackEnd.Dtos;
using EcoFashionBackEnd.Dtos.Design;
using EcoFashionBackEnd.Dtos.DesignShow;
using EcoFashionBackEnd.Services;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace EcoFashionBackEnd.Controllers;



//[Authorize]
[Route("api/Design")]
[ApiController]
public class DesignController : ControllerBase
{
    private readonly DesignService _designService;
    private readonly DesignerService _designerService;

    private readonly IMapper _mapper;

    public DesignController(DesignService designService, DesignerService designerService,IMapper mapper)
    {
        _designService = designService;
        _designerService = designerService;
        _mapper = mapper;
    }

    [HttpGet("designs-with-products")]
    public async Task<IActionResult> GetDesignsWithProducts()
    {
        var result = await _designService.GetDesignsWithProductsAsync();
        return Ok(ApiResult<List<DesignWithProductInfoDto>>.Succeed(result));
    }

    [HttpGet("{designId}/designer/{designerId}")]
    public async Task<IActionResult> GetDesignDetailWithProducts(int designId, Guid designerId)
    {

        try
        {
            var result = await _designService.GetDesignDetailWithProductsAsync(designId, designerId);
            return Ok(ApiResult<DesignDetailDto>.Succeed(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<DesignDetailDto>.Fail(ex.Message));
        }
    }

    [HttpGet("designer")]
    public async Task<IActionResult> GetDesignsByDesigner()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResult<bool>.Fail("Không thể xác định người dùng."));
        }

        var designerId = await _designerService.GetDesignerIdByUserId(userId);
        if (designerId == Guid.Empty)
        {
            return BadRequest(ApiResult<bool>.Fail("Không tìm thấy Designer tương ứng."));
        }
        try
        {
            // Kiểm tra tham số đầu vào
            if (designerId == Guid.Empty)
            {
                return BadRequest(ApiResult<List<DesignSummaryDto>>.Fail("DesignerId không được để trống."));
            }

            // Gọi service lấy danh sách design có product cho designer đó
            var designs = await _designService.GetDesignsWithProductsByDesignerAsync((Guid)designerId);

            // Nếu không tìm thấy design nào
            if (designs == null || designs.Count == 0)
            {
                return NotFound(ApiResult<List<DesignSummaryDto>>.Fail("Không tìm thấy thiết kế nào cho designer này."));
            }

            // Trả về kết quả thành công với data
            return Ok(ApiResult<List<DesignSummaryDto>>.Succeed(designs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<List<DesignSummaryDto>>.Fail("Có lỗi xảy ra trong quá trình xử lý."));
        }
    }

    [HttpGet("GetDesignsWithProductsPagination")]
    public async Task<IActionResult> GetAllDesignPagination([FromQuery] int page, [FromQuery] int pageSize)
    {
        var result = await _designService.GetDesignsWithProductsPaginationAsync(page, pageSize);
        return Ok(ApiResult<List<DesignWithProductInfoDto>>.Succeed(result));
    }

    [HttpGet("GetAllPagination-by-designer/{designerId}")]
    public async Task<IActionResult> GetAllDesignsByDesignerIdPagination(Guid designerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
    {


        var result = await _designService.GetDesignsWithDesignerPaginationAsync(designerId, page, pageSize);

        if (result == null || !result.Any())
            return NotFound(ApiResult<List<DesignWithProductInfoDto>>.Fail("Không tìm thấy thiết kế nào cho nhà thiết kế này."));

        return Ok(ApiResult<List<DesignWithProductInfoDto>>.Succeed(result));
    }

    [HttpGet("design-variant")]
    public async Task<IActionResult> GetDesignsWithoutProductsByDesignerId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResult<bool>.Fail("Không thể xác định người dùng."));
        }

        var designerId = await _designerService.GetDesignerIdByUserId(userId);
        if (designerId == Guid.Empty)
        {
            return BadRequest(ApiResult<bool>.Fail("Không tìm thấy Designer tương ứng."));
        }
        try
        {
            // Kiểm tra tham số đầu vào
            if (designerId == Guid.Empty)
            {
                return BadRequest(ApiResult<List<DesignSummaryDto>>.Fail("DesignerId không được để trống."));
            }

            // Gọi service lấy danh sách design có product cho designer đó
            var designs = await _designService.GetDesignsWithoutProductsByDesignerIdAsync((Guid)designerId);

            // Nếu không tìm thấy design nào
            if (designs == null || designs.Count == 0)
            {
                return NotFound(ApiResult<List<DesignSummaryDto>>.Fail("Không tìm thấy thiết kế nào cho designer này."));
            }

            // Trả về kết quả thành công với data
            return Ok(ApiResult<List<DesignSummaryDto>>.Succeed(designs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<List<DesignSummaryDto>>.Fail("Có lỗi xảy ra trong quá trình xử lý."));
        }
    }

    [HttpGet("designs-with-products/by-designer")]
    public async Task<IActionResult> GetDesignsWithProductsAnDesignerId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResult<bool>.Fail("Không thể xác định người dùng."));
        }

        var designerId = await _designerService.GetDesignerIdByUserId(userId);
        if (designerId == Guid.Empty)
        {
            return BadRequest(ApiResult<bool>.Fail("Không tìm thấy Designer tương ứng."));
        }
        try
        {

            // Kiểm tra tham số đầu vào
            if (designerId == Guid.Empty)
            {
                return BadRequest(ApiResult<List<DesignSummaryDto>>.Fail("DesignerId không được để trống."));
            }

            // Gọi service lấy danh sách design có product cho designer đó
            var designs = await _designService.GetDesignsWithProductsAndDesignerIdAsync((Guid)designerId);

            // Nếu không tìm thấy design nào
            if (designs == null || designs.Count == 0)
            {
                return NotFound(ApiResult<List<DesignWithProductInfoDto>>.Fail("Không tìm thấy thiết kế nào cho designer này."));
            }

            // Trả về kết quả thành công với data
            return Ok(ApiResult<List<DesignWithProductInfoDto>>.Succeed(designs));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet("designProductDetails/{designId}")]
    public async Task<IActionResult> GetDesignWithProductsDetail(int designId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResult<bool>.Fail("Không thể xác định người dùng."));
        }

        var designerId = await _designerService.GetDesignerIdByUserId(userId);
        if (designerId == Guid.Empty)
        {
            return BadRequest(ApiResult<bool>.Fail("Không tìm thấy Designer tương ứng."));
        }

        try
        {
            var result = await _designService.GetProductsByDesignAsync(designId, (Guid)designerId);
            return Ok(ApiResult<List<ProductDto>>.Succeed(result));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResult<List<ProductDto>>.Fail(ex.Message));
        }
    }



    [HttpPut("update-basic-info")]
    public async Task<IActionResult> UpdateProductBasicInfo([FromForm] UpdateProductDto input)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        {
            return Unauthorized(ApiResult<bool>.Fail("Không thể xác định người dùng."));
        }

        var designerId = await _designerService.GetDesignerIdByUserId(userId);
        if (designerId == Guid.Empty)
        {
            return BadRequest(ApiResult<bool>.Fail("Không tìm thấy Designer tương ứng."));
        }

        var success = await _designService.UpdateProductBasicInfoAsync(input, (Guid)designerId);

        if (!success)
        {
            return BadRequest(ApiResult<object>.Fail("Cập nhật thất bại. Thiết kế không tồn tại hoặc bạn không có quyền."));
        }

        return Ok(ApiResult<object>.Succeed("Cập nhật thành công."));
    }




  
}