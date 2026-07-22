using System.ComponentModel.DataAnnotations;

namespace BusManagement.Model.Dtos;

public partial class TM_EMPLOYEE_ROUTE_HISTORYDTO : AuditableDto
{
    /// <summary>
    /// Mã nhân viên (ID)
    /// </summary>
    public int INT_EMPLOYEE_ID { get; set; }

    [StringLength(50)]
    public string CHR_EMPLOYEE_ID { get; set; } = string.Empty;

    /// <summary>
    /// Mã đơn đăng ký
    /// </summary>
    public int INT_REQUEST_ID { get; set; }

    /// <summary>
    /// Mã tuyến xe cũ (chiều đi)
    /// </summary>
    public int? INT_OLD_ROUTE_ASSIGN_PICKUP_ID { get; set; }

    /// <summary>
    /// Mã tuyến xe mới (chiều đi)
    /// </summary>
    public int? INT_NEW_ROUTE_ASSIGN_PICKUP_ID { get; set; }

    /// <summary>
    /// Mã tuyến xe cũ (chiều về)
    /// </summary>
    public int? INT_OLD_ROUTE_ASSIGN_RETURN_ID { get; set; }

    /// <summary>
    /// Mã tuyến xe mới (chiều về)
    /// </summary>
    public int? INT_NEW_ROUTE_ASSIGN_RETURN_ID { get; set; }

    public int? INT_OLD_PICKUP_TURN_ID { get; set; } = 1; // Chiều đi: thuộc lượt nào (mặc định chỉ có 1 lượt)
    public int? INT_NEW_PICKUP_TURN_ID { get; set; } = 1; // Chiều đi: thuộc lượt nào (mặc định chỉ có 1 lượt)
    public int? INT_OLD_RETURN_TURN_ID { get; set; } // Chiều về: thuộc lượt nào
    public int? INT_NEW_RETURN_TURN_ID { get; set; } // Chiều về: thuộc lượt nào

    /// <summary>
    /// Điểm đón cũ
    /// </summary>
    public int? INT_OLD_PICKUP_STOP_ID { get; set; }

    /// <summary>
    /// Điểm đón mới
    /// </summary>
    public int? INT_NEW_PICKUP_STOP_ID { get; set; }

    /// <summary>
    /// Điểm trả cũ
    /// </summary>
    public int? INT_OLD_RETURN_STOP_ID { get; set; }

    /// <summary>
    /// Điểm trả mới
    /// </summary>
    public int? INT_NEW_RETURN_STOP_ID { get; set; }

    /// <summary>
    /// Tuyến mới có hiệu lực từ
    /// </summary>
    public DateTime DTM_NEW_EFFECTIVE_FROM { get; set; }

    /// <summary>
    /// Loại thay đổi:
    /// NEW_REGISTER
    /// CHANGE_ROUTE
    /// CHANGE_STOP
    /// CANCEL_ROUTE
    /// REACTIVE
    /// </summary>
    [StringLength(50)]
    public string NVCHR_ACTION_TYPE { get; set; } = string.Empty;
}
