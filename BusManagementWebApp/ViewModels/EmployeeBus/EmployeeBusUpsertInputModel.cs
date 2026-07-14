namespace BusManagement.ViewModels.EmployeeBus;

public class EmployeeBusUpsertInputModel
{
    public int? ID { get; set; }

    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Ward { get; set; }
    public string? AddressDetail { get; set; }
    public int? BusStopId { get; set; }
    public int? RouteAssignId { get; set; }
    public bool? IsActive { get; set; } = true;

    public string? SearchEmployeeCode { get; set; }
    public string? SearchEmployeeName { get; set; }
    public string? SearchDepartment { get; set; }
    public string? SearchPhone { get; set; }
    public string? SearchCity { get; set; }
    public string? SearchWard { get; set; }
    public int? SearchBusStopId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
