using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.Admin;

public class WardPageViewModel
{
    public int? CityId { get; set; }
    public string? CityName { get; set; }
    public string? WardName { get; set; }
    public bool? IsActive { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public List<TM_WARDDTO> Wards { get; set; } = new();
    public List<TM_CITYDTO> Cities { get; set; } = new();
}
