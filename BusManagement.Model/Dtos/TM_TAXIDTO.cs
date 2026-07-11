namespace BusManagement.Model.Dtos;

public partial class TM_TAXIDTO : AuditableDto
{
    public string? CHR_LICENSE_PLATE { get; set; }
    public string? NVCHR_CATEGORY { get; set; }
    public string? NVCHR_BRAND { get; set; }
    public int? INT_CAPACITY { get; set; }
    public int? INT_DRIVER { get; set; }
    public string? NVCHR_DRIVER { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
