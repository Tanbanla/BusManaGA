using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_STOP")]
public partial class TM_BUS_STOP : AuditableEntity
{
    public string? NVCHR_STOP_NAME { get; set; }
    public string? NVCHR_STOP_ADDRESS { get; set; }
    public string? NVCHR_STOP_RETURN_ADDRESS { get; set; }
    public string? NVCHR_STOP_CITY { get; set; }
    public string? NVCHR_STOP_WARD { get; set; }
    public string? CHR_COORDINATES { get; set; }
    public double? FLT_DISTANCE_TO_BIVN { get; set; }
    public double? FLT_DISTANCE_BIVN_TO_RETURN { get; set; }
    public double? FLT_DISTANCE_TO_TTF { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
