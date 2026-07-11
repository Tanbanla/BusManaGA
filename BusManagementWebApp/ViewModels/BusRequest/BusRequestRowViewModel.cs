namespace BusManagement.ViewModels.BusRequest;

public class BusRequestRowViewModel
{
    public int Id { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Position { get; set; }
    public string? Address { get; set; }
    public int? RequestTypeId { get; set; }
    public string? RequestTypeName { get; set; }
    public int? BusRouteAssignId { get; set; }
    public string? RouteDisplay { get; set; }
    public int? BusStopId { get; set; }
    public string? BusStopDisplay { get; set; }
    public DateTime? UseOrCancelDate { get; set; }
    public string? Note { get; set; }
    public string? QlscApprove { get; set; }
    public int? Status { get; set; }
    public string? StatusText { get; set; }
    public string? StatusRowClass { get; set; }
    public string? StatusBadgeClass { get; set; }
    public bool CanEdit { get; set; }
}
