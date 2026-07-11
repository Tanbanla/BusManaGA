namespace BusManagement.Model.Dtos;

public partial class TM_BUS_EVALUATIONDTO : AuditableDto
{
    public int? INT_DRIVER_ID { get; set; }
    public int? INT_BUS_ID { get; set; }
    public string? CHR_BUS_CODE { get; set; }
    public int? INT_RATING { get; set; }
    public string? NVCHR_COMMENT { get; set; }
    public string? CHR_EVALUATION_TYPE { get; set; }
    public DateTime? DTM_EVALUATION_DATE { get; set; }
}
