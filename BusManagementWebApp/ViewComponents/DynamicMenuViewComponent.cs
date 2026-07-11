using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BusManagement.Model.Common;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace BusManagement.ViewComponents;

public class DynamicMenuViewComponent : ViewComponent
{
    private readonly ITmMenuService _tmMenuService;
    private readonly ITmMenuRoleService _tmMenuRoleService;
    private readonly ITmUserService _tmUserService;

    public DynamicMenuViewComponent(ITmMenuService tmMenuService, ITmMenuRoleService tmMenuRoleService, ITmUserService tmUserService)
    {
        _tmMenuService = tmMenuService;
        _tmMenuRoleService = tmMenuRoleService;
        _tmUserService = tmUserService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var roleId = await ResolveCurrentRoleIdAsync();

        var menusResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = "BIT_IS_ACTIVE", Operator = "=", Value = true }
            },
            SortOptions = new()
            {
                new SortOption { Field = "INT_SORT_ORDER", SortDirection = "ASC" },
                new SortOption { Field = "ID", SortDirection = "ASC" }
            }
        });

        var menus = menusResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>();
        var menuRolesResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000
        });
        var menuRoles = menuRolesResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>();
        var roleMap = menuRoles
            .Where(x => x.INT_ROLE_ID.HasValue)
            .GroupBy(x => x.INT_MENU_ID)
            .ToDictionary(g => g.Key, g => g.Select(x => x.INT_ROLE_ID!.Value).Distinct().ToHashSet());

        var authorizedMenus = menus
            .Where(m => IsMenuAuthorizedForRole(m.ID, roleMap, roleId))
            .ToList();

        var vm = new DynamicMenuViewModel
        {
            Items = BuildTree(authorizedMenus)
        };

        return View(vm);
    }

    private async Task<int?> ResolveCurrentRoleIdAsync()
    {
        var token = HttpContext?.Request.Cookies["AccessToken"];
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        string? adid = null;
        string? employeeCode = null;

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            var roleClaim = jwt.Claims.FirstOrDefault(c =>
                c.Type == "role_id"
                || c.Type == "INT_ROLE_ID"
                || c.Type == "int_role_id"
                || c.Type == "roleid"
                || c.Type == ClaimTypes.Role)?.Value;

            if (int.TryParse(roleClaim, out var parsedRoleId) && parsedRoleId > 0)
            {
                return parsedRoleId;
            }

            adid = jwt.Claims.FirstOrDefault(c => c.Type == "adid")?.Value;
            employeeCode = jwt.Claims.FirstOrDefault(c => c.Type == "employee_code")?.Value;
        }
        catch
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(adid) && string.IsNullOrWhiteSpace(employeeCode))
        {
            return null;
        }

        var filters = new List<FilterOption>();
        if (!string.IsNullOrWhiteSpace(adid))
        {
            filters.Add(new FilterOption { Field = "CHR_ADID", Operator = "=", Value = adid.Trim() });
        }

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            filters.Add(new FilterOption
            {
                Field = "CHR_EMPLOYEE_ID",
                Operator = "=",
                Value = employeeCode.Trim(),
                LogicType = filters.Any() ? "OR" : null
            });
        }

        var userResp = await _tmUserService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = filters
        });

        var user = userResp?.Data?.Data?.FirstOrDefault();
        return user?.INT_ROLE_ID;
    }

    private static List<DynamicMenuItemViewModel> BuildTree(List<BusManagement.Model.Dtos.TM_MENUDTO> menus)
    {
        var itemMap = menus.ToDictionary(
            m => m.ID,
            m => new DynamicMenuItemViewModel
            {
                Id = m.ID,
                MenuCode = m.CHR_MENU_CODE,
                MenuName = m.NVCHR_MENU_NAME,
                Controller = m.CHR_CONTROLLER,
                Action = m.CHR_ACTION,
                Icon = m.CHR_ICON,
                SortOrder = m.INT_SORT_ORDER ?? int.MaxValue
            });

        var roots = new List<DynamicMenuItemViewModel>();

        foreach (var menu in menus)
        {
            if (!itemMap.TryGetValue(menu.ID, out var item))
            {
                continue;
            }

            var parentId = menu.INT_PARENT_ID;
            if (parentId.HasValue && parentId.Value > 0 && itemMap.TryGetValue(parentId.Value, out var parent))
            {
                parent.Children.Add(item);
            }
            else
            {
                roots.Add(item);
            }
        }

        static void SortRecursive(List<DynamicMenuItemViewModel> nodes)
        {
            nodes.Sort((a, b) =>
            {
                var sortCompare = a.SortOrder.CompareTo(b.SortOrder);
                if (sortCompare != 0) return sortCompare;
                return a.Id.CompareTo(b.Id);
            });

            foreach (var child in nodes)
            {
                SortRecursive(child.Children);
            }
        }

        SortRecursive(roots);
        return roots;
    }

    private static bool IsMenuAuthorizedForRole(int menuId, Dictionary<int, HashSet<int>> roleMap, int? roleId)
    {
        if (!roleMap.TryGetValue(menuId, out var allowedRoleIds) || allowedRoleIds.Count == 0)
        {
            return true;
        }

        return roleId.HasValue && allowedRoleIds.Contains(roleId.Value);
    }
}
