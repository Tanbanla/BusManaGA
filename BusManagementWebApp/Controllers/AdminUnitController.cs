using System.Globalization;
using System.Security.Claims;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.Admin;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace BusManagement.Controllers;

public class AdminUnitController : Controller
{
    private readonly ITmCityService _tmCityService;
    private readonly ITmWardService _tmWardService;

    public AdminUnitController(ITmCityService tmCityService, ITmWardService tmWardService)
    {
        _tmCityService = tmCityService;
        _tmWardService = tmWardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string tab = "cities", string? code = null, string? name = null, bool? isActive = null, int page = 1, int pageSize = 10, int? cityId = null, string? wardName = null)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        if (tab == "wards")
        {
            var filters = BuildWardFilters(cityId, wardName, isActive);
            var searchOptions = new SearchOptions
            {
                PageNumber = page,
                PageSize = pageSize,
                Filters = filters,
                SortOptions = new() { new SortOption { Field = "ID", SortDirection = "DESC" } }
            };

            var resp = await _tmWardService.SearchByConditionAsync(searchOptions);
            var citiesResp = await _tmCityService.SearchByConditionAsync(new SearchOptions { PageNumber = 1, PageSize = 1000 });

            var vm = new WardPageViewModel
            {
                CityId = cityId,
                CityName = citiesResp?.Data?.Data?.FirstOrDefault(c => c.ID == cityId)?.NVCHR_CITY_NAME,
                WardName = wardName,
                IsActive = isActive,
                PageNumber = page,
                PageSize = pageSize,
                Wards = resp?.Data?.Data?.ToList() ?? new(),
                TotalCount = (int)(resp?.Data?.TotalCount ?? 0),
                TotalPages = (int)(resp?.Data?.TotalPages ?? 0),
                Cities = citiesResp?.Data?.Data?.ToList() ?? new()
            };

            if (vm.TotalPages <= 0)
            {
                vm.TotalPages = vm.TotalCount > 0 ? (int)Math.Ceiling(vm.TotalCount / (double)vm.PageSize) : 1;
            }

            ViewData["ActiveTab"] = "wards";
            return View(vm);
        }

        // default cities tab
        var cityFilters = BuildCityFilters(code, name, isActive);
        var citySearchOptions = new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = cityFilters,
            SortOptions = new() { new SortOption { Field = "ID", SortDirection = "DESC" } }
        };

        var cityResp = await _tmCityService.SearchByConditionAsync(citySearchOptions);

        var cityVm = new CityPageViewModel
        {
            SearchCode = code,
            SearchName = name,
            IsActive = isActive,
            PageNumber = page,
            PageSize = pageSize,
            Cities = cityResp?.Data?.Data?.ToList() ?? new(),
            TotalCount = (int)(cityResp?.Data?.TotalCount ?? 0),
            TotalPages = (int)(cityResp?.Data?.TotalPages ?? 0)
        };

        if (cityVm.TotalPages <= 0)
        {
            cityVm.TotalPages = cityVm.TotalCount > 0 ? (int)Math.Ceiling(cityVm.TotalCount / (double)cityVm.PageSize) : 1;
        }

        ViewData["ActiveTab"] = "cities";
        return View(cityVm);
    }

    #region City Actions
    [HttpPost]
    public async Task<IActionResult> CreateCity(TM_CITYDTO input, int page = 1, int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(input.NVCHR_CITY_NAME))
        {
            TempData["CityError"] = "Vui lòng nhập tên thành phố.";
            return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_CITY
        {
            CHR_CITY_CODE = input.CHR_CITY_CODE?.Trim(),
            NVCHR_CITY_NAME = input.NVCHR_CITY_NAME?.Trim(),
            BIT_IS_ACTIVE = input.BIT_IS_ACTIVE ?? true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmCityService.AddAsyncV2(entity);
        TempData[resp.Success ? "CitySuccess" : "CityError"] = resp.Success ? "Thêm thành phố thành công." : (resp.Message ?? "Không thể thêm thành phố.");
        return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCity(TM_CITYDTO input, int page = 1, int pageSize = 10)
    {
        if (input.ID <= 0)
        {
            TempData["CityError"] = "Không xác định được thành phố cần cập nhật.";
            return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
        }

        var currentResp = await _tmCityService.GetByIdAsync(input.ID);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["CityError"] = currentResp.Message ?? "Không tìm thấy thành phố.";
            return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
        }

        if (string.IsNullOrWhiteSpace(input.NVCHR_CITY_NAME))
        {
            TempData["CityError"] = "Vui lòng nhập tên thành phố.";
            return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_CITY
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_CITY_CODE = input.CHR_CITY_CODE?.Trim(),
            NVCHR_CITY_NAME = input.NVCHR_CITY_NAME?.Trim(),
            BIT_IS_ACTIVE = input.BIT_IS_ACTIVE
        };

        var resp = await _tmCityService.UpdateAsync(entity);
        TempData[resp.Success ? "CitySuccess" : "CityError"] = resp.Success ? "Cập nhật thành phố thành công." : (resp.Message ?? "Không thể cập nhật thành phố.");
        return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCity(int id, int page = 1, int pageSize = 10)
    {
        var resp = await _tmCityService.DeleteAsync(id);
        TempData[resp.Success ? "CitySuccess" : "CityError"] = resp.Success ? "Xóa thành phố thành công." : (resp.Message ?? "Không thể xóa thành phố.");
        return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteManyCities(List<int> ids, int page = 1, int pageSize = 10)
    {
        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["CityError"] = "Vui lòng chọn ít nhất 1 thành phố để xóa.";
            return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
        }

        var resp = await _tmCityService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "CitySuccess" : "CityError"] = resp.Success ? $"Đã xóa {ids.Count} thành phố." : (resp.Message ?? "Không thể xóa danh sách thành phố.");
        return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> ExportCities(string? code, string? name, bool? isActive)
    {
        var filters = BuildCityFilters(code, name, isActive);
        var resp = await _tmCityService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000, Filters = filters });
        var rows = resp?.Data?.Data?.ToList() ?? new List<TM_CITYDTO>();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Cities");
        var headers = new[] { "STT", "Mã thành phố", "Tên thành phố" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");
        headerRange.Style.Font.FontColor = XLColor.White;

        var row = 2; var stt = 1;
        foreach (var item in rows)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.CHR_CITY_CODE;
            sheet.Cell(row, 3).Value = item.NVCHR_CITY_NAME;
            row++; stt++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachThanhPho_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ImportCities(IFormFile? importFile, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["CityError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0; var failed = 0;

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            var code = cells.ElementAtOrDefault(1)?.Trim();
            var name = cells.ElementAtOrDefault(2)?.Trim();
            var active = ParseBool(cells.ElementAtOrDefault(3)) ?? true;
            if (string.IsNullOrWhiteSpace(name)) return false;

            var entity = new TM_CITY
            {
                CHR_CITY_CODE = code,
                NVCHR_CITY_NAME = name,
                BIT_IS_ACTIVE = active,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var resp = await _tmCityService.AddAsyncV2(entity);
            return resp.Success;
        }

        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                TempData["CityError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>
                {
                    sheet.Cell(r,1).GetString(),
                    sheet.Cell(r,2).GetString(),
                    sheet.Cell(r,3).GetString()
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
                if (row == 1 && (line.Contains("City", StringComparison.OrdinalIgnoreCase) || line.Contains("Thành phố", StringComparison.OrdinalIgnoreCase))) continue;
                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "CitySuccess" : "CityError"] = failed == 0 ? $"Import thành công {success} thành phố." : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";
        return RedirectToAction(nameof(Index), new { tab = "cities", page, pageSize });
    }
    #endregion

    #region Ward Actions
    [HttpPost]
    public async Task<IActionResult> CreateWard(TM_WARDDTO input, int page = 1, int pageSize = 10)
    {
        if (!input.INT_CITY_ID.HasValue || input.INT_CITY_ID.Value <= 0 || string.IsNullOrWhiteSpace(input.NVCHR_WARD_NAME))
        {
            TempData["WardError"] = "Vui lòng chọn thành phố và nhập tên xã/phường.";
            return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId = input.INT_CITY_ID });
        }

        var actor = GetCurrentActor();
        var entity = new TM_WARD
        {
            INT_CITY_ID = input.INT_CITY_ID,
            CHR_WARD_CODE = input.CHR_WARD_CODE?.Trim(),
            NVCHR_WARD_NAME = input.NVCHR_WARD_NAME?.Trim(),
            BIT_IS_ACTIVE = input.BIT_IS_ACTIVE ?? true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmWardService.AddAsyncV2(entity);
        TempData[resp.Success ? "WardSuccess" : "WardError"] = resp.Success ? "Thêm xã/phường thành công." : (resp.Message ?? "Không thể thêm xã/phường.");
        return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId = input.INT_CITY_ID });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateWard(TM_WARDDTO input, int page = 1, int pageSize = 10)
    {
        if (input.ID <= 0)
        {
            TempData["WardError"] = "Không xác định được xã/phường cần cập nhật.";
            return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId = input.INT_CITY_ID });
        }

        var currentResp = await _tmWardService.GetByIdAsync(input.ID);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["WardError"] = currentResp.Message ?? "Không tìm thấy xã/phường.";
            return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId = input.INT_CITY_ID });
        }

        if (!input.INT_CITY_ID.HasValue || input.INT_CITY_ID.Value <= 0 || string.IsNullOrWhiteSpace(input.NVCHR_WARD_NAME))
        {
            TempData["WardError"] = "Vui lòng chọn thành phố và nhập tên xã/phường.";
            return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId = input.INT_CITY_ID });
        }

        var actor = GetCurrentActor();
        var entity = new TM_WARD
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_CITY_ID = input.INT_CITY_ID,
            CHR_WARD_CODE = input.CHR_WARD_CODE?.Trim(),
            NVCHR_WARD_NAME = input.NVCHR_WARD_NAME?.Trim(),
            BIT_IS_ACTIVE = input.BIT_IS_ACTIVE
        };

        var resp = await _tmWardService.UpdateAsync(entity);
        TempData[resp.Success ? "WardSuccess" : "WardError"] = resp.Success ? "Cập nhật xã/phường thành công." : (resp.Message ?? "Không thể cập nhật xã/phường.");
        return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId = input.INT_CITY_ID });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteWard(int id, int? cityId, int page = 1, int pageSize = 10)
    {
        var resp = await _tmWardService.DeleteAsync(id);
        TempData[resp.Success ? "WardSuccess" : "WardError"] = resp.Success ? "Xóa xã/phường thành công." : (resp.Message ?? "Không thể xóa xã/phường.");
        return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteManyWards(List<int> ids, int? cityId, int page = 1, int pageSize = 10)
    {
        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["WardError"] = "Vui lòng chọn ít nhất 1 xã/phường để xóa.";
            return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId });
        }

        var resp = await _tmWardService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "WardSuccess" : "WardError"] = resp.Success ? $"Đã xóa {ids.Count} xã/phường." : (resp.Message ?? "Không thể xóa danh sách xã/phường.");
        return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId });
    }

    [HttpGet]
    public async Task<IActionResult> ExportWards(int? cityId, string? wardName, bool? isActive)
    {
        var filters = BuildWardFilters(cityId, wardName, isActive);
        var resp = await _tmWardService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000, Filters = filters });
        var rows = resp?.Data?.Data?.ToList() ?? new List<TM_WARDDTO>();

        using var workbook = new XLWorkbook();
        // sheet with data
        var sheet = workbook.Worksheets.Add("Wards");
        var headers = new[] { "STT", "Thành phố", "Mã xã", "Tên xã" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];
        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");
        headerRange.Style.Font.FontColor = XLColor.White;

        // create a hidden sheet with city list (ID - Name) for dropdown
        var cityResp = await _tmCityService.SearchByConditionAsync(new SearchOptions { PageNumber = 1, PageSize = 10000 });
        var citiesList = cityResp?.Data?.Data?.ToList() ?? new List<TM_CITYDTO>();
        var citiesSheet = workbook.Worksheets.Add("_CitiesLookup");
        for (var i = 0; i < citiesList.Count; i++)
        {
            var c = citiesList[i];
            citiesSheet.Cell(i + 1, 1).Value = c.ID + " - " + (c.NVCHR_CITY_NAME ?? string.Empty);
        }
        // hide lookup sheet
        citiesSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var row = 2; var stt = 1;
        foreach (var item in rows)
        {
            sheet.Cell(row, 1).Value = stt;
            // write city as "ID - Name" if available
            if (item.INT_CITY_ID.HasValue && item.INT_CITY_ID.Value > 0)
            {
                var cityName = citiesList.FirstOrDefault(c => c.ID == item.INT_CITY_ID.Value)?.NVCHR_CITY_NAME ?? string.Empty;
                sheet.Cell(row, 2).Value = item.INT_CITY_ID + " - " + cityName;
            }
            else
            {
                sheet.Cell(row, 2).Value = string.Empty;
            }

            sheet.Cell(row, 3).Value = item.CHR_WARD_CODE;
            sheet.Cell(row, 4).Value = item.NVCHR_WARD_NAME;

            // add data validation on city cell to use lookup range
            if (citiesList.Count > 0)
            {
                var dv = sheet.Range(row, 2, row, 2).SetDataValidation();
                var lastLookupRow = citiesList.Count;
                dv.List(citiesSheet.Range(1, 1, lastLookupRow, 1));
            }

            row++; stt++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachXaPhuong_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ImportWards(IFormFile? importFile, int? cityId, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["WardError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId });
        }

        var actor = GetCurrentActor();
        var success = 0; var failed = 0;

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            // Expected columns: City (id or code or name or "id - name"), WardCode, WardName, [IsActive]
            var cityKey = cells.ElementAtOrDefault(1)?.Trim();
            var wardCode = cells.ElementAtOrDefault(2)?.Trim();
            var wardName = cells.ElementAtOrDefault(3)?.Trim();
            var active = ParseBool(cells.ElementAtOrDefault(4)) ?? true;

            if (string.IsNullOrWhiteSpace(cityKey) || string.IsNullOrWhiteSpace(wardName)) return false;

            // If cityKey looks like "123 - Name", try parse id
            int? resolvedCityId = null;
            if (int.TryParse(cityKey.Split('-')[0].Trim(), out var parsedId))
            {
                resolvedCityId = parsedId;
            }

            TM_CITYDTO city = null;
            if (resolvedCityId.HasValue)
            {
                var cityRespById = await _tmCityService.GetByIdAsync(resolvedCityId.Value);
                if (cityRespById.Success) city = cityRespById.Data;
            }

            if (city == null)
            {
                // try find city by code or exact name
                var cityFilter = new List<FilterOption>
                {
                    new FilterOption { Field = "CHR_CITY_CODE", Operator = "=", Value = cityKey },
                    new FilterOption { Field = "NVCHR_CITY_NAME", Operator = "=", Value = cityKey, LogicType = "OR" }
                };
                var cityResp = await _tmCityService.SearchByConditionAsync(new SearchOptions { PageNumber = 1, PageSize = 1, Filters = cityFilter });
                city = cityResp?.Data?.Data?.FirstOrDefault();
            }

            if (city == null)
            {
                return false;
            }

            var entity = new TM_WARD
            {
                INT_CITY_ID = city.ID,
                CHR_WARD_CODE = wardCode,
                NVCHR_WARD_NAME = wardName,
                BIT_IS_ACTIVE = active,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var resp = await _tmWardService.AddAsyncV2(entity);
            return resp.Success;
        }

        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                TempData["WardError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId });
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>
                {
                    sheet.Cell(r, 1).GetString(),
                    sheet.Cell(r, 2).GetString(),
                    sheet.Cell(r, 3).GetString(),
                    sheet.Cell(r, 4).GetString()
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
                if (row == 1 && (line.Contains("City", StringComparison.OrdinalIgnoreCase) || line.Contains("Thành phố", StringComparison.OrdinalIgnoreCase))) continue;
                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "WardSuccess" : "WardError"] = failed == 0 ? $"Import thành công {success} xã/phường." : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";
        return RedirectToAction(nameof(Index), new { tab = "wards", page, pageSize, cityId });
    }
    #endregion

    private static List<FilterOption> BuildCityFilters(string? code, string? name, bool? isActive)
    {
        var filters = new List<FilterOption>();
        if (!string.IsNullOrWhiteSpace(code)) filters.Add(new FilterOption { Field = "CHR_CITY_CODE", Operator = "LIKE", Value = $"%{code.Trim()}%" });
        if (!string.IsNullOrWhiteSpace(name)) filters.Add(new FilterOption { Field = "NVCHR_CITY_NAME", Operator = "LIKE", Value = $"%{name.Trim()}%" });
        if (isActive.HasValue) filters.Add(new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = isActive.Value });
        return filters;
    }

    private static List<FilterOption> BuildWardFilters(int? cityId, string? wardName, bool? isActive)
    {
        var filters = new List<FilterOption>();
        if (cityId.HasValue) filters.Add(new FilterOption { Field = "INT_CITY_ID", Operator = "=", Value = cityId.Value });
        if (!string.IsNullOrWhiteSpace(wardName)) filters.Add(new FilterOption { Field = "NVCHR_WARD_NAME", Operator = "LIKE", Value = $"%{wardName.Trim()}%" });
        if (isActive.HasValue) filters.Add(new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = isActive.Value });
        return filters;
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
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else { inQuotes = !inQuotes; }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString()); sb.Clear();
            }
            else { sb.Append(ch); }
        }
        result.Add(sb.ToString());
        return result;
    }
}
