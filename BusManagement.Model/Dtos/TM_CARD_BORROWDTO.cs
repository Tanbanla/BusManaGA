namespace BusManagement.Model.Dtos;

public partial class TM_CARD_BORROWDTO : AuditableDto
{
    public int? INT_CARD_ID { get; set; }
    public string? CHR_CARD_CODE { get; set; }
    public string? CHR_EMPLOYEE_ID { get; set; }
    public DateTime? DTM_BORROW_TIME { get; set; }
    public DateTime? DTM_RETURN_TIME { get; set; }
}
