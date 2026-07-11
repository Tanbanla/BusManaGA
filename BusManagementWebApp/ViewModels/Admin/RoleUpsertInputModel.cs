namespace BusManagement.ViewModels.Admin;

public class RoleUpsertInputModel
{
    public int? ID { get; set; }

    public string? RoleName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; } = true;

    public string? Keyword { get; set; }
    public bool? SearchIsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
