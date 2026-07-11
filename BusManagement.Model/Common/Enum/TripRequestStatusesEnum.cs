namespace BusManagement.Model.Common.Enum
{
    public static class TripRequestStatuses
    {
        public const int Deleted = -1;
        public const int PendingQlsc = 0;
        public const int QlscApproved = 1;
        public const int QlscRejected = 2;
        public const int QltcApproved = 3;
        public const int QltcRejected = 4;
        public const int QuoteSentByGa = 5;
        public const int EmployeeRejectedQuote = 6;
        public const int EmployeeAcceptedQuote = 7;
        public const int Completed = 8;
        public const int GaRejected = 9;
    }
}
