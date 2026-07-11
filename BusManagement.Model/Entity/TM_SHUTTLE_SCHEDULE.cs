using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_SHUTTLE_SCHEDULE")]
public partial class TM_SHUTTLE_SCHEDULE : AuditableEntity
{
    [Required]
    [MaxLength(200)]
    public string STR_ROUTE_NAME { get; set; } = string.Empty;

    public int INT_PICKUP_LOCATION_ID { get; set; }

    public int INT_DROPOFF_LOCATION_ID { get; set; }

    public TimeSpan TIM_DEPARTURE_TIME { get; set; }

    [MaxLength(500)]
    public string? STR_NOTE { get; set; }

    public bool BIT_IS_LAST_TRIP { get; set; }

    public bool BIT_IS_ACTIVE { get; set; } = true;
}
