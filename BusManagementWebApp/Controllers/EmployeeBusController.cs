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
    private readonly ITmBusRouteAssignService _tmBusRouteAssignService;
    private readonly ITmBusService _tmBusService;
    private readonly ITmBusRouteService _tmBusRouteService;
    private readonly ITmBusRouteStopService _tmBusRouteStopService;
    private readonly ITmEmployeeRouteService _tmEmployeeRouteService;
    private readonly ITmEmployeeRouteHistoryService _tmEmployeeRouteHistoryService;
    private readonly ITmCityService _tmCityService;
    private readonly ITmWardService _tmWardService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ISectionService _sectionService;

    public EmployeeBusController(
        IEmployeeService employeeService,
        ITmEmployeeService tmEmployeeService,
        ITmBusStopService tmBusStopService,
        ITmBusRouteAssignService tmBusRouteAssignService,
        ITmBusService tmBusService,
        ITmBusRouteService tmBusRouteService,
        ITmBusRouteStopService tmBusRouteStopService,
        ITmEmployeeRouteService tmEmployeeRouteService,
        ITmEmployeeRouteHistoryService tmEmployeeRouteHistoryService,
        ITmCityService tmCityService,
        ITmWardService tmWardService,
        ITmRoleService tmRoleService,
        ISectionService sectionService)
    {
        _employeeService = employeeService;
        _tmEmployeeService = tmEmployeeService;
        _tmBusStopService = tmBusStopService;
        _tmBusRouteAssignService = tmBusRouteAssignService;
        _tmBusService = tmBusService;
        _tmBusRouteService = tmBusRouteService;
        _tmBusRouteStopService = tmBusRouteStopService;
        _tmEmployeeRouteService = tmEmployeeRouteService;
        _tmEmployeeRouteHistoryService = tmEmployeeRouteHistoryService;
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

        var routeAssignResp = await _tmBusRouteAssignService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_ROUTE_ASSIGN.BIT_IS_ACTIVE), Operator = "=", Value = true }
            }
        });

        var routeAssigns = routeAssignResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_ASSIGNDTO>();
        var routeAssignLookups = await BuildRouteAssignLookupAsync(routeAssigns);

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
            RouteAssignOptions = routeAssignLookups,
            PickupRouteMap = routeAssignLookups.ToDictionary(x => x.Id, x => x.DisplayName ?? "-"),
            ReturnRouteMap = routeAssignLookups.ToDictionary(x => x.Id, x => x.DisplayName ?? "-"),
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
                INT_BUS_ROUTE_ASSIGN_ID = input.RouteAssignId,
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
            if (updateResp.Success)
            {
                await SyncEmployeeRouteAsync(updateEntity.ID, updateEntity.CHR_EMPLOYEE_ID, input.RouteAssignId, input.BusStopId, actor, "UPSERT_FROM_EMPLOYEE_BUS");
            }
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
            INT_BUS_ROUTE_ASSIGN_ID = input.RouteAssignId,
            BIT_IS_USE_BUS = true,
            BIT_IS_ACTIVE = true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };
        var resp = await _tmEmployeeService.AddAsyncV2(localEmployee);
        if (resp.Success)
        {
            var savedId = resp.Data?.ID ?? localEmployee.ID;
            var savedCode = resp.Data?.CHR_EMPLOYEE_ID ?? localEmployee.CHR_EMPLOYEE_ID;
            await SyncEmployeeRouteAsync(savedId, savedCode, input.RouteAssignId, input.BusStopId, actor, "NEW_REGISTER");
        }
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
            INT_BUS_ROUTE_ASSIGN_ID = input.RouteAssignId,
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
        if (resp.Success)
        {
            await SyncEmployeeRouteAsync(entity.ID, entity.CHR_EMPLOYEE_ID, input.RouteAssignId, input.BusStopId, actor, "UPDATE_FROM_EMPLOYEE_BUS");
        }
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

        var actor = GetCurrentActor();
        var current = (await _tmEmployeeService.GetByIdAsync(id))?.Data;
        if (current != null)
        {
            await SyncEmployeeRouteAsync(current.ID, current.CHR_EMPLOYEE_ID, null, null, actor, "CANCEL_ROUTE");
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

        var actor = GetCurrentActor();
        var employeeResp = await _tmEmployeeService.GetByIdsAsync(ids);
        foreach (var employee in employeeResp?.Data ?? Enumerable.Empty<TM_EMPLOYEEDTO>())
        {
            await SyncEmployeeRouteAsync(employee.ID, employee.CHR_EMPLOYEE_ID, null, null, actor, "CANCEL_ROUTE");
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
        var cityResp = await _tmCityService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var wardResp = await _tmWardService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var routeAssignResp = await _tmBusRouteAssignService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_ROUTE_ASSIGN.BIT_IS_ACTIVE), Operator = "=", Value = true }
            }
        });

        var sectionsResp = await _sectionService.GetAllAsync();
        var busStopMap = (busStopsResp?.Data?.Data ?? new List<TM_BUS_STOPDTO>()).ToDictionary(x => x.ID, x => x);
        var sectionMap = (sectionsResp?.Data ?? new List<TM_SECTIONDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var cities = (cityResp?.Data?.Data ?? new List<TM_CITYDTO>()).Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_CITY_NAME)).ToList();
        var wards = (wardResp?.Data?.Data ?? new List<TM_WARDDTO>()).Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_WARD_NAME)).ToList();
        var activeRouteAssigns = routeAssignResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_ASSIGNDTO>();
        var routeAssignLookups = await BuildRouteAssignLookupAsync(activeRouteAssigns);
        var routeAssignMap = routeAssignLookups.ToDictionary(x => x.Id, x => x.DisplayName ?? string.Empty);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("EmployeesUseBus");
        var headers = new[]
        {
            "STT", "Mã NV", "Họ và tên", "Phòng ban", "SĐT", "Tỉnh", "Xã", "Địa chỉ chi tiết", "Điểm đón trả", "Tuyến xe đang hoạt động", "Trạng thái"
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
            sheet.Cell(row, 4).Value = section?.CHR_NAME ?? section?.CHR_SECTION_NAME ?? item.CHR_CODE_SEC ?? string.Empty;
            sheet.Cell(row, 5).Value = item.CHR_PHONE;
            sheet.Cell(row, 6).Value = item.NVCHR_CITY;
            sheet.Cell(row, 7).Value = item.NVCHR_WARD;
            sheet.Cell(row, 8).Value = item.NVCHR_ADDRESS_DETAIL;
            sheet.Cell(row, 9).Value = busStopName;
            sheet.Cell(row, 10).Value = item.INT_BUS_ROUTE_ASSIGN_ID.HasValue && routeAssignMap.TryGetValue(item.INT_BUS_ROUTE_ASSIGN_ID.Value, out var routeDisplay)
                ? routeDisplay
                : string.Empty;
            sheet.Cell(row, 11).Value = item.BIT_IS_ACTIVE == false ? "Ngưng hoạt động" : "Đang hoạt động";

            row++;
            stt++;
        }

        var sectionSheet = workbook.Worksheets.Add("_SectionLookup");
        var sectionItems = (sectionsResp?.Data ?? new List<TM_SECTIONDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        for (var i = 0; i < sectionItems.Count; i++)
        {
            sectionSheet.Cell(i + 1, 1).Value = sectionItems[i].CHR_NAME ?? sectionItems[i].CHR_SECTION_NAME ?? sectionItems[i].CHR_CODE ?? string.Empty;
            sectionSheet.Cell(i + 1, 2).Value = sectionItems[i].CHR_CODE ?? string.Empty;
        }
        sectionSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var citySheet = workbook.Worksheets.Add("_CityLookup");
        var cityItems = cities.GroupBy(x => x.NVCHR_CITY_NAME!.Trim(), StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        for (var i = 0; i < cityItems.Count; i++)
        {
            citySheet.Cell(i + 1, 1).Value = cityItems[i].NVCHR_CITY_NAME ?? string.Empty;
            citySheet.Cell(i + 1, 2).Value = cityItems[i].ID;
        }
        citySheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var wardSheet = workbook.Worksheets.Add("_WardLookup");
        var wardItems = wards.GroupBy(x => x.NVCHR_WARD_NAME!.Trim(), StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
        for (var i = 0; i < wardItems.Count; i++)
        {
            wardSheet.Cell(i + 1, 1).Value = wardItems[i].NVCHR_WARD_NAME ?? string.Empty;
            wardSheet.Cell(i + 1, 2).Value = wardItems[i].ID;
        }
        wardSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var stopSheet = workbook.Worksheets.Add("_BusStopLookup");
        var stopItems = busStopMap.Values.OrderBy(x => x.NVCHR_STOP_CITY).ThenBy(x => x.NVCHR_STOP_WARD).ThenBy(x => x.NVCHR_STOP_NAME).ToList();
        for (var i = 0; i < stopItems.Count; i++)
        {
            stopSheet.Cell(i + 1, 1).Value = stopItems[i].NVCHR_STOP_NAME ?? string.Empty;
            stopSheet.Cell(i + 1, 2).Value = stopItems[i].ID;
        }
        stopSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var routeSheet = workbook.Worksheets.Add("_RouteAssignLookup");
        for (var i = 0; i < routeAssignLookups.Count; i++)
        {
            routeSheet.Cell(i + 1, 1).Value = routeAssignLookups[i].DisplayName ?? string.Empty;
            routeSheet.Cell(i + 1, 2).Value = routeAssignLookups[i].Id;
        }
        routeSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var statusSheet = workbook.Worksheets.Add("_StatusLookup");
        statusSheet.Cell(1, 1).Value = "Đang hoạt động";
        statusSheet.Cell(1, 2).Value = 1;
        statusSheet.Cell(2, 1).Value = "Ngưng hoạt động";
        statusSheet.Cell(2, 2).Value = 0;
        statusSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var validationEndRow = Math.Max(row + 100, 200);
        for (var r = 2; r <= validationEndRow; r++)
        {
            if (sectionItems.Count > 0) sheet.Range(r, 4, r, 4).SetDataValidation().List(sectionSheet.Range(1, 1, sectionItems.Count, 1));
            if (cityItems.Count > 0) sheet.Range(r, 6, r, 6).SetDataValidation().List(citySheet.Range(1, 1, cityItems.Count, 1));
            if (wardItems.Count > 0) sheet.Range(r, 7, r, 7).SetDataValidation().List(wardSheet.Range(1, 1, wardItems.Count, 1));
            if (stopItems.Count > 0) sheet.Range(r, 9, r, 9).SetDataValidation().List(stopSheet.Range(1, 1, stopItems.Count, 1));
            if (routeAssignLookups.Count > 0) sheet.Range(r, 10, r, 10).SetDataValidation().List(routeSheet.Range(1, 1, routeAssignLookups.Count, 1));
            sheet.Range(r, 11, r, 11).SetDataValidation().List(statusSheet.Range(1, 1, 2, 1));
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

        var sections = (await _sectionService.GetAllAsync())?.Data ?? new List<TM_SECTIONDTO>();
        var sectionByName = sections
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .ToDictionary(
                x => (x.CHR_NAME ?? x.CHR_SECTION_NAME ?? x.CHR_CODE ?? string.Empty).Trim(),
                x => x.CHR_CODE?.Trim() ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        var busStops = ((await _tmBusStopService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 }))?.Data?.Data ?? new List<TM_BUS_STOPDTO>())
            .ToList();
        var stopByName = busStops
            .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_STOP_NAME))
            .GroupBy(x => x.NVCHR_STOP_NAME!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ID, StringComparer.OrdinalIgnoreCase);

        var routeAssigns = ((await _tmBusRouteAssignService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_ROUTE_ASSIGN.BIT_IS_ACTIVE), Operator = "=", Value = true }
            }
        }))?.Data?.Data ?? new List<TM_BUS_ROUTE_ASSIGNDTO>()).ToList();
        var routeLookup = await BuildRouteAssignLookupAsync(routeAssigns);
        var routeByDisplay = routeLookup
            .Where(x => !string.IsNullOrWhiteSpace(x.DisplayName))
            .ToDictionary(x => x.DisplayName!.Trim(), x => x.Id, StringComparer.OrdinalIgnoreCase);

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (cells.Count < 3) return false;

            var offset = 0;
            if (int.TryParse(cells.ElementAtOrDefault(0)?.Trim(), out _)
                && !string.IsNullOrWhiteSpace(cells.ElementAtOrDefault(1)))
            {
                offset = 1;
            }

            var employeeCodeValue = cells.ElementAtOrDefault(offset + 0)?.Trim();
            var employeeNameValue = cells.ElementAtOrDefault(offset + 1)?.Trim();
            var departmentText = cells.ElementAtOrDefault(offset + 2)?.Trim();
            var phoneValue = cells.ElementAtOrDefault(offset + 3)?.Trim();
            var cityValue = cells.ElementAtOrDefault(offset + 4)?.Trim();
            var wardValue = cells.ElementAtOrDefault(offset + 5)?.Trim();
            var addressDetailValue = cells.ElementAtOrDefault(offset + 6)?.Trim();
            var busStopText = cells.ElementAtOrDefault(offset + 7)?.Trim();
            var routeAssignText = cells.ElementAtOrDefault(offset + 8)?.Trim();
            var statusText = cells.ElementAtOrDefault(offset + 9)?.Trim();

            if (string.IsNullOrWhiteSpace(employeeCodeValue) || string.IsNullOrWhiteSpace(employeeNameValue))
            {
                return false;
            }

            var duplicateMessage = await ValidateDuplicateEmployeeCodeAsync(employeeCodeValue, null);
            if (!string.IsNullOrWhiteSpace(duplicateMessage))
            {
                return false;
            }

            var departmentValue = string.Empty;
            if (!string.IsNullOrWhiteSpace(departmentText))
            {
                if (!sectionByName.TryGetValue(departmentText, out departmentValue))
                {
                    departmentValue = departmentText;
                }
            }

            int? mappedBusStopId = null;
            if (!string.IsNullOrWhiteSpace(busStopText) && stopByName.TryGetValue(busStopText, out var stopId))
            {
                mappedBusStopId = stopId;
            }

            int? mappedRouteAssignId = null;
            if (!string.IsNullOrWhiteSpace(routeAssignText) && routeByDisplay.TryGetValue(routeAssignText, out var routeId))
            {
                mappedRouteAssignId = routeId;
            }

            var isActive = ParseBool(statusText) ?? true;

            var entity = new TM_EMPLOYEE
            {
                CHR_EMPLOYEE_ID = employeeCodeValue,
                NVCHR_EMPLOYEE_NAME = employeeNameValue,
                CHR_CODE_SEC = departmentValue,
                CHR_PHONE = phoneValue,
                NVCHR_CITY = cityValue,
                NVCHR_WARD = wardValue,
                NVCHR_ADDRESS_DETAIL = addressDetailValue,
                INT_BUS_STOP_ID = mappedBusStopId,
                INT_BUS_ROUTE_ASSIGN_ID = mappedRouteAssignId,
                BIT_IS_USE_BUS = true,
                BIT_IS_ACTIVE = isActive,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmEmployeeService.AddAsyncV2(entity);
            if (addResp.Success)
            {
                var savedId = addResp.Data?.ID ?? entity.ID;
                var savedCode = addResp.Data?.CHR_EMPLOYEE_ID ?? entity.CHR_EMPLOYEE_ID;
                await SyncEmployeeRouteAsync(savedId, savedCode, mappedRouteAssignId, mappedBusStopId, actor, "NEW_REGISTER");
            }
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
                var cells = new List<string>();
                for (var c = 1; c <= 11; c++)
                {
                    cells.Add(sheet.Cell(r, c).GetString());
                }

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

    [HttpGet]
    public async Task<IActionResult> GetActiveRouteAssignOptions(int? busStopId)
    {
        var assignResp = await _tmBusRouteAssignService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_ROUTE_ASSIGN.BIT_IS_ACTIVE), Operator = "=", Value = true }
            }
        });

        var assigns = assignResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_ASSIGNDTO>();
        if (busStopId.HasValue && busStopId.Value > 0)
        {
            var routeStopResp = await _tmBusRouteStopService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_BUS_ROUTE_STOP.INT_BUS_STOP_ID), Operator = "=", Value = busStopId.Value }
                }
            });

            var routeIds = (routeStopResp?.Data?.Data ?? Enumerable.Empty<TM_BUS_ROUTE_STOPDTO>())
                .Where(x => x.INT_ROUTE_ID.HasValue)
                .Select(x => x.INT_ROUTE_ID!.Value)
                .Distinct()
                .ToHashSet();

            assigns = assigns
                .Where(x => (x.INT_ROUTE_PICKUP_ID.HasValue && routeIds.Contains(x.INT_ROUTE_PICKUP_ID.Value))
                         || (x.INT_ROUTE_RETURN_ID.HasValue && routeIds.Contains(x.INT_ROUTE_RETURN_ID.Value)))
                .ToList();
        }

        var lookup = await BuildRouteAssignLookupAsync(assigns);
        return Json(lookup.Select(x => new { id = x.Id, displayName = x.DisplayName }));
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveRouteCapacities()
    {
        var assignResp = await _tmBusRouteAssignService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_ROUTE_ASSIGN.BIT_IS_ACTIVE), Operator = "=", Value = true }
            }
        });

        var assigns = assignResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_ASSIGNDTO>();
        var lookups = await BuildRouteAssignLookupAsync(assigns);
        var displayMap = lookups.ToDictionary(x => x.Id, x => x.DisplayName ?? string.Empty);

        var result = assigns.Select(x =>
        {
            var capacity = Math.Max(0, x.INT_CAPACITY ?? 0);
            var currentUser = Math.Max(0, x.INT_CURRENT_USER ?? 0);
            var percentRaw = capacity > 0 ? (currentUser * 100.0 / capacity) : 0;
            var percentBar = Math.Min(100, Math.Max(0, percentRaw));
            var level = percentRaw >= 100 ? "danger" : percentRaw >= 80 ? "warning" : "success";
            return new
            {
                id = x.ID,
                routeDisplay = displayMap.TryGetValue(x.ID, out var d) ? d : string.Empty,
                currentUser,
                capacity,
                percentRaw,
                percentBar,
                level
            };
        }).OrderByDescending(x => x.percentRaw).ThenBy(x => x.routeDisplay).ToList();

        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetRouteCapacityDetail(int assignId)
    {
        if (assignId <= 0)
        {
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        var assign = (await _tmBusRouteAssignService.GetByIdAsync(assignId))?.Data;
        if (assign == null)
        {
            return Json(new { success = false, message = "Không tìm thấy tuyến xe." });
        }

        var bus = (await _tmBusService.GetByIdAsync(assign.INT_BUS_ID ?? 0))?.Data;
        var pickupRoute = assign.INT_ROUTE_PICKUP_ID.HasValue ? (await _tmBusRouteService.GetByIdAsync(assign.INT_ROUTE_PICKUP_ID.Value))?.Data : null;
        var returnRoute = assign.INT_ROUTE_RETURN_ID.HasValue ? (await _tmBusRouteService.GetByIdAsync(assign.INT_ROUTE_RETURN_ID.Value))?.Data : null;
        var pickupStops = await GetRouteStopsForDetailAsync(assign.INT_ROUTE_PICKUP_ID);
        var returnStops = await GetRouteStopsForDetailAsync(assign.INT_ROUTE_RETURN_ID);

        return Json(new
        {
            success = true,
            data = new
            {
                busCode = bus?.CHR_BUS_CODE,
                busName = bus?.NVCHR_BUS_NAME,
                licensePlate = bus?.CHR_LICENSE_PLATE,
                busCapacity = assign.INT_CAPACITY ?? bus?.INT_CAPACITY ?? 0,
                currentUser = assign.INT_CURRENT_USER ?? 0,
                pickupRouteName = pickupRoute?.NVCHR_ROUTE_NAME,
                returnRouteName = returnRoute?.NVCHR_ROUTE_NAME,
                pickupStops,
                returnStops
            }
        });
    }

    private async Task<List<EmployeeRouteAssignLookupItem>> BuildRouteAssignLookupAsync(List<TM_BUS_ROUTE_ASSIGNDTO> assigns)
    {
        var buses = ((await _tmBusService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 }))?.Data?.Data ?? Enumerable.Empty<TM_BUSDTO>())
            .ToDictionary(x => x.ID, x => x);
        var routes = ((await _tmBusRouteService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 }))?.Data?.Data ?? Enumerable.Empty<TM_BUS_ROUTEDTO>())
            .ToDictionary(x => x.ID, x => x);

        return assigns
            .Select(a =>
            {
                var assignId = a.ID;
                if (assignId <= 0) return null;
                buses.TryGetValue(a.INT_BUS_ID ?? 0, out var bus);
                routes.TryGetValue(a.INT_ROUTE_PICKUP_ID ?? 0, out var pickupRoute);
                var pickupName = pickupRoute?.NVCHR_ROUTE_NAME;
                if (string.IsNullOrWhiteSpace(pickupName)) return null;
                return new EmployeeRouteAssignLookupItem
                {
                    Id = assignId,
                    DisplayName = $"{bus?.CHR_BUS_CODE} - {pickupName}"
                };
            })
            .Where(x => x != null)
            .Cast<EmployeeRouteAssignLookupItem>()
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .OrderBy(x => x.DisplayName)
            .ToList();
    }

    private async Task<List<object>> GetRouteStopsForDetailAsync(int? routeId)
    {
        if (!routeId.HasValue || routeId.Value <= 0) return new List<object>();

        var routeStopResp = await _tmBusRouteStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_ROUTE_STOP.INT_ROUTE_ID), Operator = "=", Value = routeId.Value }
            },
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_BUS_ROUTE_STOP.INT_ORDER), SortDirection = "ASC" }
            }
        });

        var routeStops = routeStopResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_STOPDTO>();
        var stopIds = routeStops.Where(x => x.INT_BUS_STOP_ID.HasValue).Select(x => x.INT_BUS_STOP_ID!.Value).Distinct().ToList();
        var stopMap = stopIds.Any()
            ? ((await _tmBusStopService.GetByIdsAsync(stopIds))?.Data ?? Enumerable.Empty<TM_BUS_STOPDTO>()).ToDictionary(x => x.ID, x => x)
            : new Dictionary<int, TM_BUS_STOPDTO>();

        return routeStops.Select(x =>
        {
            stopMap.TryGetValue(x.INT_BUS_STOP_ID ?? 0, out var stop);
            return (object)new
            {
                order = x.INT_ORDER ?? 0,
                stopName = stop?.NVCHR_STOP_NAME ?? string.Empty,
                stopAddress = stop?.NVCHR_STOP_ADDRESS ?? string.Empty,
                pickupTime = x.DTM_PICKUP_TIME?.ToString("HH:mm") ?? string.Empty
            };
        }).ToList();
    }

    private async Task<TM_EMPLOYEE_ROUTEDTO?> GetCurrentActiveEmployeeRouteAsync(string employeeCode)
    {
        var resp = await _tmEmployeeRouteService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE_ROUTE.CHR_EMPLOYEE_ID), Operator = "=", Value = employeeCode },
                new() { Field = nameof(TM_EMPLOYEE_ROUTE.BIT_IS_ACTIVE), Operator = "=", Value = true }
            },
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_EMPLOYEE_ROUTE.DTM_EFFECTIVE_FROM), SortDirection = "DESC" },
                new SortOption { Field = nameof(TM_EMPLOYEE_ROUTE.ID), SortDirection = "DESC" }
            }
        });

        return resp?.Data?.Data?.FirstOrDefault();
    }

    private async Task SyncEmployeeRouteAsync(int employeeId, string? employeeCodeRaw, int? routeAssignId, int? busStopId, string actor, string actionType)
    {
        if (string.IsNullOrWhiteSpace(employeeCodeRaw)) return;

        var employeeCode = employeeCodeRaw.Trim();
        var activeRoute = await GetCurrentActiveEmployeeRouteAsync(employeeCode);

        var oldRouteAssignId = activeRoute?.INT_ROUTE_ASSIGN_ID;
        var oldPickupStopId = activeRoute?.INT_PICKUP_STOP_ID;
        var oldReturnStopId = activeRoute?.INT_RETURN_STOP_ID;

        var newRouteAssignId = routeAssignId;
        var newPickupStopId = routeAssignId.HasValue ? busStopId : null;
        var newReturnStopId = routeAssignId.HasValue ? busStopId : null;

        var resolvedActionType = actionType;
        if (string.IsNullOrWhiteSpace(resolvedActionType) || resolvedActionType == "UPSERT_FROM_EMPLOYEE_BUS" || resolvedActionType == "UPDATE_FROM_EMPLOYEE_BUS")
        {
            if (!routeAssignId.HasValue) resolvedActionType = "CANCEL_ROUTE";
            else if (activeRoute == null) resolvedActionType = "NEW_REGISTER";
            else if (oldRouteAssignId != newRouteAssignId) resolvedActionType = "CHANGE_ROUTE";
            else if (oldPickupStopId != newPickupStopId) resolvedActionType = "CHANGE_STOP";
            else resolvedActionType = "CHANGE_ROUTE";
        }

        await _tmEmployeeRouteHistoryService.AddAsync(new TM_EMPLOYEE_ROUTE_HISTORY
        {
            INT_EMPLOYEE_ID = employeeId,
            CHR_EMPLOYEE_ID = employeeCode,
            INT_REQUEST_ID = 0,
            INT_OLD_ROUTE_ASSIGN_ID = oldRouteAssignId,
            INT_NEW_ROUTE_ASSIGN_ID = newRouteAssignId,
            INT_OLD_PICKUP_STOP_ID = oldPickupStopId,
            INT_NEW_PICKUP_STOP_ID = newPickupStopId,
            INT_OLD_RETURN_STOP_ID = oldReturnStopId,
            INT_NEW_RETURN_STOP_ID = newReturnStopId,
            DTM_NEW_EFFECTIVE_FROM = DateTime.Now,
            NVCHR_ACTION_TYPE = resolvedActionType,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        });

        if (!routeAssignId.HasValue)
        {
            if (activeRoute != null && activeRoute.BIT_IS_ACTIVE)
            {
                await _tmEmployeeRouteService.UpdateAsync(new TM_EMPLOYEE_ROUTE
                {
                    ID = activeRoute.ID,
                    CHR_USER_CREATE = activeRoute.CHR_USER_CREATE,
                    DTM_CREATE = activeRoute.DTM_CREATE,
                    CHR_USER_UPDATE = actor,
                    DTM_UPDATE = DateTime.Now,
                    INT_EMPLOYEE_ID = activeRoute.INT_EMPLOYEE_ID,
                    CHR_EMPLOYEE_ID = activeRoute.CHR_EMPLOYEE_ID,
                    INT_ROUTE_ASSIGN_ID = activeRoute.INT_ROUTE_ASSIGN_ID,
                    INT_PICKUP_STOP_ID = activeRoute.INT_PICKUP_STOP_ID,
                    INT_RETURN_STOP_ID = activeRoute.INT_RETURN_STOP_ID,
                    DTM_EFFECTIVE_FROM = activeRoute.DTM_EFFECTIVE_FROM,
                    DTM_EFFECTIVE_TO = DateTime.Now,
                    BIT_IS_ACTIVE = false
                });
                await RecalculateRouteAssignCurrentUserAsync(activeRoute.INT_ROUTE_ASSIGN_ID, actor);
            }
            return;
        }

        if (!busStopId.HasValue || busStopId.Value <= 0) return;

        if (activeRoute == null)
        {
            await _tmEmployeeRouteService.AddAsyncV2(new TM_EMPLOYEE_ROUTE
            {
                INT_EMPLOYEE_ID = employeeId,
                CHR_EMPLOYEE_ID = employeeCode,
                INT_ROUTE_ASSIGN_ID = routeAssignId.Value,
                INT_PICKUP_STOP_ID = busStopId.Value,
                INT_RETURN_STOP_ID = busStopId.Value,
                DTM_EFFECTIVE_FROM = DateTime.Now,
                DTM_EFFECTIVE_TO = null,
                BIT_IS_ACTIVE = true,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            });
            await RecalculateRouteAssignCurrentUserAsync(routeAssignId.Value, actor);
            return;
        }

        var sameRoute = activeRoute.INT_ROUTE_ASSIGN_ID == routeAssignId.Value
            && activeRoute.INT_PICKUP_STOP_ID == busStopId.Value
            && activeRoute.INT_RETURN_STOP_ID == busStopId.Value;

        await _tmEmployeeRouteService.UpdateAsync(new TM_EMPLOYEE_ROUTE
        {
            ID = activeRoute.ID,
            CHR_USER_CREATE = activeRoute.CHR_USER_CREATE,
            DTM_CREATE = activeRoute.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_EMPLOYEE_ID = activeRoute.INT_EMPLOYEE_ID,
            CHR_EMPLOYEE_ID = activeRoute.CHR_EMPLOYEE_ID,
            INT_ROUTE_ASSIGN_ID = routeAssignId.Value,
            INT_PICKUP_STOP_ID = busStopId.Value,
            INT_RETURN_STOP_ID = busStopId.Value,
            DTM_EFFECTIVE_FROM = DateTime.Now,
            DTM_EFFECTIVE_TO = null,
            BIT_IS_ACTIVE = true
        });

        if (!sameRoute)
        {
            await RecalculateRouteAssignCurrentUserAsync(activeRoute.INT_ROUTE_ASSIGN_ID, actor);
            await RecalculateRouteAssignCurrentUserAsync(routeAssignId.Value, actor);
        }
        else
        {
            await RecalculateRouteAssignCurrentUserAsync(routeAssignId.Value, actor);
        }
    }

    private async Task AdjustRouteAssignCurrentUserAsync(int routeAssignId, int delta, string actor)
    {
        if (routeAssignId <= 0 || delta == 0) return;

        var resp = await _tmBusRouteAssignService.GetByIdAsync(routeAssignId);
        var current = resp?.Data;
        if (current == null) return;

        var value = current.INT_CURRENT_USER ?? 0;
        var next = Math.Max(0, value + delta);
        if (next == value) return;

        await _tmBusRouteAssignService.UpdateAsync(new TM_BUS_ROUTE_ASSIGN
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_ROUTE_PICKUP_ID = current.INT_ROUTE_PICKUP_ID,
            INT_ROUTE_RETURN_ID = current.INT_ROUTE_RETURN_ID,
            INT_BUS_ID = current.INT_BUS_ID,
            INT_DRIVER_ID = current.INT_DRIVER_ID,
            INT_CURRENT_USER = next,
            CHR_BRA_TYPE = current.CHR_BRA_TYPE,
            INT_CAPACITY = current.INT_CAPACITY,
            DTM_EFFECTIVE_FROM = current.DTM_EFFECTIVE_FROM,
            DTM_EFFECTIVE_TO = current.DTM_EFFECTIVE_TO,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE
        });
    }

    private async Task RecalculateRouteAssignCurrentUserAsync(int? routeAssignId, string actor)
    {
        if (!routeAssignId.HasValue || routeAssignId.Value <= 0) return;

        var activeRouteResp = await _tmEmployeeRouteService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE_ROUTE.INT_ROUTE_ASSIGN_ID), Operator = "=", Value = routeAssignId.Value },
                new() { Field = nameof(TM_EMPLOYEE_ROUTE.BIT_IS_ACTIVE), Operator = "=", Value = true }
            }
        });

        var activeCount = (activeRouteResp?.Data?.Data ?? Enumerable.Empty<TM_EMPLOYEE_ROUTEDTO>()).Count();

        var assignResp = await _tmBusRouteAssignService.GetByIdAsync(routeAssignId.Value);
        var assign = assignResp?.Data;
        if (assign == null) return;

        if ((assign.INT_CURRENT_USER ?? 0) == activeCount) return;

        await _tmBusRouteAssignService.UpdateAsync(new TM_BUS_ROUTE_ASSIGN
        {
            ID = assign.ID,
            CHR_USER_CREATE = assign.CHR_USER_CREATE,
            DTM_CREATE = assign.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_ROUTE_PICKUP_ID = assign.INT_ROUTE_PICKUP_ID,
            INT_ROUTE_RETURN_ID = assign.INT_ROUTE_RETURN_ID,
            INT_BUS_ID = assign.INT_BUS_ID,
            INT_DRIVER_ID = assign.INT_DRIVER_ID,
            INT_CURRENT_USER = activeCount,
            CHR_BRA_TYPE = assign.CHR_BRA_TYPE,
            INT_CAPACITY = assign.INT_CAPACITY,
            DTM_EFFECTIVE_FROM = assign.DTM_EFFECTIVE_FROM,
            DTM_EFFECTIVE_TO = assign.DTM_EFFECTIVE_TO,
            BIT_IS_ACTIVE = assign.BIT_IS_ACTIVE
        });
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
