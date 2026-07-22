namespace BusManagement.ViewModels.EmployeeBus;

public class EmployeeBusUpsertInputModel
{
    public int? ID { get; set; }

    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Ward { get; set; }
    public string? AddressDetail { get; set; }

    // Pickup stop and route
    public int? PickupStopId { get; set; }
    public int? PickupRouteAssignId { get; set; }

    // Return stop and route
    public int? ReturnStopId { get; set; }
    public int? ReturnRouteAssignId { get; set; }

    // Legacy fields (for backward compatibility)
    public int? BusStopId { get; set; }
    public int? RouteAssignId { get; set; }

    public bool? IsUseSameStopAndRoute { get; set; } = true; // Dùng chung điểm đón trả và tuyến xe
    public bool? IsActive { get; set; } = true;

    // Search fields
    public string? SearchEmployeeCode { get; set; }
    public string? SearchEmployeeName { get; set; }
    public string? SearchDepartment { get; set; }
    public string? SearchPhone { get; set; }
    public string? SearchCity { get; set; }
    public string? SearchWard { get; set; }
    public int? SearchBusStopId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    // Helper methods
    public int? GetEffectivePickupStopId()
    {
        return PickupStopId ?? BusStopId;
    }

    public int? GetEffectiveReturnStopId()
    {
        if (IsUseSameStopAndRoute == true)
        {
            return GetEffectivePickupStopId();
        }
        return ReturnStopId ?? BusStopId;
    }

    public int? GetEffectivePickupRouteId()
    {
        return PickupRouteAssignId ?? RouteAssignId;
    }

    public int? GetEffectiveReturnRouteId()
    {
        if (IsUseSameStopAndRoute == true)
        {
            return GetEffectivePickupRouteId();
        }
        return ReturnRouteAssignId ?? RouteAssignId;
    }
}
