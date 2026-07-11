namespace BusManagement.Model.Common.Enum
{
    public enum CardPrintType
    {
        NewCard = 1,          // In mới
        RePrint = 2,          // In lại
        LostCard = 3,         // Mất thẻ
        ChangeRoute = 4,      // Đổi tuyến
        RenewCard = 5         // Gia hạn
    }


    public enum CardPrintStatus
    {
        Waiting = 1,          // Chờ in
        Printing = 2,         // Đang in
        Printed = 3,          // Đã in
        Delivered = 4,        // Đã phát
        Cancelled = 5,        // Hủy
        PrintError = 6        // Lỗi in
    }

    public enum CardStatus
    {
        Waiting = 0, // Chờ in 
        Printed = 1,// Đã in 
        Using = 2, // Đang sử dụng 
        Lost = 3, //Thẻ mất 
        Cancel = 4,// Thẻ hủy
        Expried = 5// Thẻ hết hạn
    }

}
