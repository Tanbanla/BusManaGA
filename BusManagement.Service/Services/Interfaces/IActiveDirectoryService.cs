using System.Security.Claims;

namespace BusManagement.Service.Services.Interfaces;

public interface IActiveDirectoryService
{
    /// <summary>
    /// Kiểm tra xem user có quyền xử lý với email xác nhận không
    /// </summary>
    /// <param name="userEmail">Email của user đang đăng nhập</param>
    /// <param name="confirmEmail">Email xác nhận (có thể là user hoặc DL)</param>
    /// <returns>True nếu có quyền, False nếu không</returns>
    Task<bool> CanUserProcessConfirmationAsync(string userEmail, string confirmEmail);

    /// <summary>
    /// Kiểm tra user có thuộc mail group không
    /// </summary>
    /// <param name="username">Username (AD ID) của user không có domain prefix</param>
    /// <param name="groupMail">Email của group cần kiểm tra</param>
    /// <returns>True nếu user thuộc group</returns>
    bool IsCurrentUserInMailGroup(string username, string groupMail);

    /// <summary>
    /// Kiểm tra user có thuộc mail group không sử dụng ClaimsPrincipal
    /// </summary>
    /// <param name="groupMail">Email của group</param>
    /// <param name="user">ClaimsPrincipal của user</param>
    /// <returns>True nếu user thuộc group</returns>
    bool IsCurrentUserInMailGroup(string groupMail, ClaimsPrincipal user);
}
