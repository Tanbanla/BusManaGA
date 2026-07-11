using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BusManagement.Controllers.Api;

[ApiController]
[Route("api/tm-menu-role")]
public class TmMenuRoleApiController : ControllerBase
{
    private readonly ITmMenuRoleService _tmMenuRoleService;

    public TmMenuRoleApiController(ITmMenuRoleService tmMenuRoleService)
    {
        _tmMenuRoleService = tmMenuRoleService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] int? menuId, [FromQuery] int? roleId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : pageSize;

        var filters = new List<FilterOption>();
        if (menuId.HasValue && menuId.Value > 0)
        {
            filters.Add(new FilterOption { Field = "INT_MENU_ID", Operator = "=", Value = menuId.Value });
        }

        if (roleId.HasValue && roleId.Value > 0)
        {
            filters.Add(new FilterOption { Field = "INT_ROLE_ID", Operator = "=", Value = roleId.Value });
        }

        var resp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = filters,
            SortOptions = new() { new SortOption { Field = "ID", SortDirection = "ASC" } }
        });

        return Ok(resp);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var resp = await _tmMenuRoleService.GetByIdAsync(id);
        return Ok(resp);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TM_MENU_ROLE input)
    {
        if (input.INT_MENU_ID <= 0)
        {
            return BadRequest("INT_MENU_ID is required.");
        }

        input.DTM_CREATE = DateTime.Now;
        input.CHR_USER_CREATE = string.IsNullOrWhiteSpace(input.CHR_USER_CREATE) ? "api" : input.CHR_USER_CREATE;

        var resp = await _tmMenuRoleService.AddAsyncV2(input);
        return Ok(resp);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] TM_MENU_ROLE input)
    {
        var currentResp = await _tmMenuRoleService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            return NotFound(currentResp);
        }

        input.ID = id;
        input.CHR_USER_CREATE = current.CHR_USER_CREATE;
        input.DTM_CREATE = current.DTM_CREATE;
        input.DTM_UPDATE = DateTime.Now;
        input.CHR_USER_UPDATE = string.IsNullOrWhiteSpace(input.CHR_USER_UPDATE) ? "api" : input.CHR_USER_UPDATE;

        var resp = await _tmMenuRoleService.UpdateAsync(input);
        return Ok(resp);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var resp = await _tmMenuRoleService.DeleteAsync(id);
        return Ok(resp);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncMenuRoleRequest request)
    {
        if (request.MenuId <= 0)
        {
            return BadRequest("MenuId is required.");
        }

        var actor = string.IsNullOrWhiteSpace(request.Actor) ? "api" : request.Actor.Trim();
        var selectedRoleIds = (request.RoleIds ?? new List<int>()).Where(x => x > 0).Distinct().ToHashSet();

        var existingResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = "INT_MENU_ID", Operator = "=", Value = request.MenuId }
            }
        });

        var existing = existingResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>();
        var existingRoleIds = existing.Where(x => x.INT_ROLE_ID.HasValue).Select(x => x.INT_ROLE_ID!.Value).ToHashSet();

        var removeIds = existing
            .Where(x => x.INT_ROLE_ID.HasValue && !selectedRoleIds.Contains(x.INT_ROLE_ID.Value))
            .Select(x => x.ID)
            .ToList();

        if (removeIds.Any())
        {
            await _tmMenuRoleService.DeleteRangeAsync(removeIds);
        }

        var addRoleIds = selectedRoleIds.Where(x => !existingRoleIds.Contains(x)).ToList();
        if (addRoleIds.Any())
        {
            var entities = addRoleIds.Select(rid => new TM_MENU_ROLE
            {
                INT_MENU_ID = request.MenuId,
                INT_ROLE_ID = rid,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            }).ToList();

            await _tmMenuRoleService.AddMultiAsyncV2(entities);
        }

        return Ok(new { Success = true, Message = "Synced" });
    }

    public class SyncMenuRoleRequest
    {
        public int MenuId { get; set; }
        public List<int>? RoleIds { get; set; }
        public string? Actor { get; set; }
    }
}
