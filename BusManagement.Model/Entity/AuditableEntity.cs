using System;
using System.ComponentModel.DataAnnotations;

namespace BusManagement.Model.Entity;

public abstract class AuditableEntity
{
    [Key]
    public int ID { get; set; }

    [Required]
    [MaxLength(100)]
    public string CHR_USER_CREATE { get; set; } = string.Empty;

    [Required]
    public DateTime DTM_CREATE { get; set; } = DateTime.Now;

    [MaxLength(100)]
    public string? CHR_USER_UPDATE { get; set; }

    public DateTime? DTM_UPDATE { get; set; }
}
