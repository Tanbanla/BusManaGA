namespace BusManagement.ViewModels.BusOtAssign;

public class BusOtAssignRowViewModel
{
    public int ID { get; set; }
    public int? BusId { get; set; }
    public string? BusCode { get; set; }
    public string? BusName { get; set; }
    public int? DriverId { get; set; }
    public int? RouteId { get; set; }
    public string? RouteType { get; set; } // Hải phòng, Hà nội
    public string? Shift { get; set; } // Ca làm việc
    public string? ShiftInfo { get; set; } // Giờ của ca làm việc
    public string? ShiftNote { get; set; } // Note của ca làm việc (nhà máy)
    public string? RouteName { get; set; } // Lộ trình
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? LicensePlate { get; set; }
    public string? BusCapacity { get; set; }
    public DateTime? OtDate { get; set; }
    public string? Type { get; set; } // Loại xe: Nhân viên, Công nhân
    public int? CurrentUser { get; set; }
    public bool? IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public List<RouteStopOtViewModel>? RouteStops { get; set; } = new();
}

public class BusOtAssignPageViewModel
{
    public string? RouteType { get; set; }
    public string? Shift { get; set; }
    public string? BusCode { get; set; }
    public string? DriverName { get; set; }
    public string? LicensePlate { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }

    public List<BusOtAssignRowViewModel> OtAssigns { get; set; } = new();
    public List<IGrouping<string, BusOtAssignRowViewModel>> GroupedOtAssigns { get; set; } = new();
    public List<string> RouteTypes { get; set; } = new();
    public List<string> Shifts { get; set; } = new();

    public bool CanManage { get; set; }
}

public class BusOtAssignUpsertInputModel
{
    public int? ID { get; set; }
    public int? RouteId { get; set; }
    public int? BusId { get; set; }
    public int? DriverId { get; set; }
    public string? RouteType { get; set; }
    public string? Shift { get; set; }
    public string? ShiftTime { get; set; }
    public string? ShiftNote { get; set; }
    public DateTime? OtDate { get; set; }
    public int? OtType { get; set; } = 0; // Ngày thường = 0, mặc định
    public string? Type { get; set; } = "Nhân viên"; // Mặc định

    // Search filters
    public string? SearchRouteType { get; set; }
    public string? SearchShift { get; set; }
    public string? SearchBusCode { get; set; }
    public string? SearchDriverName { get; set; }
    public string? SearchLicensePlate { get; set; }
    public DateTime? SearchFromDate { get; set; }
    public DateTime? SearchToDate { get; set; }
}

public class RouteStopOtViewModel
{
    public int Order { get; set; }
    public int? StopId { get; set; }
    public string? StopName { get; set; }
    public string? StopAddress { get; set; }
    public TimeSpan? PickupTime { get; set; }
    public int? Turn { get; set; }
    public string? TurnName { get; set; }
}
