using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.BusRequest;

public class BusRequestPageViewModel
{
    public string? Department { get; set; }
    public string? Departments { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Position { get; set; }
    public string? Address { get; set; }
    public int? RequestTypeId { get; set; }
    public string? RequestTypeIds { get; set; }
    public string? RouteKeyword { get; set; }
    public int? BusStopId { get; set; }
    public string? BusStopIds { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Note { get; set; }
    public int? Status { get; set; }

    public List<string> SelectedDepartments { get; set; } = new();
    public List<int> SelectedRequestTypeIds { get; set; } = new();
    public List<int> SelectedBusStopIds { get; set; } = new();

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public bool CanManageAll { get; set; }
    public string? CurrentEmployeeCode { get; set; }
    public string? CurrentEmployeeName { get; set; }
    public string? CurrentAddress { get; set; }
    public string? CurrentDepartmentCode { get; set; }

    public List<BusRequestRowViewModel> Requests { get; set; } = new();
    public List<TM_REQUEST_TYPEDTO> RequestTypes { get; set; } = new();
    public List<TM_SECTIONDTO> Sections { get; set; } = new();
    public List<TM_USERDTO> QlscUsers { get; set; } = new();
    public List<TM_BUS_STOPDTO> BusStops { get; set; } = new();
    public List<BusRouteAssignLookupItem> RouteAssignOptions { get; set; } = new();

    public Dictionary<string, List<string>> VehicleAreas { get; set; } = new();
}

public class BusRouteAssignLookupItem
{
    public int Id { get; set; }
    public string? VehicleType { get; set; }
    public string? Area { get; set; }
    public string? DisplayName { get; set; }
    public int? BusStopId { get; set; }
}
