using System.Globalization;
using System.Security.Claims;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.EmployeeBus;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace BusManagement.Controllers;

public class EmployeeBusController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly ITmBusStopService _tmBusStopService;
    private readonly ITmCityService _tmCityService;
    private readonly ITmWardService _tmWardService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ISectionService _sectionService;

    public EmployeeBusController(
        IEmployeeService employeeService,
        ITmEmployeeService tmEmployeeService,
        ITmBusStopService tmBusStopService,
        ITmCityService tmCityService,
        ITmWardService tmWardService,
        ITmRoleService tmRoleService,
        ISectionService sectionService)
    {
        _employeeService = employeeService;
        _tmEmployeeService = tmEmployeeService;
        _tmBusStopService = tmBusStopService;
        _tmCityService = tmCityService;
        _tmWardService = tmWardService;
        _tmRoleService = tmRoleService;
        _sectionService = sectionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? employeeCode, string? employeeName, string? department, string? phone, string? city, string? ward, int? busStopId, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var filters = BuildEmployeeFilters(employeeCode, employeeName, department, phone, city, ward, busStopId);
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

        var employeeResp = await _tmEmployeeService.SearchByConditionAsync(searchOptions);
        var busStopsResp = await _tmBusStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_STOP_CITY", SortDirection = "ASC" },
                new SortOption { Field = "NVCHR_STOP_WARD", SortDirection = "ASC" },
                new SortOption { Field = "NVCHR_STOP_NAME", SortDirection = "ASC" }
            }
        });

        var sectionsResp = await _sectionService.GetAllAsync();
        var cityResp = await _tmCityService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_CITY_NAME", SortDirection = "ASC" }
            }
        });
        var wardResp = await _tmWardService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_WARD_NAME", SortDirection = "ASC" }
            }
        });

        var vm = new EmployeeBusPageViewModel
        {
            EmployeeCode = employeeCode,
            EmployeeName = employeeName,
            Department = department,
            Phone = phone,
            City = city,
            Ward = ward,
            BusStopId = busStopId,
            PageNumber = page,
            PageSize = pageSize,
            Employees = employeeResp?.Data?.Data?.ToList() ?? new(),
            BusStops = (busStopsResp?.Data?.Data ?? new List<TM_BUS_STOPDTO>())
                .GroupBy(x => x.ID)
                .Select(g => g.First())
                .ToList(),
            Sections = (sectionsResp?.Data ?? new List<TM_SECTIONDTO>())
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
                .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(x => x.CHR_CODE)
                .ToList(),
            Cities = (cityResp?.Data?.Data ?? new List<TM_CITYDTO>())
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_CITY_NAME))
                .GroupBy(x => x.NVCHR_CITY_NAME!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList(),
            Wards = (wardResp?.Data?.Data ?? new List<TM_WARDDTO>())
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_WARD_NAME))
                .GroupBy(x => new { x.INT_CITY_ID, WardName = x.NVCHR_WARD_NAME!.Trim() })
                .Select(g => g.First())
                .ToList(),
            TotalCount = (int)(employeeResp?.Data?.TotalCount ?? 0),
            TotalPages = (int)(employeeResp?.Data?.TotalPages ?? 0),
            CanManage = await CanManageAsync()
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
    public async Task<IActionResult> ResolveEmployeeByCode(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return Json(new { success = false, message = "Mã nhân viên không hợp lệ." });
        }

        var code = employeeCode.Trim();

        var localResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = "CHR_EMPLOYEE_ID", Operator = "=", Value = code }
            }
        });

        var local = localResp?.Data?.Data?.FirstOrDefault();

        string? employeeName = local?.NVCHR_EMPLOYEE_NAME;
        string? departmentCode = local?.CHR_CODE_SEC;
        string? phone = local?.CHR_PHONE;
        string? city = local?.NVCHR_CITY;
        string? ward = local?.NVCHR_WARD;
        string? addressDetail = local?.NVCHR_ADDRESS_DETAIL;

        if (string.IsNullOrWhiteSpace(employeeName) || string.IsNullOrWhiteSpace(departmentCode) || string.IsNullOrWhiteSpace(phone)
            || string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(addressDetail))
        {
            var agent = await _employeeService.GetAgentEmployeeByEmployeeIdAsync(code);
            if (agent != null)
            {
                employeeName ??= agent.CHR_EMPLOYEE_NAME;
                departmentCode ??= agent.CHR_SEC_CODE ?? agent.CHR_SECTION;
                phone ??= agent.CHR_PHONE_NO ?? agent.CHR_PERMANENT_TEL_NO;
                city ??= agent.CHR_PROVINCE;
                addressDetail ??= agent.CHR_TEMP_ADDRESS ?? agent.CHR_PERMANENT_ADDRESS;
            }
        }

        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            var sectionsResp = await _sectionService.GetAllAsync();
            var sections = sectionsResp?.Data ?? new List<TM_SECTIONDTO>();

            var normalizedDepartment = departmentCode.Trim();
            var codeMatched = sections.Any(x =>
                !string.IsNullOrWhiteSpace(x.CHR_CODE)
                && string.Equals(x.CHR_CODE.Trim(), normalizedDepartment, StringComparison.OrdinalIgnoreCase));

            if (!codeMatched)
            {
                var matchedByName = sections.FirstOrDefault(x =>
                    (!string.IsNullOrWhiteSpace(x.CHR_NAME) && string.Equals(x.CHR_NAME.Trim(), normalizedDepartment, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(x.CHR_SECTION_NAME) && string.Equals(x.CHR_SECTION_NAME.Trim(), normalizedDepartment, StringComparison.OrdinalIgnoreCase)));

                if (!string.IsNullOrWhiteSpace(matchedByName?.CHR_CODE))
                {
                    departmentCode = matchedByName.CHR_CODE.Trim();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(employeeName) && string.IsNullOrWhiteSpace(departmentCode) && string.IsNullOrWhiteSpace(phone)
            && string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(ward) && string.IsNullOrWhiteSpace(addressDetail))
        {
            return Json(new { success = false, message = "Không tìm thấy thông tin nhân viên." });
        }

        return Json(new
        {
            success = true,
            data = new
            {
                employeeName = employeeName ?? string.Empty,
                departmentCode = departmentCode ?? string.Empty,
                phone = phone ?? string.Empty,
                city = city ?? string.Empty,
                ward = ward ?? string.Empty,
                addressDetail = addressDetail ?? string.Empty
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(EmployeeBusUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.EmployeeCode) || string.IsNullOrWhiteSpace(input.EmployeeName))
        {
            TempData["EmployeeBusError"] = "Vui lòng nhập đầy đủ Mã NV và Họ tên.";
            return RedirectToIndex(input);
        }

        var employeeCode = input.EmployeeCode?.Trim();
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            TempData["EmployeeBusError"] = "Mã nhân viên không hợp lệ.";
            return RedirectToIndex(input);
        }

        var localEmployee = await _employeeService.GetByEmployeeCodeAsync(employeeCode);
        if (localEmployee == null)
        {
            var agentEmployee = await _employeeService.GetAgentEmployeeByEmployeeIdAsync(employeeCode);
            if (agentEmployee != null)
            {
                localEmployee = await _employeeService.EnsureLocalEmployeeFromAgentAsync(agentEmployee);
            }
        }
        // Sau khi ensure thì lấy lại lần nữa cho chắc
        //localEmployee = await _employeeService.GetByEmployeeCodeAsync(input.EmployeeCode);

        var actor = GetCurrentActor();

        // Nếu đã có mã nhân viên thì cập nhật, tránh tạo trùng
        if (localEmployee != null)
        {
            var updateEntity = new TM_EMPLOYEE
            {
                ID = localEmployee.ID,
                CHR_USER_CREATE = localEmployee.CHR_USER_CREATE,
                DTM_CREATE = localEmployee.DTM_CREATE,
                CHR_USER_UPDATE = actor,
                DTM_UPDATE = DateTime.Now,
                CHR_EMPLOYEE_ID = employeeCode,
                CHR_EMPLOYEE_ADID = localEmployee.CHR_EMPLOYEE_ADID,
                CHR_EMPLOYEE_MAIL = localEmployee.CHR_EMPLOYEE_MAIL,
                NVCHR_EMPLOYEE_NAME = input.EmployeeName?.Trim(),
                INT_BUS_STOP_ID = input.BusStopId,
                CHR_SHIFT_CODE = localEmployee.CHR_SHIFT_CODE,
                CHR_CODE_DEPT = localEmployee.CHR_CODE_DEPT,
                CHR_CODE_SEC = input.Department?.Trim(),
                CHR_CODE_GROUP = localEmployee.CHR_CODE_GROUP,
                CHR_POSITION = localEmployee.CHR_POSITION,
                CHR_POSITION_GROUP = localEmployee.CHR_POSITION_GROUP,
                CHR_PHONE = input.Phone?.Trim(),
                DTM_BIRTHDAY = localEmployee.DTM_BIRTHDAY,
                CHR_ADDRESS = localEmployee.CHR_ADDRESS,
                NVCHR_CITY = input.City?.Trim(),
                NVCHR_WARD = input.Ward?.Trim(),
                NVCHR_ADDRESS_DETAIL = input.AddressDetail?.Trim(),
                NVCHR_SUGGESTED_ADDRESS = localEmployee.NVCHR_SUGGESTED_ADDRESS,
                NVCHR_SUGGESTED_PICKUP_FROM_BIVN = localEmployee.NVCHR_SUGGESTED_PICKUP_FROM_BIVN,
                BIT_GENDER = localEmployee.BIT_GENDER,
                BIT_IS_USE_BUS = true,
                BIT_IS_ACTIVE = true,
                CHR_NOTE = localEmployee.CHR_NOTE
            };

            var updateResp = await _tmEmployeeService.UpdateAsync(updateEntity);
            TempData[updateResp.Success ? "EmployeeBusSuccess" : "EmployeeBusError"] = updateResp.Success
                ? "Mã nhân viên đã tồn tại, dữ liệu được cập nhật thành công."
                : (updateResp.Message ?? "Không thể cập nhật CNV.");

            return RedirectToIndex(input);
        }

        localEmployee = new TM_EMPLOYEE
        {
            CHR_EMPLOYEE_ID = employeeCode,
            NVCHR_EMPLOYEE_NAME = input.EmployeeName?.Trim(),
            CHR_CODE_SEC = input.Department?.Trim(),
            CHR_PHONE = input.Phone?.Trim(),
            CHR_ADDRESS = null,
            NVCHR_CITY = input.City?.Trim(),
            NVCHR_WARD = input.Ward?.Trim(),
            NVCHR_ADDRESS_DETAIL = input.AddressDetail?.Trim(),
            INT_BUS_STOP_ID = input.BusStopId,
            BIT_IS_USE_BUS = true,
            BIT_IS_ACTIVE = true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };
        var resp = await _tmEmployeeService.AddAsyncV2(localEmployee);
        TempData[resp.Success ? "EmployeeBusSuccess" : "EmployeeBusError"] = resp.Success
            ? "Thêm CNV sử dụng xe công ty thành công."
            : (resp.Message ?? "Không thể thêm CNV.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(EmployeeBusUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }

        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["EmployeeBusError"] = "Không xác định được CNV cần cập nhật.";
            return RedirectToIndex(input);
        }

        var currentResp = await _tmEmployeeService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["EmployeeBusError"] = currentResp.Message ?? "Không tìm thấy CNV.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.EmployeeCode) || string.IsNullOrWhiteSpace(input.EmployeeName))
        {
            TempData["EmployeeBusError"] = "Vui lòng nhập đầy đủ Mã NV và Họ tên.";
            return RedirectToIndex(input);
        }

        //var duplicateMessage = await ValidateDuplicateEmployeeCodeAsync(input.EmployeeCode, input.ID);
        //if (!string.IsNullOrWhiteSpace(duplicateMessage))
        //{
        //    TempData["EmployeeBusError"] = duplicateMessage;
        //    return RedirectToIndex(input);
        //}

        var actor = GetCurrentActor();
        var entity = new TM_EMPLOYEE
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = input.EmployeeCode?.Trim(),
            CHR_EMPLOYEE_ADID = current.CHR_EMPLOYEE_ADID,
            CHR_EMPLOYEE_MAIL = current.CHR_EMPLOYEE_MAIL,
            NVCHR_EMPLOYEE_NAME = input.EmployeeName?.Trim(),
            INT_BUS_STOP_ID = input.BusStopId,
            CHR_SHIFT_CODE = current.CHR_SHIFT_CODE,
            CHR_CODE_DEPT = current.CHR_CODE_DEPT,
            CHR_CODE_SEC = input.Department?.Trim(),
            CHR_CODE_GROUP = current.CHR_CODE_GROUP,
            CHR_POSITION = current.CHR_POSITION,
            CHR_POSITION_GROUP = current.CHR_POSITION_GROUP,
            CHR_PHONE = input.Phone?.Trim(),
            DTM_BIRTHDAY = current.DTM_BIRTHDAY,
            CHR_ADDRESS = current.CHR_ADDRESS,
            NVCHR_CITY = input.City?.Trim(),
            NVCHR_WARD = input.Ward?.Trim(),
            NVCHR_ADDRESS_DETAIL = input.AddressDetail?.Trim(),
            BIT_GENDER = current.BIT_GENDER,
            BIT_IS_USE_BUS = true,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE,
            CHR_NOTE = current.CHR_NOTE
        };

        var resp = await _tmEmployeeService.UpdateAsync(entity);
        TempData[resp.Success ? "EmployeeBusSuccess" : "EmployeeBusError"] = resp.Success
            ? "Cập nhật CNV thành công."
            : (resp.Message ?? "Không thể cập nhật CNV.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? employeeCode, string? employeeName, string? department, string? phone, string? city, string? ward, int? busStopId, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
        }

        var resp = await _tmEmployeeService.DeleteAsync(id);
        TempData[resp.Success ? "EmployeeBusSuccess" : "EmployeeBusError"] = resp.Success
            ? "Xóa CNV thành công."
            : (resp.Message ?? "Không thể xóa CNV.");

        return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMany(List<int> ids, string? employeeCode, string? employeeName, string? department, string? phone, string? city, string? ward, int? busStopId, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
        }

        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["EmployeeBusError"] = "Vui lòng chọn ít nhất 1 CNV để xóa.";
            return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
        }

        var resp = await _tmEmployeeService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "EmployeeBusSuccess" : "EmployeeBusError"] = resp.Success
            ? $"Đã xóa {ids.Count} CNV."
            : (resp.Message ?? "Không thể xóa danh sách CNV.");

        return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? employeeCode, string? employeeName, string? department, string? phone, string? city, string? ward, int? busStopId)
    {
        var filters = BuildEmployeeFilters(employeeCode, employeeName, department, phone, city, ward, busStopId);
        var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var rows = employeeResp?.Data?.Data?.ToList() ?? new List<TM_EMPLOYEEDTO>();

        var busStopsResp = await _tmBusStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000
        });
        var sectionsResp = await _sectionService.GetAllAsync();
        var busStopMap = (busStopsResp?.Data?.Data ?? new List<TM_BUS_STOPDTO>()).ToDictionary(x => x.ID, x => x);
        var sectionMap = (sectionsResp?.Data ?? new List<TM_SECTIONDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("EmployeesUseBus");
        var headers = new[]
        {
            "STT", "Mã NV", "Họ và tên", "Mã phòng", "Tên phòng", "SĐT", "Tỉnh", "Xã", "Địa chỉ chi tiết", "Điểm đón trả ID", "Điểm đón trả"
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
        var stt = 1;
        foreach (var item in rows)
        {
            busStopMap.TryGetValue(item.INT_BUS_STOP_ID ?? 0, out var busStop);
            sectionMap.TryGetValue(item.CHR_CODE_SEC?.Trim() ?? string.Empty, out var section);
            var busStopName = busStop == null
                ? string.Empty
                : (!string.IsNullOrWhiteSpace(busStop.NVCHR_STOP_NAME)
                    ? $"{busStop.NVCHR_STOP_NAME}"
                    : busStop.NVCHR_STOP_NAME ?? busStop.NVCHR_STOP_NAME ?? string.Empty);

            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.CHR_EMPLOYEE_ID;
            sheet.Cell(row, 3).Value = item.NVCHR_EMPLOYEE_NAME;
            sheet.Cell(row, 4).Value = item.CHR_CODE_SEC;
            sheet.Cell(row, 5).Value = section?.CHR_NAME ?? section?.CHR_SECTION_NAME ?? string.Empty;
            sheet.Cell(row, 6).Value = item.CHR_PHONE;
            sheet.Cell(row, 7).Value = item.NVCHR_CITY;
            sheet.Cell(row, 8).Value = item.NVCHR_WARD;
            sheet.Cell(row, 9).Value = item.NVCHR_ADDRESS_DETAIL;
            sheet.Cell(row, 10).Value = item.INT_BUS_STOP_ID;
            sheet.Cell(row, 11).Value = busStopName;

            row++;
            stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachCNVSuDungXe_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile? importFile, string? employeeCode, string? employeeName, string? department, string? phone, string? city, string? ward, int? busStopId, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
        }

        if (importFile == null || importFile.Length == 0)
        {
            TempData["EmployeeBusError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (cells.Count < 3) return false;

            var employeeCodeValue = cells.ElementAtOrDefault(0)?.Trim();
            var employeeNameValue = cells.ElementAtOrDefault(1)?.Trim();
            var departmentValue = cells.ElementAtOrDefault(2)?.Trim();

            if (string.IsNullOrWhiteSpace(employeeCodeValue) || string.IsNullOrWhiteSpace(employeeNameValue))
            {
                return false;
            }

            var duplicateMessage = await ValidateDuplicateEmployeeCodeAsync(employeeCodeValue, null);
            if (!string.IsNullOrWhiteSpace(duplicateMessage))
            {
                return false;
            }

            var entity = new TM_EMPLOYEE
            {
                CHR_EMPLOYEE_ID = employeeCodeValue,
                NVCHR_EMPLOYEE_NAME = employeeNameValue,
                CHR_CODE_SEC = departmentValue,
                CHR_PHONE = cells.ElementAtOrDefault(3)?.Trim(),
                NVCHR_CITY = cells.ElementAtOrDefault(4)?.Trim(),
                NVCHR_WARD = cells.ElementAtOrDefault(5)?.Trim(),
                NVCHR_ADDRESS_DETAIL = cells.ElementAtOrDefault(6)?.Trim(),
                INT_BUS_STOP_ID = int.TryParse(cells.ElementAtOrDefault(7), out var busStopId) ? busStopId : null,
                BIT_IS_USE_BUS = true,
                BIT_IS_ACTIVE = ParseBool(cells.ElementAtOrDefault(8)) ?? true,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmEmployeeService.AddAsyncV2(entity);
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
                TempData["EmployeeBusError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
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

                if (row == 1 && line.Contains("EmployeeCode", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "EmployeeBusSuccess" : "EmployeeBusError"] = failed == 0
            ? $"Import thành công {success} CNV."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, phone, city, ward, busStopId, page, pageSize });
    }

    private IActionResult RedirectToIndex(EmployeeBusUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            employeeCode = input.SearchEmployeeCode,
            employeeName = input.SearchEmployeeName,
            department = input.SearchDepartment,
            phone = input.SearchPhone,
            city = input.SearchCity,
            ward = input.SearchWard,
            busStopId = input.SearchBusStopId,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private static List<FilterOption> BuildEmployeeFilters(string? employeeCode, string? employeeName, string? department, string? phone, string? city, string? ward, int? busStopId)
    {
        var filters = new List<FilterOption>
        {
            new() { Field = "BIT_IS_USE_BUS", Operator = "=", Value = true }
        };

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            filters.Add(new FilterOption { Field = "CHR_EMPLOYEE_ID", Operator = "LIKE", Value = $"%{employeeCode.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            filters.Add(new FilterOption { Field = "NVCHR_EMPLOYEE_NAME", Operator = "LIKE", Value = $"%{employeeName.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            filters.Add(new FilterOption { Field = "CHR_CODE_SEC", Operator = "=", Value = department.Trim() });
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            filters.Add(new FilterOption { Field = "CHR_PHONE", Operator = "LIKE", Value = $"%{phone.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            filters.Add(new FilterOption { Field = "NVCHR_CITY", Operator = "LIKE", Value = $"%{city.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(ward))
        {
            filters.Add(new FilterOption { Field = "NVCHR_WARD", Operator = "LIKE", Value = $"%{ward.Trim()}%" });
        }

        if (busStopId.HasValue && busStopId.Value > 0)
        {
            filters.Add(new FilterOption { Field = "INT_BUS_STOP_ID", Operator = "=", Value = busStopId.Value });
        }

        return filters;
    }

    private async Task<string?> ValidateDuplicateEmployeeCodeAsync(string? employeeCode, int? excludeId)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return null;
        }

        var resp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 10,
            Filters = new List<FilterOption>
            {
                new() { Field = "CHR_EMPLOYEE_ID", Operator = "=", Value = employeeCode.Trim() }
            }
        });

        var exists = (resp?.Data?.Data ?? new List<TM_EMPLOYEEDTO>())
            .Any(x => !excludeId.HasValue || x.ID != excludeId.Value);

        if (exists)
        {
            return "Mã nhân viên đã tồn tại trong hệ thống.";
        }

        return null;
    }

    private async Task<bool> CanManageAsync()
    {
        var adid = User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value;
        if (!string.IsNullOrWhiteSpace(adid) && (adid.Equals("admin", StringComparison.OrdinalIgnoreCase) || adid.Equals("sa", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var roleIdClaim = User?.Claims?.FirstOrDefault(c => c.Type == "role_id")?.Value;
        if (!int.TryParse(roleIdClaim, out var roleId) || roleId <= 0)
        {
            return false;
        }

        var roleResp = await _tmRoleService.GetByIdAsync(roleId);
        var roleName = roleResp?.Data?.NVCHR_ROLE_NAME?.Trim();

        return string.Equals(roleName, "GA", StringComparison.OrdinalIgnoreCase)
            || string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private string GetCurrentActor()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value
            ?? User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? "system";
    }

    private static bool? ParseBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw.Trim().ToLowerInvariant();
        return normalized switch
        {
            "1" => true,
            "0" => false,
            "true" => true,
            "false" => false,
            "yes" => true,
            "no" => false,
            "đang hoạt động" => true,
            "ngưng hoạt động" => false,
            _ => bool.TryParse(normalized, out var parsed) ? parsed : null
        };
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
