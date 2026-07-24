using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_OT_ASSIGN")] // Tuyến xe OT đang hoạt động
public partial class TM_BUS_OT_ASSIGN : AuditableEntity
{
    public string? NVCHR_BUS_OT_WORKING_NAME { get; set; }
    public int? INT_BUS_OT_ID { get; set; } // Clone từ TM_BUS_OT hay tạo mới
    public int? INT_OT_TYPE { get; set; } // Loại OT: Ngày thường, Ngày nghỉ
    public string? CHR_ROUTE_TYPE { get; set; } // Hải phòng, Hà nội
    public string? CHR_TYPE { get; set; } // Công nhân, Nhân viên
    public string? CHR_SHIFT { get; set; } // Ca làm việc
    public string? CHR_SHIFT_TIME { get; set; } // Giờ của ca làm việc
    public string? CHR_SHIFT_NOTE { get; set; } // Note ca làm việc
    public DateTime? DTM_DATE { get; set; } // Ngày OT
    public int? INT_CAPACITY { get; set; } // dung lượng xe
    public int? INT_CURRENT_USER { get; set; } // Số người đang sử dụng
    public int? INT_ROUTE_ID { get; set; }
    public int? INT_BUS_ID { get; set; }
    public int? INT_DRIVER_ID { get; set; }
    public bool? BIT_IS_ACTIVE { get; set; }
}
