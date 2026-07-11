using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.Admin;

public class RolesPageViewModel
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public List<TM_ROLEDTO> Roles { get; set; } = new();
    public List<TM_MENUDTO> Menus { get; set; } = new();
    public List<TM_MENU_ROLEDTO> MenuRoles { get; set; } = new();

    public List<TM_MENUDTO> BuildOrderedMenus()
    {
        var result = new List<TM_MENUDTO>();
        var childrenMap = Menus
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

    public int GetMenuLevel(TM_MENUDTO menu)
    {
        var map = Menus.ToDictionary(x => x.ID, x => x);
        var level = 0;
        var parentId = menu.INT_PARENT_ID;
        var visited = new HashSet<int>();

        while (parentId.HasValue && parentId.Value > 0)
        {
            if (!visited.Add(parentId.Value)) break;
            if (!map.TryGetValue(parentId.Value, out var parent)) break;
            level++;
            parentId = parent.INT_PARENT_ID;
        }

        return level;
    }

    public int GetAssignedMenuCount(int roleId)
    {
        return MenuRoles.Count(x => x.INT_ROLE_ID == roleId);
    }

    public bool IsMenuAssignedToRole(int menuId, int roleId)
    {
        return MenuRoles.Any(x => x.INT_MENU_ID == menuId && x.INT_ROLE_ID == roleId);
    }
}
