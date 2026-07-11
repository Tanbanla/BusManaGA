namespace BusManagement.Model.Dtos;

public partial class TM_WARDDTO : AuditableDto
{
    public int? INT_CITY_ID { get; set; }
    public string? CHR_WARD_CODE { get; set; }
    public string? NVCHR_WARD_NAME { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
