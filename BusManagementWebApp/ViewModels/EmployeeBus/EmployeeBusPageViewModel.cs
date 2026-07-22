using BusManagement.Model.Dtos;
using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.EmployeeBus;

public class EmployeeBusPageViewModel
{
    public string? EmployeeCode { get; set; }
    public string? EmployeeName { get; set; }
    public string? Department { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Ward { get; set; }
    public int? BusStopId { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public bool CanManage { get; set; }

    public List<TM_EMPLOYEEDTO> Employees { get; set; } = new();
    public List<TM_BUS_STOPDTO> BusStops { get; set; } = new();
    public Dictionary<int, TM_BUS_STOPDTO> BusStopMapForRouteDisplay { get; set; } = new();
    public List<TM_EMPLOYEE_ROUTEDTO> EmployeeRoutes { get; set; } = new();
    public List<EmployeeRouteAssignLookupItem> RouteAssignOptions { get; set; } = new();
    public Dictionary<int, string> PickupRouteMap { get; set; } = new();
    public Dictionary<int, string> ReturnRouteMap { get; set; } = new();
    public List<TM_SECTIONDTO> Sections { get; set; } = new();
    public List<TM_CITYDTO> Cities { get; set; } = new();
    public List<TM_WARDDTO> Wards { get; set; } = new();

    public string GetPickupRouteDisplay(int? routeAssignId)
    {
        if (!routeAssignId.HasValue || routeAssignId.Value <= 0) return "-";
        return PickupRouteMap.TryGetValue(routeAssignId.Value, out var text) && !string.IsNullOrWhiteSpace(text)
            ? text
            : "-";
    }

    public string GetReturnRouteDisplay(int? routeAssignId)
    {
        if (!routeAssignId.HasValue || routeAssignId.Value <= 0) return "-";
        return ReturnRouteMap.TryGetValue(routeAssignId.Value, out var text) && !string.IsNullOrWhiteSpace(text)
            ? text
            : "-";
    }

    public string GetDepartmentName(string? sectionCode)
    {
        if (string.IsNullOrWhiteSpace(sectionCode))
        {
            return string.Empty;
        }

        var section = Sections.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.CHR_CODE)
            && string.Equals(x.CHR_CODE.Trim(), sectionCode.Trim(), StringComparison.OrdinalIgnoreCase));

        return section?.CHR_NAME
            ?? section?.CHR_SECTION_NAME
            ?? sectionCode;
    }

    public string GetBusStopDisplayName(int? busStopId)
    {
        if (!busStopId.HasValue || busStopId.Value <= 0)
        {
            return string.Empty;
        }

        var busStop = BusStops.FirstOrDefault(x => x.ID == busStopId.Value);
        if (busStop == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(busStop.NVCHR_STOP_NAME))
        {
            return $"{busStop.NVCHR_STOP_NAME}";
        }

        return busStop.NVCHR_STOP_NAME
            ?? busStop.NVCHR_STOP_NAME
            ?? string.Empty;
    }

    public string GetEmployeeRouteStopsDisplay(int employeeId)
    {
        var route = EmployeeRoutes.FirstOrDefault(x => x.INT_EMPLOYEE_ID == employeeId && x.BIT_IS_ACTIVE);
        if (route == null)
        {
            return "-";
        }

        var pickupStopName = GetBusStopDisplayName(route.INT_PICKUP_STOP_ID);
        var returnStopName = GetBusStopDisplayName(route.INT_RETURN_STOP_ID);

        if (string.IsNullOrWhiteSpace(pickupStopName) && string.IsNullOrWhiteSpace(returnStopName))
        {
            return "-";
        }

        if (string.Equals(pickupStopName, returnStopName, StringComparison.OrdinalIgnoreCase))
        {
            return pickupStopName;
        }

        return $"Đón: {pickupStopName} / Trả: {returnStopName}";
    }

    public string GetEmployeePickupRouteDisplay(int employeeId)
    {
        var route = EmployeeRoutes.FirstOrDefault(x => x.INT_EMPLOYEE_ID == employeeId && x.BIT_IS_ACTIVE);
        if (route == null || !route.INT_ROUTE_ASSIGN_PICKUP_ID.HasValue)
        {
            return "-";
        }

        return GetPickupRouteDisplay(route.INT_ROUTE_ASSIGN_PICKUP_ID);
    }

    public string GetEmployeeReturnRouteDisplay(int employeeId)
    {
        var route = EmployeeRoutes.FirstOrDefault(x => x.INT_EMPLOYEE_ID == employeeId && x.BIT_IS_ACTIVE);
        if (route == null || !route.INT_ROUTE_ASSIGN_RETURN_ID.HasValue)
        {
            return "-";
        }

        return GetReturnRouteDisplay(route.INT_ROUTE_ASSIGN_RETURN_ID);
    }
}

public class EmployeeRouteAssignLookupItem
{
    public int Id { get; set; }
    public string? DisplayName { get; set; }
}
