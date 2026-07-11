namespace BusManagement.ViewModels.Bus;

public class BusUpsertInputModel
{
    public int? ID { get; set; }

    public string? BusCode { get; set; }
    public string? BusName { get; set; }
    public string? LicensePlate { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? ModelYear { get; set; }
    public int? Capacity { get; set; }
    public int? VendorId { get; set; }
    public bool? IsActive { get; set; } = true;

    public string? SearchBusCode { get; set; }
    public string? SearchBusName { get; set; }
    public string? SearchLicensePlate { get; set; }
    public string? SearchCategory { get; set; }
    public string? SearchBrand { get; set; }
    public string? SearchModel { get; set; }
    public string? SearchModelYear { get; set; }
    public int? SearchCapacity { get; set; }
    public int? SearchVendorId { get; set; }
    public bool? SearchIsActive { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
