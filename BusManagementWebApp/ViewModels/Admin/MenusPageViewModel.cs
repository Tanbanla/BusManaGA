using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.Admin;

public class MenusPageViewModel
{
    public string? Keyword { get; set; }
    public int? ParentId { get; set; }
    public int? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public List<TM_MENUDTO> Menus { get; set; } = new();
    public List<TM_MENUDTO> AllMenus { get; set; } = new();
    public List<TM_MENU_ROLEDTO> MenuRoles { get; set; } = new();
    public List<TM_ROLEDTO> Roles { get; set; } = new();

    public string GetRoleName(int? roleId)
    {
        if (roleId is null) return "Tất cả";
        var role = Roles.FirstOrDefault(r => r.ID == roleId.Value);
        return string.IsNullOrWhiteSpace(role?.NVCHR_ROLE_NAME) ? "-" : role.NVCHR_ROLE_NAME;
    }

    public List<int> GetAssignedRoleIds(int menuId)
    {
        return MenuRoles
            .Where(x => x.INT_MENU_ID == menuId && x.INT_ROLE_ID.HasValue && x.INT_ROLE_ID.Value > 0)
            .Select(x => x.INT_ROLE_ID!.Value)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    public string GetRoleNames(int menuId)
    {
        var ids = GetAssignedRoleIds(menuId);
        if (!ids.Any()) return "Tất cả";

        var names = ids
            .Select(id => Roles.FirstOrDefault(r => r.ID == id)?.NVCHR_ROLE_NAME)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        return names.Any() ? string.Join(", ", names) : string.Join(", ", ids);
    }

    public string GetParentName(int? parentId)
    {
        if (!parentId.HasValue || parentId.Value <= 0) return "Menu gốc";
        var parent = AllMenus.FirstOrDefault(m => m.ID == parentId.Value);
        return string.IsNullOrWhiteSpace(parent?.NVCHR_MENU_NAME) ? "-" : parent.NVCHR_MENU_NAME;
    }

    public int GetLevel(TM_MENUDTO menu)
    {
        if (menu == null) return 0;

        var map = AllMenus.ToDictionary(x => x.ID, x => x);
        var level = 0;
        var visited = new HashSet<int>();
        var parentId = menu.INT_PARENT_ID;

        while (parentId.HasValue && parentId.Value > 0)
        {
            if (!visited.Add(parentId.Value)) break;
            if (!map.TryGetValue(parentId.Value, out var parent)) break;

            level++;
            parentId = parent.INT_PARENT_ID;
        }

        return level;
    }

    public List<TM_MENUDTO> BuildOrderedTree()
    {
        var result = new List<TM_MENUDTO>();
        var childrenMap = AllMenus
            .GroupBy(x => x.INT_PARENT_ID ?? 0)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.INT_SORT_ORDER ?? int.MaxValue).ThenBy(x => x.NVCHR_MENU_NAME).ToList());

        var visited = new HashSet<int>();

        void AppendChildren(int parentId)
        {
            if (!childrenMap.TryGetValue(parentId, out var children)) return;
            foreach (var child in children)
            {
                if (!visited.Add(child.ID)) continue;
                result.Add(child);
                AppendChildren(child.ID);
            }
        }

        AppendChildren(0);
        return result;
    }
}
