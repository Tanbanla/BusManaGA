using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.TripRequest;

public class TripRequestPageViewModel
{
    public string? Department { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Phone { get; set; }
    public string? Purpose { get; set; }
    public string? TripType { get; set; }
    public int? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public bool CanManageAll { get; set; }
    public string? CurrentEmployeeCode { get; set; }
    public string? CurrentEmployeeName { get; set; }
    public string? CurrentDepartmentCode { get; set; }

    public List<TripRequestRowViewModel> Requests { get; set; } = new();
    public List<string> RequestTypeNames { get; set; } = new();
    public List<TM_SECTIONDTO> Sections { get; set; } = new();
    public List<TM_USERDTO> QlscUsers { get; set; } = new();
    public List<TM_USERDTO> QltcUsers { get; set; } = new();
}

public class TripRequestRowViewModel
{
    public int Id { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Phone { get; set; }
    public int? PassengerCount { get; set; }
    public string? TripType { get; set; }
    public DateTime? PickupGo { get; set; }
    public string? FromGo { get; set; }
    public string? ToGo { get; set; }
    public DateTime? PickupReturn { get; set; }
    public string? FromReturn { get; set; }
    public string? ToReturn { get; set; }
    public string? Purpose { get; set; }
    public string? QuotePath { get; set; }
    public bool? QuoteConfirm { get; set; }
    public string? DriverInfo { get; set; }
    public string? QlscApprove { get; set; }
    public string? QltcApprove { get; set; }
    public int? Status { get; set; }
    public string? StatusText { get; set; }
    public string? StatusRowClass { get; set; }
    public string? StatusBadgeClass { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class TripRequestUpsertInputModel
{
    public int? Id { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentCode { get; set; }
    public string? Phone { get; set; }
    public int? PassengerCount { get; set; }
    public string? TripType { get; set; }
    public DateTime? PickupGoDate { get; set; }
    public string? PickupGoTime { get; set; }
    public string? FromGo { get; set; }
    public string? ToGo { get; set; }
    public DateTime? PickupReturnDate { get; set; }
    public string? PickupReturnTime { get; set; }
    public string? FromReturn { get; set; }
    public string? ToReturn { get; set; }
    public string? Purpose { get; set; }
    public string? QlscApprove { get; set; }
    public string? QltcApprove { get; set; }

    public string? SearchDepartment { get; set; }
    public string? SearchEmployeeCode { get; set; }
    public string? SearchEmployeeName { get; set; }
    public string? SearchPhone { get; set; }
    public string? SearchPurpose { get; set; }
    public string? SearchTripType { get; set; }
    public int? SearchStatus { get; set; }
    public DateTime? SearchDateFrom { get; set; }
    public DateTime? SearchDateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
