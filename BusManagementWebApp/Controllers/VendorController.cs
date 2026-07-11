using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Controllers;

public class VendorController : Controller
{
    private readonly ITmVendorService _tmVendorService;
    private readonly ITmBusService _tmBusService;
    private readonly ITmDriverService _tmDriverService;

    public VendorController(ITmVendorService tmVendorService, ITmBusService tmBusService, ITmDriverService tmDriverService)
    {
        _tmVendorService = tmVendorService;
        _tmBusService = tmBusService;
        _tmDriverService = tmDriverService;
    }

    [HttpGet]
    public async Task<IActionResult> Management(string? vendorName, string? address, string? vendorPhone, string? taxIdentifyNumber, string? directorName, string? directorPhone, string? picName, string? picPhone, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var searchOptions = new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = BuildVendorFilters(vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone),
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        };

        var resp = await _tmVendorService.SearchByConditionAsync(searchOptions);
        var rows = resp?.Data?.Data?.ToList() ?? new List<TM_VENDORDTO>();

        var vendorIds = rows.Select(x => x.ID).ToHashSet();
        var busResp = await _tmBusService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
            }
        });

        var buses = (busResp?.Data?.Data ?? Enumerable.Empty<TM_BUSDTO>())
            .Where(x => x.INT_VENDOR_ID.HasValue && vendorIds.Contains(x.INT_VENDOR_ID.Value))
            .ToList();

        var busCount45 = buses.Where(x => x.INT_CAPACITY == 45)
            .GroupBy(x => x.INT_VENDOR_ID!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var busCount29 = buses.Where(x => x.INT_CAPACITY == 29)
            .GroupBy(x => x.INT_VENDOR_ID!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var busCount16 = buses.Where(x => x.INT_CAPACITY == 16)
            .GroupBy(x => x.INT_VENDOR_ID!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var busCount7 = buses.Where(x => x.INT_CAPACITY == 7)
            .GroupBy(x => x.INT_VENDOR_ID!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var busCountTotal = buses
            .GroupBy(x => x.INT_VENDOR_ID!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var busesByVendorId = buses
            .GroupBy(x => x.INT_VENDOR_ID!.Value)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(x => x.CHR_BUS_CODE)
                    .ThenBy(x => x.CHR_LICENSE_PLATE)
                    .ToList());

        var driverResp = await _tmDriverService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
            }
        });
        var drivers = (driverResp?.Data?.Data ?? Enumerable.Empty<TM_DRIVERDTO>())
            .Where(x => x.INT_VENDOR_ID.HasValue && vendorIds.Contains(x.INT_VENDOR_ID.Value))
            .ToList();

        var driversByVendorId = drivers
            .GroupBy(x => x.INT_VENDOR_ID!.Value)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(x => x.NVCHR_DRIVER_NAME)
                    .ThenBy(x => x.CHR_DRIVER_PHONE)
                    .ToList());

        var totalCount = (int)(resp?.Data?.TotalCount ?? 0);
        var totalPages = (int)(resp?.Data?.TotalPages ?? 0);
        if (totalPages <= 0)
        {
            totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;
        }

        ViewBag.VendorName = vendorName;
        ViewBag.Address = address;
        ViewBag.VendorPhone = vendorPhone;
        ViewBag.TaxIdentifyNumber = taxIdentifyNumber;
        ViewBag.DirectorName = directorName;
        ViewBag.DirectorPhone = directorPhone;
        ViewBag.PicName = picName;
        ViewBag.PicPhone = picPhone;
        ViewBag.PageNumber = page;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = totalPages;
        ViewBag.BusCount45 = busCount45;
        ViewBag.BusCount29 = busCount29;
        ViewBag.BusCount16 = busCount16;
        ViewBag.BusCount7 = busCount7;
        ViewBag.BusCountTotal = busCountTotal;
        ViewBag.BusesByVendorId = busesByVendorId;
        ViewBag.DriversByVendorId = driversByVendorId;

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVendor(string? createVendorCode, string? createVendorName, string? createVendorPhone, string? createAddress, string? createTaxIdentifyNumber, string? createDirectorName, string? createDirectorPhone, string? createPicName, string? createPicPhone, string? vendorName, string? address, string? vendorPhone, string? taxIdentifyNumber, string? directorName, string? directorPhone, string? picName, string? picPhone, int page = 1, int pageSize = 10)
    {
        var actor = GetCurrentActor();
        var now = DateTime.Now;

        var entity = new TM_VENDOR
        {
            CHR_VENDOR_CODE = (createVendorCode ?? string.Empty).Trim(),
            NVCHR_VENDOR_NAME = (createVendorName ?? string.Empty).Trim(),
            CHR_VENDOR_PHONE = (createVendorPhone ?? string.Empty).Trim(),
            NVCHR_ADDRESS = (createAddress ?? string.Empty).Trim(),
            CHR_TAX_IDENTIFY_NUMBER = (createTaxIdentifyNumber ?? string.Empty).Trim(),
            NVCHR_DIRECTOR_NAME = (createDirectorName ?? string.Empty).Trim(),
            CHR_DIRECTOR_PHONE = (createDirectorPhone ?? string.Empty).Trim(),
            NVCHR_PIC_NAME = (createPicName ?? string.Empty).Trim(),
            CHR_PIC_PHONE = (createPicPhone ?? string.Empty).Trim(),
            CHR_USER_CREATE = actor,
            DTM_CREATE = now
        };

        var resp = await _tmVendorService.AddAsyncV2(entity);
        TempData[resp.Success ? "VendorSuccess" : "VendorError"] = resp.Success ? "Thêm nhà xe thành công." : (resp.Message ?? "Không thể thêm nhà xe.");

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateBus(
        int id,
        string? updateBusCode,
        string? updateBusName,
        string? updateLicensePlate,
        string? updateCategory,
        string? updateBrand,
        string? updateModel,
        string? updateModelYear,
        int? updateCapacity,
        bool? updateIsActive,
        string? vendorName,
        string? address,
        string? vendorPhone,
        string? taxIdentifyNumber,
        string? directorName,
        string? directorPhone,
        string? picName,
        string? picPhone,
        int page = 1,
        int pageSize = 10)
    {
        var currentResp = await _tmBusService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["VendorError"] = currentResp.Message ?? "Không tìm thấy xe để cập nhật.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        if (string.IsNullOrWhiteSpace(updateBusCode) || string.IsNullOrWhiteSpace(updateLicensePlate))
        {
            TempData["VendorError"] = "Vui lòng nhập mã xe và biển số xe.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_BUS_CODE = updateBusCode.Trim(),
            NVCHR_BUS_NAME = updateBusName?.Trim(),
            CHR_LICENSE_PLATE = updateLicensePlate.Trim(),
            NVCHR_CATEGORY = updateCategory?.Trim(),
            NVCHR_BRAND = updateBrand?.Trim(),
            NVCHR_MODEL = updateModel?.Trim(),
            NVCHR_MODEL_YEAR = updateModelYear?.Trim(),
            INT_CAPACITY = updateCapacity,
            INT_VENDOR_ID = current.INT_VENDOR_ID,
            BIT_IS_ACTIVE = updateIsActive ?? current.BIT_IS_ACTIVE
        };

        var resp = await _tmBusService.UpdateAsync(entity);
        TempData[resp.Success ? "VendorSuccess" : "VendorError"] = resp.Success
            ? "Cập nhật xe thành công."
            : (resp.Message ?? "Không thể cập nhật xe.");

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteBus(int id, string? vendorName, string? address, string? vendorPhone, string? taxIdentifyNumber, string? directorName, string? directorPhone, string? picName, string? picPhone, int page = 1, int pageSize = 10)
    {
        var resp = await _tmBusService.DeleteAsync(id);
        TempData[resp.Success ? "VendorSuccess" : "VendorError"] = resp.Success
            ? "Đã xóa xe thành công."
            : (resp.Message ?? "Không thể xóa xe.");

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreateDriver(
        int vendorId,
        string? createDriverName,
        string? createDriverPhone,
        string? createLicenseCategory,
        string? createDateOfBirth,
        string? createAccessCardCode,
        string? vendorName,
        string? address,
        string? vendorPhone,
        string? taxIdentifyNumber,
        string? directorName,
        string? directorPhone,
        string? picName,
        string? picPhone,
        int page = 1,
        int pageSize = 10)
    {
        var vendorResp = await _tmVendorService.GetByIdAsync(vendorId);
        if (!vendorResp.Success || vendorResp.Data == null)
        {
            TempData["VendorError"] = "Không tìm thấy nhà cung cấp để thêm tài xế.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        if (string.IsNullOrWhiteSpace(createDriverName))
        {
            TempData["VendorError"] = "Vui lòng nhập tên tài xế.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_DRIVER
        {
            NVCHR_DRIVER_NAME = createDriverName.Trim(),
            CHR_DRIVER_PHONE = createDriverPhone?.Trim(),
            CHR_LICENSE_CATEGORY = createLicenseCategory?.Trim(),
            CHR_DATE_OF_BIRTH = createDateOfBirth?.Trim(),
            CHR_ACCESS_CARD_CODE = createAccessCardCode?.Trim(),
            INT_VENDOR_ID = vendorId,
            BIT_IS_ACTIVE = true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmDriverService.AddAsyncV2(entity);
        TempData[resp.Success ? "VendorSuccess" : "VendorError"] = resp.Success
            ? "Thêm tài xế thành công."
            : (resp.Message ?? "Không thể thêm tài xế.");

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickUpdateDriver(
        int id,
        string? updateDriverName,
        string? updateDriverPhone,
        string? updateLicenseCategory,
        string? updateDateOfBirth,
        string? updateAccessCardCode,
        string? vendorName,
        string? address,
        string? vendorPhone,
        string? taxIdentifyNumber,
        string? directorName,
        string? directorPhone,
        string? picName,
        string? picPhone,
        int page = 1,
        int pageSize = 10)
    {
        var currentResp = await _tmDriverService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["VendorError"] = currentResp.Message ?? "Không tìm thấy tài xế để cập nhật.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_DRIVER
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            NVCHR_DRIVER_NAME = updateDriverName?.Trim(),
            CHR_DRIVER_PHONE = updateDriverPhone?.Trim(),
            CHR_LICENSE_CATEGORY = updateLicenseCategory?.Trim(),
            CHR_DATE_OF_BIRTH = updateDateOfBirth?.Trim(),
            CHR_ACCESS_CARD_CODE = updateAccessCardCode?.Trim(),
            CHR_AVARTAR_PATH = current.CHR_AVARTAR_PATH,
            INT_VENDOR_ID = current.INT_VENDOR_ID,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE
        };

        var resp = await _tmDriverService.UpdateAsync(entity);
        TempData[resp.Success ? "VendorSuccess" : "VendorError"] = resp.Success
            ? "Cập nhật tài xế thành công."
            : (resp.Message ?? "Không thể cập nhật tài xế.");

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDeleteDriver(int id, string? vendorName, string? address, string? vendorPhone, string? taxIdentifyNumber, string? directorName, string? directorPhone, string? picName, string? picPhone, int page = 1, int pageSize = 10)
    {
        var currentResp = await _tmDriverService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["VendorError"] = currentResp.Message ?? "Không tìm thấy tài xế để xóa.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_DRIVER
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            NVCHR_DRIVER_NAME = current.NVCHR_DRIVER_NAME,
            CHR_DRIVER_PHONE = current.CHR_DRIVER_PHONE,
            CHR_LICENSE_CATEGORY = current.CHR_LICENSE_CATEGORY,
            CHR_DATE_OF_BIRTH = current.CHR_DATE_OF_BIRTH,
            CHR_ACCESS_CARD_CODE = current.CHR_ACCESS_CARD_CODE,
            CHR_AVARTAR_PATH = current.CHR_AVARTAR_PATH,
            INT_VENDOR_ID = current.INT_VENDOR_ID,
            BIT_IS_ACTIVE = false
        };

        var resp = await _tmDriverService.UpdateAsync(entity);
        TempData[resp.Success ? "VendorSuccess" : "VendorError"] = resp.Success
            ? "Đã xóa tài xế thành công."
            : (resp.Message ?? "Không thể xóa tài xế.");

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreateBus(
        int vendorId,
        string? createBusCode,
        string? createBusName,
        string? createLicensePlate,
        string? createCategory,
        string? createBrand,
        string? createModel,
        string? createModelYear,
        int? createCapacity,
        bool? createIsActive,
        string? vendorName,
        string? address,
        string? vendorPhone,
        string? taxIdentifyNumber,
        string? directorName,
        string? directorPhone,
        string? picName,
        string? picPhone,
        int page = 1,
        int pageSize = 10)
    {
        var vendorResp = await _tmVendorService.GetByIdAsync(vendorId);
        if (!vendorResp.Success || vendorResp.Data == null)
        {
            TempData["VendorError"] = "Không tìm thấy nhà cung cấp để thêm xe.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        if (string.IsNullOrWhiteSpace(createBusCode) || string.IsNullOrWhiteSpace(createLicensePlate))
        {
            TempData["VendorError"] = "Vui lòng nhập mã xe và biển số xe khi thêm nhanh.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_BUS
        {
            CHR_BUS_CODE = createBusCode.Trim(),
            NVCHR_BUS_NAME = createBusName?.Trim(),
            CHR_LICENSE_PLATE = createLicensePlate.Trim(),
            NVCHR_CATEGORY = createCategory?.Trim(),
            NVCHR_BRAND = createBrand?.Trim(),
            NVCHR_MODEL = createModel?.Trim(),
            NVCHR_MODEL_YEAR = createModelYear?.Trim(),
            INT_CAPACITY = createCapacity,
            INT_VENDOR_ID = vendorId,
            BIT_IS_ACTIVE = createIsActive ?? true,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var addResp = await _tmBusService.AddAsyncV2(entity);
        TempData[addResp.Success ? "VendorSuccess" : "VendorError"] = addResp.Success
            ? "Thêm xe nhanh thành công."
            : (addResp.Message ?? "Không thể thêm xe nhanh.");

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVendor(int id, string? updateVendorCode, string? updateVendorName, string? updateVendorPhone, string? updateAddress, string? updateTaxIdentifyNumber, string? updateDirectorName, string? updateDirectorPhone, string? updatePicName, string? updatePicPhone, string? vendorName, string? address, string? vendorPhone, string? taxIdentifyNumber, string? directorName, string? directorPhone, string? picName, string? picPhone, int page = 1, int pageSize = 10)
    {
        var currentResp = await _tmVendorService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["VendorError"] = currentResp.Message ?? "Không tìm thấy nhà xe.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        var actor = GetCurrentActor();
        var entity = new TM_VENDOR
        {
            ID = current.ID,
            CHR_VENDOR_CODE = (updateVendorCode ?? string.Empty).Trim(),
            NVCHR_VENDOR_NAME = (updateVendorName ?? string.Empty).Trim(),
            CHR_VENDOR_PHONE = (updateVendorPhone ?? string.Empty).Trim(),
            NVCHR_ADDRESS = (updateAddress ?? string.Empty).Trim(),
            CHR_TAX_IDENTIFY_NUMBER = (updateTaxIdentifyNumber ?? string.Empty).Trim(),
            NVCHR_DIRECTOR_NAME = (updateDirectorName ?? string.Empty).Trim(),
            CHR_DIRECTOR_PHONE = (updateDirectorPhone ?? string.Empty).Trim(),
            NVCHR_PIC_NAME = (updatePicName ?? string.Empty).Trim(),
            CHR_PIC_PHONE = (updatePicPhone ?? string.Empty).Trim(),
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now
        };

        var resp = await _tmVendorService.UpdateAsync(entity);
        if (resp.Success)
        {
            TempData["VendorSuccess"] = "Cập nhật nhà xe thành công.";
        }

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVendor(int id, string? vendorName, string? address, string? vendorPhone, string? taxIdentifyNumber, string? directorName, string? directorPhone, string? picName, string? picPhone, int page = 1, int pageSize = 10)
    {
        var (hasActiveDriver, hasActiveBus) = await CheckActiveReferencesAsync(id);
        if (hasActiveDriver || hasActiveBus)
        {
            var reasons = new List<string>();
            if (hasActiveDriver) reasons.Add("đang có tài xế hoạt động");
            if (hasActiveBus) reasons.Add("đang có xe bus hoạt động");

            TempData["VendorError"] = $"Không thể xóa nhà cung cấp vì {string.Join(" và ", reasons)}.";
            return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
        }

        var resp = await _tmVendorService.DeleteAsync(id);
        TempData[resp.Success ? "VendorSuccess" : "VendorError"] = resp.Success ? "Đã xóa nhà xe." : (resp.Message ?? "Không thể xóa nhà xe.");

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGrid(List<int>? deletedIds, string? vendorName, string? address, string? vendorPhone, string? taxIdentifyNumber, string? directorName, string? directorPhone, string? picName, string? picPhone, int page = 1, int pageSize = 10)
    {
        deletedIds ??= new();

        var deleted = 0;
        var failed = 0;
        var blockedMessages = new List<string>();

        foreach (var id in deletedIds.Distinct())
        {
            if (id <= 0)
            {
                continue;
            }

            var (hasActiveDriver, hasActiveBus) = await CheckActiveReferencesAsync(id);
            if (hasActiveDriver || hasActiveBus)
            {
                var vendorResp = await _tmVendorService.GetByIdAsync(id);
                var vendorCode = vendorResp.Data?.CHR_VENDOR_CODE ?? id.ToString();
                var reasons = new List<string>();
                if (hasActiveDriver) reasons.Add("có tài xế hoạt động");
                if (hasActiveBus) reasons.Add("có xe bus hoạt động");
                blockedMessages.Add($"NCC {vendorCode}: {string.Join(" và ", reasons)}");
                continue;
            }

            var resp = await _tmVendorService.DeleteAsync(id);
            if (resp.Success) deleted++;
            else failed++;
        }

        if (blockedMessages.Any())
        {
            var sb = new StringBuilder();
            if (deleted > 0)
            {
                sb.Append($"Đã xóa {deleted} nhà cung cấp. ");
            }
            sb.Append("Không thể xóa một số nhà cung cấp do còn xe, tài xế hoạt động: ");
            sb.Append(string.Join("; ", blockedMessages));
            TempData["VendorError"] = sb.ToString();
        }
        else
        {
            TempData[failed == 0 ? "VendorSuccess" : "VendorError"] = failed == 0 ? (deleted > 0 ? "Xóa đã chọn thành công." : "Không có dữ liệu để xóa.") : "Xóa đã chọn thất bại.";
        }

        return RedirectToAction(nameof(Management), new { vendorName, address, vendorPhone, taxIdentifyNumber, directorName, directorPhone, picName, picPhone, page, pageSize });
    }

    private async Task<(bool hasActiveDriver, bool hasActiveBus)> CheckActiveReferencesAsync(int vendorId)
    {
        var driverResp = await _tmDriverService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "INT_VENDOR_ID", Operator = "=", Value = vendorId },
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
            }
        });

        var busResp = await _tmBusService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new FilterOption { Field = "INT_VENDOR_ID", Operator = "=", Value = vendorId },
                new FilterOption { Field = "BIT_IS_ACTIVE", Operator = "=", Value = 1 }
            }
        });

        var hasActiveDriver = driverResp?.Data?.Data?.Any() == true;
        var hasActiveBus = busResp?.Data?.Data?.Any() == true;
        return (hasActiveDriver, hasActiveBus);
    }

    private static List<FilterOption> BuildVendorFilters(string? vendorName, string? address, string? vendorPhone, string? taxIdentifyNumber, string? directorName, string? directorPhone, string? picName, string? picPhone)
    {
        var filters = new List<FilterOption>();

        if (!string.IsNullOrWhiteSpace(vendorName))
        {
            filters.Add(new FilterOption { Field = "NVCHR_VENDOR_NAME", Operator = "LIKE", Value = $"%{vendorName.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(vendorPhone))
        {
            filters.Add(new FilterOption { Field = "CHR_VENDOR_PHONE", Operator = "LIKE", Value = $"%{vendorPhone.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            filters.Add(new FilterOption { Field = "NVCHR_ADDRESS", Operator = "LIKE", Value = $"%{address.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(taxIdentifyNumber))
        {
            filters.Add(new FilterOption { Field = "CHR_TAX_IDENTIFY_NUMBER", Operator = "LIKE", Value = $"%{taxIdentifyNumber.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(directorName))
        {
            filters.Add(new FilterOption { Field = "NVCHR_DIRECTOR_NAME", Operator = "LIKE", Value = $"%{directorName.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(directorPhone))
        {
            filters.Add(new FilterOption { Field = "CHR_DIRECTOR_PHONE", Operator = "LIKE", Value = $"%{directorPhone.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(picName))
        {
            filters.Add(new FilterOption { Field = "NVCHR_PIC_NAME", Operator = "LIKE", Value = $"%{picName.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(picPhone))
        {
            filters.Add(new FilterOption { Field = "CHR_PIC_PHONE", Operator = "LIKE", Value = $"%{picPhone.Trim()}%" });
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

