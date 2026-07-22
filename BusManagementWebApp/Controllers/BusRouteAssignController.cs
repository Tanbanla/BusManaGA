using BusManagement.Model.Common;
using BusManagement.Model.Common.Enum;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.BusRouteAssign;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Globalization;
using System.Security.Claims;

namespace BusManagement.Controllers;

public class BusRouteAssignController : Controller
{
    private readonly ITmBusRouteAssignService _tmBusRouteAssignService;
    private readonly ITmBusService _tmBusService;
    private readonly ITmDriverService _tmDriverService;
    private readonly ITmBusRouteService _tmBusRouteService;
    private readonly ITmBusRouteStopService _tmBusRouteStopService;
    private readonly ITmBusStopService _tmBusStopService;
    private readonly ITmRoleService _tmRoleService;

    public BusRouteAssignController(
        ITmBusRouteAssignService tmBusRouteAssignService,
        ITmBusService tmBusService,
        ITmDriverService tmDriverService,
        ITmBusRouteService tmBusRouteService,
        ITmBusRouteStopService tmBusRouteStopService,
        ITmBusStopService tmBusStopService,
        ITmRoleService tmRoleService)
    {
        _tmBusRouteAssignService = tmBusRouteAssignService;
        _tmBusService = tmBusService;
        _tmDriverService = tmDriverService;
        _tmBusRouteService = tmBusRouteService;
        _tmBusRouteStopService = tmBusRouteStopService;
        _tmBusStopService = tmBusStopService;
        _tmRoleService = tmRoleService;
    }

    #region Staff
    [HttpGet]
    public async Task<IActionResult> Index(string? busCode, string? driverName, string? licensePlate, bool? isActive)
    {
        var filters = BuildRouteAssignFilters(busCode, driverName, licensePlate, isActive, string.Empty);
        filters.Add(new FilterOption { Field = "CHR_BRA_TYPE", Operator = "=", Value = BusRouteAssignTypeEnum.Staff.ToString() });
        var joinOptions = BuildRouteAssignJoinOptions(busCode, driverName, licensePlate, isActive, string.Empty);
        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
            {
                new SortOption { Field = $"{nameof(TM_BUS_ROUTE_ASSIGN)}.DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var resp = await _tmBusRouteAssignService.SearchByConditionAsync(searchOptions);
        var assignments = resp?.Data?.Data?.ToList() ?? new();

        var viewModels = new List<BusRouteAssignViewModel>();
        foreach (var assign in assignments)
        {
            var vm = await BuildBusRouteAssignViewModelAsync(assign);
            if (vm != null)
            {
                viewModels.Add(vm);
            }
        }

        var pageVm = new BusRouteAssignPageViewModel
        {
            BusCode = busCode,
            DriverName = driverName,
            LicensePlate = licensePlate,
            IsActive = isActive,
            RouteAssigns = viewModels,
            CanManage = await CanManageAsync()
        };

        return View(pageVm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BusRouteAssignUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusRouteAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrEmpty(input.BusRouteAssignWorkingName))
        {
            TempData["BusRouteAssignError"] = "Vui lòng nhập tên lộ trình.";
            return RedirectToIndex(input);
        }

        if (!input.BusId.HasValue || !input.DriverId.HasValue)
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn xe và lái xe.";
            return RedirectToIndex(input);
        }

        if (!input.RoutePickupId.HasValue && !input.RouteReturnId.HasValue)
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn ít nhất một lộ trình (chiều đi hoặc chiều về).";
            return RedirectToIndex(input);
        }

        var busResp = await _tmBusService.GetByIdAsync(input.BusId ?? 0);
        var bus = busResp.Data;

        var actor = GetCurrentActor();
        var entity = new TM_BUS_ROUTE_ASSIGN
        {
            INT_BUS_ID = input.BusId,
            NVCHR_BUS_ROUTE_WORKING_NAME = input.BusRouteAssignWorkingName,
            INT_ROUTE_PICKUP_ID = input.RoutePickupId,
            INT_ROUTE_RETURN_ID = input.RouteReturnId,
            INT_DRIVER_ID = input.DriverId,
            CHR_BRA_TYPE = BusRouteAssignTypeEnum.Staff.ToString(),
            DTM_EFFECTIVE_FROM = input.EffectiveDate ?? DateTime.Now,
            CHR_USER_CREATE = actor,
            INT_CAPACITY = bus?.INT_CAPACITY ?? 0,
            DTM_CREATE = DateTime.Now,
            BIT_IS_ACTIVE = true
        };

        var resp = await _tmBusRouteAssignService.AddAsyncV2(entity);
        TempData[resp.Success ? "BusRouteAssignSuccess" : "BusRouteAssignError"] = resp.Success
            ? "Thêm lộ trình xe thành công."
            : (resp.Message ?? "Không thể thêm lộ trình xe.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(BusRouteAssignUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusRouteAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }

        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["BusRouteAssignError"] = "Không xác định được lộ trình xe cần cập nhật.";
            return RedirectToIndex(input);
        }

        if (!input.BusId.HasValue || !input.DriverId.HasValue)
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn xe và lái xe.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrEmpty(input.BusRouteAssignWorkingName))
        {
            TempData["BusRouteAssignError"] = "Vui lòng nhập tên lộ trình.";
            return RedirectToIndex(input);
        }

        if (!input.RoutePickupId.HasValue && !input.RouteReturnId.HasValue)
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn ít nhất một lộ trình (chiều đi hoặc chiều về).";
            return RedirectToIndex(input);
        }

        var busResp = await _tmBusService.GetByIdAsync(input.BusId ?? 0);
        var bus = busResp.Data;

        var currentResp = await _tmBusRouteAssignService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["BusRouteAssignError"] = currentResp.Message ?? "Không tìm thấy lộ trình xe.";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_ROUTE_ASSIGN
        {
            ID = current.ID,
            NVCHR_BUS_ROUTE_WORKING_NAME = input.BusRouteAssignWorkingName,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_BUS_ID = input.BusId,
            INT_ROUTE_PICKUP_ID = input.RoutePickupId,
            INT_ROUTE_RETURN_ID = input.RouteReturnId,
            INT_DRIVER_ID = input.DriverId,
            CHR_BRA_TYPE = current.CHR_BRA_TYPE,
            DTM_EFFECTIVE_FROM = input.EffectiveDate,
            DTM_EFFECTIVE_TO = current.DTM_EFFECTIVE_TO,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE,
            INT_CAPACITY = bus?.INT_CAPACITY ?? current.INT_CAPACITY,
            INT_CURRENT_USER = current.INT_CURRENT_USER
        };

        var resp = await _tmBusRouteAssignService.UpdateAsync(entity);
        TempData[resp.Success ? "BusRouteAssignSuccess" : "BusRouteAssignError"] = resp.Success
            ? "Cập nhật lộ trình xe thành công."
            : (resp.Message ?? "Không thể cập nhật lộ trình xe.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? busCode, string? driverName, string? licensePlate, bool? isActive)
    {
        if (!await CanManageAsync())
        {
            TempData["BusRouteAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { busCode, driverName, licensePlate, isActive });
        }

        var resp = await _tmBusRouteAssignService.DeleteAsync(id);
        TempData[resp.Success ? "BusRouteAssignSuccess" : "BusRouteAssignError"] = resp.Success
            ? "Xóa lộ trình xe thành công."
            : (resp.Message ?? "Không thể xóa lộ trình xe.");

        return RedirectToAction(nameof(Index), new { busCode, driverName, licensePlate, isActive });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMany(List<int> ids, string? busCode, string? driverName, string? licensePlate, bool? isActive)
    {
        if (!await CanManageAsync())
        {
            TempData["BusRouteAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { busCode, driverName, licensePlate, isActive });
        }

        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn ít nhất 1 lộ trình xe để xóa.";
            return RedirectToAction(nameof(Index), new { busCode, driverName, licensePlate, isActive });
        }

        var resp = await _tmBusRouteAssignService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "BusRouteAssignSuccess" : "BusRouteAssignError"] = resp.Success
            ? $"Đã xóa {ids.Count} lộ trình xe."
            : (resp.Message ?? "Không thể xóa danh sách lộ trình xe.");

        return RedirectToAction(nameof(Index), new { busCode, driverName, licensePlate, isActive });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? busCode, string? driverName, string? licensePlate, bool? isActive)
    {
        var filters = BuildRouteAssignFilters(busCode, driverName, licensePlate, isActive, string.Empty);
        var joinOptions = BuildRouteAssignJoinOptions(busCode, driverName, licensePlate, isActive, string.Empty);
        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
        {
            new SortOption { Field = $"{nameof(TM_BUS_ROUTE_ASSIGN)}.DTM_CREATE", SortDirection = "DESC" }
        }
        };
        var resp = await _tmBusRouteAssignService.SearchByConditionAsync(searchOptions);
        var assignments = resp?.Data?.Data?.ToList() ?? new();

        var viewModels = new List<BusRouteAssignViewModel>();
        foreach (var assign in assignments)
        {
            var vm = await BuildBusRouteAssignViewModelAsync(assign);
            if (vm != null) viewModels.Add(vm);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BusRouteAssigns");

        // Headers khớp với cột trong view (thêm STT ở đầu)
        var headers = new[]
        {
        "STT", "Xe", "Lộ trình chiều đi", "Lộ trình chiều về",
        "Loại xe", "Lái xe", "Điện thoại", "Biển số xe"
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
            // Dùng đúng logic giống view: ưu tiên stops, fallback về RouteName
            var pickupRoute = item.PickupStops.Any()
                ? string.Join(" - ", item.PickupStops.Select(s =>
                    $"({s.PickupTime?.ToString(@"hh\:mm")}) {s.StopName}"))
                : item.PickupRouteName ?? "—";

            //var returnRoute = item.ReturnStops.Any()
            //    ? string.Join(" - ", item.ReturnStops.Select(s =>
            //        $"({s.PickupTime?.ToString(@"hh\:mm")}) {s.StopName}"))
            //    : item.ReturnRouteName ?? "—";
            string returnRoute;
            if (item.ReturnStops.Any())
            {
                var groups = item.ReturnStops.GroupBy(s => s.Turn ?? 1).OrderBy(g => g.Key);
                var parts = new List<string>();
                foreach (var g in groups)
                {
                    var turnName = g.FirstOrDefault()?.TurnName ?? ("Lượt " + g.Key);
                    var stopsText = string.Join(" - ", g.OrderBy(s => s.Order).Select(s => $"({s.PickupTime?.ToString(@"hh\:mm")}) {s.StopName}"));
                    parts.Add($"{turnName}: {stopsText}");
                }
                returnRoute = string.Join(" | ", parts);
            }
            else
            {
                returnRoute = item.ReturnRouteName ?? "—";
            }

            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.BusCode;
            sheet.Cell(row, 3).Value = pickupRoute;
            sheet.Cell(row, 4).Value = returnRoute;
            sheet.Cell(row, 5).Value = item.BusCapacity;
            sheet.Cell(row, 6).Value = item.DriverName;
            sheet.Cell(row, 7).Value = item.DriverPhone;
            sheet.Cell(row, 8).Value = item.LicensePlate;

            row++;
            stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"Lotrinh_xe_dilam_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
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
    public async Task<IActionResult> GetRoutes(int? routeTypeId)
    {
        var filters = new List<FilterOption>
        {
            new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = true }
        };

        if (routeTypeId.HasValue)
        {
            filters.Add(new FilterOption { Field = "INT_ID_ROUTE_TYPE", Operator = "=", Value = routeTypeId.Value });
        }

        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters
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

    private IActionResult RedirectToIndex(BusRouteAssignUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            busCode = input.SearchBusCode,
            driverName = input.SearchDriverName,
            licensePlate = input.SearchLicensePlate,
            isActive = input.SearchIsActive
        });
    }

    private static List<FilterOption> BuildRouteAssignFilters(string? busCode, string? driverName, string? licensePlate, bool? isActive, string? routeName)
    {
        var filters = new List<FilterOption>();

        if (!string.IsNullOrWhiteSpace(routeName))
        {
            filters.Add(new FilterOption { Field = $"r.{nameof(TM_BUS_ROUTE.NVCHR_ROUTE_NAME)}", Operator = "LIKE", Value = $"{routeName.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(busCode))
        {
            filters.Add(new FilterOption { Field = $"b.{nameof(TM_BUS.CHR_BUS_CODE)}", Operator = "LIKE", Value = $"{busCode.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(driverName))
        {
            filters.Add(new FilterOption { Field = $"d.{nameof(TM_DRIVER.NVCHR_DRIVER_NAME)}", Operator = "LIKE", Value = $"{driverName.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            filters.Add(new FilterOption { Field = $"b.{nameof(TM_BUS.CHR_LICENSE_PLATE)}", Operator = "LIKE", Value = $"{licensePlate.Trim()}" });
        }

        return filters;
    }

    private static List<JoinOption> BuildRouteAssignJoinOptions(string? busCode, string? driverName, string? licensePlate, bool? isActive, string? routeName)
    {
        var joins = new List<JoinOption>();

        if (!string.IsNullOrWhiteSpace(routeName))
        {
            joins.Add(new JoinOption { JoinType = "LEFT JOIN", Table = nameof(TM_BUS_ROUTE), Alias = "r", OnCondition = $"{nameof(TM_BUS_ROUTE_ASSIGN)}.{nameof(TM_BUS_ROUTE_ASSIGN.INT_ROUTE_PICKUP_ID)} = r.ID" });
        }

        if (!string.IsNullOrWhiteSpace(busCode) || !string.IsNullOrWhiteSpace(licensePlate))
        {
            joins.Add(new JoinOption { JoinType = "LEFT JOIN", Table = nameof(TM_BUS), Alias = "b", OnCondition = $"{nameof(TM_BUS_ROUTE_ASSIGN)}.{nameof(TM_BUS_ROUTE_ASSIGN.INT_BUS_ID)} = b.ID" });
        }

        if (!string.IsNullOrWhiteSpace(driverName))
        {
            joins.Add(new JoinOption { JoinType = "LEFT JOIN", Table = nameof(TM_DRIVER), Alias = "d", OnCondition = $"{nameof(TM_BUS_ROUTE_ASSIGN)}.{nameof(TM_BUS_ROUTE_ASSIGN.INT_DRIVER_ID)} = d.ID" });
        }

        return joins;
    }

    private async Task<BusRouteAssignViewModel?> BuildBusRouteAssignViewModelAsync(TM_BUS_ROUTE_ASSIGNDTO assign)
    {
        try
        {
            var busResp = await _tmBusService.GetByIdAsync(assign.INT_BUS_ID ?? 0);
            var bus = busResp.Data;

            var driverResp = await _tmDriverService.GetByIdAsync(assign.INT_DRIVER_ID ?? 0);
            var driver = driverResp.Data;

            if (bus == null || driver == null)
                return null;

            var vm = new BusRouteAssignViewModel
            {
                ID = assign.ID,
                BusRouteAssignWorkingName = assign.NVCHR_BUS_ROUTE_WORKING_NAME,
                BusId = assign.INT_BUS_ID,
                BusCode = bus.CHR_BUS_CODE,
                BusName = bus.NVCHR_BUS_NAME,
                LicensePlate = bus.CHR_LICENSE_PLATE,
                BusCapacity = bus.INT_CAPACITY?.ToString(),
                DriverId = driver.ID,
                DriverName = driver.NVCHR_DRIVER_NAME,
                DriverPhone = driver.CHR_DRIVER_PHONE,
                EffectiveDate = assign.DTM_EFFECTIVE_FROM,
                CreatedDate = assign.DTM_CREATE,
                CreatedBy = assign.CHR_USER_CREATE,
                UpdatedDate = assign.DTM_UPDATE,
                UpdatedBy = assign.CHR_USER_UPDATE
            };

            // Get pickup route info
            if (assign.INT_ROUTE_PICKUP_ID.HasValue && assign.INT_ROUTE_PICKUP_ID.Value > 0)
            {
                var pickupRouteResp = await _tmBusRouteService.GetByIdAsync(assign.INT_ROUTE_PICKUP_ID.Value);
                var pickupRoute = pickupRouteResp.Data;
                if (pickupRoute != null)
                {
                    vm.PickupRouteId = pickupRoute.ID;
                    vm.PickupRouteName = pickupRoute.NVCHR_ROUTE_NAME;
                    vm.CodeDept = pickupRoute.CHR_CODE_DEPT;
                    vm.PickupStops = await GetRouteStopsAsync(assign.INT_ROUTE_PICKUP_ID.Value);
                }
            }

            // Get return route info
            if (assign.INT_ROUTE_RETURN_ID.HasValue && assign.INT_ROUTE_RETURN_ID.Value > 0)
            {
                var returnRouteResp = await _tmBusRouteService.GetByIdAsync(assign.INT_ROUTE_RETURN_ID.Value);
                var returnRoute = returnRouteResp.Data;
                if (returnRoute != null)
                {
                    vm.ReturnRouteId = returnRoute.ID;
                    vm.ReturnRouteName = returnRoute.NVCHR_ROUTE_NAME;
                    vm.CodeDept = returnRoute.CHR_CODE_DEPT;
                    vm.ReturnStops = await GetRouteStopsAsync(assign.INT_ROUTE_RETURN_ID.Value);
                }
            }

            return vm;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<RouteStopInfoViewModel>> GetRouteStopsAsync(int routeId)
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

        var stops = new List<RouteStopInfoViewModel>();
        foreach (var stop in routeStops)
        {
            var busStopResp = await _tmBusStopService.GetByIdAsync(stop.INT_BUS_STOP_ID ?? 0);
            var busStop = busStopResp.Data;

            if (busStop != null)
            {
                stops.Add(new RouteStopInfoViewModel
                {
                    Order = stop.INT_ORDER ?? 0,
                    StopName = busStop.NVCHR_STOP_NAME,
                    StopAddress = busStop.NVCHR_STOP_ADDRESS,
                    PickupTime = stop.DTM_PICKUP_TIME?.TimeOfDay,
                    Turn = stop.INT_TURN,
                    TurnName = stop.NVCHR_TURN_NAME
                });
            }
        }

        return stops.OrderBy(s => s.Order).ToList();
    }
    #endregion

    #region Worker
    [HttpGet]
    public async Task<IActionResult> IndexWorker(string? busCode, string? routeName, string? driverName, string? licensePlate, bool? isActive)
    {
        var filters = BuildRouteAssignFilters(busCode, driverName, licensePlate, isActive, routeName);
        filters.Add(new FilterOption { Field = "CHR_BRA_TYPE", Operator = "=", Value = BusRouteAssignTypeEnum.Worker.ToString() });
        var joinOptions = BuildRouteAssignJoinOptions(busCode, driverName, licensePlate, isActive, routeName);
        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
            {
                new SortOption { Field = $"{nameof(TM_BUS_ROUTE_ASSIGN)}.DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var resp = await _tmBusRouteAssignService.SearchByConditionAsync(searchOptions);
        var assignments = resp?.Data?.Data?.ToList() ?? new();

        var viewModels = new List<BusRouteAssignViewModel>();
        foreach (var assign in assignments)
        {
            var vm = await BuildBusRouteAssignViewModelAsync(assign);
            if (vm != null)
            {
                viewModels.Add(vm);
            }
        }

        var pageVm = new BusRouteAssignPageViewModel
        {
            BusCode = busCode,
            RouteName = routeName,
            DriverName = driverName,
            LicensePlate = licensePlate,
            IsActive = isActive,
            RouteAssigns = viewModels,
            CanManage = await CanManageAsync()
        };

        return View("IndexWorker", pageVm);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorker(BusRouteAssignUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusRouteAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndexWorker(input);
        }

        if (string.IsNullOrEmpty(input.BusRouteAssignWorkingName))
        {
            TempData["BusRouteAssignError"] = "Vui lòng nhập tên lộ trình.";
            return RedirectToIndex(input);
        }

        if (!input.BusId.HasValue || !input.DriverId.HasValue)
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn xe và lái xe.";
            return RedirectToIndexWorker(input);
        }

        if (!input.RoutePickupId.HasValue)
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn lộ trình đón công nhân.";
            return RedirectToIndexWorker(input);
        }

        var busResp = await _tmBusService.GetByIdAsync(input.BusId ?? 0);
        var bus = busResp.Data;

        var actor = GetCurrentActor();
        var entity = new TM_BUS_ROUTE_ASSIGN
        {
            INT_BUS_ID = input.BusId,
            NVCHR_BUS_ROUTE_WORKING_NAME = input.BusRouteAssignWorkingName,
            INT_ROUTE_PICKUP_ID = input.RoutePickupId,
            INT_ROUTE_RETURN_ID = null,
            INT_DRIVER_ID = input.DriverId,
            CHR_BRA_TYPE = BusRouteAssignTypeEnum.Worker.ToString(),
            DTM_EFFECTIVE_FROM = input.EffectiveDate ?? DateTime.Now,
            CHR_USER_CREATE = actor,
            INT_CAPACITY = bus?.INT_CAPACITY ?? 0,
            DTM_CREATE = DateTime.Now,
            BIT_IS_ACTIVE = true
        };

        var resp = await _tmBusRouteAssignService.AddAsyncV2(entity);
        TempData[resp.Success ? "BusRouteAssignSuccess" : "BusRouteAssignError"] = resp.Success
            ? "Thêm lộ trình xe thành công."
            : (resp.Message ?? "Không thể thêm lộ trình xe.");

        return RedirectToIndexWorker(input);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateWorker(BusRouteAssignUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusRouteAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndexWorker(input);
        }

        if (string.IsNullOrEmpty(input.BusRouteAssignWorkingName))
        {
            TempData["BusRouteAssignError"] = "Vui lòng nhập tên lộ trình.";
            return RedirectToIndex(input);
        }

        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["BusRouteAssignError"] = "Không xác định được lộ trình xe cần cập nhật.";
            return RedirectToIndexWorker(input);
        }

        if (!input.BusId.HasValue || !input.DriverId.HasValue)
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn xe và lái xe.";
            return RedirectToIndexWorker(input);
        }

        if (!input.RoutePickupId.HasValue)
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn lộ trình đón công nhân.";
            return RedirectToIndexWorker(input);
        }

        var busResp = await _tmBusService.GetByIdAsync(input.BusId ?? 0);
        var bus = busResp.Data;

        var currentResp = await _tmBusRouteAssignService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["BusRouteAssignError"] = currentResp.Message ?? "Không tìm thấy lộ trình xe.";
            return RedirectToIndexWorker(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_ROUTE_ASSIGN
        {
            ID = current.ID,
            NVCHR_BUS_ROUTE_WORKING_NAME = input.BusRouteAssignWorkingName,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_BUS_ID = input.BusId,
            INT_ROUTE_PICKUP_ID = input.RoutePickupId,
            INT_ROUTE_RETURN_ID = null,
            INT_DRIVER_ID = input.DriverId,
            CHR_BRA_TYPE = current.CHR_BRA_TYPE,
            DTM_EFFECTIVE_FROM = input.EffectiveDate,
            INT_CAPACITY = bus?.INT_CAPACITY ?? current.INT_CAPACITY,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE ?? true
        };

        var resp = await _tmBusRouteAssignService.UpdateAsync(entity);
        TempData[resp.Success ? "BusRouteAssignSuccess" : "BusRouteAssignError"] = resp.Success
            ? "Cập nhật lộ trình xe thành công."
            : (resp.Message ?? "Không thể cập nhật lộ trình xe.");

        return RedirectToIndexWorker(input);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteWorker(int id, string? busCode, string? driverName, string? licensePlate, bool? isActive)
    {
        if (!await CanManageAsync())
        {
            TempData["BusRouteAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(IndexWorker), new { busCode, driverName, licensePlate, isActive });
        }

        var resp = await _tmBusRouteAssignService.DeleteAsync(id);
        TempData[resp.Success ? "BusRouteAssignSuccess" : "BusRouteAssignError"] = resp.Success
            ? "Xóa lộ trình xe thành công."
            : (resp.Message ?? "Không thể xóa lộ trình xe.");

        return RedirectToAction(nameof(IndexWorker), new { busCode, driverName, licensePlate, isActive });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteManyWorker(List<int> ids, string? busCode, string? driverName, string? licensePlate, bool? isActive)
    {
        if (!await CanManageAsync())
        {
            TempData["BusRouteAssignError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(IndexWorker), new { busCode, driverName, licensePlate, isActive });
        }

        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["BusRouteAssignError"] = "Vui lòng chọn ít nhất 1 lộ trình xe để xóa.";
            return RedirectToAction(nameof(IndexWorker), new { busCode, driverName, licensePlate, isActive });
        }

        var resp = await _tmBusRouteAssignService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "BusRouteAssignSuccess" : "BusRouteAssignError"] = resp.Success
            ? $"Đã xóa {ids.Count} lộ trình xe."
            : (resp.Message ?? "Không thể xóa danh sách lộ trình xe.");

        return RedirectToAction(nameof(IndexWorker), new { busCode, driverName, licensePlate, isActive });
    }

    [HttpGet]
    public async Task<IActionResult> ExportWorker(string? busCode, string? routeName, string? driverName, string? licensePlate, bool? isActive)
    {
        var filters = BuildRouteAssignFilters(busCode, driverName, licensePlate, isActive, routeName);
        filters.Add(new FilterOption { Field = "CHR_BRA_TYPE", Operator = "=", Value = BusRouteAssignTypeEnum.Worker.ToString() });
        var joinOptions = BuildRouteAssignJoinOptions(busCode, driverName, licensePlate, isActive, routeName);
        var searchOptions = new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
            {
                new SortOption { Field = $"{nameof(TM_BUS_ROUTE_ASSIGN)}.DTM_CREATE", SortDirection = "DESC" }
            }
        };
        var resp = await _tmBusRouteAssignService.SearchByConditionAsync(searchOptions);
        var assignments = resp?.Data?.Data?.ToList() ?? new();

        var viewModels = new List<BusRouteAssignViewModel>();
        foreach (var assign in assignments)
        {
            var vm = await BuildBusRouteAssignViewModelAsync(assign);
            if (vm != null) viewModels.Add(vm);
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BusRouteAssignsWorker");

        var headers = new[]
        {
            "STT", "Tuyến xe", "Lái xe", "SĐT", "Lộ trình - Điểm đón"
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
            var pickupRoute = item.PickupStops.Any()
                ? string.Join(" - ", item.PickupStops.Select(s =>
                    $"({s.PickupTime?.ToString(@"hh\:mm")}) {s.StopName}"))
                : item.PickupRouteName ?? "—";

            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.PickupRouteName;
            sheet.Cell(row, 3).Value = item.DriverName;
            sheet.Cell(row, 4).Value = item.DriverPhone;
            sheet.Cell(row, 5).Value = pickupRoute;

            row++;
            stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"Lotrinh_xe_cong_nhan_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    private IActionResult RedirectToIndexWorker(BusRouteAssignUpsertInputModel input)
    {
        return RedirectToAction(nameof(IndexWorker), new
        {
            busCode = input.SearchBusCode,
            driverName = input.SearchDriverName,
            licensePlate = input.SearchLicensePlate,
            isActive = input.SearchIsActive
        });
    }

    #endregion

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
}
