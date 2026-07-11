using System.ComponentModel.DataAnnotations;

namespace BusManagement.Model.Dtos;

public abstract class AuditableDto
{
    public int ID { get; set; }

    public string CHR_USER_CREATE { get; set; } = string.Empty;

    public DateTime DTM_CREATE { get; set; } = DateTime.Now;

    public string? CHR_USER_UPDATE { get; set; }

    public DateTime? DTM_UPDATE { get; set; }
}
