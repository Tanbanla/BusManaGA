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
    private readonly ITmEmployeeRouteService _tmEmployeeRouteService;

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
        ISectionService sectionService,
        ITmEmployeeRouteService tmEmployeeRouteService)
    {
        _tmBusRouteAssignService = tmBusRouteAssignService;
        _tmBusService = tmBusService;
        _tmBusRouteService = tmBusRouteService;
        _tmBusRequestService = tmBusRequestService;
        _tmEmployeeService = tmEmployeeService;
        _sectionService = sectionService;
        _tmEmployeeRouteService = tmEmployeeRouteService;
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
        if (string.IsNullOrWhiteSpace(vehicleType)) vehicleType = "Công nhân"; // Mặc định hiển thị data công nhân
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

        // Load active employee routes to calculate pickup/return usage per assignment
        var employeeRouteResp = await _tmEmployeeRouteService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE_ROUTE.BIT_IS_ACTIVE), Operator = "=", Value = true }
            }
        });

        var allEmployeeRoutes = employeeRouteResp?.Data?.Data ?? new List<TM_EMPLOYEE_ROUTEDTO>();

        foreach (var assign in assigns)
        {
            buses.TryGetValue(assign.INT_BUS_ID ?? 0, out var bus);
            routes.TryGetValue(assign.INT_ROUTE_PICKUP_ID ?? 0, out var route);

            if (bus == null || route == null) continue;

            var routeName = route.NVCHR_ROUTE_NAME ?? string.Empty;
            if (string.IsNullOrWhiteSpace(routeName)) continue;

            var vehicleTypeResolved = ResolveVehicleType(assign.CHR_BRA_TYPE);
            var area = ResolveArea(routeName);
            var routeDisplay = $"{assign.NVCHR_BUS_ROUTE_WORKING_NAME}";
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
                // Default aggregate (worker view)
                CurrentUsed = currentUsed,
                AvailableSeats = availableSeats,
                UsagePercent = usagePercent,
                IsFullCapacity = isFullCapacity,
                HasAvailableSeats = hasAvailableSeats,
                CapacityStatusClass = ResolveCapacityStatusClass(usagePercent),
                NoteStatusClass = isFullCapacity ? "bg-danger text-white" : ""
            };

            // If vehicle type is Staff (Nhân viên), compute pickup/return specific metrics
            if (string.Equals(vehicleTypeResolved, "Nhân viên", StringComparison.OrdinalIgnoreCase))
            {
                // Count employee routes assigned to this assignment
                var pickupAssigned = allEmployeeRoutes.Where(r => r.INT_ROUTE_ASSIGN_PICKUP_ID == assign.ID).ToList();
                var returnAssigned = allEmployeeRoutes.Where(r => r.INT_ROUTE_ASSIGN_RETURN_ID == assign.ID).ToList();

                var pickupUsed = pickupAssigned.Count;
                var returnUsed = returnAssigned.Count;
                var pickupAvailable = Math.Max(0, capacity - pickupUsed);
                var returnAvailable = Math.Max(0, capacity - returnUsed);
                var pickupPercent = capacity > 0 ? (pickupUsed * 100.0 / capacity) : 0;
                var returnPercent = capacity > 0 ? (returnUsed * 100.0 / capacity) : 0;

                // route names for pickup/return
                string pickupRouteName = string.Empty;
                string returnRouteName = string.Empty;
                if (assign.INT_ROUTE_PICKUP_ID.HasValue && routes.TryGetValue(assign.INT_ROUTE_PICKUP_ID.Value, out var rpu)) pickupRouteName = rpu.NVCHR_ROUTE_NAME ?? string.Empty;
                if (assign.INT_ROUTE_RETURN_ID.HasValue && routes.TryGetValue(assign.INT_ROUTE_RETURN_ID.Value, out var rre)) returnRouteName = rre.NVCHR_ROUTE_NAME ?? string.Empty;

                reportRow.PickupRouteDisplay = pickupRouteName;
                reportRow.ReturnRouteDisplay = returnRouteName;
                reportRow.PickupUsed = pickupUsed;
                reportRow.PickupAvailable = pickupAvailable;
                reportRow.PickupUsagePercent = pickupPercent;
                reportRow.ReturnUsed = returnUsed;
                reportRow.ReturnAvailable = returnAvailable;
                reportRow.ReturnUsagePercent = returnPercent;
                reportRow.CapacityStatusClass = ResolveCapacityStatusClass(pickupPercent); // for pickup column
                reportRow.NoteStatusClass = pickupAvailable <= 0 ? "bg-danger text-white" : "";
            }

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
        string[] headers;
        var isStaffView = string.Equals(vm.VehicleType, "Nhân viên", StringComparison.OrdinalIgnoreCase);
        if (isStaffView)
        {
            headers = new[] { "STT", "Xe", "Lộ trình chiều đi", "Lộ trình chiều về", "Loại xe", "Số lượng sử dụng chiều đi", "Số người có thể lấy thêm chiều đi", "Tỉ lệ sử dụng chiều đi (%)", "Số lượng sử dụng chiều về", "Số người có thể lấy thêm chiều về", "Tỉ lệ sử dụng chiều về (%)" };
        }
        else
        {
            headers = new[] { "STT", "Tuyến xe", "Phòng ban", "Loại xe", "Số ghế", "Số lượng sử dụng", "Số ghế còn trống", "Tỉ lệ sử dụng (%)", "Ghi chú" };
        }

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
            if (isStaffView)
            {
                // Staff view: show vehicle (bus code part of RouteDisplay), pickup/return route names, and per-direction metrics
                sheet.Cell(row, 2).Value = item.RouteDisplay; // contains bus code - route
                sheet.Cell(row, 3).Value = item.PickupRouteDisplay ?? string.Empty;
                sheet.Cell(row, 4).Value = item.ReturnRouteDisplay ?? string.Empty;
                sheet.Cell(row, 5).Value = item.VehicleType;
                sheet.Cell(row, 6).Value = item.PickupUsed;
                sheet.Cell(row, 7).Value = item.PickupAvailable;
                sheet.Cell(row, 8).Value = item.PickupUsagePercent.ToString("F1");
                sheet.Cell(row, 9).Value = item.ReturnUsed;
                sheet.Cell(row, 10).Value = item.ReturnAvailable;
                sheet.Cell(row, 11).Value = item.ReturnUsagePercent.ToString("F1");
            }
            else
            {
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

        // Determine vehicle type for this assignment
        var vehicleTypeResolved = ResolveVehicleType(assign.CHR_BRA_TYPE);

        // If staff vehicle, use TM_EMPLOYEE_ROUTE to fetch users by pickup/return assignment
        if (string.Equals(vehicleTypeResolved, "Nhân viên", StringComparison.OrdinalIgnoreCase))
        {
            // fetch active employee routes
            var empRouteResp = await _tmEmployeeRouteService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_EMPLOYEE_ROUTE.BIT_IS_ACTIVE), Operator = "=", Value = true }
                }
            });

            var empRoutes = empRouteResp?.Data?.Data?.ToList() ?? new List<TM_EMPLOYEE_ROUTEDTO>();

            // Pickup users
            var pickupRoutes = empRoutes.Where(r => r.INT_ROUTE_ASSIGN_PICKUP_ID == assignId).ToList();
            var pickupCodes = pickupRoutes.Select(r => r.CHR_EMPLOYEE_ID?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // Return users grouped by turn
            var returnRoutes = empRoutes.Where(r => r.INT_ROUTE_ASSIGN_RETURN_ID == assignId).ToList();
            var returnGrouped = returnRoutes
                .Where(r => !string.IsNullOrWhiteSpace(r.CHR_EMPLOYEE_ID))
                .GroupBy(r => r.INT_RETURN_TURN ?? 0)
                .ToDictionary(g => g.Key, g => g.Select(x => x.CHR_EMPLOYEE_ID!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList());

            // Aggregate employee codes to query details
            var allCodes = pickupCodes.Union(returnGrouped.SelectMany(kv => kv.Value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            var employeeDetails = new List<BusCapacityEmployeeDetailRowViewModel>();
            if (allCodes.Any())
            {
                var empFilters = new List<FilterOption>();
                for (var i = 0; i < allCodes.Count; i++)
                {
                    empFilters.Add(new FilterOption
                    {
                        Field = "CHR_EMPLOYEE_ID",
                        Operator = "=",
                        Value = allCodes[i],
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

                var deptResp = await _sectionService.GetAllAsync();
                var departments = deptResp?.Data ?? new List<TM_SECTIONDTO>();
                var deptMap = departments
                    .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
                    .ToDictionary(x => x.CHR_CODE!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

                employeeDetails = employees.Select(emp =>
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
                }).OrderBy(x => x.EmployeeCode).ToList();
            }

            // Build grouped return details with full employee detail objects
            var returnGroupsDetailed = new Dictionary<int, List<BusCapacityEmployeeDetailRowViewModel>>();
            foreach (var kv in returnGrouped)
            {
                var turn = kv.Key;
                var codes = kv.Value;
                var list = employeeDetails.Where(e => codes.Contains(e.EmployeeCode)).ToList();
                returnGroupsDetailed[turn] = list;
            }

            // pickup details
            var pickupDetails = employeeDetails.Where(e => pickupCodes.Contains(e.EmployeeCode)).ToList();

            return Json(new
            {
                success = true,
                data = new
                {
                    assignId = assign.ID,
                    totalUsers = pickupDetails.Count + returnGrouped.Values.SelectMany(x => x).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    pickup = new { total = pickupDetails.Count, employees = pickupDetails },
                    @return = new { groups = returnGroupsDetailed }
                }
            });
        }

        // Default behavior (worker): load approved requests for this route assignment
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

        // Load employee details (old behavior)
        var employeeCodes = requests
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
            .Select(x => x.CHR_EMPLOYEE_ID!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var employeeDetailsOld = new List<BusCapacityEmployeeDetailRowViewModel>();

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

            var deptResp = await _sectionService.GetAllAsync();
            var departments = deptResp?.Data ?? new List<TM_SECTIONDTO>();
            var deptMap = departments
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
                .ToDictionary(x => x.CHR_CODE!.Trim(), x => x, StringComparer.OrdinalIgnoreCase);

            employeeDetailsOld = employees
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
                totalUsers = employeeDetailsOld.Count,
                employees = employeeDetailsOld
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
