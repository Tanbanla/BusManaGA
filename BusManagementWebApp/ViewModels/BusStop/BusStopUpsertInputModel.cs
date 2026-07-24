namespace BusManagement.ViewModels.BusStop;

public class BusStopUpsertInputModel
{
    public int? ID { get; set; }

    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Ward { get; set; }
    public string? Coordinates { get; set; }
    public double? DistanceToBivn { get; set; }
    public double? DistanceToTtf { get; set; }
    public string? ReturnAddress { get; set; }
    public double? DistanceBivnToReturn { get; set; }
    public bool? IsActive { get; set; } = true;

    public string? SearchCity { get; set; }
    public string? SearchWard { get; set; }
    public string? SearchStopName { get; set; }
    public bool? SearchIsActive { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
