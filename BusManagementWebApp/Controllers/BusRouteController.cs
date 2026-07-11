using BusManagement.Model.Common;
using BusManagement.Model.Common.Enum;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Implementations;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.Route;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace BusManagement.Controllers;

public class BusRouteController : Controller
{
    private readonly ITmBusRouteService _tmBusRouteService;
    private readonly ITmBusStopService _tmBusStopService;
    private readonly ITmBusRouteStopService _tmBusRouteStopService;
    private readonly ITmRoleService _tmRoleService;

    public BusRouteController(ITmBusRouteService tmBusRouteService, ITmBusStopService tmBusStopService, ITmBusRouteStopService tmBusRouteStopService, ITmRoleService tmRoleService)
    {
        _tmBusRouteService = tmBusRouteService;
        _tmBusStopService = tmBusStopService;
        _tmBusRouteStopService = tmBusRouteStopService;
        _tmRoleService = tmRoleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? name, string? type, string? dept, bool? isActive, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var filters = BuildRouteFilters(name, type, dept, isActive);
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

        var resp = await _tmBusRouteService.SearchByConditionAsync(searchOptions);

        var vm = new RoutePageViewModel
        {
            Name = name,
            Type = type,
            Dept = dept,
            IsActive = isActive,
            PageNumber = page,
            PageSize = pageSize,
            Routes = resp?.Data?.Data?.ToList() ?? new(),
            TotalCount = (int)(resp?.Data?.TotalCount ?? 0),
            TotalPages = (int)(resp?.Data?.TotalPages ?? 0),
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

    [HttpPost]
    public async Task<IActionResult> Create(RouteUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["RouteError"] = "Vui lòng nhập tên lộ trình.";
            return RedirectToIndex(input);
        }

        if (!TryResolveRouteType(input.RouteTypeId, input.Type, out var routeTypeId, out var routeTypeText))
        {
            TempData["RouteError"] = "Vui lòng chọn loại tuyến hợp lệ (Công nhân/Nhân viên).";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_ROUTE
        {
            NVCHR_ROUTE_NAME = input.Name?.Trim(),
            NVCHR_ROUTE_TYPE = routeTypeText,
            CHR_CODE_DEPT = input.Dept?.Trim(),
            CHR_CODE_SEC = input.Sec?.Trim(),
            INT_ID_ROUTE_TYPE = routeTypeId,
            BIT_IS_ACTIVE = input.IsActive ?? true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmBusRouteService.AddAsyncV2(entity);
        TempData[resp.Success ? "RouteSuccess" : "RouteError"] = resp.Success
            ? "Thêm lộ trình thành công."
            : (resp.Message ?? "Không thể thêm lộ trình.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(RouteUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }
        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["RouteError"] = "Không xác định được lộ trình cần cập nhật.";
            return RedirectToIndex(input);
        }

        var currentResp = await _tmBusRouteService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["RouteError"] = currentResp.Message ?? "Không tìm thấy lộ trình.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.Name))
        {
            TempData["RouteError"] = "Vui lòng nhập tên lộ trình.";
            return RedirectToIndex(input);
        }

        if (!TryResolveRouteType(input.RouteTypeId, input.Type, out var routeTypeId, out var routeTypeText))
        {
            TempData["RouteError"] = "Vui lòng chọn loại tuyến hợp lệ (Công nhân/Nhân viên).";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_ROUTE
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            NVCHR_ROUTE_NAME = input.Name?.Trim(),
            NVCHR_ROUTE_TYPE = routeTypeText,
            CHR_CODE_DEPT = input.Dept?.Trim(),
            CHR_CODE_SEC = input.Sec?.Trim(),
            INT_ID_ROUTE_TYPE = routeTypeId,
            BIT_IS_ACTIVE = input.IsActive
        };

        var resp = await _tmBusRouteService.UpdateAsync(entity);
        TempData[resp.Success ? "RouteSuccess" : "RouteError"] = resp.Success
            ? "Cập nhật lộ trình thành công."
            : (resp.Message ?? "Không thể cập nhật lộ trình.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? name, string? type, string? dept, bool? isActive, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { name, type, dept, isActive, page, pageSize });
        }
        await DeleteRouteStopsAsync(id);
        var resp = await _tmBusRouteService.DeleteAsync(id);
        TempData[resp.Success ? "RouteSuccess" : "RouteError"] = resp.Success
            ? "Xóa lộ trình thành công."
            : (resp.Message ?? "Không thể xóa lộ trình.");

        return RedirectToAction(nameof(Index), new { name, type, dept, isActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMany(List<int> ids, string? name, string? type, string? dept, bool? isActive, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["EmployeeBusError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { name, type, dept, isActive, page, pageSize });
        }
        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["RouteError"] = "Vui lòng chọn ít nhất 1 lộ trình để xóa.";
            return RedirectToAction(nameof(Index), new { name, type, dept, isActive, page, pageSize });
        }

        foreach (var routeId in ids)
        {
            await DeleteRouteStopsAsync(routeId);
        }

        var resp = await _tmBusRouteService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "RouteSuccess" : "RouteError"] = resp.Success
            ? $"Đã xóa {ids.Count} lộ trình."
            : (resp.Message ?? "Không thể xóa danh sách lộ trình.");

        return RedirectToAction(nameof(Index), new { name, type, dept, isActive, page, pageSize });
    }
    private async Task DeleteRouteStopsAsync(int routeId)
    {
        var existingResp = await _tmBusRouteStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
        {
            new FilterOption
            {
                Field = "INT_ROUTE_ID",
                Operator = "=",
                Value = routeId
            }
        }
        });

        var existing = existingResp?.Data?.Data?.ToList()
                       ?? new List<TM_BUS_ROUTE_STOPDTO>();

        if (existing.Any())
        {
            var ids = existing.Select(x => x.ID).ToList();

            await _tmBusRouteStopService.DeleteRangeAsync(ids);
        }
    }
    [HttpGet]
    public async Task<IActionResult> Export(string? name, string? type, string? dept, bool? isActive)
    {
        var filters = BuildRouteFilters(name, type, dept, isActive);
        var resp = await _tmBusRouteService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var rows = resp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTEDTO>();
        var routeTypeItems = GetRouteTypeItems();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Routes");
        var headers = new[]
        {
            "STT","Tên lộ trình","Loại","Mã phòng","Mã bộ phận","Loại tuyến","Trạng thái","Thời gian tạo","Thời gian cập nhật","Người cập nhật"
        };

        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var lookupSheet = workbook.Worksheets.Add("_RouteTypeLookup");
        for (var i = 0; i < routeTypeItems.Count; i++)
        {
            lookupSheet.Cell(i + 1, 1).Value = routeTypeItems[i].Text;
            lookupSheet.Cell(i + 1, 2).Value = routeTypeItems[i].Id;
        }
        lookupSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var row = 2;
        var stt = 1;
        foreach (var item in rows)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.NVCHR_ROUTE_NAME;
            sheet.Cell(row, 3).Value = item.NVCHR_ROUTE_TYPE;
            sheet.Cell(row, 4).Value = item.CHR_CODE_DEPT;
            sheet.Cell(row, 5).Value = item.CHR_CODE_SEC;
            sheet.Cell(row, 6).Value = item.INT_ID_ROUTE_TYPE;
            sheet.Cell(row, 7).Value = item.BIT_IS_ACTIVE == true ? "Đang hoạt động" : "Ngưng hoạt động";
            sheet.Cell(row, 8).Value = item.DTM_CREATE.ToString("dd/MM/yyyy HH:mm:ss");
            sheet.Cell(row, 9).Value = item.DTM_UPDATE?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty;
            sheet.Cell(row, 10).Value = item.CHR_USER_UPDATE;
            row++; stt++;
        }

        var validationEndRow = Math.Max(row + 100, 200);
        if (routeTypeItems.Count > 0)
        {
            for (var r = 2; r <= validationEndRow; r++)
            {
                var typeTextValidation = sheet.Range(r, 3, r, 3).SetDataValidation();
                typeTextValidation.List(lookupSheet.Range(1, 1, routeTypeItems.Count, 1));

                var typeIdValidation = sheet.Range(r, 6, r, 6).SetDataValidation();
                typeIdValidation.List(lookupSheet.Range(1, 2, routeTypeItems.Count, 2));
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachLoTrinh_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile? importFile, string? name, string? type, string? dept, bool? isActive, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["RouteError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { name, type, dept, isActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (!cells.Any()) return false;

            var offset = 0;
            var firstCell = cells.ElementAtOrDefault(0)?.Trim();
            if (int.TryParse(firstCell, out _)
                && !string.IsNullOrWhiteSpace(cells.ElementAtOrDefault(1)))
            {
                // exported format: STT, RouteName, RouteType, Dept, Sec, RouteTypeId, Capacity, ...
                offset = 1;
            }

            var routeName = cells.ElementAtOrDefault(offset)?.Trim();
            if (string.IsNullOrWhiteSpace(routeName)) return false;

            var routeTypeTextRaw = cells.ElementAtOrDefault(offset + 1)?.Trim();
            var routeTypeIdRaw = ParseInt(cells.ElementAtOrDefault(offset + 4));
            if (!TryResolveRouteType(routeTypeIdRaw, routeTypeTextRaw, out var routeTypeId, out var routeTypeText))
            {
                return false;
            }

            var entity = new TM_BUS_ROUTE
            {
                NVCHR_ROUTE_NAME = routeName,
                NVCHR_ROUTE_TYPE = routeTypeText,
                CHR_CODE_DEPT = cells.ElementAtOrDefault(offset + 2)?.Trim(),
                CHR_CODE_SEC = cells.ElementAtOrDefault(offset + 3)?.Trim(),
                INT_ID_ROUTE_TYPE = routeTypeId,
                BIT_IS_ACTIVE = true,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmBusRouteService.AddAsyncV2(entity);
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
                TempData["RouteError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Index), new { name, type, dept, isActive, page, pageSize });
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>();
                for (var c = 1; c <= 10; c++) cells.Add(sheet.Cell(r, c).GetString());
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
                if (row == 1 && (line.Contains("Tên") || line.Contains("Name"))) continue;
                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "RouteSuccess" : "RouteError"] = failed == 0
            ? $"Import thành công {success} lộ trình."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Index), new { name, type, dept, isActive, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> GetRouteStops(int routeId)
    {
        // get all active stops
        var stopsResp = await _tmBusStopService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000, Filters = new List<FilterOption>() });
        var stops = stopsResp?.Data?.Data?.ToList() ?? new List<TM_BUS_STOPDTO>();

        var rs = await _tmBusRouteStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption> { new FilterOption { Field = "INT_ROUTE_ID", Operator = "=", Value = routeId } },
            SortOptions = new() { new SortOption { Field = "INT_ORDER", SortDirection = "ASC" } }
        });

        var routeStops = rs?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_STOPDTO>();

        return Json(new { stops, routeStops });
    }

    [HttpGet]
    public async Task<IActionResult> GetRoutesForCopy()
    {
        var resp = await _tmBusRouteService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption> { new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = true } },
            SortOptions = new() { new SortOption { Field = "NVCHR_ROUTE_NAME", SortDirection = "ASC" } }
        });

        var routes = resp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTEDTO>();
        return Json(routes);
    }

    [HttpGet]
    public async Task<IActionResult> CopyRouteStops(int fromRouteId, int toRouteId)
    {
        // get source route stops
        var fromResp = await _tmBusRouteStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption> { new FilterOption { Field = "INT_ROUTE_ID", Operator = "=", Value = fromRouteId } },
            SortOptions = new() { new SortOption { Field = "INT_ORDER", SortDirection = "ASC" } }
        });
        var fromStops = fromResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_STOPDTO>();

        if (!fromStops.Any())
        {
            return Json(new { success = false, message = "Lộ trình nguồn không có điểm đón/trả nào." });
        }

        // remove existing stops from target
        var toResp = await _tmBusRouteStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption> { new FilterOption { Field = "INT_ROUTE_ID", Operator = "=", Value = toRouteId } }
        });
        var toStops = toResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_STOPDTO>();
        if (toStops.Any())
        {
            await _tmBusRouteStopService.DeleteRangeAsync(toStops.Select(x => x.ID).ToList());
        }

        // copy from to target
        var actor = GetCurrentActor();
        var order = 1;
        foreach (var src in fromStops)
        {
            var entity = new TM_BUS_ROUTE_STOP
            {
                INT_ROUTE_ID = toRouteId,
                INT_BUS_STOP_ID = src.INT_BUS_STOP_ID,
                INT_ORDER = order++,
                DTM_PICKUP_TIME = src.DTM_PICKUP_TIME
            };
            await _tmBusRouteStopService.AddAsyncV2(entity);
        }

        return Json(new { success = true, message = $"Đã sao chép {fromStops.Count} điểm từ lộ trình khác." });
    }

    [HttpPost]
    public async Task<IActionResult> SaveRouteStops(int routeId, int[] stopIds, string[] pickupTimes)
    {
        // remove existing
        var existingResp = await _tmBusRouteStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption> { new FilterOption { Field = "INT_ROUTE_ID", Operator = "=", Value = routeId } }
        });

        var existing = existingResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_STOPDTO>();
        if (existing.Any())
        {
            var ids = existing.Select(x => x.ID).ToList();
            await _tmBusRouteStopService.DeleteRangeAsync(ids);
        }

        var actor = GetCurrentActor();
        for (var i = 0; i < stopIds.Length; i++)
        {
            var sId = stopIds[i];
            DateTime? dt = null;
            if (i < pickupTimes.Length && DateTime.TryParse(pickupTimes[i], out var parsed)) dt = parsed;

            var entity = new TM_BUS_ROUTE_STOP
            {
                INT_ROUTE_ID = routeId,
                INT_BUS_STOP_ID = sId,
                INT_ORDER = i + 1,
                DTM_PICKUP_TIME = dt,
            };

            await _tmBusRouteStopService.AddAsyncV2(entity);

            var currentResp = await _tmBusRouteService.GetByIdAsync(routeId);
            var current = currentResp.Data;
            if (!currentResp.Success || current == null)
            {
                TempData["RouteError"] = currentResp.Message ?? "Không tìm thấy lộ trình.";
                return RedirectToAction(nameof(Index));
            }

            var busRouteDto = new TM_BUS_ROUTE
            {
                ID = current.ID,
                CHR_USER_CREATE = current.CHR_USER_CREATE,
                DTM_CREATE = current.DTM_CREATE,
                CHR_USER_UPDATE = actor,
                DTM_UPDATE = DateTime.Now,
                NVCHR_ROUTE_NAME = current.NVCHR_ROUTE_NAME,
                NVCHR_ROUTE_TYPE = current.NVCHR_ROUTE_TYPE,
                CHR_CODE_DEPT = current.CHR_CODE_DEPT,
                CHR_CODE_SEC = current.CHR_CODE_SEC,
                INT_ID_ROUTE_TYPE = current.INT_ID_ROUTE_TYPE,
                BIT_IS_ACTIVE = current.BIT_IS_ACTIVE
            };
            await _tmBusRouteService.UpdateAsync(busRouteDto);
        }

        TempData["RouteSuccess"] = "Cập nhật cấu hình điểm thành công.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectToIndex(RouteUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            name = input.SearchName,
            type = input.SearchType,
            dept = input.SearchDept,
            isActive = input.SearchIsActive,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private static List<FilterOption> BuildRouteFilters(string? name, string? type, string? dept, bool? isActive)
    {
        var filters = new List<FilterOption>();
        if (!string.IsNullOrWhiteSpace(name)) filters.Add(new FilterOption { Field = "NVCHR_ROUTE_NAME", Operator = "LIKE", Value = $"%{name.Trim()}%" });
        if (!string.IsNullOrWhiteSpace(type)) filters.Add(new FilterOption { Field = "NVCHR_ROUTE_TYPE", Operator = "LIKE", Value = $"%{type.Trim()}%" });
        if (!string.IsNullOrWhiteSpace(dept)) filters.Add(new FilterOption { Field = "CHR_CODE_DEPT", Operator = "LIKE", Value = $"%{dept.Trim()}%" });
        if (isActive.HasValue) filters.Add(new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = isActive.Value });
        return filters;
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

    private static int? ParseInt(string? s)
    {
        if (int.TryParse(s, out var v)) return v;
        return null;
    }

    private static List<(int Id, string Text)> GetRouteTypeItems()
    {
        return new List<(int Id, string Text)>
        {
            ((int)BusRouteAssignTypeEnum.Worker, "Công nhân"),
            ((int)BusRouteAssignTypeEnum.Staff, "Nhân viên")
        };
    }

    private static bool TryResolveRouteType(int? routeTypeId, string? routeTypeText, out int resolvedId, out string resolvedText)
    {
        resolvedId = 0;
        resolvedText = string.Empty;
        var items = GetRouteTypeItems();

        if (routeTypeId.HasValue)
        {
            var byId = items.FirstOrDefault(x => x.Id == routeTypeId.Value);
            if (byId.Id == routeTypeId.Value)
            {
                resolvedId = byId.Id;
                resolvedText = byId.Text;
                return true;
            }
        }

        var normalized = routeTypeText?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        var byText = items.FirstOrDefault(x => string.Equals(x.Text, normalized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(byText.Text))
        {
            resolvedId = byText.Id;
            resolvedText = byText.Text;
            return true;
        }

        if (int.TryParse(normalized, out var parsedId))
        {
            var byParsedId = items.FirstOrDefault(x => x.Id == parsedId);
            if (byParsedId.Id == parsedId)
            {
                resolvedId = byParsedId.Id;
                resolvedText = byParsedId.Text;
                return true;
            }
        }

        if (string.Equals(normalized, BusRouteAssignTypeEnum.Worker.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var workerId = (int)BusRouteAssignTypeEnum.Worker;
            resolvedId = workerId;
            resolvedText = items.First(x => x.Id == workerId).Text;
            return true;
        }

        if (string.Equals(normalized, BusRouteAssignTypeEnum.Staff.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var staffId = (int)BusRouteAssignTypeEnum.Staff;
            resolvedId = staffId;
            resolvedText = items.First(x => x.Id == staffId).Text;
            return true;
        }

        return false;
    }

    private static bool? ParseBool(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
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

    private string GetCurrentActor()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value
            ?? User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? "system";
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null) return result;
        var sb = new System.Text.StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"'); i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString()); sb.Clear();
            }
            else sb.Append(ch);
        }
        result.Add(sb.ToString());
        return result;
    }
}
