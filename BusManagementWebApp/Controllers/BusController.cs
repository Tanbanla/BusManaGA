using System.Globalization;
using System.Security.Claims;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.Bus;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace BusManagement.Controllers;

public class BusController : Controller
{
    private readonly ITmBusService _tmBusService;
    private readonly ITmVendorService _tmVendorService;
    private readonly ITmBusEvaluationService _tmBusEvaluationService;
    private readonly ITmDriverService _tmDriverService;

    public BusController(
        ITmBusService tmBusService,
        ITmVendorService tmVendorService,
        ITmBusEvaluationService tmBusEvaluationService,
        ITmDriverService tmDriverService)
    {
        _tmBusService = tmBusService;
        _tmVendorService = tmVendorService;
        _tmBusEvaluationService = tmBusEvaluationService;
        _tmDriverService = tmDriverService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var filters = BuildBusFilters(busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive);

        var resp = await _tmBusService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var buses = resp?.Data?.Data?.ToList() ?? new List<TM_BUSDTO>();

        var vendorResp = await _tmVendorService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_VENDOR_NAME", SortDirection = "ASC" }
            }
        });

        var vendors = vendorResp?.Data?.Data?.ToList() ?? new List<TM_VENDORDTO>();
        var driverResp = await _tmDriverService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_DRIVER_NAME", SortDirection = "ASC" }
            }
        });
        var drivers = driverResp?.Data?.Data?.ToList() ?? new List<TM_DRIVERDTO>();
        var vendorNamesById = vendors
            .GroupBy(x => x.ID)
            .ToDictionary(g => g.Key, g =>
            {
                var item = g.First();
                var code = item.CHR_VENDOR_CODE?.Trim();
                var name = item.NVCHR_VENDOR_NAME?.Trim();
                if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                {
                    return $"{name}";
                }

                return !string.IsNullOrWhiteSpace(name) ? name : (code ?? g.Key.ToString());
            });
        var driverNamesById = drivers
            .GroupBy(x => x.ID)
            .ToDictionary(g => g.Key, g =>
            {
                var item = g.First();
                var name = item.NVCHR_DRIVER_NAME?.Trim();
                var phone = item.CHR_DRIVER_PHONE?.Trim();
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(phone))
                {
                    return $"{name} ({phone})";
                }

                return !string.IsNullOrWhiteSpace(name) ? name : (phone ?? g.Key.ToString());
            });

        var evaluationsByBusId = new Dictionary<int, List<TM_BUS_EVALUATIONDTO>>();
        foreach (var bus in buses)
        {
            var evalResp = await _tmBusEvaluationService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 1000,
                Filters = new List<FilterOption>
                {
                    new FilterOption { Field = "INT_BUS_ID", Operator = "=", Value = bus.ID }
                },
                SortOptions = new()
                {
                    new SortOption { Field = "DTM_EVALUATION_DATE", SortDirection = "DESC" },
                    new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
                }
            });

            evaluationsByBusId[bus.ID] = evalResp?.Data?.Data?.ToList() ?? new List<TM_BUS_EVALUATIONDTO>();
        }

        var vm = new BusPageViewModel
        {
            BusCode = busCode,
            BusName = busName,
            LicensePlate = licensePlate,
            Category = category,
            Brand = brand,
            Model = model,
            ModelYear = modelYear,
            Capacity = capacity,
            VendorId = vendorId,
            IsActive = isActive,
            PageNumber = page,
            PageSize = pageSize,
            Buses = buses,
            Vendors = vendors,
            Drivers = drivers,
            VendorNamesById = vendorNamesById,
            DriverNamesById = driverNamesById,
            EvaluationsByBusId = evaluationsByBusId,
            TotalCount = (int)(resp?.Data?.TotalCount ?? 0),
            TotalPages = (int)(resp?.Data?.TotalPages ?? 0)
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
    public async Task<IActionResult> Create(BusUpsertInputModel input)
    {
        if (string.IsNullOrWhiteSpace(input.BusCode) || string.IsNullOrWhiteSpace(input.LicensePlate))
        {
            TempData["BusError"] = "Vui lòng nhập ký hiệu xe và biển số xe.";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS
        {
            CHR_BUS_CODE = input.BusCode?.Trim(),
            NVCHR_BUS_NAME = input.BusName?.Trim(),
            CHR_LICENSE_PLATE = input.LicensePlate?.Trim(),
            NVCHR_CATEGORY = input.Category?.Trim(),
            NVCHR_BRAND = input.Brand?.Trim(),
            NVCHR_MODEL = input.Model?.Trim(),
            NVCHR_MODEL_YEAR = input.ModelYear?.Trim(),
            INT_CAPACITY = input.Capacity,
            INT_VENDOR_ID = input.VendorId,
            BIT_IS_ACTIVE = input.IsActive ?? true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmBusService.AddAsyncV2(entity);
        TempData[resp.Success ? "BusSuccess" : "BusError"] = resp.Success
            ? "Thêm xe bus thành công."
            : (resp.Message ?? "Không thể thêm xe bus.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(BusUpsertInputModel input)
    {
        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["BusError"] = "Không xác định được xe bus cần cập nhật.";
            return RedirectToIndex(input);
        }

        var currentResp = await _tmBusService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["BusError"] = currentResp.Message ?? "Không tìm thấy xe bus.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.BusCode) || string.IsNullOrWhiteSpace(input.LicensePlate))
        {
            TempData["BusError"] = "Vui lòng nhập ký hiệu xe và biển số xe.";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_BUS_CODE = input.BusCode?.Trim(),
            NVCHR_BUS_NAME = input.BusName?.Trim(),
            CHR_LICENSE_PLATE = input.LicensePlate?.Trim(),
            NVCHR_CATEGORY = input.Category?.Trim(),
            NVCHR_BRAND = input.Brand?.Trim(),
            NVCHR_MODEL = input.Model?.Trim(),
            NVCHR_MODEL_YEAR = input.ModelYear?.Trim(),
            INT_CAPACITY = input.Capacity,
            INT_VENDOR_ID = input.VendorId,
            BIT_IS_ACTIVE = input.IsActive
        };

        var resp = await _tmBusService.UpdateAsync(entity);
        TempData[resp.Success ? "BusSuccess" : "BusError"] = resp.Success
            ? "Cập nhật xe bus thành công."
            : (resp.Message ?? "Không thể cập nhật xe bus.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(
        int id,
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        var resp = await _tmBusService.DeleteAsync(id);
        TempData[resp.Success ? "BusSuccess" : "BusError"] = resp.Success
            ? "Xóa xe bus thành công."
            : (resp.Message ?? "Không thể xóa xe bus.");

        return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvaluation(
        int busId,
        int? driverId,
        int? rating,
        string? evaluationType,
        string? comment,
        string? evaluationDate,
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        var busResp = await _tmBusService.GetByIdAsync(busId);
        var bus = busResp.Data;
        if (!busResp.Success || bus == null)
        {
            TempData["BusError"] = "Không tìm thấy xe bus để thêm đánh giá.";
            return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_EVALUATION
        {
            INT_BUS_ID = busId,
            CHR_BUS_CODE = bus.CHR_BUS_CODE,
            INT_DRIVER_ID = driverId,
            INT_RATING = rating,
            CHR_EVALUATION_TYPE = evaluationType?.Trim(),
            NVCHR_COMMENT = comment?.Trim(),
            DTM_EVALUATION_DATE = ParseDateTime(evaluationDate) ?? DateTime.Now,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmBusEvaluationService.AddAsyncV2(entity);
        TempData[resp.Success ? "BusSuccess" : "BusError"] = resp.Success
            ? "Thêm đánh giá thành công."
            : (resp.Message ?? "Không thể thêm đánh giá.");

        return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEvaluation(
        int id,
        int busId,
        int? driverId,
        int? rating,
        string? evaluationType,
        string? comment,
        string? evaluationDate,
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        var currentResp = await _tmBusEvaluationService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["BusError"] = currentResp.Message ?? "Không tìm thấy đánh giá để cập nhật.";
            return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
        }

        var busResp = await _tmBusService.GetByIdAsync(busId);
        var bus = busResp.Data;
        if (!busResp.Success || bus == null)
        {
            TempData["BusError"] = "Không tìm thấy xe bus để cập nhật đánh giá.";
            return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS_EVALUATION
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_BUS_ID = busId,
            CHR_BUS_CODE = bus.CHR_BUS_CODE,
            INT_DRIVER_ID = driverId,
            INT_RATING = rating,
            CHR_EVALUATION_TYPE = evaluationType?.Trim(),
            NVCHR_COMMENT = comment?.Trim(),
            DTM_EVALUATION_DATE = ParseDateTime(evaluationDate) ?? current.DTM_EVALUATION_DATE
        };

        var resp = await _tmBusEvaluationService.UpdateAsync(entity);
        TempData[resp.Success ? "BusSuccess" : "BusError"] = resp.Success
            ? "Cập nhật đánh giá thành công."
            : (resp.Message ?? "Không thể cập nhật đánh giá.");

        return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvaluation(
        int id,
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        var resp = await _tmBusEvaluationService.DeleteAsync(id);
        TempData[resp.Success ? "BusSuccess" : "BusError"] = resp.Success
            ? "Đã xóa đánh giá."
            : (resp.Message ?? "Không thể xóa đánh giá.");

        return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteManyEvaluations(
        int busId,
        List<int>? ids,
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        ids = ids?.Distinct().Where(x => x > 0).ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["BusError"] = "Vui lòng chọn ít nhất 1 đánh giá để xóa.";
            return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
        }

        var deleted = 0;
        foreach (var id in ids)
        {
            var resp = await _tmBusEvaluationService.DeleteAsync(id);
            if (resp.Success) deleted++;
        }

        TempData[deleted == ids.Count ? "BusSuccess" : "BusError"] = deleted == ids.Count
            ? $"Đã xóa {deleted} đánh giá."
            : $"Đã xóa {deleted}/{ids.Count} đánh giá.";

        return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMany(
        List<int> ids,
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        ids = ids?.Distinct().ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["BusError"] = "Vui lòng chọn ít nhất 1 xe bus để xóa.";
            return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
        }

        var resp = await _tmBusService.DeleteRangeAsync(ids);
        TempData[resp.Success ? "BusSuccess" : "BusError"] = resp.Success
            ? $"Đã xóa {ids.Count} xe bus."
            : (resp.Message ?? "Không thể xóa danh sách xe bus.");

        return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive)
    {
        var filters = BuildBusFilters(busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive);
        var resp = await _tmBusService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var rows = resp?.Data?.Data?.ToList() ?? new List<TM_BUSDTO>();
        var vendorResp = await _tmVendorService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_VENDOR_NAME", SortDirection = "ASC" }
            }
        });

        var vendorMap = (vendorResp?.Data?.Data ?? new List<TM_VENDORDTO>())
            .GroupBy(x => x.ID)
            .ToDictionary(g => g.Key, g =>
            {
                var first = g.First();
                var code = first.CHR_VENDOR_CODE?.Trim();
                var name = first.NVCHR_VENDOR_NAME?.Trim();
                if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(name))
                {
                    return $"{code} - {name}";
                }

                return !string.IsNullOrWhiteSpace(name) ? name : (code ?? g.Key.ToString());
            });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Buses");
        var headers = new[]
        {
            "STT", "Ký hiệu xe", "Tên xe", "Biển số", "Loại xe", "Hãng xe", "Model", "Năm sản xuất", "Sức chứa", "Nhà cung cấp", "Trạng thái", "Thời gian tạo", "Thời gian cập nhật", "Người cập nhật"
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
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.CHR_BUS_CODE;
            sheet.Cell(row, 3).Value = item.NVCHR_BUS_NAME;
            sheet.Cell(row, 4).Value = item.CHR_LICENSE_PLATE;
            sheet.Cell(row, 5).Value = item.NVCHR_CATEGORY;
            sheet.Cell(row, 6).Value = item.NVCHR_BRAND;
            sheet.Cell(row, 7).Value = item.NVCHR_MODEL;
            sheet.Cell(row, 8).Value = item.NVCHR_MODEL_YEAR;
            sheet.Cell(row, 9).Value = item.INT_CAPACITY;
            sheet.Cell(row, 10).Value = item.INT_VENDOR_ID.HasValue && vendorMap.TryGetValue(item.INT_VENDOR_ID.Value, out var vendorText)
                ? vendorText
                : string.Empty;
            sheet.Cell(row, 11).Value = item.BIT_IS_ACTIVE == true ? "Đang hoạt động" : "Ngưng hoạt động";
            sheet.Cell(row, 12).Value = item.DTM_CREATE.ToString("dd/MM/yyyy HH:mm:ss");
            sheet.Cell(row, 13).Value = item.DTM_UPDATE?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty;
            sheet.Cell(row, 14).Value = item.CHR_USER_UPDATE;
            row++;
            stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachXeBus_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Import(
        IFormFile? importFile,
        int? importVendorId,
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["BusError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
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
                offset = 1;
            }

            var busCodeValue = cells.ElementAtOrDefault(offset)?.Trim();
            var licensePlateValue = cells.ElementAtOrDefault(offset + 2)?.Trim();
            if (string.IsNullOrWhiteSpace(busCodeValue) || string.IsNullOrWhiteSpace(licensePlateValue))
            {
                return false;
            }

            var entity = new TM_BUS
            {
                CHR_BUS_CODE = busCodeValue,
                NVCHR_BUS_NAME = cells.ElementAtOrDefault(offset + 1)?.Trim(),
                CHR_LICENSE_PLATE = licensePlateValue,
                NVCHR_CATEGORY = cells.ElementAtOrDefault(offset + 3)?.Trim(),
                NVCHR_BRAND = cells.ElementAtOrDefault(offset + 4)?.Trim(),
                NVCHR_MODEL = cells.ElementAtOrDefault(offset + 5)?.Trim(),
                NVCHR_MODEL_YEAR = cells.ElementAtOrDefault(offset + 6)?.Trim(),
                INT_CAPACITY = ParseInt(cells.ElementAtOrDefault(offset + 7)),
                INT_VENDOR_ID = importVendorId,
                BIT_IS_ACTIVE = ParseBool(cells.ElementAtOrDefault(offset + 9)) ?? true,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmBusService.AddAsyncV2(entity);
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
                TempData["BusError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
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

                if (row == 1 && (line.Contains("Ký hiệu xe", StringComparison.OrdinalIgnoreCase) || line.Contains("BusCode", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (await TryImportCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "BusSuccess" : "BusError"] = failed == 0
            ? $"Import thành công {success} xe bus."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> ExportEvaluations(int busId, int? driverId)
    {
        var busResp = await _tmBusService.GetByIdAsync(busId);
        var bus = busResp.Data;
        if (!busResp.Success || bus == null)
        {
            TempData["BusError"] = "Không tìm thấy xe bus để xuất đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        var evalFilters = new List<FilterOption>
        {
            new FilterOption { Field = "INT_BUS_ID", Operator = "=", Value = busId }
        };
        if (driverId.HasValue && driverId.Value > 0)
        {
            evalFilters.Add(new FilterOption { Field = "INT_DRIVER_ID", Operator = "=", Value = driverId.Value });
        }

        var evalResp = await _tmBusEvaluationService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = evalFilters,
            SortOptions = new()
            {
                new SortOption { Field = "DTM_EVALUATION_DATE", SortDirection = "DESC" },
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var rows = evalResp?.Data?.Data?.ToList() ?? new List<TM_BUS_EVALUATIONDTO>();
        var driverResp = await _tmDriverService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_DRIVER_NAME", SortDirection = "ASC" }
            }
        });
        var drivers = driverResp?.Data?.Data?.ToList() ?? new List<TM_DRIVERDTO>();
        var driverLookup = drivers
            .GroupBy(x => x.ID)
            .ToDictionary(g => g.Key, g =>
            {
                var item = g.First();
                var name = item.NVCHR_DRIVER_NAME?.Trim();
                var phone = item.CHR_DRIVER_PHONE?.Trim();
                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(phone))
                {
                    return $"{name} ({phone})";
                }

                return !string.IsNullOrWhiteSpace(name) ? name : (phone ?? g.Key.ToString());
            });

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BusEvaluations");
        var headers = new[]
        {
            "STT", "Ký hiệu xe", "Tài xế", "Điểm đánh giá", "Loại đánh giá", "Nội dung", "Ngày đánh giá", "Thời gian tạo", "Thời gian cập nhật", "Người cập nhật"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var driverSheet = workbook.Worksheets.Add("_DriverLookup");
        for (var i = 0; i < drivers.Count; i++)
        {
            if (driverLookup.TryGetValue(drivers[i].ID, out var driverText) && !string.IsNullOrWhiteSpace(driverText))
            {
                driverSheet.Cell(i + 1, 1).Value = driverText;
            }
        }
        driverSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var row = 2;
        var stt = 1;
        foreach (var item in rows)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.CHR_BUS_CODE;
            sheet.Cell(row, 3).Value = item.INT_DRIVER_ID.HasValue && driverLookup.TryGetValue(item.INT_DRIVER_ID.Value, out var driverText)
                ? driverText
                : (item.INT_DRIVER_ID?.ToString() ?? string.Empty);
            sheet.Cell(row, 4).Value = item.INT_RATING;
            sheet.Cell(row, 5).Value = item.CHR_EVALUATION_TYPE;
            sheet.Cell(row, 6).Value = item.NVCHR_COMMENT;
            sheet.Cell(row, 7).Value = item.DTM_EVALUATION_DATE?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty;
            sheet.Cell(row, 8).Value = item.DTM_CREATE.ToString("dd/MM/yyyy HH:mm:ss");
            sheet.Cell(row, 9).Value = item.DTM_UPDATE?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty;
            sheet.Cell(row, 10).Value = item.CHR_USER_UPDATE;
            row++;
            stt++;
        }

        var validationEndRow = Math.Max(row + 100, 200);
        if (drivers.Count > 0)
        {
            for (var r = 2; r <= validationEndRow; r++)
            {
                var driverValidation = sheet.Range(r, 3, r, 3).SetDataValidation();
                driverValidation.List(driverSheet.Range(1, 1, drivers.Count, 1));
            }
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var busCodeSafe = string.IsNullOrWhiteSpace(bus.CHR_BUS_CODE) ? bus.ID.ToString() : bus.CHR_BUS_CODE.Trim();
        var fileName = $"DanhGiaXe_{busCodeSafe}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> ImportEvaluations(
        int busId,
        IFormFile? importFile,
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive,
        int page = 1,
        int pageSize = 10)
    {
        var busResp = await _tmBusService.GetByIdAsync(busId);
        var bus = busResp.Data;
        if (!busResp.Success || bus == null)
        {
            TempData["BusError"] = busResp.Message ?? "Không tìm thấy xe bus để import đánh giá.";
            return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
        }

        if (importFile == null || importFile.Length == 0)
        {
            TempData["BusError"] = "Vui lòng chọn file đánh giá để import.";
            return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;
        var driverResp = await _tmDriverService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_DRIVER_NAME", SortDirection = "ASC" }
            }
        });
        var drivers = driverResp?.Data?.Data?.ToList() ?? new List<TM_DRIVERDTO>();
        var driverLookupByName = drivers
            .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_DRIVER_NAME))
            .GroupBy(x => x.NVCHR_DRIVER_NAME!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ID, StringComparer.OrdinalIgnoreCase);
        var driverLookupByDisplayName = drivers
            .Select(x => new
            {
                x.ID,
                Display = $"{x.NVCHR_DRIVER_NAME?.Trim()} ({x.CHR_DRIVER_PHONE?.Trim()})"
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Display))
            .GroupBy(x => x.Display, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ID, StringComparer.OrdinalIgnoreCase);

        async Task<bool> TryImportEvaluationCellsAsync(List<string> cells)
        {
            if (!cells.Any()) return false;

            var offset = 0;
            var firstCell = cells.ElementAtOrDefault(0)?.Trim();
            if (int.TryParse(firstCell, out _)
                && !string.IsNullOrWhiteSpace(cells.ElementAtOrDefault(1)))
            {
                offset = 1;
            }

            var driverRaw = cells.ElementAtOrDefault(offset + 1)?.Trim();
            var parsedDriverId = ParseInt(driverRaw);
            if (!parsedDriverId.HasValue && !string.IsNullOrWhiteSpace(driverRaw))
            {
                if (driverLookupByDisplayName.TryGetValue(driverRaw, out var resolvedFromDisplay))
                {
                    parsedDriverId = resolvedFromDisplay;
                }
                else if (driverLookupByName.TryGetValue(driverRaw, out var resolvedFromName))
                {
                    parsedDriverId = resolvedFromName;
                }
            }

            var entity = new TM_BUS_EVALUATION
            {
                INT_BUS_ID = busId,
                CHR_BUS_CODE = bus.CHR_BUS_CODE,
                INT_DRIVER_ID = parsedDriverId,
                INT_RATING = ParseInt(cells.ElementAtOrDefault(offset + 2)),
                CHR_EVALUATION_TYPE = cells.ElementAtOrDefault(offset + 3)?.Trim(),
                NVCHR_COMMENT = cells.ElementAtOrDefault(offset + 4)?.Trim(),
                DTM_EVALUATION_DATE = ParseDateTime(cells.ElementAtOrDefault(offset + 5)) ?? DateTime.Now,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmBusEvaluationService.AddAsyncV2(entity);
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
                TempData["BusError"] = "Không đọc được sheet dữ liệu đánh giá trong file Excel.";
                return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
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
                    sheet.Cell(r, 10).GetString()
                };

                if (await TryImportEvaluationCellsAsync(cells)) success++; else failed++;
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

                if (row == 1 && (line.Contains("Điểm đánh giá", StringComparison.OrdinalIgnoreCase) || line.Contains("Rating", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (await TryImportEvaluationCellsAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "BusSuccess" : "BusError"] = failed == 0
            ? $"Import thành công {success} đánh giá cho xe {bus.CHR_BUS_CODE}."
            : $"Import đánh giá hoàn tất cho xe {bus.CHR_BUS_CODE}: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Index), new { busCode, busName, licensePlate, category, brand, model, modelYear, capacity, vendorId, isActive, page, pageSize });
    }

    private IActionResult RedirectToIndex(BusUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            busCode = input.SearchBusCode,
            busName = input.SearchBusName,
            licensePlate = input.SearchLicensePlate,
            category = input.SearchCategory,
            brand = input.SearchBrand,
            model = input.SearchModel,
            modelYear = input.SearchModelYear,
            capacity = input.SearchCapacity,
            vendorId = input.SearchVendorId,
            isActive = input.SearchIsActive,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private static List<FilterOption> BuildBusFilters(
        string? busCode,
        string? busName,
        string? licensePlate,
        string? category,
        string? brand,
        string? model,
        string? modelYear,
        int? capacity,
        int? vendorId,
        bool? isActive)
    {
        var filters = new List<FilterOption>();

        if (!string.IsNullOrWhiteSpace(busCode))
        {
            filters.Add(new FilterOption { Field = "CHR_BUS_CODE", Operator = "LIKE", Value = $"%{busCode.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(busName))
        {
            filters.Add(new FilterOption { Field = "NVCHR_BUS_NAME", Operator = "LIKE", Value = $"%{busName.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            filters.Add(new FilterOption { Field = "CHR_LICENSE_PLATE", Operator = "LIKE", Value = $"%{licensePlate.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filters.Add(new FilterOption { Field = "NVCHR_CATEGORY", Operator = "LIKE", Value = $"%{category.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            filters.Add(new FilterOption { Field = "NVCHR_BRAND", Operator = "LIKE", Value = $"%{brand.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(model))
        {
            filters.Add(new FilterOption { Field = "NVCHR_MODEL", Operator = "LIKE", Value = $"%{model.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(modelYear))
        {
            filters.Add(new FilterOption { Field = "NVCHR_MODEL_YEAR", Operator = "LIKE", Value = $"%{modelYear.Trim()}%" });
        }

        if (capacity.HasValue)
        {
            filters.Add(new FilterOption { Field = "INT_CAPACITY", Operator = "=", Value = capacity.Value });
        }

        if (vendorId.HasValue)
        {
            filters.Add(new FilterOption { Field = "INT_VENDOR_ID", Operator = "=", Value = vendorId.Value });
        }

        if (isActive.HasValue)
        {
            filters.Add(new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = isActive.Value });
        }

        return filters;
    }

    private static int? ParseInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw.Trim(), out var value) ? value : null;
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

    private static DateTime? ParseDateTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var trimmed = raw.Trim();
        var formats = new[]
        {
            "dd/MM/yyyy",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy HH:mm:ss",
            "yyyy-MM-dd",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd HH:mm:ss"
        };

        if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact))
        {
            return exact;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.CurrentCulture, DateTimeStyles.None, out var local))
        {
            return local;
        }

        return null;
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
}
