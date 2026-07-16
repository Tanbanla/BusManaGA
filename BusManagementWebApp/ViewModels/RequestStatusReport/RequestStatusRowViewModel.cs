namespace BusManagement.ViewModels.RequestStatusReport
{
    public class RequestStatusRowViewModel
    {
        public int Id { get; set; }
        public string? RequestType { get; set; }
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? DepartmentName { get; set; }
        public DateTime CreatedDate { get; set; }
        public int StatusCode { get; set; }
        public string? StatusText { get; set; }
        public string? StatusBadgeClass { get; set; }
        public bool IsDelayed { get; set; }
        public string? CurrentApprover { get; set; }
        public string? DelayedApproverEmail { get; set; }
        public string? RequestSourceType { get; set; } // "BusRequest" or "TripRequest"
        // Approval details to help user navigate to approval page
        public string? ApprovalStage { get; set; }
        public string? ApprovalUrl { get; set; }
        public bool IsGaStep { get; set; }
        public int? HistoryId { get; set; } // For viewing history
        public bool CanViewHistory { get; set; }
        public string? HistoryUrl { get; set; }
    }
}
