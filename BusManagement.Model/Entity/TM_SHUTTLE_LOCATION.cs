using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_SHUTTLE_LOCATION")]
public partial class TM_SHUTTLE_LOCATION : AuditableEntity
{
    [Required]
    [MaxLength(20)]
    public string STR_LOCATION_CODE { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string STR_LOCATION_NAME { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? STR_DESCRIPTION { get; set; }

    [MaxLength(20)]
    public string? STR_BUILDING_CODE { get; set; }

    [MaxLength(20)]
    public string? STR_FLOOR_CODE { get; set; }

    public decimal? DEC_X_COORDINATE { get; set; }

    public decimal? DEC_Y_COORDINATE { get; set; }

    public bool BIT_IS_ACTIVE { get; set; } = true;
}
