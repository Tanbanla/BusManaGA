using System.Collections.Generic;
using BusManagement.Model.Dtos;

namespace BusManagement.ViewModels.Route;

public class RoutePageViewModel
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Dept { get; set; }
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool CanManage { get; set; }
    public List<TM_BUS_ROUTEDTO> Routes { get; set; } = new();
}
