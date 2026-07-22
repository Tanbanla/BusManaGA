namespace BusManagement.Model.Dtos;

public partial class TM_CARD_USAGEDTO : AuditableDto
{
    public int? INT_BORROW_ID { get; set; }
    public string? CHR_EMPLOYEE_ID { get; set; }
    public string? NVCHR_EMPLOYEE_NAME { get; set; }
    public DateTime? DTM_USE_DATE { get; set; }
    public string? NVCHR_FROM_ADDRESS { get; set; }
    public string? NVCHR_TO_ADDRESS { get; set; }
    public int? INT_AMOUNT { get; set; }
    public string? NVCHR_NOTE { get; set; }
    public string? CHR_SEC_COST { get; set; }
    public string? CHR_TRAN_CODE { get; set; }
    public int? INT_NUMBER_MOVE { get; set; }
    public bool? BIT_DELETE { get; set; }
    public string? NVCHR_REASON { get; set; }
}
