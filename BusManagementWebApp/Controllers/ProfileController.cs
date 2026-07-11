using Microsoft.AspNetCore.Mvc;
using BusManagement.Services.UserContext;
using BusManagement.Service.Services.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Controllers;

public class ProfileController : Controller
{
    private readonly ICurrentUserProvider _currentUserProvider;
    private readonly ITmUserService _tmUserService;
    private readonly ITmRoleService _tmRoleService;

    public ProfileController(
        ICurrentUserProvider currentUserProvider,
        ITmUserService tmUserService,
        ITmRoleService tmRoleService)
    {
        _currentUserProvider = currentUserProvider;
        _tmUserService = tmUserService;
        _tmRoleService = tmRoleService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile()
    {
        var currentUser = _currentUserProvider.Current;
        if (currentUser == null)
        {
            return Json(new { success = false, message = "Not authenticated" });
        }

        var localUser = await GetCurrentUserAsync(currentUser.Adid, currentUser.EmployeeCode);
        if (localUser == null)
        {
            return Json(new { success = false, message = "User not found." });
        }

        var roleName = await GetRoleNameAsync(localUser.INT_ROLE_ID);

        return Json(new
        {
            success = true,
            data = new
            {
                EmployeeCode = localUser.CHR_EMPLOYEE_ID,
                EmployeeName = localUser.NVCHR_NAME,
                Position = localUser.CHR_POSITION,
                SectionCode = localUser.CHR_SEC_CODE,
                UserId = localUser.ID,
                AvatarPath = localUser.CHR_AVATAR_PATH,
                AvatarUrl = localUser.CHR_AVATAR_PATH,
                RoleId = localUser.INT_ROLE_ID,
                RoleName = roleName
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateMyProfile(IFormFile? avatarFile)
    {
        var currentUser = _currentUserProvider.Current;
        if (currentUser == null)
        {
            return Json(new { success = false, message = "Not authenticated" });
        }

        var localUser = await GetCurrentUserAsync(currentUser.Adid, currentUser.EmployeeCode);
        if (localUser == null)
        {
            return Json(new { success = false, message = "User not found." });
        }

        if (avatarFile != null && avatarFile.Length > 0)
        {
            var avatarUrl = SaveAvatarFile(avatarFile);
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return Json(new { success = false, message = "Không thể lưu avatar." });
            }

            localUser.CHR_AVATAR_PATH = avatarUrl;
        }

        localUser.CHR_USER_UPDATE = currentUser.Adid;
        localUser.DTM_UPDATE = DateTime.Now;

        var updateEntity = new TM_USER
        {
            ID = localUser.ID,
            CHR_EMPLOYEE_ID = localUser.CHR_EMPLOYEE_ID,
            CHR_ADID = localUser.CHR_ADID,
            NVCHR_NAME = localUser.NVCHR_NAME,
            CHR_PASSWORD = localUser.CHR_PASSWORD,
            CHR_SEC_CODE = localUser.CHR_SEC_CODE,
            CHR_SEC_NAME = localUser.CHR_SEC_NAME,
            CHR_POSITION = localUser.CHR_POSITION,
            INT_ROLE_ID = localUser.INT_ROLE_ID,
            DTM_LAST_LOGIN = localUser.DTM_LAST_LOGIN,
            INT_LOCK = localUser.INT_LOCK,
            INT_LOCK_DAY = localUser.INT_LOCK_DAY,
            CHR_NOTE = localUser.CHR_NOTE,
            CHR_AVATAR_PATH = localUser.CHR_AVATAR_PATH,
            BIT_IS_ACTIVE = localUser.BIT_IS_ACTIVE,
            CHR_USER_CREATE = localUser.CHR_USER_CREATE,
            DTM_CREATE = localUser.DTM_CREATE,
            CHR_USER_UPDATE = localUser.CHR_USER_UPDATE,
            DTM_UPDATE = localUser.DTM_UPDATE
        };

        var updateResp = await _tmUserService.UpdateAsync(updateEntity);
        if (!updateResp.Success)
        {
            return Json(new { success = false, message = updateResp.Message ?? "Không thể cập nhật hồ sơ." });
        }

        var roleName = await GetRoleNameAsync(localUser.INT_ROLE_ID);

        return Json(new
        {
            success = true,
            message = "Cập nhật hồ sơ thành công.",
            avatarUrl = localUser.CHR_AVATAR_PATH,
            roleName = roleName
        });
    }

    private async Task<TM_USERDTO?> GetCurrentUserAsync(string? adid, string? employeeCode)
    {
        if (!string.IsNullOrWhiteSpace(adid))
        {
            var byAdid = await GetTmUserByFieldAsync("CHR_ADID", adid);
            if (byAdid != null) return byAdid;
        }

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            var byEmployeeCode = await GetTmUserByFieldAsync("CHR_EMPLOYEE_ID", employeeCode);
            if (byEmployeeCode != null) return byEmployeeCode;
        }

        return null;
    }

    private async Task<TM_USERDTO?> GetTmUserByFieldAsync(string fieldName, string fieldValue)
    {
        var response = await _tmUserService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new FilterOption
                {
                    Field = fieldName,
                    Operator = "=",
                    Value = fieldValue
                }
            }
        });

        return response.Success ? response.Data?.Data?.FirstOrDefault() : null;
    }

    private async Task<string> GetRoleNameAsync(int? roleId)
    {
        if (roleId == null || roleId.Value <= 0) return string.Empty;

        var roleResp = await _tmRoleService.GetByIdAsync(roleId.Value);
        if (!roleResp.Success || roleResp.Data == null) return string.Empty;

        return roleResp.Data.NVCHR_ROLE_NAME ?? string.Empty;
    }

    [HttpGet("Profile/AvatarImage/{y}/{m}/{d}/{fileName}")]
    public IActionResult AvatarImage(string y, string m, string d, string fileName)
    {
        try
        {
            var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
            var env = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
            var uploadCfg = config?.GetValue<string>("Files:UploadPath") ?? "uploads/avatars";
            var contentRoot = env?.ContentRootPath ?? System.IO.Directory.GetCurrentDirectory();
            string physicalUploadPath = System.IO.Path.IsPathRooted(uploadCfg)
                ? uploadCfg
                : System.IO.Path.Combine(contentRoot, uploadCfg.TrimStart('~', '/', '\\'));

            var safeY = System.IO.Path.GetFileName(y);
            var safeM = System.IO.Path.GetFileName(m);
            var safeD = System.IO.Path.GetFileName(d);
            var safeFile = System.IO.Path.GetFileName(fileName);

            var physicalPath = System.IO.Path.Combine(physicalUploadPath, safeY, safeM, safeD, safeFile);
            if (!System.IO.File.Exists(physicalPath)) return NotFound();

            var ext = System.IO.Path.GetExtension(safeFile).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(physicalPath, contentType);
        }
        catch
        {
            return NotFound();
        }
    }

    private string? SaveAvatarFile(IFormFile avatarFile)
    {
        try
        {
            var config = HttpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
            var env = HttpContext.RequestServices.GetService(typeof(Microsoft.AspNetCore.Hosting.IWebHostEnvironment)) as Microsoft.AspNetCore.Hosting.IWebHostEnvironment;
            var uploadCfg = config?.GetValue<string>("Files:UploadPath") ?? "uploads/avatars";
            var contentRoot = env?.ContentRootPath ?? System.IO.Directory.GetCurrentDirectory();
            string physicalUploadPath = System.IO.Path.IsPathRooted(uploadCfg)
                ? uploadCfg
                : System.IO.Path.Combine(contentRoot, uploadCfg.TrimStart('~', '/', '\\'));

            var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
            var ext = System.IO.Path.GetExtension(avatarFile.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !allowedExts.Contains(ext))
            {
                ext = ".png";
            }

            var now = DateTime.Now;
            var y = now.Year.ToString("0000");
            var mm = now.Month.ToString("00");
            var dd = now.Day.ToString("00");

            var subFolder = System.IO.Path.Combine(physicalUploadPath, y, mm, dd);
            System.IO.Directory.CreateDirectory(subFolder);

            var fileName = Guid.NewGuid().ToString("N") + ext.ToLowerInvariant();
            var fullPath = System.IO.Path.Combine(subFolder, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                avatarFile.CopyTo(stream);
            }

            return $"/Profile/AvatarImage/{y}/{mm}/{dd}/{fileName}";
        }
        catch
        {
            return null;
        }
    }
}
