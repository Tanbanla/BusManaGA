namespace BusManagement.ViewModels.Admin;

public class UserUpsertInputModel
{
    public int? ID { get; set; }

    public string? EmployeeId { get; set; }
    public string? Adid { get; set; }
    public string? EmployeeName { get; set; }
    public string? Position { get; set; }
    public string? Password { get; set; }
    public string? SecCode { get; set; }
    public string? SecName { get; set; }
    public string? AvatarPath { get; set; }
    public string? Note { get; set; }

    public int? RoleId { get; set; }
    public int? Lock { get; set; } = 0;
    public int? LockDay { get; set; } = 0;
    public bool? IsActive { get; set; } = true;

    public string? Keyword { get; set; }
    public string? SearchName { get; set; }
    public string? SearchPosition { get; set; }
    public int? SearchRoleId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
