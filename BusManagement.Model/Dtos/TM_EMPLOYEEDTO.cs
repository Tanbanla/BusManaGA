namespace BusManagement.Model.Dtos;

public partial class TM_EMPLOYEEDTO : AuditableDto
{
    public string? CHR_EMPLOYEE_ID { get; set; }
    public string? CHR_EMPLOYEE_ADID { get; set; }
    public string? CHR_EMPLOYEE_MAIL { get; set; }
    public string? NVCHR_EMPLOYEE_NAME { get; set; }
    public int? INT_BUS_STOP_ID { get; set; }
    public int? INT_BUS_ROUTE_ASSIGN_ID { get; set; }
    public string? CHR_SHIFT_CODE { get; set; }
    public string? CHR_CODE_DEPT { get; set; }
    public string? CHR_CODE_SEC { get; set; }
    public string? CHR_CODE_GROUP { get; set; }
    public string? CHR_POSITION { get; set; }
    public string? CHR_POSITION_GROUP { get; set; }
    public string? CHR_PHONE { get; set; }
    public DateTime? DTM_BIRTHDAY { get; set; }
    public string? CHR_ADDRESS { get; set; }
    public string? NVCHR_CITY { get; set; }
    public string? NVCHR_WARD { get; set; }
    public string? NVCHR_ADDRESS_DETAIL { get; set; }
    public string? NVCHR_SUGGESTED_ADDRESS { get; set; }
    public string? NVCHR_SUGGESTED_PICKUP_FROM_BIVN { get; set; }
    public bool? BIT_GENDER { get; set; }
    public bool? BIT_IS_USE_BUS { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
    public string? CHR_NOTE { get; set; }
}
