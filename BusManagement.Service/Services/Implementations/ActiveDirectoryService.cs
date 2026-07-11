using BusManagement.Service.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.DirectoryServices.AccountManagement;
using System.Security.Claims;

namespace BusManagement.Service.Services.Implementations
{
    public class ActiveDirectoryService : IActiveDirectoryService
    {
        private readonly ILogger<ActiveDirectoryService> _logger;
        private readonly string _adContainer;
        private static readonly Dictionary<string, (bool Result, DateTime CachedAt)> _permissionCache = new();
        private static readonly object _cacheLock = new object();
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5); // Cache 5 phút

        public ActiveDirectoryService(ILogger<ActiveDirectoryService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _adContainer = configuration["ActiveDirectory:Container"] ?? @"OU=AP BIVN User,OU=AP UserOU,DC=ap,DC=brothergroup,DC=net";
        }

        /// <summary>
        /// Kiểm tra xem user có quyền xử lý với email xác nhận không
        /// </summary>
        public async Task<bool> CanUserProcessConfirmationAsync(string userEmail, string confirmEmail)
        {
            try
            {
                if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(confirmEmail))
                {
                    return false;
                }

                // Normalize inputs để tránh FormatException
                var normalizedUserEmail = userEmail.Trim().ToLower();
                var normalizedConfirmEmail = confirmEmail.Trim().ToLower();

                // Lấy username từ email
                var currentUsername = normalizedUserEmail.Contains("@") ? normalizedUserEmail.Split('@')[0] : normalizedUserEmail;
                var confirmUsername = normalizedConfirmEmail.Contains("@") ? normalizedConfirmEmail.Split('@')[0] : normalizedConfirmEmail;

                // Kiểm tra trực tiếp trước
                if (normalizedUserEmail.Equals(normalizedConfirmEmail, StringComparison.OrdinalIgnoreCase) ||
                    currentUsername.Equals(confirmUsername, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                // Check cache trước khi gọi AD
                var cacheKey = $"{currentUsername}_{normalizedConfirmEmail}";
                lock (_cacheLock)
                {
                    if (_permissionCache.TryGetValue(cacheKey, out var cached))
                    {
                        if (DateTime.Now - cached.CachedAt < CacheExpiry)
                        {
                            _logger.LogDebug("Cache hit for permission check: {CacheKey} = {Result}", cacheKey, cached.Result);
                            return cached.Result;
                        }
                        else
                        {
                            _permissionCache.Remove(cacheKey);
                        }
                    }
                }

                // Chỉ khi cần thiết mới check AD group (expensive operation)
                bool result = false;
                if (normalizedConfirmEmail.Contains("@") || normalizedConfirmEmail.Contains(" "))
                {
                    // Có thể là group email hoặc display name, cần check AD
                    result = await Task.Run(() => IsCurrentUserInMailGroup(currentUsername, normalizedConfirmEmail));
                }
                else
                {
                    // Fallback cho các trường hợp đặc biệt
                    result = currentUsername.Equals(normalizedConfirmEmail, StringComparison.OrdinalIgnoreCase);
                }

                // Cache kết quả
                lock (_cacheLock)
                {
                    _permissionCache[cacheKey] = (result, DateTime.Now);

                    // Cleanup old cache entries nếu cache quá lớn
                    if (_permissionCache.Count > 1000)
                    {
                        var expiredKeys = _permissionCache
                            .Where(kvp => DateTime.Now - kvp.Value.CachedAt > CacheExpiry)
                            .Select(kvp => kvp.Key)
                            .ToList();

                        foreach (var key in expiredKeys)
                        {
                            _permissionCache.Remove(key);
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user confirmation permission for user: {UserEmail}, confirm: {ConfirmEmail}", userEmail, confirmEmail);

                // Safe fallback
                try
                {
                    var userPart = userEmail?.Contains("@") == true ? userEmail.Split('@')[0] : userEmail ?? "";
                    var confirmPart = confirmEmail?.Contains("@") == true ? confirmEmail.Split('@')[0] : confirmEmail ?? "";
                    return userPart.Equals(confirmPart, StringComparison.OrdinalIgnoreCase);
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Kiểm tra user có thuộc mail group không
        /// </summary>
        /// <param name="username">Username (AD ID) của user không có domain prefix</param>
        /// <param name="groupMail">Email của group cần kiểm tra</param>
        /// <returns>True nếu user thuộc group</returns>
        public bool IsCurrentUserInMailGroup(string username, string groupMail)
        {
            PrincipalContext ctx = null;
            UserPrincipal userPrincipal = null;

            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(groupMail))
                {
                    return false;
                }

                // Sanitize inputs để tránh FormatException
                var cleanUsername = username.Trim();
                var cleanGroupMail = groupMail.Trim();

                // Validate username format
                if (cleanUsername.Contains("@") || cleanUsername.Contains("\\") || cleanUsername.Length > 50)
                {
                    _logger.LogWarning("Invalid username format: {Username}", cleanUsername);
                    return false;
                }

                ctx = new PrincipalContext(ContextType.Domain, "AP", _adContainer);

                // Tìm user bằng username với timeout
                userPrincipal = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, cleanUsername);

                if (userPrincipal == null)
                {
                    _logger.LogWarning("User not found in AD: {Username}", cleanUsername);
                    return false;
                }

                // Optimize: Chỉ lấy groups cần thiết và có timeout
                var groups = userPrincipal.GetGroups();

                // Check nhanh bằng SamAccountName hoặc Name trước
                foreach (var group in groups)
                {
                    try
                    {
                        if (group.SamAccountName != null &&
                            (group.SamAccountName.Equals(cleanGroupMail, StringComparison.OrdinalIgnoreCase) ||
                             (group.Name != null && group.Name.Equals(cleanGroupMail, StringComparison.OrdinalIgnoreCase))))
                        {
                            _logger.LogInformation("User {Username} is member of group {GroupMail}", cleanUsername, cleanGroupMail);
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Skip problematic groups và continue
                        _logger.LogDebug(ex, "Error checking group {GroupName} for user {Username}", group.SamAccountName ?? "Unknown", cleanUsername);
                        continue;
                    }
                }

                _logger.LogInformation("User {Username} is NOT member of any group with email {GroupMail}", cleanUsername, cleanGroupMail);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking group membership for user: {Username}, group: {GroupMail}", username, groupMail);
                return false;
            }
            finally
            {
                // Proper cleanup để tránh memory leaks
                userPrincipal?.Dispose();
                ctx?.Dispose();
            }
        }

        /// <summary>
        /// Kiểm tra user có thuộc mail group không sử dụng ClaimsPrincipal
        /// </summary>
        /// <param name="groupMail">Email của group</param>
        /// <param name="user">ClaimsPrincipal của user</param>
        /// <returns>True nếu user thuộc group</returns>
        public bool IsCurrentUserInMailGroup(string groupMail, ClaimsPrincipal user)
        {
            try
            {
                string sam = user.Identity.Name; // DOMAIN\username
                if (string.IsNullOrEmpty(sam))
                {
                    return false;
                }

                // Lấy username từ DOMAIN\username format
                string username = sam.Contains("\\") ? sam.Split('\\').Last() : sam;

                return IsCurrentUserInMailGroup(username, groupMail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking group membership for user: {user.Identity?.Name}, group: {groupMail}");
                return false;
            }
        }
    }
}
