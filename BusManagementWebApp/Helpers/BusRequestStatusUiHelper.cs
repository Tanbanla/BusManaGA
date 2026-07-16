using BusManagement.Model.Common.Enum;
using BusManagement.Model.Entity;

namespace BusManagementWebApp.Helpers;

public static class BusRequestStatusUiHelper
{
    public static string GetStatusText(int? status)
    {
        return status switch
        {
            (int)BusRequestStatus.PendingQlscApproval => "Chưa xử lý",
            (int)BusRequestStatus.QlscApproved => "Đang xử lý",
            (int)BusRequestStatus.QlscRejected => "Bị từ chối",
            (int)BusRequestStatus.GaApproved => "Hoàn thành",
            (int)BusRequestStatus.GaRejected => "Bị từ chối",
            _ => "Chưa xử lý"
        };
    }

    public static string GetRowClass(int? status)
    {
        return status switch
        {
            (int)BusRequestStatus.PendingQlscApproval => "bus-status-row-pending",
            (int)BusRequestStatus.QlscApproved => "bus-status-row-processing",
            (int)BusRequestStatus.GaApproved => "bus-status-row-approved",
            (int)BusRequestStatus.QlscRejected or (int)BusRequestStatus.GaRejected => "bus-status-row-rejected",
            _ => "bus-status-row-pending"
        };
    }

    public static string GetBadgeClass(int? status)
    {
        return status switch
        {
            (int)BusRequestStatus.PendingQlscApproval => "bg-secondary",
            (int)BusRequestStatus.QlscApproved => "bg-primary",
            (int)BusRequestStatus.GaApproved => "bg-success",
            (int)BusRequestStatus.QlscRejected or (int)BusRequestStatus.GaRejected => "bg-danger",
            _ => "bg-secondary"
        };
    }

    public static bool IsEditableStatus(int? status)
    {
        return status == (int)BusRequestStatus.PendingQlscApproval
            || status == (int)BusRequestStatus.QlscRejected
            || status == (int)BusRequestStatus.GaRejected;
    }
}
