using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BusManagement.Model.Entity;

[Table("TM_BUS_REQUEST_FAILED")]
public partial class TM_BUS_REQUEST_FAILED : AuditableEntity
{

    /// <summary>
    /// Mã nhân viên
    /// </summary>
    [StringLength(50)]
    public string CHR_EMPLOYEE_ID { get; set; } = string.Empty;

    /// <summary>
    /// Mã tuyến
    /// FK -> TM_BUS_ROUTE_ASSIGN.ID
    /// </summary>
    public int INT_ROUTE_ASSIGN_ID { get; set; }

    /// <summary>
    /// Mã điểm đón/trả
    /// FK -> TM_BUS_STOP.ID
    /// </summary>
    public int INT_BUS_STOP_ID { get; set; }

    /// <summary>
    /// Ngày bắt đầu sử dụng
    /// </summary>
    public DateTime? DTM_START_DATE { get; set; }

    /// <summary>
    /// Ngày hủy
    /// </summary>
    public DateTime? DTM_CANCEL_DATE { get; set; }

    /// <summary>
    /// Loại đơn đăng ký
    /// </summary>
    public int INT_REQUEST_TYPE { get; set; }

    /// <summary>
    /// Trạng thái đơn
    /// </summary>
    public int INT_STATUS { get; set; }

    /// <summary>
    /// QLSC phê duyệt
    /// </summary>
    [StringLength(50)]
    public string? CHR_QLSC_APPROVE { get; set; }

    /// <summary>
    /// GA xác nhận
    /// </summary>
    [StringLength(50)]
    public string? CHR_GA_CONFIRM { get; set; }

    /// <summary>
    /// Lý do thất bại
    /// </summary>
    [StringLength(1000)]
    public string? CHR_REASON_FAILED { get; set; }

    /// <summary>
    /// Ngày đăng ký
    /// </summary>
    public DateTime DTM_REQUEST_DATE { get; set; }

}
