using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_OT")] // Tuyến xe OT cố định (master)
public partial class TM_BUS_OT : AuditableEntity
{
    public string? NVCHR_BUS_OT_WORKING_NAME { get; set; }
    public int? INT_OT_TYPE { get; set; } // Loại OT: Ngày thường, Ngày nghỉ
    public string? CHR_ROUTE_TYPE { get; set; } // Hải phòng, Hà nội
    public string? CHR_TYPE { get; set; } // Công nhân, Nhân viên
    public string? CHR_SHIFT { get; set; } // Ca làm việc
    public string? CHR_SHIFT_TIME { get; set; } // Giờ của ca làm việc
    public string? CHR_SHIFT_NOTE { get; set; } // Note ca làm việc
    public int? INT_ROUTE_ID { get; set; }
    public int? INT_BUS_ID { get; set; }
    public int? INT_DRIVER_ID { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
