using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.BusStop;

public class BusStopPageViewModel
{
    public string? City { get; set; }
    public string? Ward { get; set; }
    public string? StopName { get; set; }
    public bool? IsActive { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public List<TM_BUS_STOPDTO> BusStops { get; set; } = new();
    public List<TM_CITYDTO> Cities { get; set; } = new();
    public List<TM_WARDDTO> Wards { get; set; } = new();
}
