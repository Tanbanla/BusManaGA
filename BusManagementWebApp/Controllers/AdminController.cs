using Microsoft.AspNetCore.Mvc;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.Admin;
using ClosedXML.Excel;
using System.Security.Claims;

namespace BusManagement.Controllers;

public class AdminController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly ITmUserService _tmUserService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ITmMenuService _tmMenuService;
    private readonly ITmMenuRoleService _tmMenuRoleService;

    public AdminController(IEmployeeService employeeService, ITmUserService tmUserService, ITmRoleService tmRoleService, ITmMenuService tmMenuService, ITmMenuRoleService tmMenuRoleService)
    {
        _employeeService = employeeService;
        _tmUserService = tmUserService;
        _tmRoleService = tmRoleService;
        _tmMenuService = tmMenuService;
        _tmMenuRoleService = tmMenuRoleService;
    }

    public IActionResult Dashboard()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? keyword, string? name, string? position, int? roleId, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var filters = BuildUserFilters(keyword, name, position, roleId);

        var searchOptions = new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var usersResp = await _tmUserService.SearchByConditionAsync(searchOptions);
        var rolesResp = await _tmRoleService.GetAllAsync();

        var vm = new UsersPageViewModel
        {
            Keyword = keyword,
            Name = name,
            Position = position,
            RoleId = roleId,
            PageNumber = page,
            PageSize = pageSize,
            Users = usersResp?.Data?.Data?.ToList() ?? new(),
            Roles = rolesResp?.Data?.ToList() ?? new(),
            TotalCount = (int)(usersResp?.Data?.TotalCount ?? 0),
            TotalPages = (int)(usersResp?.Data?.TotalPages ?? 0)
        };

        if (vm.TotalPages <= 0)
        {
            vm.TotalPages = vm.TotalCount > 0
                ? (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize)
                : 1;
        }

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ResolveUserProfile(string? employeeCode, string? adid)
    {
        var empCode = employeeCode?.Trim();
        var adidTrim = adid?.Trim();

        if (string.IsNullOrWhiteSpace(empCode) && string.IsNullOrWhiteSpace(adidTrim))
        {
            return Json(new { success = false, message = "Vui lòng nhập Mã nhân viên hoặc ADID." });
        }

        BusManagement.Model.Dtos.TM_USERDTO? localUser = null;
        if (!string.IsNullOrWhiteSpace(adidTrim))
        {
            var userByAdid = await _tmUserService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_ADID", Operator = "=", Value = adidTrim }
                }
            });
            localUser = userByAdid?.Data?.Data?.FirstOrDefault();
        }

        if (localUser == null && !string.IsNullOrWhiteSpace(empCode))
        {
            var userByEmpCode = await _tmUserService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_EMPLOYEE_ID", Operator = "=", Value = empCode }
                }
            });
            localUser = userByEmpCode?.Data?.Data?.FirstOrDefault();
        }

        string? resolvedEmployeeCode = localUser?.CHR_EMPLOYEE_ID ?? empCode;
        string? resolvedAdid = localUser?.CHR_ADID ?? adidTrim;
        string? employeeName = localUser?.NVCHR_NAME;
        string? positionValue = localUser?.CHR_POSITION;
        string? secCode = localUser?.CHR_SEC_CODE;
        string? secName = localUser?.CHR_SEC_NAME;

        if (string.IsNullOrWhiteSpace(employeeName) || string.IsNullOrWhiteSpace(positionValue) || string.IsNullOrWhiteSpace(secCode) || string.IsNullOrWhiteSpace(secName))
        {
            BusManagement.Model.Models.TM_EMPLOYEE? agent = null;
            if (!string.IsNullOrWhiteSpace(adidTrim))
            {
                agent = await _employeeService.GetAgentEmployeeByAdidAsync(adidTrim);
            }
            else if (!string.IsNullOrWhiteSpace(empCode))
            {
                agent = await _employeeService.GetAgentEmployeeByEmployeeIdAsync(empCode);
            }

            if (agent != null)
            {
                resolvedEmployeeCode ??= agent.CHR_EMPLOYEE_ID;
                resolvedAdid ??= agent.CHR_EMPLOYEE_ADID;
                employeeName ??= agent.CHR_EMPLOYEE_NAME;
                positionValue ??= agent.CHR_POSITION_NAME ?? agent.CHR_POSITION;
                secCode ??= agent.CHR_SEC_CODE ?? agent.CHR_SECTION;
                secName ??= agent.CHR_SEC_NAME;
            }
        }

        if (string.IsNullOrWhiteSpace(employeeName) && string.IsNullOrWhiteSpace(positionValue) && string.IsNullOrWhiteSpace(secCode) && string.IsNullOrWhiteSpace(secName))
        {
            return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên." });
        }

        return Json(new
        {
            success = true,
            data = new
            {
                employeeCode = resolvedEmployeeCode ?? string.Empty,
                adid = resolvedAdid ?? string.Empty,
                employeeName = employeeName ?? string.Empty,
                position = positionValue ?? string.Empty,
                secCode = secCode ?? string.Empty,
                secName = secName ?? string.Empty
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser(UserUpsertInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.Adid) || string.IsNullOrWhiteSpace(input.EmployeeId) || string.IsNullOrWhiteSpace(input.EmployeeName))
        {
            TempData["AdminUserError"] = "Vui lòng nhập đầy đủ Mã nhân viên, ADID và Họ tên.";
            return RedirectToUsers(input);
        }

        var duplicateMessage = await ValidateDuplicateUserAsync(input.Adid, input.EmployeeId, null);
        if (!string.IsNullOrWhiteSpace(duplicateMessage))
        {
            TempData["AdminUserError"] = duplicateMessage;
            return RedirectToUsers(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_USER
        {
            CHR_EMPLOYEE_ID = input.EmployeeId?.Trim(),
            CHR_ADID = input.Adid?.Trim(),
            NVCHR_NAME = input.EmployeeName?.Trim(),
            CHR_POSITION = input.Position?.Trim(),
            CHR_PASSWORD = input.Password?.Trim(),
            CHR_SEC_CODE = input.SecCode?.Trim(),
            CHR_SEC_NAME = input.SecName?.Trim(),
            INT_ROLE_ID = input.RoleId,
            INT_LOCK = input.Lock,
            INT_LOCK_DAY = input.LockDay,
            CHR_NOTE = input.Note,
            CHR_AVATAR_PATH = input.AvatarPath,
            BIT_IS_ACTIVE = input.IsActive,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmUserService.AddAsyncV2(entity);
        TempData[resp.Success ? "AdminUserSuccess" : "AdminUserError"] = resp.Success
            ? "Thêm người dùng thành công."
            : (resp.Message ?? "Không thể thêm người dùng.");

        return RedirectToUsers(input);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateUser(UserUpsertInputModel input)
    {
        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["AdminUserError"] = "Không xác định được người dùng cần cập nhật.";
            return RedirectToUsers(input);
        }

        var currentResp = await _tmUserService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["AdminUserError"] = currentResp.Message ?? "Không tìm thấy người dùng.";
            return RedirectToUsers(input);
        }

        var duplicateMessage = await ValidateDuplicateUserAsync(input.Adid, input.EmployeeId, input.ID);
        if (!string.IsNullOrWhiteSpace(duplicateMessage))
        {
            TempData["AdminUserError"] = duplicateMessage;
            return RedirectToUsers(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_USER
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = input.EmployeeId?.Trim(),
            CHR_ADID = input.Adid?.Trim(),
            NVCHR_NAME = input.EmployeeName?.Trim(),
            CHR_POSITION = input.Position?.Trim(),
            CHR_PASSWORD = string.IsNullOrWhiteSpace(input.Password) ? current.CHR_PASSWORD : input.Password?.Trim(),
            CHR_SEC_CODE = input.SecCode?.Trim(),
            CHR_SEC_NAME = input.SecName?.Trim(),
            INT_ROLE_ID = input.RoleId,
            DTM_LAST_LOGIN = current.DTM_LAST_LOGIN,
            INT_LOCK = input.Lock,
            INT_LOCK_DAY = input.LockDay,
            CHR_NOTE = input.Note,
            CHR_AVATAR_PATH = input.AvatarPath,
            BIT_IS_ACTIVE = input.IsActive
        };

        var resp = await _tmUserService.UpdateAsync(entity);
        TempData[resp.Success ? "AdminUserSuccess" : "AdminUserError"] = resp.Success
            ? "Cập nhật người dùng thành công."
            : (resp.Message ?? "Không thể cập nhật người dùng.");

        return RedirectToUsers(input);
    }

    [HttpGet]
    public async Task<IActionResult> Roles(string? keyword, bool? isActive, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var filters = BuildRoleFilters(keyword, isActive);
        var searchOptions = new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var rolesResp = await _tmRoleService.SearchByConditionAsync(searchOptions);
        var menusResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "INT_SORT_ORDER", SortDirection = "ASC" },
                new SortOption { Field = "ID", SortDirection = "ASC" }
            }
        });
        var menuRolesResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000
        });

        var vm = new RolesPageViewModel
        {
            Keyword = keyword,
            IsActive = isActive,
            PageNumber = page,
            PageSize = pageSize,
            Roles = rolesResp?.Data?.Data?.ToList() ?? new(),
            Menus = menusResp?.Data?.Data?.ToList() ?? new(),
            MenuRoles = menuRolesResp?.Data?.Data?.ToList() ?? new(),
            TotalCount = (int)(rolesResp?.Data?.TotalCount ?? 0),
            TotalPages = (int)(rolesResp?.Data?.TotalPages ?? 0)
        };

        if (vm.TotalPages <= 0)
        {
            vm.TotalPages = vm.TotalCount > 0
                ? (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize)
                : 1;
        }

        return View(vm);
    }

    public IActionResult RolePermissions()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole(RoleUpsertInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.RoleName))
        {
            TempData["AdminRoleError"] = "Vui lòng nhập tên quyền.";
            return RedirectToRoles(input);
        }

        var duplicateMessage = await ValidateDuplicateRoleNameAsync(input.RoleName, null);
        if (!string.IsNullOrWhiteSpace(duplicateMessage))
        {
            TempData["AdminRoleError"] = duplicateMessage;
            return RedirectToRoles(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_ROLE
        {
            NVCHR_ROLE_NAME = input.RoleName?.Trim(),
            NVCHR_DESCRIPTION = input.Description?.Trim(),
            BIT_IS_ACTIVE = input.IsActive,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmRoleService.AddAsyncV2(entity);
        TempData[resp.Success ? "AdminRoleSuccess" : "AdminRoleError"] = resp.Success
            ? "Thêm quyền thành công."
            : (resp.Message ?? "Không thể thêm quyền.");

        return RedirectToRoles(input);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateRole(RoleUpsertInputModel input)
    {
        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["AdminRoleError"] = "Không xác định được quyền cần cập nhật.";
            return RedirectToRoles(input);
        }

        if (string.IsNullOrWhiteSpace(input.RoleName))
        {
            TempData["AdminRoleError"] = "Vui lòng nhập tên quyền.";
            return RedirectToRoles(input);
        }

        var currentResp = await _tmRoleService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["AdminRoleError"] = currentResp.Message ?? "Không tìm thấy quyền.";
            return RedirectToRoles(input);
        }

        var duplicateMessage = await ValidateDuplicateRoleNameAsync(input.RoleName, input.ID);
        if (!string.IsNullOrWhiteSpace(duplicateMessage))
        {
            TempData["AdminRoleError"] = duplicateMessage;
            return RedirectToRoles(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_ROLE
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE ?? actor,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            NVCHR_ROLE_NAME = input.RoleName?.Trim(),
            NVCHR_DESCRIPTION = input.Description?.Trim(),
            BIT_IS_ACTIVE = input.IsActive
        };

        var resp = await _tmRoleService.UpdateAsync(entity);
        TempData[resp.Success ? "AdminRoleSuccess" : "AdminRoleError"] = resp.Success
            ? "Cập nhật quyền thành công."
            : (resp.Message ?? "Không thể cập nhật quyền.");

        return RedirectToRoles(input);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRole(int id, string? keyword, bool? searchIsActive, int page = 1, int pageSize = 10)
    {
        var usersUsingRole = await _tmUserService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = "INT_ROLE_ID", Operator = "=", Value = id }
            }
        });

        if ((usersUsingRole?.Data?.TotalCount ?? 0) > 0)
        {
            TempData["AdminRoleError"] = "Quyền đang được gán cho người dùng, không thể xóa.";
            return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
        }

        await ClearRoleFromMenusAsync(new[] { id }, GetCurrentActor());

        var resp = await _tmRoleService.DeleteAsync(id);
        TempData[resp.Success ? "AdminRoleSuccess" : "AdminRoleError"] = resp.Success
            ? "Xóa quyền thành công."
            : (resp.Message ?? "Không thể xóa quyền.");

        return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteRoles(List<int> ids, string? keyword, bool? searchIsActive, int page = 1, int pageSize = 10)
    {
        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["AdminRoleError"] = "Vui lòng chọn ít nhất 1 quyền để xóa.";
            return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
        }

        foreach (var roleId in ids)
        {
            var usersUsingRole = await _tmUserService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = "INT_ROLE_ID", Operator = "=", Value = roleId }
                }
            });

            if ((usersUsingRole?.Data?.TotalCount ?? 0) > 0)
            {
                TempData["AdminRoleError"] = "Có quyền đang được gán cho người dùng, không thể xóa hàng loạt.";
                return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
            }
        }

        await ClearRoleFromMenusAsync(ids, GetCurrentActor());

        var resp = await _tmRoleService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "AdminRoleSuccess" : "AdminRoleError"] = resp.Success
            ? $"Đã xóa {ids.Count} quyền."
            : (resp.Message ?? "Không thể xóa danh sách quyền.");

        return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> AssignRoleMenus(int roleId, List<int>? menuIds, string? keyword, bool? searchIsActive, int page = 1, int pageSize = 10)
    {
        var roleResp = await _tmRoleService.GetByIdAsync(roleId);
        if (!roleResp.Success || roleResp.Data == null)
        {
            TempData["AdminRoleError"] = "Không tìm thấy quyền cần gán màn hình.";
            return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var selectedIds = (menuIds ?? new List<int>()).Where(x => x > 0).Distinct().ToHashSet();
        var mappingResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = "INT_ROLE_ID", Operator = "=", Value = roleId }
            }
        });

        var existingMappings = mappingResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>();
        var existingMenuIds = existingMappings.Select(x => x.INT_MENU_ID).ToHashSet();

        var removeIds = existingMappings
            .Where(x => !selectedIds.Contains(x.INT_MENU_ID))
            .Select(x => x.ID)
            .ToList();
        if (removeIds.Any())
        {
            await _tmMenuRoleService.DeleteRangeAsync(removeIds);
        }

        var addMenuIds = selectedIds.Where(x => !existingMenuIds.Contains(x)).ToList();
        if (addMenuIds.Any())
        {
            var addEntities = addMenuIds.Select(mid => new TM_MENU_ROLE
            {
                INT_MENU_ID = mid,
                INT_ROLE_ID = roleId,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            }).ToList();

            await _tmMenuRoleService.AddMultiAsyncV2(addEntities);
        }

        TempData["AdminRoleSuccess"] = "Gán màn hình cho quyền thành công.";
        return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> ExportRoles(string? keyword, bool? isActive)
    {
        var filters = BuildRoleFilters(keyword, isActive);
        var rolesResp = await _tmRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var roles = rolesResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_ROLEDTO>();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Roles");
        var headers = new[]
        {
            "Tên quyền", "Mô tả", "Trạng thái"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var row = 2;
        foreach (var r in roles)
        {
            sheet.Cell(row, 1).Value = r.NVCHR_ROLE_NAME;
            sheet.Cell(row, 2).Value = r.NVCHR_DESCRIPTION;
            sheet.Cell(row, 3).Value = r.BIT_IS_ACTIVE == true ? "true" : "false";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachQuyen_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ImportRoles(IFormFile? importFile, string? keyword, bool? searchIsActive, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["AdminRoleError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (cells.Count < 1) return false;

            var roleName = cells.ElementAtOrDefault(0)?.Trim();
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return false;
            }

            var duplicateMessage = await ValidateDuplicateRoleNameAsync(roleName, null);
            if (!string.IsNullOrWhiteSpace(duplicateMessage))
            {
                return false;
            }

            bool? isActiveValue = bool.TryParse(cells.ElementAtOrDefault(2), out var activeParsed) ? activeParsed : true;

            var entity = new TM_ROLE
            {
                NVCHR_ROLE_NAME = roleName,
                NVCHR_DESCRIPTION = cells.ElementAtOrDefault(1)?.Trim(),
                BIT_IS_ACTIVE = isActiveValue,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmRoleService.AddAsyncV2(entity);
            return addResp.Success;
        }

        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                TempData["AdminRoleError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>
                {
                    sheet.Cell(r, 1).GetString(),
                    sheet.Cell(r, 2).GetString(),
                    sheet.Cell(r, 3).GetString()
                };

                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }
        else
        {
            using var stream = importFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            var row = 0;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                row++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (row == 1 && line.Contains("RoleName", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "AdminRoleSuccess" : "AdminRoleError"] = failed == 0
            ? $"Import thành công {success} quyền."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Roles), new { keyword, isActive = searchIsActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(int id, string? keyword, string? searchName, string? searchPosition, int? searchRoleId, int page = 1, int pageSize = 10)
    {
        var resp = await _tmUserService.DeleteAsync(id);
        TempData[resp.Success ? "AdminUserSuccess" : "AdminUserError"] = resp.Success
            ? "Xóa người dùng thành công."
            : (resp.Message ?? "Không thể xóa người dùng.");

        return RedirectToAction(nameof(Users), new { keyword, name = searchName, position = searchPosition, roleId = searchRoleId, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> Menus(string? keyword, int? parentId, int? roleId, bool? isActive, int page = 1, int pageSize = 200)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 200 : pageSize;

        var filters = BuildMenuFilters(keyword, parentId, roleId, isActive);
        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "INT_SORT_ORDER", SortDirection = "ASC" },
                new SortOption { Field = "ID", SortDirection = "ASC" }
            }
        };

        var menusResp = await _tmMenuService.SearchByConditionAsync(searchOptions);
        var allMenusResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "INT_SORT_ORDER", SortDirection = "ASC" },
                new SortOption { Field = "ID", SortDirection = "ASC" }
            }
        });
        var menuRolesResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 100000
        });
        var rolesResp = await _tmRoleService.GetAllAsync();

        var menuData = menusResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>();
        var menuRoles = menuRolesResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>();
        if (roleId.HasValue && roleId.Value > 0)
        {
            var menuIdsByRole = menuRoles
                .Where(x => x.INT_ROLE_ID == roleId.Value)
                .Select(x => x.INT_MENU_ID)
                .ToHashSet();

            var menusWithAnyRole = menuRoles.Select(x => x.INT_MENU_ID).ToHashSet();
            menuData = menuData
                .Where(x => menuIdsByRole.Contains(x.ID) || !menusWithAnyRole.Contains(x.ID))
                .ToList();
        }

        var orderedMenus = OrderMenusByHierarchy(menuData);
        var totalCount = orderedMenus.Count;
        var pagedData = orderedMenus.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;

        var vm = new MenusPageViewModel
        {
            Keyword = keyword,
            ParentId = parentId,
            RoleId = roleId,
            IsActive = isActive,
            PageNumber = page,
            PageSize = pageSize,
            Menus = pagedData,
            AllMenus = allMenusResp?.Data?.Data?.ToList() ?? new(),
            MenuRoles = menuRoles,
            Roles = rolesResp?.Data?.ToList() ?? new(),
            TotalCount = totalCount,
            TotalPages = totalPages
        };

        if (vm.TotalPages <= 0)
        {
            vm.TotalPages = vm.TotalCount > 0
                ? (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize)
                : 1;
        }

        return View(vm);
    }

    private static List<BusManagement.Model.Dtos.TM_MENUDTO> OrderMenusByHierarchy(List<BusManagement.Model.Dtos.TM_MENUDTO> menus)
    {
        if (menus == null || menus.Count == 0)
        {
            return new List<BusManagement.Model.Dtos.TM_MENUDTO>();
        }

        var result = new List<BusManagement.Model.Dtos.TM_MENUDTO>();
        var childrenMap = menus
            .GroupBy(x => x.INT_PARENT_ID ?? 0)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.INT_SORT_ORDER ?? int.MaxValue).ThenBy(x => x.ID).ToList());

        var menuIdSet = menus.Select(x => x.ID).ToHashSet();
        var roots = menus
            .Where(x => !x.INT_PARENT_ID.HasValue || x.INT_PARENT_ID.Value <= 0 || !menuIdSet.Contains(x.INT_PARENT_ID.Value))
            .OrderBy(x => x.INT_SORT_ORDER ?? int.MaxValue)
            .ThenBy(x => x.ID)
            .ToList();

        var visited = new HashSet<int>();

        void AppendNode(BusManagement.Model.Dtos.TM_MENUDTO node)
        {
            if (!visited.Add(node.ID)) return;
            result.Add(node);

            if (!childrenMap.TryGetValue(node.ID, out var children)) return;
            foreach (var child in children)
            {
                AppendNode(child);
            }
        }

        foreach (var root in roots)
        {
            AppendNode(root);
        }

        foreach (var menu in menus.OrderBy(x => x.INT_SORT_ORDER ?? int.MaxValue).ThenBy(x => x.ID))
        {
            AppendNode(menu);
        }

        return result;
    }

    [HttpPost]
    public async Task<IActionResult> CreateMenu(MenuUpsertInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.MenuCode) || string.IsNullOrWhiteSpace(input.MenuName))
        {
            TempData["AdminMenuError"] = "Vui lòng nhập đầy đủ Mã menu và Tên menu.";
            return RedirectToMenus(input);
        }

        var duplicateMessage = await ValidateDuplicateMenuCodeAsync(input.MenuCode, null);
        if (!string.IsNullOrWhiteSpace(duplicateMessage))
        {
            TempData["AdminMenuError"] = duplicateMessage;
            return RedirectToMenus(input);
        }

        if (input.ParentId.HasValue && input.ParentId.Value > 0)
        {
            var parentResp = await _tmMenuService.GetByIdAsync(input.ParentId.Value);
            if (!parentResp.Success || parentResp.Data == null)
            {
                TempData["AdminMenuError"] = "Menu cha không tồn tại.";
                return RedirectToMenus(input);
            }
        }

        var actor = GetCurrentActor();
        var selectedRoleIds = NormalizeRoleIds(input.RoleIds, input.RoleId);
        var entity = new TM_MENU
        {
            CHR_MENU_CODE = input.MenuCode?.Trim(),
            NVCHR_MENU_NAME = input.MenuName?.Trim(),
            INT_PARENT_ID = input.ParentId,
            CHR_CONTROLLER = input.Controller?.Trim(),
            CHR_ACTION = input.Action?.Trim(),
            CHR_ICON = input.Icon?.Trim(),
            INT_SORT_ORDER = input.SortOrder,
            BIT_IS_ACTIVE = input.IsActive,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmMenuService.AddAsyncV2(entity);
        if (resp.Success && resp.Data != null)
        {
            await SyncMenuRolesAsync(resp.Data.ID, selectedRoleIds, actor);
        }
        TempData[resp.Success ? "AdminMenuSuccess" : "AdminMenuError"] = resp.Success
            ? "Thêm menu thành công."
            : (resp.Message ?? "Không thể thêm menu.");

        return RedirectToMenus(input);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateMenu(MenuUpsertInputModel input)
    {
        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["AdminMenuError"] = "Không xác định được menu cần cập nhật.";
            return RedirectToMenus(input);
        }

        var currentResp = await _tmMenuService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["AdminMenuError"] = currentResp.Message ?? "Không tìm thấy menu.";
            return RedirectToMenus(input);
        }

        if (string.IsNullOrWhiteSpace(input.MenuCode) || string.IsNullOrWhiteSpace(input.MenuName))
        {
            TempData["AdminMenuError"] = "Vui lòng nhập đầy đủ Mã menu và Tên menu.";
            return RedirectToMenus(input);
        }

        var duplicateMessage = await ValidateDuplicateMenuCodeAsync(input.MenuCode, input.ID);
        if (!string.IsNullOrWhiteSpace(duplicateMessage))
        {
            TempData["AdminMenuError"] = duplicateMessage;
            return RedirectToMenus(input);
        }

        if (input.ParentId.HasValue && input.ParentId.Value == input.ID.Value)
        {
            TempData["AdminMenuError"] = "Menu cha không hợp lệ.";
            return RedirectToMenus(input);
        }

        if (input.ParentId.HasValue && input.ParentId.Value > 0)
        {
            var allMenusResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions { PageNumber = 1, PageSize = 100000 });
            var allMenus = allMenusResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>();

            var descendants = GetDescendantMenuIds(input.ID.Value, allMenus);
            if (descendants.Contains(input.ParentId.Value))
            {
                TempData["AdminMenuError"] = "Không thể chọn menu con làm menu cha.";
                return RedirectToMenus(input);
            }
        }

        var actor = GetCurrentActor();
        var selectedRoleIds = NormalizeRoleIds(input.RoleIds, input.RoleId);
        var entity = new TM_MENU
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE ?? actor,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_MENU_CODE = input.MenuCode?.Trim(),
            NVCHR_MENU_NAME = input.MenuName?.Trim(),
            INT_PARENT_ID = input.ParentId,
            CHR_CONTROLLER = input.Controller?.Trim(),
            CHR_ACTION = input.Action?.Trim(),
            CHR_ICON = input.Icon?.Trim(),
            INT_SORT_ORDER = input.SortOrder,
            BIT_IS_ACTIVE = input.IsActive
        };

        var resp = await _tmMenuService.UpdateAsync(entity);
        if (resp.Success)
        {
            await SyncMenuRolesAsync(entity.ID, selectedRoleIds, actor);
        }
        TempData[resp.Success ? "AdminMenuSuccess" : "AdminMenuError"] = resp.Success
            ? "Cập nhật menu thành công."
            : (resp.Message ?? "Không thể cập nhật menu.");

        return RedirectToMenus(input);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMenu(int id, string? keyword, int? searchParentId, int? searchRoleId, bool? searchIsActive, int page = 1, int pageSize = 10)
    {
        var childrenResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = "INT_PARENT_ID", Operator = "=", Value = id }
            }
        });

        if ((childrenResp?.Data?.TotalFilter ?? 0) > 0)
        {
            TempData["AdminMenuError"] = "Menu đang có menu con. Vui lòng xóa menu con trước.";
            return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
        }

        await DeleteMenuRoleMappingsByMenuIdsAsync(new[] { id });

        var resp = await _tmMenuService.DeleteAsync(id);
        TempData[resp.Success ? "AdminMenuSuccess" : "AdminMenuError"] = resp.Success
            ? "Xóa menu thành công."
            : (resp.Message ?? "Không thể xóa menu.");

        return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMenus(List<int> ids, string? keyword, int? searchParentId, int? searchRoleId, bool? searchIsActive, int page = 1, int pageSize = 10)
    {
        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["AdminMenuError"] = "Vui lòng chọn ít nhất 1 menu để xóa.";
            return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
        }

        var allResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions { PageNumber = 1, PageSize = 100000 });
        var allMenus = allResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>();
        var selectedSet = ids.ToHashSet();

        var blocked = ids
            .Where(id => allMenus.Any(x => x.INT_PARENT_ID == id && !selectedSet.Contains(x.ID)))
            .ToList();

        if (blocked.Any())
        {
            TempData["AdminMenuError"] = "Một số menu đang có menu con chưa được chọn xóa. Vui lòng chọn đầy đủ menu con trước.";
            return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
        }

        await DeleteMenuRoleMappingsByMenuIdsAsync(ids);

        var resp = await _tmMenuService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "AdminMenuSuccess" : "AdminMenuError"] = resp.Success
            ? $"Đã xóa {ids.Count} menu."
            : (resp.Message ?? "Không thể xóa danh sách menu.");

        return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> AssignMenuRole(int id, List<int>? assignedRoleIds, string? keyword, int? searchParentId, int? searchRoleId, bool? searchIsActive, int page = 1, int pageSize = 10)
    {
        var currentResp = await _tmMenuService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["AdminMenuError"] = currentResp.Message ?? "Không tìm thấy menu.";
            return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var selectedRoleIds = NormalizeRoleIds(assignedRoleIds, null);
        await SyncMenuRolesAsync(id, selectedRoleIds, actor);

        TempData["AdminMenuSuccess"] = "Gán nhóm quyền cho màn hình thành công.";

        return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> ExportMenus(string? keyword, int? parentId, int? roleId, bool? isActive)
    {
        var filters = BuildMenuFilters(keyword, parentId, roleId, isActive);
        var menusResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "INT_SORT_ORDER", SortDirection = "ASC" },
                new SortOption { Field = "ID", SortDirection = "ASC" }
            }
        });

        var menus = menusResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>();
        var menuRolesResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions { PageNumber = 1, PageSize = 100000 });
        var menuRoles = menuRolesResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>();
        if (roleId.HasValue && roleId.Value > 0)
        {
            var menuIdsByRole = menuRoles.Where(x => x.INT_ROLE_ID == roleId.Value).Select(x => x.INT_MENU_ID).ToHashSet();
            var menusWithAnyRole = menuRoles.Select(x => x.INT_MENU_ID).ToHashSet();
            menus = menus.Where(x => menuIdsByRole.Contains(x.ID) || !menusWithAnyRole.Contains(x.ID)).ToList();
        }
        var allMenus = await _tmMenuService.SearchByConditionAsync(new SearchOptions { PageNumber = 1, PageSize = 100000 });
        var allMenuMap = (allMenus?.Data?.Data ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>()).ToDictionary(x => x.ID, x => x);
        var menuRoleMap = menuRoles
            .GroupBy(x => x.INT_MENU_ID)
            .ToDictionary(g => g.Key, g => g.Where(x => x.INT_ROLE_ID.HasValue).Select(x => x.INT_ROLE_ID!.Value).Distinct().OrderBy(x => x).ToList());

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Menus");
        var headers = new[]
        {
            "Mã menu", "Tên menu", "Mã menu cha", "Controller", "Action", "Icon", "Thứ tự", "RoleIds", "Trạng thái"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var row = 2;
        foreach (var m in menus)
        {
            allMenuMap.TryGetValue(m.INT_PARENT_ID ?? 0, out var parentMenu);

            sheet.Cell(row, 1).Value = m.CHR_MENU_CODE;
            sheet.Cell(row, 2).Value = m.NVCHR_MENU_NAME;
            sheet.Cell(row, 3).Value = parentMenu?.CHR_MENU_CODE ?? string.Empty;
            sheet.Cell(row, 4).Value = m.CHR_CONTROLLER;
            sheet.Cell(row, 5).Value = m.CHR_ACTION;
            sheet.Cell(row, 6).Value = m.CHR_ICON;
            sheet.Cell(row, 7).Value = m.INT_SORT_ORDER;
            sheet.Cell(row, 8).Value = menuRoleMap.TryGetValue(m.ID, out var roleIds) && roleIds.Any()
                ? string.Join(',', roleIds)
                : string.Empty;
            sheet.Cell(row, 9).Value = m.BIT_IS_ACTIVE == true ? "true" : "false";
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachMenu_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ImportMenus(IFormFile? importFile, string? keyword, int? searchParentId, int? searchRoleId, bool? searchIsActive, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["AdminMenuError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;
        var allMenusResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions { PageNumber = 1, PageSize = 100000 });
        var allMenus = allMenusResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>();
        var codeMap = allMenus
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_MENU_CODE))
            .GroupBy(x => x.CHR_MENU_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ID, StringComparer.OrdinalIgnoreCase);

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (cells.Count < 2) return false;

            var menuCode = cells.ElementAtOrDefault(0)?.Trim();
            var menuName = cells.ElementAtOrDefault(1)?.Trim();
            var parentCode = cells.ElementAtOrDefault(2)?.Trim();

            if (string.IsNullOrWhiteSpace(menuCode) || string.IsNullOrWhiteSpace(menuName))
            {
                return false;
            }

            var duplicateMessage = await ValidateDuplicateMenuCodeAsync(menuCode, null);
            if (!string.IsNullOrWhiteSpace(duplicateMessage))
            {
                return false;
            }

            int? parentId = null;
            if (!string.IsNullOrWhiteSpace(parentCode))
            {
                if (!codeMap.TryGetValue(parentCode, out var mappedParentId))
                {
                    return false;
                }
                parentId = mappedParentId;
            }

            int? sortOrder = int.TryParse(cells.ElementAtOrDefault(6), out var sortValue) ? sortValue : 0;
            var roleIds = NormalizeRoleIds(ParseRoleIds(cells.ElementAtOrDefault(7), null), null);
            bool? isActiveValue = bool.TryParse(cells.ElementAtOrDefault(8), out var activeParsed) ? activeParsed : true;

            var entity = new TM_MENU
            {
                CHR_MENU_CODE = menuCode,
                NVCHR_MENU_NAME = menuName,
                INT_PARENT_ID = parentId,
                CHR_CONTROLLER = cells.ElementAtOrDefault(3)?.Trim(),
                CHR_ACTION = cells.ElementAtOrDefault(4)?.Trim(),
                CHR_ICON = cells.ElementAtOrDefault(5)?.Trim(),
                INT_SORT_ORDER = sortOrder,
                BIT_IS_ACTIVE = isActiveValue,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmMenuService.AddAsyncV2(entity);
            if (!addResp.Success || addResp.Data == null)
            {
                return false;
            }

            await SyncMenuRolesAsync(addResp.Data.ID, roleIds, actor);

            if (!string.IsNullOrWhiteSpace(menuCode))
            {
                codeMap[menuCode] = addResp.Data.ID;
            }

            return true;
        }

        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                TempData["AdminMenuError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>
                {
                    sheet.Cell(r, 1).GetString(),
                    sheet.Cell(r, 2).GetString(),
                    sheet.Cell(r, 3).GetString(),
                    sheet.Cell(r, 4).GetString(),
                    sheet.Cell(r, 5).GetString(),
                    sheet.Cell(r, 6).GetString(),
                    sheet.Cell(r, 7).GetString(),
                    sheet.Cell(r, 8).GetString(),
                    sheet.Cell(r, 9).GetString()
                };

                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }
        else
        {
            using var stream = importFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            var row = 0;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                row++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (row == 1 && line.Contains("MenuCode", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "AdminMenuSuccess" : "AdminMenuError"] = failed == 0
            ? $"Import thành công {success} menu."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Menus), new { keyword, parentId = searchParentId, roleId = searchRoleId, isActive = searchIsActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUsers(List<int> ids, string? keyword, string? searchName, string? searchPosition, int? searchRoleId, int page = 1, int pageSize = 10)
    {
        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["AdminUserError"] = "Vui lòng chọn ít nhất 1 người dùng để xóa.";
            return RedirectToAction(nameof(Users), new { keyword, name = searchName, position = searchPosition, roleId = searchRoleId, page, pageSize });
        }

        var resp = await _tmUserService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "AdminUserSuccess" : "AdminUserError"] = resp.Success
            ? $"Đã xóa {ids.Count} người dùng."
            : (resp.Message ?? "Không thể xóa danh sách người dùng.");

        return RedirectToAction(nameof(Users), new { keyword, name = searchName, position = searchPosition, roleId = searchRoleId, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> ToggleLockUser(int id, string? lockUser, string? keyword, string? searchName, string? searchPosition, int? searchRoleId, int page = 1, int pageSize = 10)
    {
        var currentResp = await _tmUserService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["AdminUserError"] = currentResp.Message ?? "Không tìm thấy người dùng.";
            return RedirectToAction(nameof(Users), new { keyword, name = searchName, position = searchPosition, roleId = searchRoleId, page, pageSize });
        }

        var shouldLock = lockUser?.Trim().ToLowerInvariant() switch
        {
            "1" => true,
            "true" => true,
            "0" => false,
            "false" => false,
            _ => current.INT_LOCK != 1
        };

        var actor = GetCurrentActor();
        var entity = new TM_USER
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = current.CHR_EMPLOYEE_ID,
            CHR_ADID = current.CHR_ADID,
            NVCHR_NAME = current.NVCHR_NAME,
            CHR_POSITION = current.CHR_POSITION,
            CHR_PASSWORD = current.CHR_PASSWORD,
            CHR_SEC_CODE = current.CHR_SEC_CODE,
            CHR_SEC_NAME = current.CHR_SEC_NAME,
            INT_ROLE_ID = current.INT_ROLE_ID,
            DTM_LAST_LOGIN = current.DTM_LAST_LOGIN,
            INT_LOCK = shouldLock ? 1 : 0,
            INT_LOCK_DAY = shouldLock ? Math.Max(current.INT_LOCK_DAY ?? 0, 1) : 0,
            CHR_NOTE = current.CHR_NOTE,
            CHR_AVATAR_PATH = current.CHR_AVATAR_PATH,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE
        };

        var resp = await _tmUserService.UpdateAsync(entity);
        TempData[resp.Success ? "AdminUserSuccess" : "AdminUserError"] = resp.Success
            ? (shouldLock ? "Đã khóa tài khoản." : "Đã mở khóa tài khoản.")
            : (resp.Message ?? "Không thể cập nhật trạng thái khóa.");

        return RedirectToAction(nameof(Users), new { keyword, name = searchName, position = searchPosition, roleId = searchRoleId, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> ExportUsers(string? keyword, string? name, string? position, int? roleId)
    {
        var filters = BuildUserFilters(keyword, name, position, roleId);
        var search = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new() { new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" } }
        };

        var usersResp = await _tmUserService.SearchByConditionAsync(search);
        var users = usersResp?.Data?.Data?.ToList() ?? new List<Model.Dtos.TM_USERDTO>();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Users");

        var headers = new[]
        {
            "Mã nhân viên", "ADID", "Họ tên", "Vị trí", "Mã phòng", "Tên phòng", "RoleId", "Trạng thái", "Khóa", "Số ngày khóa", "Đăng nhập gần nhất"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var row = 2;
        foreach (var u in users)
        {
            sheet.Cell(row, 1).Value = u.CHR_EMPLOYEE_ID;
            sheet.Cell(row, 2).Value = u.CHR_ADID;
            sheet.Cell(row, 3).Value = u.NVCHR_NAME;
            sheet.Cell(row, 4).Value = u.CHR_POSITION;
            sheet.Cell(row, 5).Value = u.CHR_SEC_CODE;
            sheet.Cell(row, 6).Value = u.CHR_SEC_NAME;
            sheet.Cell(row, 7).Value = u.INT_ROLE_ID;
            sheet.Cell(row, 8).Value = u.BIT_IS_ACTIVE == true ? "Đang hoạt động" : "Ngưng hoạt động";
            sheet.Cell(row, 9).Value = u.INT_LOCK;
            sheet.Cell(row, 10).Value = u.INT_LOCK_DAY;
            sheet.Cell(row, 11).Value = u.DTM_LAST_LOGIN?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty;
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachNguoiDung_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ImportUsers(IFormFile? importFile, string? keyword, string? searchName, string? searchPosition, int? searchRoleId, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["AdminUserError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Users), new { keyword, name = searchName, position = searchPosition, roleId = searchRoleId, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (cells.Count < 3) return false;

            var employeeId = cells.ElementAtOrDefault(0)?.Trim();
            var adid = cells.ElementAtOrDefault(1)?.Trim();
            var employeeName = cells.ElementAtOrDefault(2)?.Trim();

            if (string.IsNullOrWhiteSpace(employeeId) || string.IsNullOrWhiteSpace(adid) || string.IsNullOrWhiteSpace(employeeName))
            {
                return false;
            }

            var duplicateMessage = await ValidateDuplicateUserAsync(adid, employeeId, null);
            if (!string.IsNullOrWhiteSpace(duplicateMessage))
            {
                return false;
            }

            int? roleIdValue = int.TryParse(cells.ElementAtOrDefault(6), out var roleIdParsed) ? roleIdParsed : null;
            bool? isActive = bool.TryParse(cells.ElementAtOrDefault(7), out var activeParsed) ? activeParsed : true;
            int? lockValue = int.TryParse(cells.ElementAtOrDefault(8), out var lockParsed) ? lockParsed : 0;
            int? lockDayValue = int.TryParse(cells.ElementAtOrDefault(9), out var lockDayParsed) ? lockDayParsed : 0;

            var entity = new TM_USER
            {
                CHR_EMPLOYEE_ID = employeeId,
                CHR_ADID = adid,
                NVCHR_NAME = employeeName,
                CHR_POSITION = cells.ElementAtOrDefault(3),
                CHR_SEC_CODE = cells.ElementAtOrDefault(4),
                CHR_SEC_NAME = cells.ElementAtOrDefault(5),
                INT_ROLE_ID = roleIdValue,
                BIT_IS_ACTIVE = isActive,
                INT_LOCK = lockValue,
                INT_LOCK_DAY = lockDayValue,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmUserService.AddAsyncV2(entity);
            return addResp.Success;
        }

        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                TempData["AdminUserError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Users), new { keyword, name = searchName, position = searchPosition, roleId = searchRoleId, page, pageSize });
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>
                {
                    sheet.Cell(r, 1).GetString(),
                    sheet.Cell(r, 2).GetString(),
                    sheet.Cell(r, 3).GetString(),
                    sheet.Cell(r, 4).GetString(),
                    sheet.Cell(r, 5).GetString(),
                    sheet.Cell(r, 6).GetString(),
                    sheet.Cell(r, 7).GetString(),
                    sheet.Cell(r, 8).GetString(),
                    sheet.Cell(r, 9).GetString(),
                    sheet.Cell(r, 10).GetString(),
                    sheet.Cell(r, 11).GetString()
                };

                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }
        else
        {
            using var stream = importFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            var row = 0;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                row++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (row == 1 && line.Contains("EmployeeId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "AdminUserSuccess" : "AdminUserError"] = failed == 0
            ? $"Import thành công {success} người dùng."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Users), new { keyword, name = searchName, position = searchPosition, roleId = searchRoleId, page, pageSize });
    }

    private string GetCurrentActor()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value
            ?? User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? "system";
    }

    private IActionResult RedirectToUsers(UserUpsertInputModel input)
    {
        return RedirectToAction(nameof(Users), new
        {
            keyword = input.Keyword,
            name = input.SearchName,
            position = input.SearchPosition,
            roleId = input.SearchRoleId,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private IActionResult RedirectToRoles(RoleUpsertInputModel input)
    {
        return RedirectToAction(nameof(Roles), new
        {
            keyword = input.Keyword,
            isActive = input.SearchIsActive,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private IActionResult RedirectToMenus(MenuUpsertInputModel input)
    {
        return RedirectToAction(nameof(Menus), new
        {
            keyword = input.Keyword,
            parentId = input.SearchParentId,
            roleId = input.SearchRoleId,
            isActive = input.SearchIsActive,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private List<FilterOption> BuildUserFilters(string? keyword, string? name, string? position, int? roleId)
    {
        var filters = new List<FilterOption>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            filters.Add(new FilterOption { Field = "CHR_EMPLOYEE_ID", Operator = "LIKE", Value = $"%{keyword.Trim()}%" });
            filters.Add(new FilterOption { Field = "CHR_ADID", Operator = "LIKE", Value = $"%{keyword.Trim()}%", LogicType = "OR" });
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            filters.Add(new FilterOption { Field = "NVCHR_NAME", Operator = "LIKE", Value = $"%{name.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(position))
        {
            filters.Add(new FilterOption { Field = "CHR_POSITION", Operator = "LIKE", Value = $"%{position.Trim()}%" });
        }

        if (roleId.HasValue && roleId.Value > 0)
        {
            filters.Add(new FilterOption { Field = "INT_ROLE_ID", Operator = "=", Value = roleId.Value });
        }

        return filters;
    }

    private List<FilterOption> BuildRoleFilters(string? keyword, bool? isActive)
    {
        var filters = new List<FilterOption>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var keywordTrim = keyword.Trim();
            filters.Add(new FilterOption { Field = "NVCHR_ROLE_NAME", Operator = "LIKE", Value = $"%{keywordTrim}%" });
            filters.Add(new FilterOption { Field = "NVCHR_DESCRIPTION", Operator = "LIKE", Value = $"%{keywordTrim}%", LogicType = "OR" });
        }

        if (isActive.HasValue)
        {
            filters.Add(new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = isActive.Value });
        }

        return filters;
    }

    private List<FilterOption> BuildMenuFilters(string? keyword, int? parentId, int? roleId, bool? isActive)
    {
        var filters = new List<FilterOption>();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var keywordTrim = keyword.Trim();
            filters.Add(new FilterOption { Field = "CHR_MENU_CODE", Operator = "LIKE", Value = $"%{keywordTrim}%" });
            filters.Add(new FilterOption { Field = "NVCHR_MENU_NAME", Operator = "LIKE", Value = $"%{keywordTrim}%", LogicType = "OR" });
            filters.Add(new FilterOption { Field = "CHR_CONTROLLER", Operator = "LIKE", Value = $"%{keywordTrim}%", LogicType = "OR" });
            filters.Add(new FilterOption { Field = "CHR_ACTION", Operator = "LIKE", Value = $"%{keywordTrim}%", LogicType = "OR" });
        }

        if (parentId.HasValue)
        {
            if (parentId.Value > 0)
            {
                filters.Add(new FilterOption { Field = "INT_PARENT_ID", Operator = "=", Value = parentId.Value });
            }
            else
            {
                filters.Add(new FilterOption { Field = "INT_PARENT_ID", Operator = "IS NULL" });
            }
        }

        if (isActive.HasValue)
        {
            filters.Add(new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = isActive.Value });
        }

        return filters;
    }

    private async Task<string?> ValidateDuplicateUserAsync(string? adid, string? employeeId, int? excludeId)
    {
        if (!string.IsNullOrWhiteSpace(adid))
        {
            var adidResp = await _tmUserService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 10,
                Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_ADID", Operator = "=", Value = adid.Trim() }
                }
            });

            var adidExists = (adidResp?.Data?.Data ?? new List<Model.Dtos.TM_USERDTO>())
                .Any(x => !excludeId.HasValue || x.ID != excludeId.Value);

            if (adidExists)
            {
                return "ADID đã tồn tại trong hệ thống.";
            }
        }

        if (!string.IsNullOrWhiteSpace(employeeId))
        {
            var empResp = await _tmUserService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 10,
                Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_EMPLOYEE_ID", Operator = "=", Value = employeeId.Trim() }
                }
            });

            var empExists = (empResp?.Data?.Data ?? new List<Model.Dtos.TM_USERDTO>())
                .Any(x => !excludeId.HasValue || x.ID != excludeId.Value);

            if (empExists)
            {
                return "Mã nhân viên đã tồn tại trong hệ thống.";
            }
        }

        return null;
    }

    private async Task<string?> ValidateDuplicateRoleNameAsync(string? roleName, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return null;
        }

        var roleResp = await _tmRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 20,
            Filters = new List<FilterOption>
            {
                new() { Field = "NVCHR_ROLE_NAME", Operator = "=", Value = roleName.Trim() }
            }
        });

        var roleExists = (roleResp?.Data?.Data ?? new List<BusManagement.Model.Dtos.TM_ROLEDTO>())
            .Any(x => !excludeId.HasValue || x.ID != excludeId.Value);

        if (roleExists)
        {
            return "Tên quyền đã tồn tại trong hệ thống.";
        }

        return null;
    }

    private async Task ClearRoleFromMenusAsync(IEnumerable<int> roleIds, string actor)
    {
        var roleIdSet = roleIds.Where(x => x > 0).ToHashSet();
        if (!roleIdSet.Any()) return;

        var mappingsResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 100000
        });

        var mappingIds = (mappingsResp?.Data?.Data ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>())
            .Where(x => x.INT_ROLE_ID.HasValue && roleIdSet.Contains(x.INT_ROLE_ID.Value))
            .Select(x => x.ID)
            .ToList();

        if (mappingIds.Any())
        {
            await _tmMenuRoleService.DeleteRangeAsync(mappingIds);
        }
    }

    private static List<int> NormalizeRoleIds(IEnumerable<int>? roleIds, int? fallbackRoleId)
    {
        var result = roleIds?
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToList() ?? new List<int>();

        if (!result.Any() && fallbackRoleId.HasValue && fallbackRoleId.Value > 0)
        {
            result.Add(fallbackRoleId.Value);
        }

        return result;
    }

    private static List<int> ParseRoleIds(string? rawRoleIds, int? fallbackRoleId)
    {
        var result = new HashSet<int>();
        if (!string.IsNullOrWhiteSpace(rawRoleIds))
        {
            foreach (var part in rawRoleIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var parsed) && parsed > 0)
                {
                    result.Add(parsed);
                }
            }
        }

        if (!result.Any() && fallbackRoleId.HasValue && fallbackRoleId.Value > 0)
        {
            result.Add(fallbackRoleId.Value);
        }

        return result.OrderBy(x => x).ToList();
    }

    private async Task SyncMenuRolesAsync(int menuId, IEnumerable<int>? roleIds, string actor)
    {
        var normalizedRoleIds = NormalizeRoleIds(roleIds, null).ToHashSet();

        var existingResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = "INT_MENU_ID", Operator = "=", Value = menuId }
            }
        });

        var existing = existingResp?.Data?.Data?.ToList() ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>();
        var removeIds = existing
            .Where(x => !x.INT_ROLE_ID.HasValue || !normalizedRoleIds.Contains(x.INT_ROLE_ID.Value))
            .Select(x => x.ID)
            .ToList();

        if (removeIds.Any())
        {
            await _tmMenuRoleService.DeleteRangeAsync(removeIds);
        }

        var existingRoleIds = existing
            .Where(x => x.INT_ROLE_ID.HasValue)
            .Select(x => x.INT_ROLE_ID!.Value)
            .ToHashSet();

        var addRoleIds = normalizedRoleIds.Where(x => !existingRoleIds.Contains(x)).ToList();
        if (addRoleIds.Any())
        {
            var addEntities = addRoleIds.Select(rid => new TM_MENU_ROLE
            {
                INT_MENU_ID = menuId,
                INT_ROLE_ID = rid,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            }).ToList();

            await _tmMenuRoleService.AddMultiAsyncV2(addEntities);
        }
    }

    private async Task DeleteMenuRoleMappingsByMenuIdsAsync(IEnumerable<int> menuIds)
    {
        var ids = menuIds.Where(x => x > 0).Distinct().ToHashSet();
        if (!ids.Any()) return;

        var mappingsResp = await _tmMenuRoleService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 100000
        });

        var mappingIds = (mappingsResp?.Data?.Data ?? new List<BusManagement.Model.Dtos.TM_MENU_ROLEDTO>())
            .Where(x => ids.Contains(x.INT_MENU_ID))
            .Select(x => x.ID)
            .ToList();

        if (mappingIds.Any())
        {
            await _tmMenuRoleService.DeleteRangeAsync(mappingIds);
        }
    }

    private async Task<string?> ValidateDuplicateMenuCodeAsync(string? menuCode, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(menuCode))
        {
            return null;
        }

        var menuResp = await _tmMenuService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 20,
            Filters = new List<FilterOption>
            {
                new() { Field = "CHR_MENU_CODE", Operator = "=", Value = menuCode.Trim() }
            }
        });

        var menuExists = (menuResp?.Data?.Data ?? new List<BusManagement.Model.Dtos.TM_MENUDTO>())
            .Any(x => !excludeId.HasValue || x.ID != excludeId.Value);

        if (menuExists)
        {
            return "Mã menu đã tồn tại trong hệ thống.";
        }

        return null;
    }

    private static HashSet<int> GetDescendantMenuIds(int rootId, List<BusManagement.Model.Dtos.TM_MENUDTO> allMenus)
    {
        var result = new HashSet<int>();
        var childrenMap = allMenus
            .GroupBy(x => x.INT_PARENT_ID ?? 0)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ID).ToList());

        void Walk(int parentId)
        {
            if (!childrenMap.TryGetValue(parentId, out var children)) return;
            foreach (var childId in children)
            {
                if (!result.Add(childId)) continue;
                Walk(childId);
            }
        }

        Walk(rootId);
        return result;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null)
        {
            return result;
        }

        var sb = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
