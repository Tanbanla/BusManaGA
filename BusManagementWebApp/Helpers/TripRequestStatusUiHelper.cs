using BusManagement.Model.Common.Enum;

namespace BusManagementWebApp.Helpers;

public static class TripRequestStatusUiHelper
{
    public static string GetStatusText(int? status)
    {
        return status switch
        {
            TripRequestStatuses.PendingQlsc => "Chờ QLSC duyệt",
            TripRequestStatuses.QlscApproved => "Chờ QLTC duyệt",
            TripRequestStatuses.QlscRejected => "QLSC từ chối",
            TripRequestStatuses.QltcApproved => "Chờ GA báo giá",
            TripRequestStatuses.QltcRejected => "QLTC từ chối",
            TripRequestStatuses.QuoteSentByGa => "Chờ CNV phản hồi báo giá",
            TripRequestStatuses.EmployeeRejectedQuote => "CNV từ chối báo giá",
            TripRequestStatuses.EmployeeAcceptedQuote => "Chờ GA điền lái xe",
            TripRequestStatuses.Completed => "Hoàn thành",
            TripRequestStatuses.GaRejected => "GA từ chối",
            _ => "Chờ xử lý"
        };
    }

    public static string GetRowClass(int? status)
    {
        return status switch
        {
            TripRequestStatuses.Completed => "bus-status-row-approved",
            TripRequestStatuses.QlscRejected or TripRequestStatuses.QltcRejected or TripRequestStatuses.GaRejected or TripRequestStatuses.EmployeeRejectedQuote => "bus-status-row-rejected",
            TripRequestStatuses.PendingQlsc => "bus-status-row-pending",
            _ => "bus-status-row-processing"
        };
    }

    public static string GetBadgeClass(int? status)
    {
        return status switch
        {
            TripRequestStatuses.Completed => "bg-success",
            TripRequestStatuses.QlscRejected or TripRequestStatuses.QltcRejected or TripRequestStatuses.GaRejected or TripRequestStatuses.EmployeeRejectedQuote => "bg-danger",
            TripRequestStatuses.PendingQlsc => "bg-secondary",
            _ => "bg-primary"
        };
    }

    public static bool CanEditByOwner(int? status)
    {
        return status == TripRequestStatuses.PendingQlsc
               || status == TripRequestStatuses.QlscRejected
               || status == TripRequestStatuses.QltcRejected
               || status == TripRequestStatuses.GaRejected
               || status == TripRequestStatuses.EmployeeRejectedQuote;
    }

    public static bool CanSoftDeleteByOwner(int? status)
    {
        return status == TripRequestStatuses.PendingQlsc || status == TripRequestStatuses.QlscApproved;
    }
}
