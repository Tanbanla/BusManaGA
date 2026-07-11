using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_CARD")]
public partial class TM_CARD : AuditableEntity
{
    public string? CHR_EMPLOYEE_ID { get; set; }
    public string? CHR_CARD_CODE { get; set; }
    public int? INT_CARD_TYPE { get; set; }
    public string? NVCHR_CARD_TYPE { get; set; }
    public int? INT_CARD_SCOPE { get; set; }
    public string? NVCHR_CARD_SCOPE { get; set; }
    public string? CHR_BUS_CODE { get; set; }
    public int? INT_BUS_ID { get; set; }
    public int? INT_TAXI_ID { get; set; }
    public int? INT_BUS_STOP_ID { get; set; }
    public DateTime? DTM_ISSUE_DATE { get; set; }
    public DateTime? DTM_EXPRIRATION_DATE { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
