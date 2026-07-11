using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.Admin;

public class UsersPageViewModel
{
    public string? Keyword { get; set; }
    public string? Name { get; set; }
    public string? Position { get; set; }
    public int? RoleId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<TM_USERDTO> Users { get; set; } = new();
    public List<TM_ROLEDTO> Roles { get; set; } = new();

    public string GetRoleName(int? roleId)
    {
        if (roleId is null) return "-";
        var role = Roles.FirstOrDefault(r => r.ID == roleId.Value);
        return string.IsNullOrWhiteSpace(role?.NVCHR_ROLE_NAME) ? "-" : role.NVCHR_ROLE_NAME;
    }
}
