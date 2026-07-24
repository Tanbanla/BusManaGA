using BusManagement.Model.Common;
using BusManagement.Model.Common.Enum;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.BusOtAssign;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace BusManagement.Controllers;

public class BusOtAssignController : Controller
{
    private readonly ITmBusOtAssignService _tmBusOtAssignService;
    private readonly ITmEmployeeRouteOTService _tmEmployeeRouteOTService;
    private readonly ITmBusService _tmBusService;
    private readonly ITmBusRouteService _tmBusRouteService;
    private readonly ITmBusRouteStopService _tmBusRouteStopService;
    private readonly ITmBusStopService _tmBusStopService;
    private readonly ITmDriverService _tmDriverService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ITmEmployeeService _tmEmployeeService;

    public BusOtAssignController(
        ITmBusOtAssignService tmBusOtAssignService,
        ITmEmployeeRouteOTService tmEmployeeRouteOTService,
        ITmBusService tmBusService,
        ITmBusRouteService tmBusRouteService,
        ITmBusRouteStopService tmBusRouteStopService,
        ITmBusStopService tmBusStopService,
        ITmDriverService tmDriverService,
        ITmRoleService tmRoleService,
        ITmEmployeeService tmEmployeeService)
    {
        _tmBusOtAssignService = tmBusOtAssignService;
        _tmEmployeeRouteOTService = tmEmployeeRouteOTService;
        _tmBusService = tmBusService;
        _tmBusRouteService = tmBusRouteService;
        _tmBusRouteStopService = tmBusRouteStopService;
        _tmBusStopService = tmBusStopService;
        _tmDriverService = tmDriverService;
        _tmRoleService = tmRoleService;
        _tmEmployeeService = tmEmployeeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? routeType, string? shift, string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate == null) fromDate = DateTime.Today;
        if (toDate == null) toDate = DateTime.Today;
        var filters = BuildOtAssignFilters(routeType, shift, busCode, driverName, licensePlate, fromDate, toDate);
        // INT_OT_TYPE = 0 là ngày thường
        filters.Add(new FilterOption { Field = "INT_OT_TYPE", Operator = "=", Value = 0 });

        var joinOptions = BuildOtAssignJoinOptions(busCode, driverName, licensePlate);

        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
            {
                new SortOption { Field = $"{nameof(TM_BUS_OT_ASSIGN)}.DTM_DATE", SortDirection = "DESC" },
                new SortOption { Field = $"{nameof(TM_BUS_OT_ASSIGN)}.DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var resp = await _tmBusOtAssignService.SearchByConditionAsync(searchOptions);
        var assignments = resp?.Data?.Data?.ToList() ?? new();

        var viewModels = new List<BusOtAssignRowViewModel>();
        foreach (var assign in assignments)
        {
            var vm = await BuildBusOtAssignViewModelAsync(assign);
            if (vm != null)
            {
                viewModels.Add(vm);
            }
        }

        // Group by CHR_ROUTE_TYPE
        var groupedByRouteType = viewModels.GroupBy(x => x.RouteType ?? "Không xác định")
            .OrderBy(g => g.Key)
            .ToList();

        var pageVm = new BusOtAssignPageViewModel
        {
            RouteType = routeType,
            Shift = shift,
            BusCode = busCode,
            DriverName = driverName,
            LicensePlate = licensePlate,
            FromDate = fromDate,
            ToDate = toDate,
            OtAssigns = viewModels,
            GroupedOtAssigns = groupedByRouteType,
            CanManage = await CanManageAsync()
        };

        return View(pageVm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BusOtAssignUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusOtAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }

        if (!input.BusId.HasValue || !input.DriverId.HasValue)
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn xe và lái xe.";
            return RedirectToIndex(input);
        }

        if (!input.RouteId.HasValue)
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn lộ trình.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.RouteType))
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn loại tuyến.";
            return RedirectToIndex(input);
        }

        var busResp = await _tmBusService.GetByIdAsync(input.BusId ?? 0);
        var bus = busResp.Data;

        var actor = GetCurrentActor();
        var entity = new TM_BUS_OT_ASSIGN
        {
            INT_BUS_ID = input.BusId,
            INT_DRIVER_ID = input.DriverId,
            INT_ROUTE_ID = input.RouteId,
            CHR_ROUTE_TYPE = input.RouteType,
            CHR_TYPE = "Nhân viên", // default
            CHR_SHIFT = input.Shift,
            DTM_DATE = input.OtDate ?? DateTime.Now,
            INT_CAPACITY = bus?.INT_CAPACITY ?? 0,
            INT_OT_TYPE = 0, // default: ngày thường
            BIT_IS_ACTIVE = true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmBusOtAssignService.AddAsyncV2(entity);
        TempData[resp.Success ? "BusOtAssignSuccess" : "BusOtAssignError"] = resp.Success
            ? "Thêm xe OT thành công."
            : (resp.Message ?? "Không thể thêm xe OT.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(BusOtAssignUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusOtAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }

        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["BusOtAssignError"] = "Không xác định được xe OT cần cập nhật.";
            return RedirectToIndex(input);
        }

        if (!input.BusId.HasValue || !input.DriverId.HasValue)
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn xe và lái xe.";
            return RedirectToIndex(input);
        }

        if (!input.RouteId.HasValue)
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn lộ trình.";
            return RedirectToIndex(input);
        }

        var busResp = await _tmBusService.GetByIdAsync(input.BusId ?? 0);
        var bus = busResp.Data;

        var currentResp = await _tmBusOtAssignService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["BusOtAssignError"] = currentResp.Message ?? "Không tìm thấy xe OT.";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_OT_ASSIGN
        {
            ID = current.ID,
            INT_BUS_ID = input.BusId,
            INT_DRIVER_ID = input.DriverId,
            INT_ROUTE_ID = input.RouteId,
            CHR_ROUTE_TYPE = input.RouteType ?? current.CHR_ROUTE_TYPE,
            CHR_TYPE = current.CHR_TYPE ?? "Nhân viên",
            CHR_SHIFT = input.Shift ?? current.CHR_SHIFT,
            DTM_DATE = input.OtDate ?? current.DTM_DATE,
            INT_CAPACITY = bus?.INT_CAPACITY ?? current.INT_CAPACITY,
            INT_OT_TYPE = current.INT_OT_TYPE ?? 0,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_CURRENT_USER = current.INT_CURRENT_USER,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE
        };

        var resp = await _tmBusOtAssignService.UpdateAsync(entity);
        TempData[resp.Success ? "BusOtAssignSuccess" : "BusOtAssignError"] = resp.Success
            ? "Cập nhật xe OT thành công."
            : (resp.Message ?? "Không thể cập nhật xe OT.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? routeType, string? shift, string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (!await CanManageAsync())
        {
            TempData["BusOtAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { routeType, shift, busCode, driverName, licensePlate, fromDate, toDate });
        }

        var resp = await _tmBusOtAssignService.DeleteAsync(id);
        TempData[resp.Success ? "BusOtAssignSuccess" : "BusOtAssignError"] = resp.Success
            ? "Xóa xe OT thành công."
            : (resp.Message ?? "Không thể xóa xe OT.");

        return RedirectToAction(nameof(Index), new { routeType, shift, busCode, driverName, licensePlate, fromDate, toDate });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMany(List<int> ids, string? routeType, string? shift, string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (!await CanManageAsync())
        {
            TempData["BusOtAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { routeType, shift, busCode, driverName, licensePlate, fromDate, toDate });
        }

        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn ít nhất 1 xe OT để xóa.";
            return RedirectToAction(nameof(Index), new { routeType, shift, busCode, driverName, licensePlate, fromDate, toDate });
        }

        var resp = await _tmBusOtAssignService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "BusOtAssignSuccess" : "BusOtAssignError"] = resp.Success
            ? $"Đã xóa {ids.Count} xe OT."
            : (resp.Message ?? "Không thể xóa danh sách xe OT.");

        return RedirectToAction(nameof(Index), new { routeType, shift, busCode, driverName, licensePlate, fromDate, toDate });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? routeType, string? shift, string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate == null) fromDate = DateTime.Today;
        if (toDate == null) toDate = DateTime.Today;
        var filters = BuildOtAssignFilters(routeType, shift, busCode, driverName, licensePlate, fromDate, toDate);
        filters.Add(new FilterOption { Field = "INT_OT_TYPE", Operator = "=", Value = 0 });

        var joinOptions = BuildOtAssignJoinOptions(busCode, driverName, licensePlate);

        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
            {
                new SortOption { Field = $"{nameof(TM_BUS_OT_ASSIGN)}.DTM_DATE", SortDirection = "DESC" },
                new SortOption { Field = $"{nameof(TM_BUS_OT_ASSIGN)}.DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var resp = await _tmBusOtAssignService.SearchByConditionAsync(searchOptions);
        var assignments = resp?.Data?.Data?.ToList() ?? new();

        var viewModels = new List<BusOtAssignRowViewModel>();
        foreach (var assign in assignments)
        {
            var vm = await BuildBusOtAssignViewModelAsync(assign);
            if (vm != null)
                viewModels.Add(vm);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BusOtAssigns");

        var headers = new[]
        {
            "STT", "Loại tuyến", "Ca làm việc", "Ngày OT", "Xe", "Lộ trình", "Lái xe", "Điện thoại", "Biển số xe", "Dung lượng"
        };

        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var row = 2;
        var stt = 1;
        foreach (var item in viewModels)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.RouteType;
            sheet.Cell(row, 3).Value = item.ShiftInfo;
            sheet.Cell(row, 4).Value = item.OtDate?.ToString("dd/MM/yyyy");
            sheet.Cell(row, 5).Value = item.BusCode;
            sheet.Cell(row, 6).Value = item.RouteName;
            sheet.Cell(row, 7).Value = item.DriverName;
            sheet.Cell(row, 8).Value = item.DriverPhone;
            sheet.Cell(row, 9).Value = item.LicensePlate;
            sheet.Cell(row, 10).Value = item.BusCapacity;

            row++;
            stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"Sap_xep_xe_OT_ngay_thuong_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetBusses()
    {
        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new()
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = true }
            }
        };

        var resp = await _tmBusService.SearchByConditionAsync(searchOptions);
        var buses = resp?.Data?.Data?.ToList() ?? new();

        return Json(buses.Select(b => new
        {
            id = b.ID,
            code = b.CHR_BUS_CODE,
            name = b.NVCHR_BUS_NAME,
            licensePlate = b.CHR_LICENSE_PLATE,
            capacity = b.INT_CAPACITY
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetDrivers()
    {
        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new()
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = true }
            }
        };

        var resp = await _tmDriverService.SearchByConditionAsync(searchOptions);
        var drivers = resp?.Data?.Data?.ToList() ?? new();

        return Json(drivers.Select(d => new
        {
            id = d.ID,
            name = d.NVCHR_DRIVER_NAME,
            phone = d.CHR_DRIVER_PHONE
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetRoutes()
    {
        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new()
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = true }
            }
        };

        var resp = await _tmBusRouteService.SearchByConditionAsync(searchOptions);
        var routes = resp?.Data?.Data?.ToList() ?? new();

        return Json(routes.Select(r => new
        {
            id = r.ID,
            name = r.NVCHR_ROUTE_NAME,
            type = r.NVCHR_ROUTE_TYPE
        }));
    }

    [HttpGet]
    public async Task<IActionResult> GetRouteStops(int routeId)
    {
        var filters = new List<FilterOption>
        {
            new FilterOption { Field = "INT_ROUTE_ID", Operator = "=", Value = routeId }
        };

        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "INT_ORDER", SortDirection = "ASC" }
            }
        };

        var resp = await _tmBusRouteStopService.SearchByConditionAsync(searchOptions);
        var routeStops = resp?.Data?.Data?.ToList() ?? new();

        var result = new List<dynamic>();
        foreach (var stop in routeStops)
        {
            var busStopResp = await _tmBusStopService.GetByIdAsync(stop.INT_BUS_STOP_ID ?? 0);
            var busStop = busStopResp.Data;
            if (busStop != null)
            {
                result.Add(new
                {
                    order = stop.INT_ORDER,
                    stopId = stop.INT_BUS_STOP_ID,
                    stopName = busStop.NVCHR_STOP_NAME,
                    stopAddress = busStop.NVCHR_STOP_ADDRESS,
                    pickupTime = stop.DTM_PICKUP_TIME?.ToString("HH:mm")
                });
            }
        }

        return Json(result);
    }

    private IActionResult RedirectToIndex(BusOtAssignUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            routeType = input.SearchRouteType,
            shift = input.SearchShift,
            busCode = input.SearchBusCode,
            driverName = input.SearchDriverName,
            licensePlate = input.SearchLicensePlate,
            fromDate = input.SearchFromDate,
            toDate = input.SearchToDate
        });
    }

    private static List<FilterOption> BuildOtAssignFilters(string? routeType, string? shift, string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate == null) fromDate = DateTime.Today;
        if (toDate == null) toDate = DateTime.Today;
        var filters = new List<FilterOption>();

        if (!string.IsNullOrWhiteSpace(routeType))
        {
            filters.Add(new FilterOption { Field = "CHR_ROUTE_TYPE", Operator = "LIKE", Value = $"{routeType.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(shift))
        {
            filters.Add(new FilterOption { Field = "CHR_SHIFT", Operator = "LIKE", Value = $"{shift.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(busCode))
        {
            filters.Add(new FilterOption { Field = "b.CHR_BUS_CODE", Operator = "LIKE", Value = $"{busCode.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(driverName))
        {
            filters.Add(new FilterOption { Field = "d.NVCHR_DRIVER_NAME", Operator = "LIKE", Value = $"{driverName.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            filters.Add(new FilterOption { Field = "b.CHR_LICENSE_PLATE", Operator = "LIKE", Value = $"{licensePlate.Trim()}" });
        }

        if (fromDate.HasValue)
        {
            filters.Add(new FilterOption { Field = "DTM_DATE", Operator = ">=", Value = fromDate.Value });
        }

        if (toDate.HasValue)
        {
            // Add 1 day to include the entire to date
            var endDate = toDate.Value.AddDays(1);
            filters.Add(new FilterOption { Field = "DTM_DATE", Operator = "<", Value = endDate });
        }

        return filters;
    }

    private static List<JoinOption> BuildOtAssignJoinOptions(string? busCode, string? driverName, string? licensePlate)
    {
        var joins = new List<JoinOption>();

        if (!string.IsNullOrWhiteSpace(busCode) || !string.IsNullOrWhiteSpace(licensePlate))
        {
            joins.Add(new JoinOption { JoinType = "LEFT JOIN", Table = nameof(TM_BUS), Alias = "b", OnCondition = $"{nameof(TM_BUS_OT_ASSIGN)}.INT_BUS_ID = b.ID" });
        }

        if (!string.IsNullOrWhiteSpace(driverName))
        {
            joins.Add(new JoinOption { JoinType = "LEFT JOIN", Table = nameof(TM_DRIVER), Alias = "d", OnCondition = $"{nameof(TM_BUS_OT_ASSIGN)}.INT_DRIVER_ID = d.ID" });
        }

        return joins;
    }

    private async Task<BusOtAssignRowViewModel?> BuildBusOtAssignViewModelAsync(TM_BUS_OT_ASSIGNDTO assign)
    {
        try
        {
            var busResp = await _tmBusService.GetByIdAsync(assign.INT_BUS_ID ?? 0);
            var bus = busResp.Data;

            var driverResp = await _tmDriverService.GetByIdAsync(assign.INT_DRIVER_ID ?? 0);
            var driver = driverResp.Data;

            if (bus == null || driver == null)
                return null;

            var routeResp = await _tmBusRouteService.GetByIdAsync(assign.INT_ROUTE_ID ?? 0);
            var route = routeResp.Data;

            var vm = new BusOtAssignRowViewModel
            {
                ID = assign.ID,
                BusId = assign.INT_BUS_ID,
                BusCode = bus.CHR_BUS_CODE,
                BusName = bus.NVCHR_BUS_NAME,
                LicensePlate = bus.CHR_LICENSE_PLATE,
                BusCapacity = bus.INT_CAPACITY?.ToString(),
                DriverId = driver.ID,
                DriverName = driver.NVCHR_DRIVER_NAME,
                DriverPhone = driver.CHR_DRIVER_PHONE,
                RouteId = assign.INT_ROUTE_ID,
                RouteName = route?.NVCHR_ROUTE_NAME ?? "—",
                RouteType = assign.CHR_ROUTE_TYPE,
                Shift = assign.CHR_SHIFT,
                ShiftInfo = assign.CHR_SHIFT_TIME,
                ShiftNote = assign.CHR_SHIFT_NOTE,
                OtDate = assign.DTM_DATE,
                Type = assign.CHR_SHIFT_NOTE ?? assign.CHR_TYPE,
                CreatedDate = assign.DTM_CREATE,
                CreatedBy = assign.CHR_USER_CREATE,
                UpdatedDate = assign.DTM_UPDATE,
                UpdatedBy = assign.CHR_USER_UPDATE
            };

            // Get route stops if available
            if (assign.INT_ROUTE_ID.HasValue && assign.INT_ROUTE_ID.Value > 0)
            {
                vm.RouteStops = await GetRouteStopsAsync(assign.INT_ROUTE_ID.Value);
            }

            // Get employee count for this assignment (for Holiday screen)
            if (assign.INT_OT_TYPE == 1)
            {
                var empFilters = new List<FilterOption>
                {
                    new FilterOption { Field = "INT_OT_ASSIGN_ID", Operator = "=", Value = assign.ID }
                };

                var empSearchOptions = new SearchOptions
                {
                    PageNumber = -1,
                    PageSize = 100000,
                    Filters = empFilters
                };

                var empResp = await _tmEmployeeRouteOTService.SearchByConditionAsync(empSearchOptions);
                var employees = empResp?.Data?.Data?.ToList() ?? new();
                vm.CurrentUser = employees.Count;
            }

            return vm;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<RouteStopOtViewModel>> GetRouteStopsAsync(int routeId)
    {
        var filters = new List<FilterOption>
        {
            new FilterOption { Field = "INT_ROUTE_ID", Operator = "=", Value = routeId }
        };

        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "INT_ORDER", SortDirection = "ASC" }
            }
        };

        var stopsResp = await _tmBusRouteStopService.SearchByConditionAsync(searchOptions);
        var routeStops = stopsResp?.Data?.Data?.ToList() ?? new();

        var stops = new List<RouteStopOtViewModel>();
        foreach (var stop in routeStops)
        {
            var busStopResp = await _tmBusStopService.GetByIdAsync(stop.INT_BUS_STOP_ID ?? 0);
            var busStop = busStopResp.Data;

            if (busStop != null)
            {
                stops.Add(new RouteStopOtViewModel
                {
                    Order = stop.INT_ORDER ?? 0,
                    StopName = busStop.NVCHR_STOP_NAME,
                    StopAddress = busStop.NVCHR_STOP_ADDRESS,
                    PickupTime = stop.DTM_PICKUP_TIME?.TimeOfDay
                });
            }
        }

        return stops.OrderBy(s => s.Order).ToList();
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

    #region Holiday (INT_OT_TYPE = 1)
    [HttpGet]
    public async Task<IActionResult> IndexHoliday(string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate == null) fromDate = DateTime.Today;
        if (toDate == null) toDate = DateTime.Today;
        var filters = BuildOtAssignFilters(null, null, busCode, driverName, licensePlate, fromDate, toDate);
        // INT_OT_TYPE = 1 là ngày nghỉ
        filters.Add(new FilterOption { Field = "INT_OT_TYPE", Operator = "=", Value = 1 });

        var joinOptions = BuildOtAssignJoinOptions(busCode, driverName, licensePlate);

        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
            {
                new SortOption { Field = $"{nameof(TM_BUS_OT_ASSIGN)}.DTM_DATE", SortDirection = "DESC" },
                new SortOption { Field = $"{nameof(TM_BUS_OT_ASSIGN)}.DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var resp = await _tmBusOtAssignService.SearchByConditionAsync(searchOptions);
        var assignments = resp?.Data?.Data?.ToList() ?? new();

        var viewModels = new List<BusOtAssignRowViewModel>();
        foreach (var assign in assignments)
        {
            var vm = await BuildBusOtAssignViewModelAsync(assign);
            if (vm != null)
            {
                viewModels.Add(vm);
            }
        }

        // Group by Date for display
        var groupedByDate = viewModels.OrderBy(x => x.OtDate?.Date).GroupBy(x => x.OtDate?.Date.ToString("dd/MM/yyyy") ?? "").ToList();

        var pageVm = new BusOtAssignPageViewModel
        {
            BusCode = busCode,
            DriverName = driverName,
            LicensePlate = licensePlate,
            FromDate = fromDate,
            ToDate = toDate,
            OtAssigns = viewModels,
            GroupedOtAssigns = groupedByDate,
            CanManage = await CanManageAsync()
        };

        return View(pageVm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateHoliday(BusOtAssignUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusOtAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndexHoliday(input);
        }

        if (!input.BusId.HasValue || !input.DriverId.HasValue)
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn xe và lái xe.";
            return RedirectToIndexHoliday(input);
        }

        if (!input.RouteId.HasValue)
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn lộ trình.";
            return RedirectToIndexHoliday(input);
        }

        if (string.IsNullOrWhiteSpace(input.Shift))
        {
            TempData["BusOtAssignError"] = "Vui lòng nhập tên ca.";
            return RedirectToIndexHoliday(input);
        }

        var busResp = await _tmBusService.GetByIdAsync(input.BusId ?? 0);
        var bus = busResp.Data;

        var actor = GetCurrentActor();
        var entity = new TM_BUS_OT_ASSIGN
        {
            INT_BUS_ID = input.BusId,
            INT_DRIVER_ID = input.DriverId,
            INT_ROUTE_ID = input.RouteId,
            CHR_ROUTE_TYPE = input.RouteType,
            CHR_SHIFT = input.Shift,
            CHR_SHIFT_TIME = input.ShiftTime,
            CHR_SHIFT_NOTE = input.ShiftNote,
            DTM_DATE = input.OtDate ?? DateTime.Now,
            INT_CAPACITY = bus?.INT_CAPACITY ?? 0,
            INT_OT_TYPE = 1, // default: ngày nghỉ
            CHR_TYPE = "Nhân viên",
            BIT_IS_ACTIVE = true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmBusOtAssignService.AddAsyncV2(entity);
        TempData[resp.Success ? "BusOtAssignSuccess" : "BusOtAssignError"] = resp.Success
            ? "Thêm xe OT thành công."
            : (resp.Message ?? "Không thể thêm xe OT.");

        return RedirectToIndexHoliday(input);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateHoliday(BusOtAssignUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusOtAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndexHoliday(input);
        }

        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["BusOtAssignError"] = "Không xác định được xe OT cần cập nhật.";
            return RedirectToIndexHoliday(input);
        }

        if (!input.BusId.HasValue || !input.DriverId.HasValue)
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn xe và lái xe.";
            return RedirectToIndexHoliday(input);
        }

        if (!input.RouteId.HasValue)
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn lộ trình.";
            return RedirectToIndexHoliday(input);
        }

        var busResp = await _tmBusService.GetByIdAsync(input.BusId ?? 0);
        var bus = busResp.Data;

        var currentResp = await _tmBusOtAssignService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["BusOtAssignError"] = currentResp.Message ?? "Không tìm thấy xe OT.";
            return RedirectToIndexHoliday(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_OT_ASSIGN
        {
            ID = current.ID,
            INT_BUS_ID = input.BusId,
            INT_DRIVER_ID = input.DriverId,
            INT_ROUTE_ID = input.RouteId,
            CHR_ROUTE_TYPE = input.RouteType ?? current.CHR_ROUTE_TYPE,
            CHR_SHIFT = input.Shift ?? current.CHR_SHIFT,
            CHR_SHIFT_TIME = input.ShiftTime ?? current.CHR_SHIFT_TIME,
            CHR_SHIFT_NOTE = input.ShiftNote ?? current.CHR_SHIFT_NOTE,
            DTM_DATE = input.OtDate ?? current.DTM_DATE,
            INT_CAPACITY = bus?.INT_CAPACITY ?? current.INT_CAPACITY,
            INT_OT_TYPE = current.INT_OT_TYPE ?? 1,
            CHR_TYPE = current.CHR_TYPE ?? "Nhân viên",
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_CURRENT_USER = current.INT_CURRENT_USER,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE
        };

        var resp = await _tmBusOtAssignService.UpdateAsync(entity);
        TempData[resp.Success ? "BusOtAssignSuccess" : "BusOtAssignError"] = resp.Success
            ? "Cập nhật xe OT thành công."
            : (resp.Message ?? "Không thể cập nhật xe OT.");

        return RedirectToIndexHoliday(input);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteHoliday(int id, string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (!await CanManageAsync())
        {
            TempData["BusOtAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(IndexHoliday), new { busCode, driverName, licensePlate, fromDate, toDate });
        }

        var resp = await _tmBusOtAssignService.DeleteAsync(id);
        TempData[resp.Success ? "BusOtAssignSuccess" : "BusOtAssignError"] = resp.Success
            ? "Xóa xe OT thành công."
            : (resp.Message ?? "Không thể xóa xe OT.");

        return RedirectToAction(nameof(IndexHoliday), new { busCode, driverName, licensePlate, fromDate, toDate });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteManyHoliday(List<int> ids, string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (!await CanManageAsync())
        {
            TempData["BusOtAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(IndexHoliday), new { busCode, driverName, licensePlate, fromDate, toDate });
        }

        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["BusOtAssignError"] = "Vui lòng chọn ít nhất 1 xe OT để xóa.";
            return RedirectToAction(nameof(IndexHoliday), new { busCode, driverName, licensePlate, fromDate, toDate });
        }

        var resp = await _tmBusOtAssignService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "BusOtAssignSuccess" : "BusOtAssignError"] = resp.Success
            ? $"Đã xóa {ids.Count} xe OT."
            : (resp.Message ?? "Không thể xóa danh sách xe OT.");

        return RedirectToAction(nameof(IndexHoliday), new { busCode, driverName, licensePlate, fromDate, toDate });
    }

    [HttpGet]
    public async Task<IActionResult> ExportHoliday(string? busCode, string? driverName, string? licensePlate, DateTime? fromDate, DateTime? toDate)
    {
        if (fromDate == null) fromDate = DateTime.Today;
        if (toDate == null) toDate = DateTime.Today;
        var filters = BuildOtAssignFilters(null, null, busCode, driverName, licensePlate, fromDate, toDate);
        filters.Add(new FilterOption { Field = "INT_OT_TYPE", Operator = "=", Value = 1 });

        var joinOptions = BuildOtAssignJoinOptions(busCode, driverName, licensePlate);

        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
            {
                new SortOption { Field = $"{nameof(TM_BUS_OT_ASSIGN)}.DTM_DATE", SortDirection = "DESC" },
                new SortOption { Field = $"{nameof(TM_BUS_OT_ASSIGN)}.DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var resp = await _tmBusOtAssignService.SearchByConditionAsync(searchOptions);
        var assignments = resp?.Data?.Data?.ToList() ?? new();

        var viewModels = new List<BusOtAssignRowViewModel>();
        foreach (var assign in assignments)
        {
            var vm = await BuildBusOtAssignViewModelAsync(assign);
            if (vm != null)
                viewModels.Add(vm);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BusOtHoliday");

        var headers = new[]
        {
            "STT", "Ngày dùng xe", "Xe", "Ca làm việc", "Giờ ca", "Note ca", "Lộ trình", "Dung lượng", "Số lượng CNV", "Lái xe", "SĐT", "Biển số xe"
        };

        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var row = 2;
        var stt = 1;
        foreach (var item in viewModels.OrderBy(x => x.OtDate))
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.OtDate?.ToString("dd/MM/yyyy") ?? "";
            sheet.Cell(row, 3).Value = item.RouteType ?? "";
            sheet.Cell(row, 4).Value = item.Shift ?? "";
            sheet.Cell(row, 5).Value = item.ShiftInfo ?? "";
            sheet.Cell(row, 6).Value = item.Type ?? "";
            sheet.Cell(row, 7).Value = item.RouteName ?? "";
            sheet.Cell(row, 8).Value = item.BusCapacity ?? "";
            sheet.Cell(row, 9).Value = item.CurrentUser ?? 0;
            sheet.Cell(row, 10).Value = item.DriverName ?? "";
            sheet.Cell(row, 11).Value = item.DriverPhone ?? "";
            sheet.Cell(row, 12).Value = item.LicensePlate ?? "";

            row++;
            stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"Sap_xep_xe_OT_ngay_nghi_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployeesByAssign(int assignId)
    {
        try
        {
            var filters = new List<FilterOption>
            {
                new FilterOption { Field = "INT_BUS_OT_ASSIGN_ID", Operator = "=", Value = assignId }
            };

            var searchOptions = new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = filters
            };

            var resp = await _tmEmployeeRouteOTService.SearchByConditionAsync(searchOptions);
            var employeeRouteOts = resp?.Data?.Data?.ToList() ?? new();

            var result = new List<dynamic>();
            foreach (var empRouteOt in employeeRouteOts)
            {
                // Get employee details from employee ID
                var empResp = await _tmEmployeeService.GetByIdAsync(empRouteOt.INT_EMPLOYEE_ID);
                var emp = empResp?.Data;
                if (emp != null)
                {
                    result.Add(new
                    {
                        employeeCode = emp.CHR_EMPLOYEE_ID,
                        employeeName = emp.NVCHR_EMPLOYEE_NAME,
                        department = emp.CHR_CODE_DEPT ?? "—"
                    });
                }
            }

            return Json(result);
        }
        catch
        {
            return Json(new List<object>());
        }
    }

    private IActionResult RedirectToIndexHoliday(BusOtAssignUpsertInputModel input)
    {
        return RedirectToAction(nameof(IndexHoliday), new
        {
            busCode = input.SearchBusCode,
            driverName = input.SearchDriverName,
            licensePlate = input.SearchLicensePlate,
            fromDate = input.SearchFromDate,
            toDate = input.SearchToDate
        });
    }
    #endregion
}
