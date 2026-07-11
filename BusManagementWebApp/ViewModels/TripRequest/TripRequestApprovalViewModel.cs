namespace BusManagement.ViewModels.TripRequest;

public class TripRequestApprovalPageViewModel
{
    public string Stage { get; set; } = "qlsc";
    public string StageTitle { get; set; } = string.Empty;
    public bool CanProcess { get; set; }
    public string? AccessDeniedMessage { get; set; }
    public List<TripRequestRowViewModel> Requests { get; set; } = new();
}

public class TripRequestApprovalActionInputModel
{
    public string Stage { get; set; } = "qlsc";
    public string? SelectedIds { get; set; }
    public string? RejectReason { get; set; }
    public string? DriverInfo { get; set; }
    public bool? QuoteConfirm { get; set; }
}

public class TripRequestApprovalHistoryPageViewModel
{
    public int RequestId { get; set; }
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
    public string? DriverInfo { get; set; }
    public bool? QuoteConfirm { get; set; }
    public int? Status { get; set; }
    public string? StatusText { get; set; }
    public string? StatusBadgeClass { get; set; }
    public List<TripRequestWorkflowStepViewModel> Steps { get; set; } = new();
    public List<TripRequestApprovalHistoryItemViewModel> Histories { get; set; } = new();
}

public class TripRequestWorkflowStepViewModel
{
    public string StepName { get; set; } = string.Empty;
    public string StateText { get; set; } = string.Empty;
    public string CssClass { get; set; } = string.Empty;
    public string? Actor { get; set; }
    public DateTime? ActionTime { get; set; }
    public string? Reason { get; set; }
}

public class TripRequestApprovalHistoryItemViewModel
{
    public string? Action { get; set; }
    public string? Actor { get; set; }
    public DateTime? ActionTime { get; set; }
    public string? Reason { get; set; }
    public bool? Result { get; set; }
}
