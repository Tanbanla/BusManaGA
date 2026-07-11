using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.Admin;

public class CityPageViewModel
{
    public string? SearchCode { get; set; }
    public string? SearchName { get; set; }
    public bool? IsActive { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }

    public List<TM_CITYDTO> Cities { get; set; } = new();
}
