using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace BusManagement.Controllers.Api;

//[Authorize]
[AllowAnonymous]
[ApiController]
[Route("api/[controller]")]
/// <summary>
/// Controller cơ sở cung cấp tập hợp các API CRUD, tìm kiếm, đếm và kiểm tra tồn tại cho thực thể T.
/// Các controller nghiệp vụ kế thừa từ đây sẽ tự động có đầy đủ endpoint chuẩn hóa.
/// </summary>
public class BaseController<T, TKey, TDto> : ControllerBase where T : class
{
    protected readonly IBaseService<T, TKey, TDto> _service;

    public BaseController(IBaseService<T, TKey, TDto> service)
    {
        _service = service;
    }

    /// <summary>
    /// Lấy toàn bộ danh sách bản ghi (không phân trang).
    /// </summary>
    /// <returns>Danh sách tất cả đối tượng dạng DTO.</returns>
    [HttpGet("get-all")]
    public virtual async Task<IActionResult> Get()
    {
        return ResponseResult(await _service.GetAllAsync());
    }

    /// <summary>
    /// Lấy tổng số lượng tất cả bản ghi của thực thể.
    /// </summary>
    /// <returns>Tổng số bản ghi.</returns>
    [HttpGet("get-count-all")]
    public virtual async Task<IActionResult> GetCountAll()
    {
        return ResponseResult(await _service.GetCountAllAsync());
    }

    /// <summary>
    /// Đếm số lượng bản ghi thỏa mãn các điều kiện tìm kiếm / lọc.
    /// </summary>
    /// <param name="options">Tùy chọn tìm kiếm (từ khóa, trường tìm kiếm, bộ lọc).</param>
    /// <returns>Số lượng bản ghi phù hợp.</returns>
    [HttpPost("get-count-by-condition")]
    public virtual async Task<IActionResult> GetCountByConditionAsync([FromBody] SearchOptions options)
    {
        return ResponseResult(await _service.GetCountByConditionAsync(options));
    }

    /// <summary>
    /// Tìm kiếm theo điều kiện và trả về danh sách phân trang (có hỗ trợ sắp xếp, lọc).
    /// </summary>
    /// <param name="options">Tùy chọn tìm kiếm gồm page, size, sort, filters.</param>
    /// <returns>Dữ liệu phân trang gồm danh sách DTO và thông tin trang.</returns>
    [HttpPost("search-by-condition")]
    public virtual async Task<IActionResult> SearchByConditionAsync([FromBody] SearchOptions options)
    {
        return ResponseResult(await _service.SearchByConditionAsync(options));
    }

    /// <summary>
    /// Lấy chi tiết một bản ghi theo khóa chính.
    /// </summary>
    /// <param name="id">Khóa chính của bản ghi.</param>
    /// <returns>Đối tượng DTO tương ứng nếu tồn tại.</returns>
    [HttpGet("get/{id}")]
    public virtual async Task<IActionResult> GetById(TKey id)
    {
        return ResponseResult(await _service.GetByIdAsync(id));
    }

    /// <summary>
    /// Lấy danh sách bản ghi theo tập các khóa chính.
    /// </summary>
    /// <param name="ids">Danh sách khóa chính.</param>
    /// <returns>Danh sách DTO tương ứng.</returns>
    [HttpPost("get-by-ids")]
    public virtual async Task<IActionResult> GetByIdsAsync([FromBody] IEnumerable<TKey> ids)
    {
        return ResponseResult(await _service.GetByIdsAsync(ids));
    }

    ///// <summary>
    ///// Thêm mới một bản ghi, trả về khóa chính được sinh ra.
    ///// </summary>
    ///// <param name="entity">Thực thể cần thêm.</param>
    ///// <returns>Giá trị khóa chính của bản ghi mới.</returns>
    //[HttpPost("add")]
    //public virtual async Task<IActionResult> Add([FromBody] T entity)
    //{
    //    return ResponseResult(await _service.AddAsync(entity));
    //}

    ///// <summary>
    ///// Thêm mới một bản ghi, trả về DTO của bản ghi vừa tạo.
    ///// </summary>
    ///// <param name="entity">Thực thể cần thêm.</param>
    ///// <returns>DTO của bản ghi mới.</returns>
    //[HttpPost("add-v2")]
    //public virtual async Task<IActionResult> AddV2([FromBody] T entity)
    //{
    //    return ResponseResult(await _service.AddAsyncV2(entity));
    //}

    ///// <summary>
    ///// Thêm nhiều bản ghi, trả về tổng số bản ghi đã thêm thành công.
    ///// </summary>
    ///// <param name="entities">Danh sách thực thể.</param>
    ///// <returns>Số lượng bản ghi thêm được.</returns>
    //[HttpPost("add-multi")]
    //public virtual async Task<IActionResult> AddMulti([FromBody] IEnumerable<T> entities)
    //{
    //    return ResponseResult(await _service.AddMultiAsync(entities));
    //}

    ///// <summary>
    ///// Thêm nhiều bản ghi, trả về danh sách DTO của các bản ghi vừa thêm.
    ///// </summary>
    ///// <param name="entities">Danh sách thực thể.</param>
    ///// <returns>Danh sách DTO mới tạo.</returns>
    //[HttpPost("add-multi-v2")]
    //public virtual async Task<IActionResult> AddMultiV2([FromBody] IEnumerable<T> entities)
    //{
    //    return ResponseResult(await _service.AddMultiAsyncV2(entities));
    //}

    ///// <summary>
    ///// Cập nhật một bản ghi (id trên route phục vụ định danh, xử lý id nằm trong entity nếu cần).
    ///// </summary>
    ///// <param name="id">Khóa chính trên URL.</param>
    ///// <param name="entity">Thực thể với dữ liệu đã chỉnh sửa.</param>
    ///// <returns>DTO sau khi cập nhật.</returns>
    //[HttpPut("update/{id}")]
    //public virtual async Task<IActionResult> Update(TKey id, [FromBody] T entity)
    //{
    //    return ResponseResult(await _service.UpdateAsync(entity));
    //}

    ///// <summary>
    ///// Cập nhật nhiều bản ghi cùng lúc.
    ///// </summary>
    ///// <param name="entities">Danh sách thực thể đã chỉnh sửa.</param>
    ///// <returns>Số lượng bản ghi cập nhật thành công.</returns>
    //[HttpPut("update-multi")]
    //public virtual async Task<IActionResult> UpdateMulti([FromBody] IEnumerable<T> entities)
    //{
    //    return ResponseResult(await _service.UpdateMultiAsync(entities));
    //}

    ///// <summary>
    ///// Xóa vật lý một bản ghi theo khóa chính.
    ///// </summary>
    ///// <param name="id">Khóa chính.</param>
    ///// <returns>Trạng thái thành công hoặc thất bại.</returns>
    //[HttpDelete("delete/{id}")]
    //public virtual async Task<IActionResult> Delete(TKey id)
    //{
    //    return ResponseResult(await _service.DeleteAsync(id));
    //}

    ///// <summary>
    ///// Xóa logic (đánh dấu) một bản ghi theo khóa chính.
    ///// </summary>
    ///// <param name="id">Khóa chính.</param>
    ///// <returns>Trạng thái thành công hoặc thất bại.</returns>
    //[HttpDelete("delete-logic/{id}")]
    //public virtual async Task<IActionResult> DeleteLogic(TKey id)
    //{
    //    return ResponseResult(await _service.DeleteLogicAsync(id));
    //}

    ///// <summary>
    ///// Xóa vật lý nhiều bản ghi theo danh sách khóa chính.
    ///// </summary>
    ///// <param name="ids">Danh sách khóa chính.</param>
    ///// <returns>Trạng thái thành công hoặc thất bại.</returns>
    //[HttpDelete("delete-multi")]
    //public virtual async Task<IActionResult> DeleteMulti([FromBody] IEnumerable<TKey> ids)
    //{
    //    return ResponseResult(await _service.DeleteRangeAsync(ids));
    //}

    ///// <summary>
    ///// Xóa logic nhiều bản ghi theo danh sách khóa chính.
    ///// </summary>
    ///// <param name="ids">Danh sách khóa chính.</param>
    ///// <returns>Trạng thái thành công hoặc thất bại.</returns>
    //[HttpDelete("delete-logic-multi")]
    //public virtual async Task<IActionResult> DeleteLogicMulti([FromBody] IEnumerable<TKey> ids)
    //{
    //    return ResponseResult(await _service.DeleteLogicMultiAsync(ids));
    //}

    /// <summary>
    /// Kiểm tra một giá trị có tồn tại trong trường dữ liệu được chỉ định hay không.
    /// </summary>
    /// <param name="fieldName">Tên trường.</param>
    /// <param name="value">Giá trị cần kiểm tra.</param>
    /// <returns>Kết quả true/false.</returns>
    [HttpGet("is-exists")]
    public virtual async Task<IActionResult> IsExists([FromQuery] string fieldName, [FromQuery] string value)
    {
        return ResponseResult(await _service.IsExistsAsync(fieldName, value));
    }

    #region common
    /// <summary>
    /// Lấy giá trị khóa chính của thực thể T (mặc định property "Id").
    /// </summary>
    protected virtual TKey GetEntityId(T entity)
    {
        var property = typeof(T).GetProperty("Id");
        return (TKey)property.GetValue(entity);
    }
    /// <summary>
    /// Lấy giá trị khóa chính của DTO (mặc định property "Id").
    /// </summary>
    protected virtual TKey GetDtoEntityId(TDto entity)
    {
        var property = typeof(T).GetProperty("Id");
        return (TKey)property.GetValue(entity);
    }
    /// <summary>
    /// Chuẩn hóa xử lý trả về GenericResponse (non-generic) tùy theo trạng thái thành công và mã lỗi.
    /// </summary>
    protected virtual IActionResult ResponseResult(GenericResponse result)
    {
        if (!result.Success)
        {
            if (result.Status == 403)
            {
                return Forbid();
            }

            if (result.Status == 404)
            {
                return NotFound(result);
            }

            if (result.Status == 409)
            {
                return StatusCode(409);
            }

            return BadRequest(result);
        }

        return new ObjectResult(result);
    }
    /// <summary>
    /// Chuẩn hóa xử lý trả về GenericResponse<T> tùy theo trạng thái thành công và mã lỗi.
    /// </summary>
    protected virtual IActionResult ResponseResult<T>(GenericResponse<T> result)
    {
        if (!result.Success)
        {
            if (result.Status == 403)
            {
                return Forbid();
            }

            if (result.Status == 404)
            {
                return NotFound(result);
            }

            if (result.Status == 409)
            {
                return StatusCode(409);
            }

            return BadRequest(result);
        }

        return new ObjectResult(result);
    }
    /// <summary>
    /// Lấy bearer token từ Header Authorization (loại bỏ tiền tố "Bearer ").
    /// </summary>
    protected string GetBearerToken()
    {
        string text = base.Request.Headers[HeaderNames.Authorization].ToString();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        return text.Replace("Bearer ", "");
    }
    #endregion
}
