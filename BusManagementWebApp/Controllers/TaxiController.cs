using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Controllers;

public class TaxiController : Controller
{
    private readonly ITmTaxiService _tmTaxiService;

    public TaxiController(ITmTaxiService tmTaxiService)
    {
        _tmTaxiService = tmTaxiService;
    }

    [HttpGet]
    public async Task<IActionResult> Management(string? brand, string? category, string? licensePlate, string? driverInfo, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var filters = BuildTaxiFilters(brand, category, licensePlate, driverInfo);

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

        var resp = await _tmTaxiService.SearchByConditionAsync(searchOptions);
        var rows = resp?.Data?.Data?.ToList() ?? new List<TM_TAXIDTO>();

        var totalCount = (int)(resp?.Data?.TotalCount ?? 0);
        var accurateCountResp = await _tmTaxiService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 100000,
            Filters = filters
        });
        if (accurateCountResp.Success)
        {
            totalCount = accurateCountResp.Data?.Data?.Count ?? totalCount;
        }
        var totalPages = (int)(resp?.Data?.TotalPages ?? 0);
        if (totalPages <= 0)
        {
            totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;
        }

        ViewBag.Brand = brand;
        ViewBag.Category = category;
        ViewBag.LicensePlate = licensePlate;
        ViewBag.DriverInfo = driverInfo;
        ViewBag.PageNumber = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = totalPages;

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTaxi(string? createLicensePlate, string? createCategory, string? createBrand, int? createCapacity, int? createDriverCode, string? createDriverInfo, bool? createIsActive, string? brand, string? category, string? licensePlate, string? driverInfo, int page = 1, int pageSize = 10)
    {
        var actor = GetCurrentActor();
        var now = DateTime.Now;

        var entity = new TM_TAXI
        {
            CHR_LICENSE_PLATE =(createLicensePlate ?? string.Empty).Trim(),
            NVCHR_CATEGORY =(createCategory?? string.Empty).Trim(),
            NVCHR_BRAND = (createBrand?? string.Empty).Trim(),
            INT_CAPACITY = createCapacity,
            INT_DRIVER = createDriverCode,
            NVCHR_DRIVER = (createDriverInfo ?? string.Empty).Trim(),
            BIT_IS_ACTIVE = createIsActive ?? true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = now
        };

        var resp = await _tmTaxiService.AddAsyncV2(entity);
        TempData[resp.Success ? "TaxiSuccess" : "TaxiError"] = resp.Success ? "Thêm xe Taxi thành công." : (resp.Message ?? "Không thể thêm xe Taxi.");
        return RedirectToAction(nameof(Management), new { brand, category, licensePlate, driverInfo, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaxi(int id, string? updateLicensePlate, string? updateCategory, string? updateBrand, int? updateCapacity, int? updateDriverCode, string? updateDriverInfo, bool? updateIsActive, string? brand, string? category, string? licensePlate, string? driverInfo, int page = 1, int pageSize = 10)
    {
        var currentResp = await _tmTaxiService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["TaxiError"] = currentResp.Message ?? "Không tìm thấy xe Taxi.";
            return RedirectToAction(nameof(Management), new { brand, category, licensePlate, driverInfo, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_TAXI
        {
            ID = current.ID,
            CHR_LICENSE_PLATE = (updateLicensePlate ?? string.Empty).Trim(),
            NVCHR_CATEGORY = (updateCategory ?? string.Empty).Trim(),
            NVCHR_BRAND = (updateBrand ?? string.Empty).Trim(),
            INT_CAPACITY = updateCapacity ?? current.INT_CAPACITY,
            INT_DRIVER = updateDriverCode ?? current.INT_DRIVER,
            NVCHR_DRIVER = (updateDriverInfo ?? string.Empty).Trim(),
            BIT_IS_ACTIVE = updateIsActive ?? current.BIT_IS_ACTIVE,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now
        };

        var resp = await _tmTaxiService.UpdateAsync(entity);
        if (resp.Success)
        {
            TempData["TaxiSuccess"] = "Cập nhật xe Taxi thành công.";
        }

        return RedirectToAction(nameof(Management), new { brand, category, licensePlate, driverInfo, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTaxi(int id, string? brand, string? category, string? licensePlate, string? driverInfo, int page = 1, int pageSize = 10)
    {
        var currentResp = await _tmTaxiService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["TaxiError"] = currentResp.Message ?? "Không tìm thấy xe Taxi.";
            return RedirectToAction(nameof(Management), new { brand, category, licensePlate, driverInfo, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_TAXI
        {
            ID = current.ID,
            CHR_LICENSE_PLATE = current.CHR_LICENSE_PLATE,
            NVCHR_CATEGORY = current.NVCHR_CATEGORY,
            NVCHR_BRAND = current.NVCHR_BRAND,
            INT_CAPACITY = current.INT_CAPACITY,
            INT_DRIVER = current.INT_DRIVER,
            NVCHR_DRIVER = current.NVCHR_DRIVER,
            BIT_IS_ACTIVE = false,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now
        };

        var resp = await _tmTaxiService.UpdateAsync(entity);
        TempData[resp.Success ? "TaxiSuccess" : "TaxiError"] = resp.Success ? "Đã xóa xe Taxi." : (resp.Message ?? "Không thể xóa xe Taxi.");

        return RedirectToAction(nameof(Management), new { brand, category, licensePlate, driverInfo, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGrid(List<int>? deletedIds, string? brand, string? category, string? licensePlate, string? driverInfo, int page = 1, int pageSize = 10)
    {
        deletedIds ??= new();

        var actor = GetCurrentActor();
        var deleted = 0;
        var failed = 0;

        foreach (var id in deletedIds.Distinct())
        {
            if (id <= 0)
            {
                continue;
            }

            var currentDeleteResp = await _tmTaxiService.GetByIdAsync(id);
            var currentDelete = currentDeleteResp.Data;
            if (!currentDeleteResp.Success || currentDelete == null)
            {
                failed++;
                continue;
            }

            var softDeleteEntity = new TM_TAXI
            {
                ID = currentDelete.ID,
                CHR_LICENSE_PLATE = currentDelete.CHR_LICENSE_PLATE,
                NVCHR_CATEGORY = currentDelete.NVCHR_CATEGORY,
                NVCHR_BRAND = currentDelete.NVCHR_BRAND,
                INT_CAPACITY = currentDelete.INT_CAPACITY,
                INT_DRIVER = currentDelete.INT_DRIVER,
                NVCHR_DRIVER = currentDelete.NVCHR_DRIVER,
                BIT_IS_ACTIVE = false,
                CHR_USER_CREATE = currentDelete.CHR_USER_CREATE,
                DTM_CREATE = currentDelete.DTM_CREATE,
                CHR_USER_UPDATE = actor,
                DTM_UPDATE = DateTime.Now
            };

            var softDeleteResp = await _tmTaxiService.UpdateAsync(softDeleteEntity);
            if (softDeleteResp.Success) deleted++;
            else failed++;
        }

        TempData[failed == 0 ? "TaxiSuccess" : "TaxiError"] = failed == 0 ? (deleted > 0 ? "Xóa đã chọn thành công." : "Không có dữ liệu để xóa.") : "Xóa đã chọn thất bại.";

        return RedirectToAction(nameof(Management), new
        {
            brand,
            category,
            licensePlate,
            driverInfo,
            page,
            pageSize
        });
    }

    private static List<FilterOption> BuildTaxiFilters(string? brand, string? category, string? licensePlate, string? driverInfo)
    {
        var filters = new List<FilterOption>
        {
            new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
        };

        if (!string.IsNullOrWhiteSpace(brand))
        {
            filters.Add(new FilterOption { Field = "NVCHR_BRAND", Operator = "LIKE", Value = $"%{brand.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            filters.Add(new FilterOption { Field = "NVCHR_CATEGORY", Operator = "LIKE", Value = $"%{category.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            filters.Add(new FilterOption { Field = "CHR_LICENSE_PLATE", Operator = "LIKE", Value = $"%{licensePlate.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(driverInfo))
        {
            filters.Add(new FilterOption { Field = "NVCHR_DRIVER", Operator = "LIKE", Value = $"%{driverInfo.Trim()}%" });
        }

        return filters;
    }

    private string GetCurrentActor()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value
            ?? User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? "system";
    }
}