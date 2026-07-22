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
        // General aggregate (kept for backward compatibility / worker view)
        public int CurrentUsed { get; set; }
        public int AvailableSeats { get; set; }
        public double UsagePercent { get; set; }

        // Detailed fields for staff view (pickup/return)
        public string? PickupRouteDisplay { get; set; }
        public string? ReturnRouteDisplay { get; set; }
        public int PickupUsed { get; set; }
        public int PickupAvailable { get; set; }
        public double PickupUsagePercent { get; set; }
        public int ReturnUsed { get; set; }
        public int ReturnAvailable { get; set; }
        public double ReturnUsagePercent { get; set; }
        public string? Note { get; set; }
        public bool IsFullCapacity { get; set; }
        public bool HasAvailableSeats { get; set; }
        public string? CapacityStatusClass { get; set; }
        public string? NoteStatusClass { get; set; }
    }
}
