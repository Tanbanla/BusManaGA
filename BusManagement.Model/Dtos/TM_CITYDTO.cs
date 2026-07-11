namespace BusManagement.Model.Dtos;

public partial class TM_CITYDTO : AuditableDto
{
    public string? CHR_CITY_CODE { get; set; }
    public string? NVCHR_CITY_NAME { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
