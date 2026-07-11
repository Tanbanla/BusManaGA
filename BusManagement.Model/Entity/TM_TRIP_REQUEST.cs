using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_TRIP_REQUEST")]
public partial class TM_TRIP_REQUEST : AuditableEntity
{
    public string? CHR_EMPLOYEE_ID { get; set; }
    public string? CHR_TRIP_TYPE { get; set; }
    public string? NVCHR_TRIP_TYPE { get; set; }
    public DateTime? DTM_PICKUP_GO { get; set; }
    public DateTime? DTM_PICKUP_RETURN { get; set; }
    public string? NVCHR_FROM_LOCATION_GO { get; set; }
    public string? NVCHR_TO_LOCATION_GO { get; set; }
    public string? NVCHR_FROM_LOCATION_RETURN { get; set; }
    public string? NVCHR_TO_LOCATION_RETURN { get; set; }
    public string? NVCHR_PURPOSE { get; set; }
    public int? INT_PASSENGER_COUNT { get; set; }
    public string? CHR_QLSC_APPROVE { get; set; }
    public string? CHR_QLTC_APPROVE { get; set; }
    public string? CHR_GA_CONFIRM { get; set; }
    public int? INT_GROUP_BATCH_APPLICATIONS { get; set; }
    public string? CHR_QUOTE_PATH { get; set; }
    public bool? BIT_QUOTE_CONFIRM { get; set; }
    public string? NVCHR_DRIVER_INFO { get; set; }
    public int? INT_STATUS { get; set; }
    public string? CHR_STATUS { get; set; }
}
