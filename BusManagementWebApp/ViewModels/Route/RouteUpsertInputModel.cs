namespace BusManagement.ViewModels.Route;

public class RouteUpsertInputModel
{
    public int? ID { get; set; }
    public string? Name { get; set; }
    public string? Type { get; set; }
    public string? Dept { get; set; }
    public string? Sec { get; set; }
    public int? RouteTypeId { get; set; }
    public int? Capacity { get; set; }
    public bool? IsActive { get; set; }

    // search context
    public string? SearchName { get; set; }
    public string? SearchType { get; set; }
    public string? SearchDept { get; set; }
    public bool? SearchIsActive { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
