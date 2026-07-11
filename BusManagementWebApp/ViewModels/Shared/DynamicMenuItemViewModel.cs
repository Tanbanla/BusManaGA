namespace BusManagement.ViewModels.Shared;

public class DynamicMenuItemViewModel
{
    public int Id { get; set; }
    public string? MenuCode { get; set; }
    public string? MenuName { get; set; }
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public string? Icon { get; set; }
    public int SortOrder { get; set; }
    public List<DynamicMenuItemViewModel> Children { get; set; } = new();
}
