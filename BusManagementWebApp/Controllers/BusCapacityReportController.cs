using BusManagement.Model.Common;
using BusManagement.Model.Common;
using BusManagement.Model.Common.Enum;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.BusCapacityReport;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace BusManagement.Controllers;

public class BusCapacityReportController : Controller
{
    private readonly ITmBusRouteAssignService _tmBusRouteAssignService;
    private readonly ITmBusService _tmBusService;
    private readonly ITmBusRouteService _tmBusRouteService;
    private readonly ITmBusRequestService _tmBusRequestService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly ISectionService _sectionService;

    private static readonly Dictionary<string, List<string>> VehicleAreaConfig = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nhân viên"] = new() { "Hà Nội", "Hải Phòng" },
        ["Công nhân"] = new() { "Hưng Yên", "Hải Phòng", "Thái Bình", "Bắc Ninh" }
    };

    public BusCapacityReportController(
        ITmBusRouteAssignService tmBusRouteAssignService,
        ITmBusService tmBusService,
        ITmBusRouteService tmBusRouteService,
        ITmBusRequestService tmBusRequestService,
        ITmEmployeeService tmEmployeeService,
        ISectionService sectionService)
    {
        _tmBusRouteAssignService = tmBusRouteAssignService;
        _tmBusService = tmBusService;
        _tmBusRouteService = tmBusRouteService;
        _tmBusRequestService = tmBusRequestService;
        _tmEmployeeService = tmEmployeeService;
        _sectionService = sectionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? employeeCode,
        string? departmentCode,
        string? vehicleType,
        int? minCapacity,
        int? maxCapacity,
        int? minUsed,
        int? maxUsed,
        int? minAvailable,
        int? maxAvailable,
        string? note,
        bool? showFullCapacity,
        bool? showAvailableOnly)
    {
        // Load all active route assignments
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

        // Load buses and routes
        var busesResp = await _tmBusService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var routesResp = await _tmBusRouteService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });

        var buses = (busesResp?.Data?.Data ?? new List<TM_BUSDTO>()).ToDictionary(x => x.ID, x => x);
        var routes = (routesResp?.Data?.Data ?? new List<TM_BUS_ROUTEDTO>()).ToDictionary(x => x.ID, x => x);

        // Load all approved bus requests for detailed employee info
        var requestsResp = await _tmBusRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.INT_STATUS)}", Operator = "=", Value = (int)BusRequestStatus.GaApproved }
            }
        });

        var approvedRequests = requestsResp?.Data?.Data?.ToList() ?? new List<TM_BUS_REQUESTDTO>();

        // Load employees for department resolution
        var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000
        });

        var employees = (employeeResp?.Data?.Data ?? new List<TM_EMPLOYEEDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
            .ToDictionary(x => x.CHR_EMPLOYEE_ID!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        // Load departments
        var deptResp = await _sectionService.GetAllAsync();
        var departments = deptResp?.Data ?? new List<TM_SECTIONDTO>();

        var deptMap = departments
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .ToDictionary(x => x.CHR_CODE!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

        // Build report data
        var reports = new List<BusCapacityReportRowViewModel>();

        foreach (var assign in assigns)
        {
            buses.TryGetValue(assign.INT_BUS_ID ?? 0, out var bus);
            routes.TryGetValue(assign.INT_ROUTE_PICKUP_ID ?? 0, out var route);

            if (bus == null || route == null) continue;

            var routeName = route.NVCHR_ROUTE_NAME ?? string.Empty;
            if (string.IsNullOrWhiteSpace(routeName)) continue;

            var vehicleTypeResolved = ResolveVehicleType(assign.CHR_BRA_TYPE);
            var area = ResolveArea(routeName);
            var routeDisplay = $"{bus.CHR_BUS_CODE} - {routeName}";
            var capacity = Math.Max(0, assign.INT_CAPACITY ?? 0);

            // Count users for this route assignment
            var usersForRoute = approvedRequests
                .Where(x => x.INT_BUS_ROUTE_ASSIGN_ID == assign.ID)
                .ToList();

            //var currentUsed = usersForRoute.Count;
            var currentUsed = assign.INT_CURRENT_USER ?? 0;
            var availableSeats = Math.Max(0, capacity - currentUsed);
            var usagePercent = capacity > 0 ? (currentUsed * 100.0 / capacity) : 0;
            var isFullCapacity = currentUsed >= capacity;
            var hasAvailableSeats = availableSeats > 0;

            // Apply filters
            if (!string.IsNullOrWhiteSpace(employeeCode))
            {
                var hasEmployee = usersForRoute.Any(x => 
                    x.CHR_EMPLOYEE_ID?.Trim().Equals(employeeCode.Trim(), StringComparison.OrdinalIgnoreCase) == true);
                if (!hasEmployee) continue;
            }

            if (!string.IsNullOrWhiteSpace(departmentCode))
            {
                var hasDepartment = usersForRoute.Any(x =>
                {
                    if (employees.TryGetValue(x.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var emp))
                    {
                        return emp.CHR_CODE_SEC?.Trim().Equals(departmentCode.Trim(), StringComparison.OrdinalIgnoreCase) == true;
                    }
                    return false;
                });
                if (!hasDepartment) continue;
            }

            if (!string.IsNullOrWhiteSpace(vehicleType) && 
                !vehicleTypeResolved.Equals(vehicleType, StringComparison.OrdinalIgnoreCase))
                continue;

            if (minCapacity.HasValue && capacity < minCapacity.Value) continue;
            if (maxCapacity.HasValue && capacity > maxCapacity.Value) continue;
            if (minUsed.HasValue && currentUsed < minUsed.Value) continue;
            if (maxUsed.HasValue && currentUsed > maxUsed.Value) continue;
            if (minAvailable.HasValue && availableSeats < minAvailable.Value) continue;
            if (maxAvailable.HasValue && availableSeats > maxAvailable.Value) continue;

            if (showFullCapacity == true && !isFullCapacity) continue;
            if (showAvailableOnly == true && !hasAvailableSeats) continue;

            // Determine department from employees using this route
            var dept = usersForRoute
                .Select(x => employees.TryGetValue(x.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var e) ? e : null)
                .FirstOrDefault(x => x != null)?
                .CHR_CODE_SEC;

            deptMap.TryGetValue(dept?.Trim() ?? string.Empty, out var deptEntity);

            var reportRow = new BusCapacityReportRowViewModel
            {
                Id = assign.ID,
                RouteDisplay = routeDisplay,
                DepartmentCode = dept,
                DepartmentName = deptEntity?.CHR_NAME ?? deptEntity?.CHR_SECTION_NAME ?? dept,
                VehicleType = vehicleTypeResolved,
                Capacity = capacity,
                CurrentUsed = currentUsed,
                AvailableSeats = availableSeats,
                UsagePercent = usagePercent,
                IsFullCapacity = isFullCapacity,
                HasAvailableSeats = hasAvailableSeats,
                CapacityStatusClass = ResolveCapacityStatusClass(usagePercent),
                NoteStatusClass = isFullCapacity ? "bg-danger text-white" : ""
            };

            reports.Add(reportRow);
        }

        // Apply note filter after building reports
        if (!string.IsNullOrWhiteSpace(note))
        {
            reports = reports
                .Where(x => (x.IsFullCapacity && note.Equals("stop", StringComparison.OrdinalIgnoreCase)) ||
                            (!x.IsFullCapacity && !note.Equals("stop", StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // Sort by usage percent descending
        reports = reports
            .OrderByDescending(x => x.UsagePercent)
            .ThenBy(x => x.RouteDisplay)
            .ToList();

        var vm = new BusCapacityReportPageViewModel
        {
            EmployeeCode = employeeCode,
            DepartmentCode = departmentCode,
            VehicleType = vehicleType,
            MinCapacity = minCapacity,
            MaxCapacity = maxCapacity,
            MinUsed = minUsed,
            MaxUsed = maxUsed,
            MinAvailable = minAvailable,
            MaxAvailable = maxAvailable,
            Note = note,
            ShowFullCapacity = showFullCapacity,
            ShowAvailableOnly = showAvailableOnly,
            Reports = reports,
            Departments = departments
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
                .OrderBy(x => x.CHR_CODE)
                .ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? employeeCode,
        string? departmentCode,
        string? vehicleType,
        int? minCapacity,
        int? maxCapacity,
        int? minUsed,
        int? maxUsed,
        int? minAvailable,
        int? maxAvailable,
        string? note,
        bool? showFullCapacity,
        bool? showAvailableOnly)
    {
        var result = await Index(employeeCode, departmentCode, vehicleType, minCapacity, maxCapacity, minUsed, maxUsed, minAvailable, maxAvailable, note, showFullCapacity, showAvailableOnly);
        if (result is not ViewResult vr || vr.Model is not BusCapacityReportPageViewModel vm)
        {
            return RedirectToAction(nameof(Index));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BusCapacityReport");
        var headers = new[] { "STT", "Tuyến xe", "Phòng ban", "Loại xe", "Số ghế", "Số lượng sử dụng", "Số ghế còn trống", "Tỉ lệ sử dụng (%)", "Ghi chú" };

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
        foreach (var item in vm.Reports)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.RouteDisplay;
            sheet.Cell(row, 3).Value = item.DepartmentName;
            sheet.Cell(row, 4).Value = item.VehicleType;
            sheet.Cell(row, 5).Value = item.Capacity;
            sheet.Cell(row, 6).Value = item.CurrentUsed;
            sheet.Cell(row, 7).Value = item.AvailableSeats;
            sheet.Cell(row, 8).Value = item.UsagePercent.ToString("F1");
            sheet.Cell(row, 9).Value = item.IsFullCapacity ? "STOP" : string.Empty;

            if (item.IsFullCapacity)
            {
                sheet.Range(row, 9, row, 9).Style.Fill.BackgroundColor = XLColor.Red;
                sheet.Range(row, 9, row, 9).Style.Font.FontColor = XLColor.White;
            }

            row++;
            stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"BaoCaoDungLuongXe_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetCapacityDetail(int assignId)
    {
        if (assignId <= 0)
        {
            return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
        }

        var assignResp = await _tmBusRouteAssignService.GetByIdAsync(assignId);
        var assign = assignResp?.Data;
        if (assign == null)
        {
            return Json(new { success = false, message = "Không tìm thấy tuyến xe." });
        }

        // Load approved requests for this route assignment
        var requestsResp = await _tmBusRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.INT_BUS_ROUTE_ASSIGN_ID)}", Operator = "=", Value = assignId },
                new() { Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.INT_STATUS)}", Operator = "=", Value = (int)BusRequestStatus.GaApproved }
            }
        });

        var requests = requestsResp?.Data?.Data?.ToList() ?? new List<TM_BUS_REQUESTDTO>();

        // Load employee details
        var employeeCodes = requests
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
            .Select(x => x.CHR_EMPLOYEE_ID!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var employeeDetails = new List<BusCapacityEmployeeDetailRowViewModel>();

        if (employeeCodes.Any())
        {
            var empFilters = new List<FilterOption>();
            for (var i = 0; i < employeeCodes.Count; i++)
            {
                empFilters.Add(new FilterOption
                {
                    Field = "CHR_EMPLOYEE_ID",
                    Operator = "=",
                    Value = employeeCodes[i],
                    LogicType = "OR"
                });
            }

            var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = empFilters
            });

            var employees = employeeResp?.Data?.Data?.ToList() ?? new List<TM_EMPLOYEEDTO>();

            // Load departments
            var deptResp = await _sectionService.GetAllAsync();
            var departments = deptResp?.Data ?? new List<TM_SECTIONDTO>();
            var deptMap = departments
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
                .ToDictionary(x => x.CHR_CODE!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

            employeeDetails = employees
                .Select(emp =>
                {
                    deptMap.TryGetValue(emp.CHR_CODE_SEC?.Trim() ?? string.Empty, out var dept);
                    return new BusCapacityEmployeeDetailRowViewModel
                    {
                        EmployeeCode = emp.CHR_EMPLOYEE_ID,
                        EmployeeName = emp.NVCHR_EMPLOYEE_NAME,
                        DepartmentName = dept?.CHR_NAME ?? dept?.CHR_SECTION_NAME ?? emp.CHR_CODE_SEC,
                        Position = emp.CHR_POSITION,
                        Address = emp.NVCHR_ADDRESS_DETAIL
                    };
                })
                .OrderBy(x => x.EmployeeCode)
                .ToList();
        }

        return Json(new
        {
            success = true,
            data = new
            {
                assignId = assign.ID,
                totalUsers = employeeDetails.Count,
                employees = employeeDetails
            }
        });
    }

    private static string ResolveVehicleType(string? braType)
    {
        return braType switch
        {
            nameof(BusRouteAssignTypeEnum.Worker) => "Công nhân",
            nameof(BusRouteAssignTypeEnum.Staff) => "Nhân viên",
            _ => "Nhân viên"
        };
    }

    private static string ResolveArea(string? routeName)
    {
        if (string.IsNullOrWhiteSpace(routeName)) return string.Empty;
        foreach (var area in VehicleAreaConfig.SelectMany(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (routeName.Contains(area, StringComparison.OrdinalIgnoreCase)) return area;
        }
        return string.Empty;
    }

    private static string ResolveCapacityStatusClass(double percent)
    {
        return percent switch
        {
            >= 100 => "bg-danger",
            >= 80 => "bg-warning",
            >= 50 => "bg-info",
            _ => "bg-success"
        };
    }
}
