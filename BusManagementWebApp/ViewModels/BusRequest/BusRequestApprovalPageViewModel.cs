namespace BusManagement.ViewModels.BusRequest;

public class BusRequestApprovalPageViewModel
{
    public string Stage { get; set; } = "qlsc";
    public string StageTitle { get; set; } = string.Empty;
    public bool CanProcess { get; set; }
    public string? AccessDeniedMessage { get; set; }
    public List<BusRequestApprovalRowViewModel> Requests { get; set; } = new();
}

public class BusRequestApprovalRowViewModel
{
    public int Id { get; set; }
    public string? DepartmentName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Position { get; set; }
    public string? Address { get; set; }
    public string? RequestTypeName { get; set; }
    public string? RouteDisplay { get; set; }
    public string? BusStopDisplay { get; set; }
    public DateTime? UseOrCancelDate { get; set; }
    public string? Note { get; set; }
    public string? CreatorAdid { get; set; }
    public int? Status { get; set; }
    public string? StatusText { get; set; }
    public string? StatusRowClass { get; set; }
    public string? StatusBadgeClass { get; set; }
}

public class BusRequestApprovalActionInputModel
{
    public string Stage { get; set; } = "qlsc";
    public string? SelectedIds { get; set; }
    public string? RejectReason { get; set; }
}

public class BusRequestApprovalHistoryPageViewModel
{
    public int RequestId { get; set; }
    public string? DepartmentName { get; set; }
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Position { get; set; }
    public string? Address { get; set; }
    public string? RequestTypeName { get; set; }
    public string? RouteDisplay { get; set; }
    public string? BusStopDisplay { get; set; }
    public DateTime? UseOrCancelDate { get; set; }
    public string? Note { get; set; }
    public int? Status { get; set; }
    public string? StatusText { get; set; }
    public string? StatusBadgeClass { get; set; }
    public List<BusRequestWorkflowStepViewModel> Steps { get; set; } = new();
    public List<BusRequestApprovalHistoryItemViewModel> Histories { get; set; } = new();
}

public class BusRequestWorkflowStepViewModel
{
    public string StepName { get; set; } = string.Empty;
    public string State { get; set; } = "pending";
    public string StateText { get; set; } = string.Empty;
    public string CssClass { get; set; } = string.Empty;
    public string? Actor { get; set; }
    public DateTime? ActionTime { get; set; }
    public string? Reason { get; set; }
}

public class BusRequestApprovalHistoryItemViewModel
{
    public string? Action { get; set; }
    public string? Actor { get; set; }
    public DateTime? ActionTime { get; set; }
    public string? Reason { get; set; }
    public bool? Result { get; set; }
}
