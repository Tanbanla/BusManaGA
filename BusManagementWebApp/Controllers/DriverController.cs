using System.Security.Claims;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace BusManagementWebApp.Controllers;

public class DriverController : Controller
{
    private readonly ITmDriverService _tmDriverService;
    private readonly ITmVendorService _tmVendorService;
    private readonly ITmDriverViolationService _tmDriverViolationService;
    private readonly ITmBusService _tmBusService;

    public DriverController(ITmDriverService tmDriverService, ITmVendorService tmVendorService, ITmDriverViolationService tmDriverViolationService, ITmBusService tmBusService)
    {
        _tmDriverService = tmDriverService;
        _tmVendorService = tmVendorService;
        _tmDriverViolationService = tmDriverViolationService;
        _tmBusService = tmBusService;
    }

    [HttpGet]
    public async Task<IActionResult> Management(int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var searchOptions = new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = BuildDriverFilters(vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode),
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var resp = await _tmDriverService.SearchByConditionAsync(searchOptions);
        var rows = resp?.Data?.Data?.ToList() ?? new List<TM_DRIVERDTO>();

        var totalCount = (int)(resp?.Data?.TotalCount ?? 0);
        var totalPages = (int)(resp?.Data?.TotalPages ?? 0);
        if (totalPages <= 0)
        {
            totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;
        }

        var vendorResp = await _tmVendorService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 100000
        });
        var vendors = vendorResp?.Data?.Data?.ToList() ?? new List<TM_VENDORDTO>();
        var vendorDict = vendors.ToDictionary(x => x.ID, x => x.NVCHR_VENDOR_NAME);

        var busResp = await _tmBusService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
            },
            SortOptions = new()
            {
                new SortOption { Field = "CHR_BUS_CODE", SortDirection = "ASC" }
            }
        });
        var buses = busResp?.Data?.Data?.ToList() ?? new List<TM_BUSDTO>();
        var busDisplayByCode = buses
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_BUS_CODE))
            .ToDictionary(
                x => x.CHR_BUS_CODE!.Trim(),
                x => string.IsNullOrWhiteSpace(x.NVCHR_BUS_NAME)
                    ? x.CHR_BUS_CODE!.Trim()
                    : $"{x.CHR_BUS_CODE!.Trim()} - {x.NVCHR_BUS_NAME!.Trim()}",
                StringComparer.OrdinalIgnoreCase);

        var violationsByDriverId = new Dictionary<int, List<TM_DRIVER_VIOLATIONDTO>>();
        foreach (var driver in rows)
        {
            var violationResp = await _tmDriverViolationService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 1000,
                Filters = new List<FilterOption>
                {
                    new FilterOption { Field = "INT_DRIVER_ID", Operator = "=", Value = driver.ID }
                },
                SortOptions = new()
                {
                    new SortOption { Field = "DTM_VIOLATION_DATE", SortDirection = "DESC" },
                    new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
                }
            });

            violationsByDriverId[driver.ID] = violationResp?.Data?.Data?.ToList() ?? new List<TM_DRIVER_VIOLATIONDTO>();
        }

        ViewBag.VendorId = vendorId;
        ViewBag.DriverName = driverName;
        ViewBag.DriverPhone = driverPhone;
        ViewBag.LicenseCategory = licenseCategory;
        ViewBag.DateOfBirth = dateOfBirth;
        ViewBag.AccessCardCode = accessCardCode;
        ViewBag.PageNumber = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = totalPages;
        ViewBag.Vendors = vendors;
        ViewBag.VendorDict = vendorDict;
        ViewBag.ViolationsByDriverId = violationsByDriverId;
        ViewBag.Buses = buses;
        ViewBag.BusDisplayByCode = busDisplayByCode;

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDriver(int? createVendorId, string? createDriverName, string? createDriverPhone, string? createLicenseCategory, string? createDateOfBirth, string? createAccessCardCode, IFormFile? createAvartarFile, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        var actor = GetCurrentActor();
        var now = DateTime.Now;

        var avatarPath = SaveAvatarFile(createAvartarFile);

        var entity = new TM_DRIVER
        {
            NVCHR_DRIVER_NAME = (createDriverName ?? string.Empty).Trim(),
            CHR_DRIVER_PHONE = (createDriverPhone ?? string.Empty).Trim(),
            CHR_LICENSE_CATEGORY = (createLicenseCategory ?? string.Empty).Trim(),
            CHR_DATE_OF_BIRTH = (createDateOfBirth ?? string.Empty).Trim(),
            CHR_ACCESS_CARD_CODE = (createAccessCardCode ?? string.Empty).Trim(),
            CHR_AVARTAR_PATH = avatarPath,
            INT_VENDOR_ID = createVendorId,
            BIT_IS_ACTIVE = true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = now
        };

        var resp = await _tmDriverService.AddAsyncV2(entity);
        TempData[resp.Success ? "DriverSuccess" : "DriverError"] = resp.Success ? "Thêm tài xế thành công." : (resp.Message ?? "Không thể thêm tài xế.");

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDriverViolation(int id, int driverId, string? busCode, string? violationType, string? description, string? violationDate, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        var currentResp = await _tmDriverViolationService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["DriverError"] = currentResp.Message ?? "Không tìm thấy vi phạm để cập nhật.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        if (string.IsNullOrWhiteSpace(violationType))
        {
            TempData["DriverError"] = "Vui lòng nhập loại vi phạm.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        var normalizedBusCode = busCode?.Trim();
        int? resolvedBusId = null;
        if (!string.IsNullOrWhiteSpace(normalizedBusCode))
        {
            var busResp = await _tmBusService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new FilterOption { Field = "CHR_BUS_CODE", Operator = "=", Value = normalizedBusCode }
                }
            });
            resolvedBusId = busResp?.Data?.Data?.FirstOrDefault()?.ID;
        }

        var actor = GetCurrentActor();
        var entity = new TM_DRIVER_VIOLATION
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_DRIVER_ID = driverId,
            INT_BUS_ID = resolvedBusId,
            CHR_BUS_CODE = normalizedBusCode,
            CHR_VIOLATION_TYPE = violationType?.Trim(),
            NVCHR_DESCRIPTION = description?.Trim(),
            DTM_VIOLATION_DATE = ParseDateTime(violationDate) ?? current.DTM_VIOLATION_DATE
        };

        var resp = await _tmDriverViolationService.UpdateAsync(entity);
        TempData[resp.Success ? "DriverSuccess" : "DriverError"] = resp.Success
            ? "Cập nhật vi phạm thành công."
            : (resp.Message ?? "Không thể cập nhật vi phạm.");

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDriverViolation(int id, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        var resp = await _tmDriverViolationService.DeleteAsync(id);
        TempData[resp.Success ? "DriverSuccess" : "DriverError"] = resp.Success
            ? "Đã xóa vi phạm."
            : (resp.Message ?? "Không thể xóa vi phạm.");

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteManyDriverViolations(int driverId, List<int>? ids, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        ids = ids?.Distinct().Where(x => x > 0).ToList() ?? new List<int>();
        if (!ids.Any())
        {
            TempData["DriverError"] = "Vui lòng chọn ít nhất 1 vi phạm để xóa.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        var deleted = 0;
        foreach (var id in ids)
        {
            var resp = await _tmDriverViolationService.DeleteAsync(id);
            if (resp.Success) deleted++;
        }

        TempData[deleted == ids.Count ? "DriverSuccess" : "DriverError"] = deleted == ids.Count
            ? $"Đã xóa {deleted} vi phạm."
            : $"Đã xóa {deleted}/{ids.Count} vi phạm.";

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> ExportDriverViolations(int driverId)
    {
        var driverResp = await _tmDriverService.GetByIdAsync(driverId);
        var driver = driverResp.Data;
        if (!driverResp.Success || driver == null)
        {
            TempData["DriverError"] = "Không tìm thấy tài xế để xuất vi phạm.";
            return RedirectToAction(nameof(Management));
        }

        var violationResp = await _tmDriverViolationService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "INT_DRIVER_ID", Operator = "=", Value = driverId }
            },
            SortOptions = new()
            {
                new SortOption { Field = "DTM_VIOLATION_DATE", SortDirection = "DESC" },
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var rows = violationResp?.Data?.Data?.ToList() ?? new List<TM_DRIVER_VIOLATIONDTO>();
        var busResp = await _tmBusService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
            },
            SortOptions = new()
            {
                new SortOption { Field = "CHR_BUS_CODE", SortDirection = "ASC" }
            }
        });
        var buses = busResp?.Data?.Data?.ToList() ?? new List<TM_BUSDTO>();
        var busDisplayByCode = buses
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_BUS_CODE))
            .ToDictionary(
                x => x.CHR_BUS_CODE!.Trim(),
                x => string.IsNullOrWhiteSpace(x.NVCHR_BUS_NAME)
                    ? x.CHR_BUS_CODE!.Trim()
                    : $"{x.CHR_BUS_CODE!.Trim()} - {x.NVCHR_BUS_NAME!.Trim()}",
                StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("DriverViolations");
        var headers = new[] { "STT", "Xe bus", "Loại vi phạm", "Mô tả", "Ngày vi phạm" };
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
        }

        var busSheet = workbook.Worksheets.Add("_BusLookup");
        for (var i = 0; i < buses.Count; i++)
        {
            var bus = buses[i];
            if (!string.IsNullOrWhiteSpace(bus.CHR_BUS_CODE) && busDisplayByCode.TryGetValue(bus.CHR_BUS_CODE.Trim(), out var busDisplay))
            {
                busSheet.Cell(i + 1, 1).Value = busDisplay;
                busSheet.Cell(i + 1, 2).Value = bus.CHR_BUS_CODE.Trim();
            }
        }
        busSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var row = 2;
        for (var i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            sheet.Cell(row, 1).Value = i + 1;
            sheet.Cell(row, 2).Value = !string.IsNullOrWhiteSpace(item.CHR_BUS_CODE) && busDisplayByCode.TryGetValue(item.CHR_BUS_CODE.Trim(), out var busDisplay)
                ? busDisplay
                : (item.CHR_BUS_CODE ?? string.Empty);
            sheet.Cell(row, 3).Value = item.CHR_VIOLATION_TYPE;
            sheet.Cell(row, 4).Value = item.NVCHR_DESCRIPTION;
            sheet.Cell(row, 5).Value = item.DTM_VIOLATION_DATE?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty;
            row++;
        }

        var validationEndRow = Math.Max(row + 100, 200);
        if (buses.Any())
        {
            for (var r = 2; r <= validationEndRow; r++)
            {
                var busValidation = sheet.Range(r, 2, r, 2).SetDataValidation();
                busValidation.List(busSheet.Range(1, 1, buses.Count, 1));
            }
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var driverSafe = string.IsNullOrWhiteSpace(driver.NVCHR_DRIVER_NAME) ? driver.ID.ToString() : driver.NVCHR_DRIVER_NAME.Trim().Replace(' ', '_');
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ViPhamTaiXe_{driverSafe}_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportDriverViolations(int driverId, IFormFile? importFile, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["DriverError"] = "Vui lòng chọn file vi phạm để import.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        var driverResp = await _tmDriverService.GetByIdAsync(driverId);
        if (!driverResp.Success || driverResp.Data == null)
        {
            TempData["DriverError"] = "Không tìm thấy tài xế để import vi phạm.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;
        var busResp = await _tmBusService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
            }
        });
        var buses = busResp?.Data?.Data?.ToList() ?? new List<TM_BUSDTO>();
        var busIdByCode = buses
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_BUS_CODE))
            .GroupBy(x => x.CHR_BUS_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ID, StringComparer.OrdinalIgnoreCase);
        var busCodeByDisplay = buses
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_BUS_CODE))
            .ToDictionary(
                x => string.IsNullOrWhiteSpace(x.NVCHR_BUS_NAME)
                    ? x.CHR_BUS_CODE!.Trim()
                    : $"{x.CHR_BUS_CODE!.Trim()} - {x.NVCHR_BUS_NAME!.Trim()}",
                x => x.CHR_BUS_CODE!.Trim(),
                StringComparer.OrdinalIgnoreCase);
        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (!cells.Any()) return false;

            var offset = 0;
            if (int.TryParse(cells.ElementAtOrDefault(0)?.Trim(), out _)
                && !string.IsNullOrWhiteSpace(cells.ElementAtOrDefault(1)))
            {
                offset = 1;
            }

            var busRaw = cells.ElementAtOrDefault(offset + 1)?.Trim();
            string? busCode = null;
            if (!string.IsNullOrWhiteSpace(busRaw))
            {
                if (busCodeByDisplay.TryGetValue(busRaw, out var mappedCode))
                {
                    busCode = mappedCode;
                }
                else
                {
                    busCode = busRaw;
                    var dashIndex = busRaw.IndexOf(" - ", StringComparison.OrdinalIgnoreCase);
                    if (dashIndex > 0)
                    {
                        busCode = busRaw.Substring(0, dashIndex).Trim();
                    }
                }
            }

            var entity = new TM_DRIVER_VIOLATION
            {
                INT_DRIVER_ID = driverId,
                INT_BUS_ID = !string.IsNullOrWhiteSpace(busCode) && busIdByCode.TryGetValue(busCode, out var resolvedBusId)
                    ? resolvedBusId
                    : null,
                CHR_BUS_CODE = busCode,
                CHR_VIOLATION_TYPE = cells.ElementAtOrDefault(offset + 2)?.Trim(),
                NVCHR_DESCRIPTION = cells.ElementAtOrDefault(offset + 3)?.Trim(),
                DTM_VIOLATION_DATE = ParseDateTime(cells.ElementAtOrDefault(offset + 4)) ?? DateTime.Now,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(entity.CHR_VIOLATION_TYPE))
            {
                entity.CHR_VIOLATION_TYPE = cells.ElementAtOrDefault(offset + 1)?.Trim();
                entity.NVCHR_DESCRIPTION = cells.ElementAtOrDefault(offset + 2)?.Trim();
                entity.DTM_VIOLATION_DATE = ParseDateTime(cells.ElementAtOrDefault(offset + 3)) ?? DateTime.Now;
                entity.CHR_BUS_CODE = cells.ElementAtOrDefault(offset + 4)?.Trim();
                if (!string.IsNullOrWhiteSpace(entity.CHR_BUS_CODE) && busIdByCode.TryGetValue(entity.CHR_BUS_CODE, out var fallbackBusId))
                {
                    entity.INT_BUS_ID = fallbackBusId;
                }
            }

            if (string.IsNullOrWhiteSpace(entity.CHR_VIOLATION_TYPE))
            {
                return false;
            }

            var addResp = await _tmDriverViolationService.AddAsyncV2(entity);
            return addResp.Success;
        }

        await ImportViolationFileAsync(importFile, TryImportCellsAsync, () => success++, () => failed++);

        TempData[failed == 0 ? "DriverSuccess" : "DriverError"] = failed == 0
            ? $"Import thành công {success} vi phạm cho tài xế."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportViolations(IFormFile? importViolationFile, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        if (importViolationFile == null || importViolationFile.Length == 0)
        {
            TempData["DriverError"] = "Vui lòng chọn file vi phạm để import.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;
        var busResp = await _tmBusService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
            }
        });
        var buses = busResp?.Data?.Data?.ToList() ?? new List<TM_BUSDTO>();
        var busIdByCode = buses
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_BUS_CODE))
            .GroupBy(x => x.CHR_BUS_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ID, StringComparer.OrdinalIgnoreCase);
        var busCodeByDisplay = buses
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_BUS_CODE))
            .ToDictionary(
                x => string.IsNullOrWhiteSpace(x.NVCHR_BUS_NAME)
                    ? x.CHR_BUS_CODE!.Trim()
                    : $"{x.CHR_BUS_CODE!.Trim()} - {x.NVCHR_BUS_NAME!.Trim()}",
                x => x.CHR_BUS_CODE!.Trim(),
                StringComparer.OrdinalIgnoreCase);

        async Task<bool> TryImportCellsAsync(List<string> cells)
        {
            if (!cells.Any()) return false;

            var offset = 0;
            if (int.TryParse(cells.ElementAtOrDefault(0)?.Trim(), out _)
                && !string.IsNullOrWhiteSpace(cells.ElementAtOrDefault(1)))
            {
                offset = 1;
            }

            int? parsedDriverId = ParseInt(cells.ElementAtOrDefault(offset));
            TM_DRIVERDTO? driver = null;
            if (parsedDriverId.HasValue && parsedDriverId.Value > 0)
            {
                var driverResp = await _tmDriverService.GetByIdAsync(parsedDriverId.Value);
                if (driverResp.Success)
                {
                    driver = driverResp.Data;
                }
            }
            else
            {
                var lookup = cells.ElementAtOrDefault(offset)?.Trim();
                if (!string.IsNullOrWhiteSpace(lookup))
                {
                    var driverResp = await _tmDriverService.SearchByConditionAsync(new SearchOptions
                    {
                        PageNumber = 1,
                        PageSize = 1,
                        Filters = new List<FilterOption>
                        {
                            new FilterOption { Field = "CHR_ACCESS_CARD_CODE", Operator = "=", Value = lookup }
                        }
                    });
                    driver = driverResp?.Data?.Data?.FirstOrDefault();

                    if (driver == null)
                    {
                        var fallbackResp = await _tmDriverService.SearchByConditionAsync(new SearchOptions
                        {
                            PageNumber = 1,
                            PageSize = 1,
                            Filters = new List<FilterOption>
                            {
                                new FilterOption { Field = "CHR_DRIVER_PHONE", Operator = "=", Value = lookup }
                            }
                        });
                        driver = fallbackResp?.Data?.Data?.FirstOrDefault();
                    }
                }
            }

            if (driver == null)
            {
                return false;
            }

            var busRaw = cells.ElementAtOrDefault(offset + 4)?.Trim();
            string? busCode = null;
            if (!string.IsNullOrWhiteSpace(busRaw))
            {
                if (busCodeByDisplay.TryGetValue(busRaw, out var mappedCode))
                {
                    busCode = mappedCode;
                }
                else
                {
                    busCode = busRaw;
                    var dashIndex = busRaw.IndexOf(" - ", StringComparison.OrdinalIgnoreCase);
                    if (dashIndex > 0)
                    {
                        busCode = busRaw.Substring(0, dashIndex).Trim();
                    }
                }
            }

            var entity = new TM_DRIVER_VIOLATION
            {
                INT_DRIVER_ID = driver.ID,
                INT_BUS_ID = !string.IsNullOrWhiteSpace(busCode) && busIdByCode.TryGetValue(busCode, out var resolvedBusId)
                    ? resolvedBusId
                    : null,
                CHR_VIOLATION_TYPE = cells.ElementAtOrDefault(offset + 1)?.Trim(),
                NVCHR_DESCRIPTION = cells.ElementAtOrDefault(offset + 2)?.Trim(),
                DTM_VIOLATION_DATE = ParseDateTime(cells.ElementAtOrDefault(offset + 3)) ?? DateTime.Now,
                CHR_BUS_CODE = busCode,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(entity.CHR_VIOLATION_TYPE))
            {
                return false;
            }

            var addResp = await _tmDriverViolationService.AddAsyncV2(entity);
            return addResp.Success;
        }

        await ImportViolationFileAsync(importViolationFile, TryImportCellsAsync, () => success++, () => failed++);

        TempData[failed == 0 ? "DriverSuccess" : "DriverError"] = failed == 0
            ? $"Import thành công {success} vi phạm."
            : $"Import hoàn tất vi phạm: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDriverViolation(int driverId, string? busCode, string? violationType, string? description, string? violationDate, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        var driverResp = await _tmDriverService.GetByIdAsync(driverId);
        if (!driverResp.Success || driverResp.Data == null)
        {
            TempData["DriverError"] = "Không tìm thấy tài xế để thêm vi phạm.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        if (string.IsNullOrWhiteSpace(violationType))
        {
            TempData["DriverError"] = "Vui lòng nhập loại vi phạm.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        var actor = GetCurrentActor();
        var normalizedBusCode = busCode?.Trim();
        int? resolvedBusId = null;
        if (!string.IsNullOrWhiteSpace(normalizedBusCode))
        {
            var busResp = await _tmBusService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new FilterOption { Field = "CHR_BUS_CODE", Operator = "=", Value = normalizedBusCode }
                }
            });

            resolvedBusId = busResp?.Data?.Data?.FirstOrDefault()?.ID;
        }

        var entity = new TM_DRIVER_VIOLATION
        {
            INT_DRIVER_ID = driverId,
            INT_BUS_ID = resolvedBusId,
            CHR_BUS_CODE = normalizedBusCode,
            CHR_VIOLATION_TYPE = violationType?.Trim(),
            NVCHR_DESCRIPTION = description?.Trim(),
            DTM_VIOLATION_DATE = ParseDateTime(violationDate) ?? DateTime.Now,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmDriverViolationService.AddAsyncV2(entity);
        TempData[resp.Success ? "DriverSuccess" : "DriverError"] = resp.Success
            ? "Thêm vi phạm thành công."
            : (resp.Message ?? "Không thể thêm vi phạm.");

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDriver(int id, int? updateVendorId, string? updateDriverName, string? updateDriverPhone, string? updateLicenseCategory, string? updateDateOfBirth, string? updateAccessCardCode, IFormFile? updateAvartarFile, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        var currentResp = await _tmDriverService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["DriverError"] = currentResp.Message ?? "Không tìm thấy tài xế.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        var actor = GetCurrentActor();

        var avatarPath = current.CHR_AVARTAR_PATH;
        if (updateAvartarFile != null && updateAvartarFile.Length > 0)
        {
            var newPath = SaveAvatarFile(updateAvartarFile);
            if (!string.IsNullOrWhiteSpace(newPath))
            {
                avatarPath = newPath;
            }
        }

        var entity = new TM_DRIVER
        {
            ID = current.ID,
            NVCHR_DRIVER_NAME = (updateDriverName ?? string.Empty).Trim(),
            CHR_DRIVER_PHONE = (updateDriverPhone ?? string.Empty).Trim(),
            CHR_LICENSE_CATEGORY = (updateLicenseCategory ?? string.Empty).Trim(),
            CHR_DATE_OF_BIRTH = (updateDateOfBirth ?? string.Empty).Trim(),
            CHR_ACCESS_CARD_CODE = (updateAccessCardCode ?? string.Empty).Trim(),
            CHR_AVARTAR_PATH = avatarPath,
            INT_VENDOR_ID = updateVendorId,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now
        };

        var resp = await _tmDriverService.UpdateAsync(entity);
        if (resp.Success)
        {
            TempData["DriverSuccess"] = "Cập nhật tài xế thành công.";
        }

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDriver(int id, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        var actor = GetCurrentActor();

        var currentResp = await _tmDriverService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["DriverError"] = "Không tìm thấy tài xế.";
            return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
        }

        var entity = new TM_DRIVER
        {
            ID = current.ID,
            NVCHR_DRIVER_NAME = current.NVCHR_DRIVER_NAME,
            CHR_DRIVER_PHONE = current.CHR_DRIVER_PHONE,
            CHR_LICENSE_CATEGORY = current.CHR_LICENSE_CATEGORY,
            CHR_DATE_OF_BIRTH = current.CHR_DATE_OF_BIRTH,
            CHR_ACCESS_CARD_CODE = current.CHR_ACCESS_CARD_CODE,
            CHR_AVARTAR_PATH = current.CHR_AVARTAR_PATH,
            INT_VENDOR_ID = current.INT_VENDOR_ID,
            BIT_IS_ACTIVE = false,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now
        };

        var resp = await _tmDriverService.UpdateAsync(entity);
        TempData[resp.Success ? "DriverSuccess" : "DriverError"] = resp.Success ? "Đã xóa tài xế." : (resp.Message ?? "Không thể xóa tài xế.");

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGrid(List<int>? deletedIds, int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode, int page = 1, int pageSize = 10)
    {
        deletedIds ??= new();
        var actor = GetCurrentActor();

        var deleted = 0;
        var failed = 0;

        foreach (var id in deletedIds.Distinct())
        {
            if (id <= 0) continue;

            var currentResp = await _tmDriverService.GetByIdAsync(id);
            var current = currentResp.Data;
            if (!currentResp.Success || current == null)
            {
                failed++;
                continue;
            }

            var entity = new TM_DRIVER
            {
                ID = current.ID,
                NVCHR_DRIVER_NAME = current.NVCHR_DRIVER_NAME,
                CHR_DRIVER_PHONE = current.CHR_DRIVER_PHONE,
                CHR_LICENSE_CATEGORY = current.CHR_LICENSE_CATEGORY,
                CHR_DATE_OF_BIRTH = current.CHR_DATE_OF_BIRTH,
                CHR_ACCESS_CARD_CODE = current.CHR_ACCESS_CARD_CODE,
                CHR_AVARTAR_PATH = current.CHR_AVARTAR_PATH,
                INT_VENDOR_ID = current.INT_VENDOR_ID,
                BIT_IS_ACTIVE = false,
                CHR_USER_CREATE = current.CHR_USER_CREATE,
                DTM_CREATE = current.DTM_CREATE,
                CHR_USER_UPDATE = actor,
                DTM_UPDATE = DateTime.Now
            };

            var resp = await _tmDriverService.UpdateAsync(entity);
            if (resp.Success) deleted++;
            else failed++;
        }

        TempData[failed == 0 ? "DriverSuccess" : "DriverError"] = failed == 0 ? (deleted > 0 ? "Xóa đã chọn thành công." : "Không có dữ liệu để xóa.") : "Xóa đã chọn thất bại.";

        return RedirectToAction(nameof(Management), new { vendorId, driverName, driverPhone, licenseCategory, dateOfBirth, accessCardCode, page, pageSize });
    }

    private static List<FilterOption> BuildDriverFilters(int? vendorId, string? driverName, string? driverPhone, string? licenseCategory, string? dateOfBirth, string? accessCardCode)
    {
        var filters = new List<FilterOption>
        {
            new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
        };

        if (vendorId.HasValue && vendorId.Value > 0)
        {
            filters.Add(new FilterOption { Field = "INT_VENDOR_ID", Operator = "=", Value = vendorId.Value });
        }

        if (!string.IsNullOrWhiteSpace(driverName))
        {
            filters.Add(new FilterOption { Field = "NVCHR_DRIVER_NAME", Operator = "LIKE", Value = $"%{driverName.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(driverPhone))
        {
            filters.Add(new FilterOption { Field = "CHR_DRIVER_PHONE", Operator = "LIKE", Value = $"%{driverPhone.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(licenseCategory))
        {
            filters.Add(new FilterOption { Field = "CHR_LICENSE_CATEGORY", Operator = "LIKE", Value = $"%{licenseCategory.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(dateOfBirth))
        {
            filters.Add(new FilterOption { Field = "CHR_DATE_OF_BIRTH", Operator = "LIKE", Value = $"%{dateOfBirth.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(accessCardCode))
        {
            filters.Add(new FilterOption { Field = "CHR_ACCESS_CARD_CODE", Operator = "LIKE", Value = $"%{accessCardCode.Trim()}%" });
        }

        return filters;
    }

    [HttpGet("Driver/DriverAvatarImage/{y}/{m}/{d}/{fileName}")]
    public IActionResult DriverAvatarImage(string y, string m, string d, string fileName)
    {
        try
        {
            var config = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var env = HttpContext.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            var uploadCfg = config?.GetValue<string>("Files:UploadPath") ?? "uploads/driverImg";
            var contentRoot = env?.ContentRootPath ?? Directory.GetCurrentDirectory();
            string physicalUploadPath = Path.IsPathRooted(uploadCfg)
                ? uploadCfg
                : Path.Combine(contentRoot, uploadCfg.TrimStart('~', '/', '\\'));

            var safeY = Path.GetFileName(y);
            var safeM = Path.GetFileName(m);
            var safeD = Path.GetFileName(d);
            var safeFile = Path.GetFileName(fileName);

            var physicalPath = Path.Combine(physicalUploadPath, safeY, safeM, safeD, safeFile);
            if (!System.IO.File.Exists(physicalPath)) return NotFound();

            var ext = Path.GetExtension(safeFile).ToLowerInvariant();
            var contentType = ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return PhysicalFile(physicalPath, contentType);
        }
        catch
        {
            return NotFound();
        }
    }

    private async Task ImportViolationFileAsync(IFormFile importFile, Func<List<string>, Task<bool>> importHandler, Action onSuccess, Action onFailed)
    {
        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                onFailed();
                return;
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>();
                for (var c = 1; c <= 12; c++)
                {
                    cells.Add(sheet.Cell(r, c).GetString());
                }

                if (await importHandler(cells)) onSuccess(); else onFailed();
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

                if (row == 1 && (line.Contains("violation", StringComparison.OrdinalIgnoreCase) || line.Contains("vi phạm", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var cells = ParseCsvLine(line);
                if (await importHandler(cells)) onSuccess(); else onFailed();
            }
        }
    }

    private static int? ParseInt(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw.Trim(), out var value) ? value : null;
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

    private string GetCurrentActor()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value
            ?? User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? "system";
    }

    private string? SaveAvatarFile(IFormFile? avatarFile)
    {
        if (avatarFile == null || avatarFile.Length <= 0) return null;

        try
        {
            var config = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var env = HttpContext.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
            var uploadCfg = config?.GetValue<string>("Files:UploadPath") ?? "uploads/driverImg";
            var contentRoot = env?.ContentRootPath ?? Directory.GetCurrentDirectory();
            string physicalUploadPath = Path.IsPathRooted(uploadCfg) ? uploadCfg : Path.Combine(contentRoot, uploadCfg.TrimStart('~', '/', '\\'));

            var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
            var ext = Path.GetExtension(avatarFile.FileName);
            if (string.IsNullOrWhiteSpace(ext) || !allowedExts.Contains(ext))
            {
                ext = ".png";
            }

            var now = DateTime.Now;
            var y = now.Year.ToString("0000");
            var mm = now.Month.ToString("00");
            var dd = now.Day.ToString("00");

            var subFolder = Path.Combine(physicalUploadPath, y, mm, dd);
            Directory.CreateDirectory(subFolder);

            var fileName = Guid.NewGuid().ToString("N") + ext.ToLowerInvariant();
            var fullPath = Path.Combine(subFolder, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                avatarFile.CopyTo(stream);
            }

            return $"/Driver/DriverAvatarImage/{y}/{mm}/{dd}/{fileName}";
        }
        catch
        {
            return null;
        }
    }
}