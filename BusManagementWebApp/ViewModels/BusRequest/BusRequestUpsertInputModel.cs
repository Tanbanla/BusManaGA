namespace BusManagement.ViewModels.BusRequest;

public class BusRequestUpsertInputModel
{
    public int? Id { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Address { get; set; }
    public DateTime? UseOrCancelDate { get; set; }
    public int? RequestTypeId { get; set; }
    public string? VehicleType { get; set; }
    public string? VehicleArea { get; set; }
    public int? BusRouteAssignId { get; set; }
    public int? BusStopId { get; set; }
    public string? QlscApprove { get; set; }
    public string? Note { get; set; }

    public string? SearchDepartment { get; set; }
    public string? SearchDepartments { get; set; }
    public string? SearchEmployeeCode { get; set; }
    public string? SearchEmployeeName { get; set; }
    public string? SearchPosition { get; set; }
    public string? SearchAddress { get; set; }
    public int? SearchRequestTypeId { get; set; }
    public string? SearchRequestTypeIds { get; set; }
    public string? SearchRoute { get; set; }
    public int? SearchBusStopId { get; set; }
    public string? SearchBusStopIds { get; set; }
    public DateTime? SearchDateFrom { get; set; }
    public DateTime? SearchDateTo { get; set; }
    public string? SearchNote { get; set; }
    public int? SearchStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
