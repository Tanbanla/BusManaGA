namespace BusManagement.Model.Dtos;

public partial class TM_CARD_USAGEDTO : AuditableDto
{
    public int? INT_CARD_ID { get; set; }
    public string? CHR_CARD_CODE { get; set; }
    public string? CHR_EMPLOYEE_ID { get; set; }
    public DateTime? DTM_USE_DATE { get; set; }
    public string? NVCHR_FROM_ADDRESS { get; set; }
    public string? NVCHR_TO_ADDRESS { get; set; }
    public int? INT_AMOUNT { get; set; }
    public string? NVCHR_NOTE { get; set; }
}
