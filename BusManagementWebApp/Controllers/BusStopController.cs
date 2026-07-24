using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Implementations;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.BusStop;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;

namespace BusManagement.Controllers;

public class BusStopController : Controller
{
    private readonly ITmBusStopService _tmBusStopService;
    private readonly ITmCityService _tmCityService;
    private readonly ITmWardService _tmWardService;
    private readonly ITmRoleService _tmRoleService;

    public BusStopController(ITmBusStopService tmBusStopService, ITmCityService tmCityService, ITmWardService tmWardService, ITmRoleService tmRoleService)
    {
        _tmBusStopService = tmBusStopService;
        _tmCityService = tmCityService;
        _tmWardService = tmWardService;
        _tmRoleService = tmRoleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? city, string? ward, string? stopName, bool? isActive, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var filters = BuildBusStopFilters(city, ward, stopName, isActive);
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

        var resp = await _tmBusStopService.SearchByConditionAsync(searchOptions);
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

        var vm = new BusStopPageViewModel
        {
            City = city,
            Ward = ward,
            StopName = stopName,
            IsActive = isActive,
            PageNumber = page,
            PageSize = pageSize,
            BusStops = resp?.Data?.Data?.ToList() ?? new(),
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
            TotalCount = (int)(resp?.Data?.TotalCount ?? 0),
            TotalPages = (int)(resp?.Data?.TotalPages ?? 0)
        };

        if (vm.TotalPages <= 0)
        {
            vm.TotalPages = vm.TotalCount > 0
                ? (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize)
                : 1;
        }

        ViewBag.CanManage = await CanManageAsync();
        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(BusStopUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusStopError"] = "Bạn không có quyền thực hiện thao tác này.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.City))
        {
            TempData["BusStopError"] = "Vui lòng nhập Thành phố, Xã phường";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_STOP
        {
            NVCHR_STOP_NAME = input.Name?.Trim(),
            NVCHR_STOP_ADDRESS = input.Address?.Trim(),
            NVCHR_STOP_CITY = input.City?.Trim(),
            NVCHR_STOP_WARD = input.Ward?.Trim(),
            CHR_COORDINATES = input.Coordinates?.Trim(),
            FLT_DISTANCE_TO_BIVN = input.DistanceToBivn,
            FLT_DISTANCE_TO_TTF = input.DistanceToTtf,
            NVCHR_STOP_RETURN_ADDRESS = input.ReturnAddress?.Trim(),
            FLT_DISTANCE_BIVN_TO_RETURN = input.DistanceBivnToReturn,
            BIT_IS_ACTIVE = input.IsActive ?? true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmBusStopService.AddAsyncV2(entity);
        TempData[resp.Success ? "BusStopSuccess" : "BusStopError"] = resp.Success
            ? "Thêm điểm đón trả thành công."
            : (resp.Message ?? "Không thể thêm điểm đón trả.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(BusStopUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["BusStopError"] = "Bạn không có quyền thực hiện thao tác này.";
            return RedirectToIndex(input);
        }

        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["BusStopError"] = "Không xác định được điểm đón trả cần cập nhật.";
            return RedirectToIndex(input);
        }

        var currentResp = await _tmBusStopService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["BusStopError"] = currentResp.Message ?? "Không tìm thấy điểm đón trả.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.City))
        {
            TempData["BusStopError"] = "Vui lòng nhập Thành phố, Xã phường";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_STOP
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            NVCHR_STOP_NAME = input.Name?.Trim(),
            NVCHR_STOP_ADDRESS = input.Address?.Trim(),
            NVCHR_STOP_CITY = input.City?.Trim(),
            NVCHR_STOP_WARD = input.Ward?.Trim(),
            CHR_COORDINATES = input.Coordinates?.Trim(),
            FLT_DISTANCE_TO_BIVN = input.DistanceToBivn,
            FLT_DISTANCE_TO_TTF = input.DistanceToTtf,
            NVCHR_STOP_RETURN_ADDRESS = input.ReturnAddress?.Trim(),
            FLT_DISTANCE_BIVN_TO_RETURN = input.DistanceBivnToReturn,
            BIT_IS_ACTIVE = input.IsActive
        };

        var resp = await _tmBusStopService.UpdateAsync(entity);
        TempData[resp.Success ? "BusStopSuccess" : "BusStopError"] = resp.Success
            ? "Cập nhật điểm đón trả thành công."
            : (resp.Message ?? "Không thể cập nhật điểm đón trả.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? city, string? ward, string? stopName, bool? isActive, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["BusStopError"] = "Bạn không có quyền thực hiện thao tác này.";
            return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
        }

        var resp = await _tmBusStopService.DeleteAsync(id);
        TempData[resp.Success ? "BusStopSuccess" : "BusStopError"] = resp.Success
            ? "Xóa điểm đón trả thành công."
            : (resp.Message ?? "Không thể xóa điểm đón trả.");

        return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMany(List<int> ids, string? city, string? ward, string? stopName, bool? isActive, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["BusStopError"] = "Bạn không có quyền thực hiện thao tác này.";
            return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
        }

        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["BusStopError"] = "Vui lòng chọn ít nhất 1 điểm đón trả để xóa.";
            return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
        }

        var resp = await _tmBusStopService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "BusStopSuccess" : "BusStopError"] = resp.Success
            ? $"Đã xóa {ids.Count} điểm đón trả."
            : (resp.Message ?? "Không thể xóa danh sách điểm đón trả.");

        return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? city, string? ward, string? stopName, bool? isActive)
    {
        var filters = BuildBusStopFilters(city, ward, stopName, isActive);
        var resp = await _tmBusStopService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var rows = resp?.Data?.Data?.ToList() ?? new List<TM_BUS_STOPDTO>();
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

        var cityNames = (cityResp?.Data?.Data ?? new List<TM_CITYDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_CITY_NAME))
            .Select(x => x.NVCHR_CITY_NAME!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var wardNames = (wardResp?.Data?.Data ?? new List<TM_WARDDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_WARD_NAME))
            .Select(x => x.NVCHR_WARD_NAME!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BusStops");
        var headers = new[]
        {
            "STT", "Thành phố", "Xã phường", "Tên điểm", "Địa chỉ chi tiết",
            "Tọa độ", "Khoảng cách tới BIVN", "Khoảng cách tới TTF", "Địa chỉ điểm trả", "Khoảng cách từ BIVN về điểm trả", "Trạng thái", "Thời gian tạo", "Thời gian cập nhật", "Người cập nhật"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var citiesSheet = workbook.Worksheets.Add("_CitiesLookup");
        for (var i = 0; i < cityNames.Count; i++)
        {
            citiesSheet.Cell(i + 1, 1).Value = cityNames[i];
        }
        citiesSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var wardsSheet = workbook.Worksheets.Add("_WardsLookup");
        for (var i = 0; i < wardNames.Count; i++)
        {
            wardsSheet.Cell(i + 1, 1).Value = wardNames[i];
        }
        wardsSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var row = 2;
        var stt = 1;
        foreach (var item in rows)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.NVCHR_STOP_CITY;
            sheet.Cell(row, 3).Value = item.NVCHR_STOP_WARD;
            sheet.Cell(row, 4).Value = item.NVCHR_STOP_NAME;
            sheet.Cell(row, 5).Value = item.NVCHR_STOP_ADDRESS;
            sheet.Cell(row, 6).Value = item.CHR_COORDINATES;
            sheet.Cell(row, 7).Value = item.FLT_DISTANCE_TO_BIVN;
            sheet.Cell(row, 8).Value = item.FLT_DISTANCE_TO_TTF;
            sheet.Cell(row, 9).Value = item.NVCHR_STOP_RETURN_ADDRESS;
            sheet.Cell(row, 10).Value = item.FLT_DISTANCE_BIVN_TO_RETURN;
            sheet.Cell(row, 11).Value = item.BIT_IS_ACTIVE == true ? "Đang hoạt động" : "Ngưng hoạt động";
            sheet.Cell(row, 12).Value = item.DTM_CREATE.ToString("dd/MM/yyyy HH:mm:ss");
            sheet.Cell(row, 13).Value = item.DTM_UPDATE?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty;
            sheet.Cell(row, 14).Value = item.CHR_USER_UPDATE;
            row++;
            stt++;
        }

        var validationEndRow = Math.Max(row + 100, 200);
        if (cityNames.Count > 0)
        {
            for (var r = 2; r <= validationEndRow; r++)
            {
                var cityValidation = sheet.Range(r, 2, r, 2).SetDataValidation();
                cityValidation.List(citiesSheet.Range(1, 1, cityNames.Count, 1));
            }
        }

        if (wardNames.Count > 0)
        {
            for (var r = 2; r <= validationEndRow; r++)
            {
                var wardValidation = sheet.Range(r, 3, r, 3).SetDataValidation();
                wardValidation.List(wardsSheet.Range(1, 1, wardNames.Count, 1));
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachDiemDonTra_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile? importFile, string? city, string? ward, string? stopName, bool? isActive, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["BusStopError"] = "Bạn không có quyền thực hiện thao tác này.";
            return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
        }

        if (importFile == null || importFile.Length == 0)
        {
            TempData["BusStopError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (cells.Count < 7) return false;

            var offset = 1;
            var firstCell = cells.ElementAtOrDefault(0)?.Trim();
            if (int.TryParse(firstCell, out _)
                && !string.IsNullOrWhiteSpace(cells.ElementAtOrDefault(1))
                && !string.IsNullOrWhiteSpace(cells.ElementAtOrDefault(2)))
            {
                // exported format: STT, City, Ward, Name, ...
                offset = 1;
            }

            var cityValue = cells.ElementAtOrDefault(offset)?.Trim();
            var wardValue = cells.ElementAtOrDefault(offset + 1)?.Trim();
            var name = cells.ElementAtOrDefault(offset + 2)?.Trim();
            var address = cells.ElementAtOrDefault(offset + 3)?.Trim();
            var coordinates = cells.ElementAtOrDefault(offset + 4)?.Trim();

            var distanceToBivnRaw = cells.ElementAtOrDefault(offset + 5);
            var distanceToTtfRaw = cells.ElementAtOrDefault(offset + 6);
            var returnAddress = cells.ElementAtOrDefault(offset + 7)?.Trim();
            var distanceBivnToReturnRaw = cells.ElementAtOrDefault(offset + 8);

            if (string.IsNullOrWhiteSpace(cityValue))
            {
                return false;
            }

            // Check if stop with same name already exists
            var existingFilters = new List<FilterOption>
            {
                new FilterOption { Field = "NVCHR_STOP_NAME", Operator = "=", Value = name }
            };
            var existingResp = await _tmBusStopService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = existingFilters
            });

            var existingStop = existingResp?.Data?.Data?.FirstOrDefault();

            if (existingStop != null)
            {
                // Update existing record
                var entity = new TM_BUS_STOP
                {
                    ID = existingStop.ID,
                    CHR_USER_CREATE = existingStop.CHR_USER_CREATE,
                    DTM_CREATE = existingStop.DTM_CREATE,
                    CHR_USER_UPDATE = actor,
                    DTM_UPDATE = DateTime.Now,
                    NVCHR_STOP_CITY = cityValue,
                    NVCHR_STOP_WARD = wardValue,
                    NVCHR_STOP_NAME = name,
                    NVCHR_STOP_ADDRESS = address,
                    CHR_COORDINATES = coordinates,
                    FLT_DISTANCE_TO_BIVN = ParseDouble(distanceToBivnRaw),
                    FLT_DISTANCE_TO_TTF = ParseDouble(distanceToTtfRaw),
                    NVCHR_STOP_RETURN_ADDRESS = returnAddress,
                    FLT_DISTANCE_BIVN_TO_RETURN = ParseDouble(distanceBivnToReturnRaw),
                    BIT_IS_ACTIVE = existingStop.BIT_IS_ACTIVE ?? true
                };

                var updateResp = await _tmBusStopService.UpdateAsync(entity);
                return updateResp.Success;
            }
            else
            {
                // Create new record
                var entity = new TM_BUS_STOP
                {
                    NVCHR_STOP_CITY = cityValue,
                    NVCHR_STOP_WARD = wardValue,
                    NVCHR_STOP_NAME = name,
                    NVCHR_STOP_ADDRESS = address,
                    CHR_COORDINATES = coordinates,
                    FLT_DISTANCE_TO_BIVN = ParseDouble(distanceToBivnRaw),
                    FLT_DISTANCE_TO_TTF = ParseDouble(distanceToTtfRaw),
                    NVCHR_STOP_RETURN_ADDRESS = returnAddress,
                    FLT_DISTANCE_BIVN_TO_RETURN = ParseDouble(distanceBivnToReturnRaw),
                    BIT_IS_ACTIVE = true,
                    CHR_USER_CREATE = actor,
                    DTM_CREATE = DateTime.Now
                };

                var addResp = await _tmBusStopService.AddAsyncV2(entity);
                return addResp.Success;
            }
        }

        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                TempData["BusStopError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
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
                    sheet.Cell(r, 11).GetString(),
                    sheet.Cell(r, 12).GetString(),
                    sheet.Cell(r, 13).GetString(),
                    sheet.Cell(r, 14).GetString()
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

                if (row == 1 && (line.Contains("City", StringComparison.OrdinalIgnoreCase) || line.Contains("Thành phố", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "BusStopSuccess" : "BusStopError"] = failed == 0
            ? $"Import thành công {success} điểm đón trả."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Index), new { city, ward, stopName, isActive, page, pageSize });
    }

    private IActionResult RedirectToIndex(BusStopUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            city = input.SearchCity,
            ward = input.SearchWard,
            stopName = input.SearchStopName,
            isActive = input.SearchIsActive,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private static List<FilterOption> BuildBusStopFilters(string? city, string? ward, string? stopName, bool? isActive)
    {
        var filters = new List<FilterOption>();

        if (!string.IsNullOrWhiteSpace(city))
        {
            filters.Add(new FilterOption { Field = "NVCHR_STOP_CITY", Operator = "LIKE", Value = $"%{city.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(ward))
        {
            filters.Add(new FilterOption { Field = "NVCHR_STOP_WARD", Operator = "LIKE", Value = $"%{ward.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(stopName))
        {
            var stopNameTrim = stopName.Trim();
            filters.Add(new FilterOption { Field = "NVCHR_STOP_NAME", Operator = "LIKE", Value = $"%{stopNameTrim}%" });
        }

        if (isActive.HasValue)
        {
            filters.Add(new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = isActive.Value });
        }

        return filters;
    }

    private static double? ParseDouble(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = raw.Trim().Replace(',', '.');
        if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        if (double.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.CurrentCulture, out value))
        {
            return value;
        }

        return null;
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

    private string GetCurrentActor()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value
            ?? User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? "system";
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
}
