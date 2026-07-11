namespace BusManagement.ViewModels.QltcManager;

public class QltcManagerUpsertInputModel
{
    public int? ID { get; set; }

    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? AddressDetail { get; set; }
    public string? SuggestedPickupFromBivn { get; set; }
    public string? SuggestedAddress { get; set; }

    public string? SearchEmployeeCode { get; set; }
    public string? SearchEmployeeName { get; set; }
    public string? SearchDepartment { get; set; }
    public string? SearchPosition { get; set; }
    public string? SearchPhone { get; set; }
    public string? SearchCity { get; set; }
    public string? SearchSuggestedAddress { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
