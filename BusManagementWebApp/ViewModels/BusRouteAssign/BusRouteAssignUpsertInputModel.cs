namespace BusManagement.ViewModels.BusRouteAssign;

public class BusRouteAssignUpsertInputModel
{
    public int? ID { get; set; }
    public string? BusRouteAssignWorkingName { get; set; }
    public int? BusId { get; set; }
    public int? RoutePickupId { get; set; }
    public int? RouteReturnId { get; set; }
    public int? DriverId { get; set; }
    public DateTime? EffectiveDate { get; set; }

    // Search filters
    public string? SearchBusCode { get; set; }
    public string? SearchDriverName { get; set; }
    public string? SearchLicensePlate { get; set; }
    public bool? SearchIsActive { get; set; }
}
