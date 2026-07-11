using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_APPROVAL_HISTORY")]
public partial class TM_APPROVAL_HISTORY
{
    public int ID { get; set; }
    public int? INT_BUS_REQEST_ID { get; set; }
    public int? INT_TRIP_REQEST_ID { get; set; }
    public int? INT_GROUP_BATCH_APPLICATIONS { get; set; }
    public string? CHR_CONFIRMER_APPROVER { get; set; }
    public int? INT_ACTION { get; set; }
    public bool? BIT_ACTION { get; set; }
    public string? CHR_ACTION { get; set; }
    public DateTime? DTM_ACTION { get; set; }
    public string? NVCHR_REASON_REJECT { get; set; }
}
