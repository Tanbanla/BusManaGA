namespace BusManagement.ViewModels.Admin;

public class MenuUpsertInputModel
{
    public int? ID { get; set; }

    public string? MenuCode { get; set; }
    public string? MenuName { get; set; }
    public int? ParentId { get; set; }
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public string? Icon { get; set; }
    public int? SortOrder { get; set; } = 0;
    public int? RoleId { get; set; }
    public List<int> RoleIds { get; set; } = new();
    public bool? IsActive { get; set; } = true;

    public string? Keyword { get; set; }
    public int? SearchParentId { get; set; }
    public int? SearchRoleId { get; set; }
    public bool? SearchIsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
