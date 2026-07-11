using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.Bus;

public class BusPageViewModel
{
    public string? BusCode { get; set; }
    public string? BusName { get; set; }
    public string? LicensePlate { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? ModelYear { get; set; }
    public int? Capacity { get; set; }
    public int? VendorId { get; set; }
    public bool? IsActive { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public List<TM_BUSDTO> Buses { get; set; } = new();
    public List<TM_VENDORDTO> Vendors { get; set; } = new();
    public List<TM_DRIVERDTO> Drivers { get; set; } = new();
    public Dictionary<int, string> VendorNamesById { get; set; } = new();
    public Dictionary<int, string> DriverNamesById { get; set; } = new();
    public Dictionary<int, List<TM_BUS_EVALUATIONDTO>> EvaluationsByBusId { get; set; } = new();
}
