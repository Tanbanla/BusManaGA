using System.ComponentModel.DataAnnotations;

namespace BusManagement.Model.Dtos;

public partial class TM_CARD_PRINT_QUEUEDTO : AuditableDto
{
    /// <summary>
    /// Mã nhân viên
    /// </summary>
    [StringLength(50)]
    public string CHR_EMPLOYEE_ID { get; set; } = string.Empty;

    /// <summary>
    /// Mã tuyến xe đang hoạt động
    /// FK -> TM_EMPLOYEE_ROUTE.ID
    /// </summary>
    public int INT_EMPLOYEE_ROUTE_ID { get; set; }

    /// <summary>
    /// Mã đơn yêu cầu
    /// FK -> TM_BUS_REQUEST.ID
    /// </summary>
    public int INT_BUS_REQUEST_ID { get; set; }

    /// <summary>
    /// Mã thẻ
    /// FK -> TM_CARD.ID
    /// </summary>
    public int? INT_CARD_ID { get; set; }

    /// <summary>
    /// Loại in
    /// </summary>
    public int INT_PRINT_TYPE { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public int INT_STATUS { get; set; }

    /// <summary>
    /// Ngày yêu cầu
    /// </summary>
    public DateTime DTM_REQUEST_DATE { get; set; }

    /// <summary>
    /// Ngày in
    /// </summary>
    public DateTime? DTM_PRINT_DATE { get; set; }

    /// <summary>
    /// Người in
    /// </summary>
    [StringLength(50)]
    public string? CHR_PRINT_BY { get; set; }

    /// <summary>
    /// Ngày phát thẻ cho CNV
    /// </summary>
    public DateTime? DTM_RECEIVE_DATE { get; set; }

    /// <summary>
    /// Người nhận thẻ
    /// </summary>
    [StringLength(50)]
    public string? CHR_RECEIVE_BY { get; set; }

    /// <summary>
    /// Lý do
    /// </summary>
    [StringLength(1000)]
    public string? NVCHR_REASON { get; set; }
}
