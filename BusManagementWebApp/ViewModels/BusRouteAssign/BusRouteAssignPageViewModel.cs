using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.BusRouteAssign;

public class BusRouteAssignPageViewModel
{
    public string? BusRouteAssignWorkingName { get; set; }
    public string? BusCode { get; set; }
    public string? RouteName { get; set; }
    public string? DriverName { get; set; }
    public string? LicensePlate { get; set; }
    public bool? IsActive { get; set; }

    public bool CanManage { get; set; }

    public List<BusRouteAssignViewModel> RouteAssigns { get; set; } = new();
}

public class BusRouteAssignViewModel
{
    public int ID { get; set; }
    public string? BusRouteAssignWorkingName { get; set; }
    public int? BusId { get; set; }
    public string? BusCode { get; set; }
    public string? BusName { get; set; }
    public string? LicensePlate { get; set; }
    public string? BusCapacity { get; set; }
    public string? CodeDept { get; set; }
    public int? DriverId { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }

    // Separate routes for pickup and return
    public int? PickupRouteId { get; set; }
    public string? PickupRouteName { get; set; }
    public int? ReturnRouteId { get; set; }
    public string? ReturnRouteName { get; set; }

    // Pickup route stops (chiều đi)
    public List<RouteStopInfoViewModel> PickupStops { get; set; } = new();

    // Return route stops (chiều về)
    public List<RouteStopInfoViewModel> ReturnStops { get; set; } = new();

    public DateTime? EffectiveDate { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
}

public class RouteStopInfoViewModel
{
    public int Order { get; set; }
    public string? StopName { get; set; }
    public string? StopAddress { get; set; }
    public TimeSpan? PickupTime { get; set; }
    public string? RouteType { get; set; }
    // Turn metadata (lượt)
    public int? Turn { get; set; }
    public string? TurnName { get; set; }
}
