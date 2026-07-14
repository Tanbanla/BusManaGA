namespace BusManagement.ViewModels.BusCapacityReport
{
    public class BusCapacityReportRowViewModel
    {
        public int Id { get; set; }
        public string? RouteDisplay { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? VehicleType { get; set; }
        public int Capacity { get; set; }
        public int CurrentUsed { get; set; }
        public int AvailableSeats { get; set; }
        public double UsagePercent { get; set; }
        public string? Note { get; set; }
        public bool IsFullCapacity { get; set; }
        public bool HasAvailableSeats { get; set; }
        public string? CapacityStatusClass { get; set; }
        public string? NoteStatusClass { get; set; }
    }
}
