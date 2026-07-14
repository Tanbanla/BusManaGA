using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_CARD_BORROW")]
public partial class TM_CARD_BORROW
{
    public int ID { get; set; }
    public int? INT_CARD_ID { get; set; }
    public string? CHR_CARD_CODE { get; set; }
    public string? CHR_EMPLOYEE_ID { get; set; }
    public DateTime? DTM_BORROW_TIME { get; set; }
    public DateTime? DTM_RETURN_TIME { get; set; }
    public bool? BIT_END { get; set; }
}
