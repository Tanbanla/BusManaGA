using System.Security.Claims;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.QltcManager;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace BusManagement.Controllers;

public class QltcManagerController : Controller
{
    private static readonly string[] QltcPositionGroups = ["Section Manager", "Assistant Section Manager", "Deputy General Manager", "Dept Manager"];

    private readonly IEmployeeService _employeeService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly ITmCityService _tmCityService;
    private readonly ITmWardService _tmWardService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ISectionService _sectionService;

    public QltcManagerController(
        IEmployeeService employeeService,
        ITmEmployeeService tmEmployeeService,
        ITmCityService tmCityService,
        ITmWardService tmWardService,
        ITmRoleService tmRoleService,
        ISectionService sectionService)
    {
        _employeeService = employeeService;
        _tmEmployeeService = tmEmployeeService;
        _tmCityService = tmCityService;
        _tmWardService = tmWardService;
        _tmRoleService = tmRoleService;
        _sectionService = sectionService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? employeeCode, string? employeeName, string? department, string? position, string? phone, string? city, string? suggestedAddress, int page = 1, int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = BuildQltcFilters(employeeCode, employeeName, department, position, phone, city, suggestedAddress),
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var sectionsResp = await _sectionService.GetAllAsync();
        var cityResp = await _tmCityService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            SortOptions = new()
            {
                new SortOption { Field = "NVCHR_CITY_NAME", SortDirection = "ASC" }
            }
        });
        var vm = new QltcManagerPageViewModel
        {
            EmployeeCode = employeeCode,
            EmployeeName = employeeName,
            Department = department,
            Position = position,
            Phone = phone,
            City = city,
            SuggestedAddress = suggestedAddress,
            PageNumber = page,
            PageSize = pageSize,
            Employees = employeeResp?.Data?.Data?.ToList() ?? new(),
            Sections = (sectionsResp?.Data ?? new List<TM_SECTIONDTO>())
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
                .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(x => x.CHR_CODE)
                .ToList(),
            Cities = (cityResp?.Data?.Data ?? new List<TM_CITYDTO>())
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_CITY_NAME))
                .GroupBy(x => x.NVCHR_CITY_NAME!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList(),
            TotalCount = (int)(employeeResp?.Data?.TotalCount ?? 0),
            TotalPages = (int)(employeeResp?.Data?.TotalPages ?? 0),
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

    [HttpGet]
    public async Task<IActionResult> ResolveEmployeeByCode(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return Json(new { success = false, message = "Mã nhân viên không hợp lệ." });
        }

        var snapshot = await ResolveQltcSnapshotAsync(employeeCode.Trim());
        if (snapshot == null)
        {
            return Json(new { success = false, message = "Không tìm thấy thông tin QLTC hợp lệ." });
        }

        return Json(new
        {
            success = true,
            data = new
            {
                employeeName = snapshot.EmployeeName,
                departmentCode = snapshot.DepartmentCode,
                departmentName = snapshot.DepartmentName,
                position = snapshot.Position,
                phone = snapshot.Phone,
                city = snapshot.City,
                addressDetail = snapshot.AddressDetail,
                suggestedPickupFromBivn = snapshot.SuggestedPickupFromBivn,
                suggestedAddress = snapshot.SuggestedAddress
            }
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(QltcManagerUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["QltcManagerError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.EmployeeCode))
        {
            TempData["QltcManagerError"] = "Vui lòng nhập mã nhân viên.";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var ok = await AddOrUpdateQltcByCodeAsync(input.EmployeeCode.Trim(), actor, input.SuggestedAddress, input.SuggestedPickupFromBivn);
        TempData[ok ? "QltcManagerSuccess" : "QltcManagerError"] = ok
            ? "Thêm QLTC thành công."
            : "Không thể thêm QLTC. Vui lòng kiểm tra mã nhân viên và chức vụ QLTC.";

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(QltcManagerUpsertInputModel input)
    {
        if (!await CanManageAsync())
        {
            TempData["QltcManagerError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToIndex(input);
        }

        if (!input.ID.HasValue || input.ID.Value <= 0)
        {
            TempData["QltcManagerError"] = "Không xác định được QLTC cần cập nhật.";
            return RedirectToIndex(input);
        }

        var currentResp = await _tmEmployeeService.GetByIdAsync(input.ID.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["QltcManagerError"] = currentResp.Message ?? "Không tìm thấy dữ liệu.";
            return RedirectToIndex(input);
        }

        var entity = new TM_EMPLOYEE
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = GetCurrentActor(),
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = current.CHR_EMPLOYEE_ID,
            CHR_EMPLOYEE_ADID = current.CHR_EMPLOYEE_ADID,
            CHR_EMPLOYEE_MAIL = current.CHR_EMPLOYEE_MAIL,
            NVCHR_EMPLOYEE_NAME = current.NVCHR_EMPLOYEE_NAME,
            INT_BUS_ROUTE_ASSIGN_ID = current.INT_BUS_ROUTE_ASSIGN_ID,
            CHR_SHIFT_CODE = current.CHR_SHIFT_CODE,
            CHR_CODE_DEPT = current.CHR_CODE_DEPT,
            CHR_CODE_SEC = current.CHR_CODE_SEC,
            CHR_CODE_GROUP = current.CHR_CODE_GROUP,
            CHR_POSITION = string.IsNullOrWhiteSpace(input.Position) ? current.CHR_POSITION : input.Position.Trim(),
            CHR_POSITION_GROUP = NormalizeQltcPositionGroup(input.Position, current.CHR_POSITION_GROUP),
            CHR_PHONE = string.IsNullOrWhiteSpace(input.Phone) ? current.CHR_PHONE : input.Phone.Trim(),
            DTM_BIRTHDAY = current.DTM_BIRTHDAY,
            CHR_ADDRESS = current.CHR_ADDRESS,
            NVCHR_CITY = input.City?.Trim(),
            NVCHR_WARD = current.NVCHR_WARD,
            NVCHR_ADDRESS_DETAIL = input.AddressDetail?.Trim(),
            NVCHR_SUGGESTED_PICKUP_FROM_BIVN = input.SuggestedPickupFromBivn?.Trim(),
            NVCHR_SUGGESTED_ADDRESS = input.SuggestedAddress?.Trim(),
            BIT_GENDER = current.BIT_GENDER,
            BIT_IS_USE_BUS = current.BIT_IS_USE_BUS,
            BIT_IS_ACTIVE = current.BIT_IS_ACTIVE,
            CHR_NOTE = current.CHR_NOTE
        };

        var resp = await _tmEmployeeService.UpdateAsync(entity);
        TempData[resp.Success ? "QltcManagerSuccess" : "QltcManagerError"] = resp.Success
            ? "Cập nhật QLTC thành công."
            : (resp.Message ?? "Không thể cập nhật QLTC.");

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, string? employeeCode, string? employeeName, string? department, string? position, string? phone, string? city, string? suggestedAddress, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["QltcManagerError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, position, phone, city, suggestedAddress, page, pageSize });
        }

        var resp = await _tmEmployeeService.DeleteAsync(id);
        TempData[resp.Success ? "QltcManagerSuccess" : "QltcManagerError"] = resp.Success
            ? "Xóa QLTC thành công."
            : (resp.Message ?? "Không thể xóa QLTC.");

        return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, position, phone, city, suggestedAddress, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? employeeCode, string? employeeName, string? department, string? position, string? phone, string? city, string? suggestedAddress)
    {
        var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = BuildQltcFilters(employeeCode, employeeName, department, position, phone, city, suggestedAddress),
            SortOptions = new()
            {
                new SortOption { Field = "DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var rows = employeeResp?.Data?.Data?.ToList() ?? new List<TM_EMPLOYEEDTO>();

        var sectionsResp = await _sectionService.GetAllAsync();
        var sectionMap = (sectionsResp?.Data ?? new List<TM_SECTIONDTO>())
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("QLTCManagers");
        var headers = new[]
        {
            "STT", "Mã nhân viên", "Tên nhân viên", "Phòng ban", "Chức vụ", "Số điện thoại", "Tỉnh", "Điểm đón trả gợi ý từ BIVN", "Địa chỉ chi tiết", "Địa chỉ gợi ý"
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
            sectionMap.TryGetValue(item.CHR_CODE_SEC?.Trim() ?? string.Empty, out var section);

            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.CHR_EMPLOYEE_ID;
            sheet.Cell(row, 3).Value = item.NVCHR_EMPLOYEE_NAME;
            sheet.Cell(row, 4).Value = section?.CHR_NAME ?? section?.CHR_SECTION_NAME ?? item.CHR_CODE_SEC;
            sheet.Cell(row, 5).Value = item.CHR_POSITION;
            sheet.Cell(row, 6).Value = item.CHR_PHONE;
            sheet.Cell(row, 7).Value = item.NVCHR_CITY;
            sheet.Cell(row, 8).Value = item.NVCHR_SUGGESTED_PICKUP_FROM_BIVN;
            sheet.Cell(row, 9).Value = item.NVCHR_ADDRESS_DETAIL;
            sheet.Cell(row, 10).Value = item.NVCHR_SUGGESTED_ADDRESS;

            row++;
            stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DanhSachQLTC_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile? importFile, string? employeeCode, string? employeeName, string? department, string? position, string? phone, string? city, string? suggestedAddress, int page = 1, int pageSize = 10)
    {
        if (!await CanManageAsync())
        {
            TempData["QltcManagerError"] = "Bạn không có quyền thực hiện chức năng này.";
            return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, position, phone, city, suggestedAddress, page, pageSize });
        }

        if (importFile == null || importFile.Length == 0)
        {
            TempData["QltcManagerError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, position, phone, city, suggestedAddress, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;
        var errorRows = new List<ImportErrorRow>();
        var sourceHeaders = new List<string>();
        var detectedColumnCount = 0;

        async Task<(bool Success, string ErrorMessage)> TryImportCellsAsync(List<string> cells)
        {
            var code = cells.ElementAtOrDefault(1)?.Trim();

            if (string.IsNullOrWhiteSpace(code))
            {
                return (false, "Thiếu mã nhân viên.");
            }

            var suggestedPickup = cells.Count >= 10
                ? cells.ElementAtOrDefault(7)?.Trim()
                : cells.ElementAtOrDefault(1)?.Trim();
            var suggested = cells.Count >= 10
                ? cells.ElementAtOrDefault(9)?.Trim()
                : cells.ElementAtOrDefault(2)?.Trim();

            var ok = await AddOrUpdateQltcByCodeAsync(code, actor, suggested, suggestedPickup);
            return ok
                ? (true, string.Empty)
                : (false, "Không thể đồng bộ dữ liệu nhân viên từ BIVN/Agent hoặc nhân viên không thuộc nhóm QLTC hợp lệ.");
        }

        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet == null)
            {
                TempData["QltcManagerError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, position, phone, city, suggestedAddress, page, pageSize });
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            var lastColumn = sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            detectedColumnCount = Math.Max(3, lastColumn);

            for (var c = 1; c <= detectedColumnCount; c++)
            {
                var header = sheet.Cell(1, c).GetString()?.Trim();
                sourceHeaders.Add(string.IsNullOrWhiteSpace(header) ? $"Column_{c}" : header);
            }

            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>();
                for (var c = 1; c <= detectedColumnCount; c++)
                {
                    cells.Add(sheet.Cell(r, c).GetString());
                }

                var result = await TryImportCellsAsync(cells);
                if (result.Success)
                {
                    success++;
                }
                else
                {
                    failed++;
                    errorRows.Add(new ImportErrorRow
                    {
                        SourceRow = r,
                        Cells = cells,
                        ErrorMessage = result.ErrorMessage
                    });
                }
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

                var cells = ParseCsvLine(line);
                if (row == 1)
                {
                    if (line.Contains("EmployeeCode", StringComparison.OrdinalIgnoreCase)
                        || line.Contains("STT", StringComparison.OrdinalIgnoreCase))
                    {
                        sourceHeaders = cells.Select((h, i) => string.IsNullOrWhiteSpace(h) ? $"Column_{i + 1}" : h.Trim()).ToList();
                        detectedColumnCount = Math.Max(3, sourceHeaders.Count);
                        continue;
                    }

                    detectedColumnCount = Math.Max(3, cells.Count);
                    sourceHeaders = Enumerable.Range(1, detectedColumnCount).Select(i => $"Column_{i}").ToList();
                }

                while (cells.Count < detectedColumnCount)
                {
                    cells.Add(string.Empty);
                }

                var result = await TryImportCellsAsync(cells);
                if (result.Success)
                {
                    success++;
                }
                else
                {
                    failed++;
                    errorRows.Add(new ImportErrorRow
                    {
                        SourceRow = row,
                        Cells = cells,
                        ErrorMessage = result.ErrorMessage
                    });
                }
            }
        }

        if (failed > 0)
        {
            if (!sourceHeaders.Any())
            {
                sourceHeaders = Enumerable.Range(1, Math.Max(3, detectedColumnCount)).Select(i => $"Column_{i}").ToList();
            }

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("ImportErrors");

            sheet.Cell(1, 1).Value = "Dòng lỗi";
            for (var i = 0; i < sourceHeaders.Count; i++)
            {
                sheet.Cell(1, i + 2).Value = sourceHeaders[i];
            }
            sheet.Cell(1, sourceHeaders.Count + 2).Value = "Lỗi";

            var rowIndex = 2;
            foreach (var item in errorRows)
            {
                sheet.Cell(rowIndex, 1).Value = item.SourceRow;
                for (var i = 0; i < sourceHeaders.Count; i++)
                {
                    sheet.Cell(rowIndex, i + 2).Value = item.Cells.ElementAtOrDefault(i) ?? string.Empty;
                }
                sheet.Cell(rowIndex, sourceHeaders.Count + 2).Value = item.ErrorMessage;
                rowIndex++;
            }

            var headerRange = sheet.Range(1, 1, 1, sourceHeaders.Count + 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#f8d7da");
            sheet.Columns().AdjustToContents();

            using var outStream = new MemoryStream();
            workbook.SaveAs(outStream);

            var errorFileName = $"QltcImportErrors_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(outStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", errorFileName);
        }

        TempData["QltcManagerSuccess"] = $"Import thành công {success} QLTC.";
        return RedirectToAction(nameof(Index), new { employeeCode, employeeName, department, position, phone, city, suggestedAddress, page, pageSize });
    }

    private class ImportErrorRow
    {
        public int SourceRow { get; set; }
        public List<string> Cells { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    private IActionResult RedirectToIndex(QltcManagerUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            employeeCode = input.SearchEmployeeCode,
            employeeName = input.SearchEmployeeName,
            department = input.SearchDepartment,
            position = input.SearchPosition,
            phone = input.SearchPhone,
            city = input.SearchCity,
            suggestedAddress = input.SearchSuggestedAddress,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private static List<FilterOption> BuildQltcFilters(string? employeeCode, string? employeeName, string? department, string? position, string? phone, string? city, string? suggestedAddress)
    {
        var filters = new List<FilterOption>
        {
            new() { Field = "CHR_POSITION_GROUP", Operator = "LIKE", Value = "%Section Manager%" }
        };

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            filters.Add(new FilterOption { Field = "CHR_EMPLOYEE_ID", Operator = "LIKE", Value = $"%{employeeCode.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            filters.Add(new FilterOption { Field = "NVCHR_EMPLOYEE_NAME", Operator = "LIKE", Value = $"%{employeeName.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            filters.Add(new FilterOption { Field = "CHR_CODE_SEC", Operator = "=", Value = department.Trim() });
        }

        if (!string.IsNullOrWhiteSpace(position))
        {
            filters.Add(new FilterOption { Field = "CHR_POSITION", Operator = "LIKE", Value = $"%{position.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            filters.Add(new FilterOption { Field = "CHR_PHONE", Operator = "LIKE", Value = $"%{phone.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            filters.Add(new FilterOption { Field = "NVCHR_CITY", Operator = "LIKE", Value = $"%{city.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(suggestedAddress))
        {
            filters.Add(new FilterOption { Field = "NVCHR_SUGGESTED_ADDRESS", Operator = "LIKE", Value = $"%{suggestedAddress.Trim()}%" });
        }

        return filters;
    }

    private async Task<QltcSnapshot?> ResolveQltcSnapshotAsync(string employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return null;
        }
        var local = await _employeeService.GetByEmployeeCodeAsync(employeeCode);
        var agent = await _employeeService.GetAgentEmployeeByEmployeeIdAsync(employeeCode);

        var employeeName = FirstNotWhiteSpace(local?.NVCHR_EMPLOYEE_NAME, agent?.CHR_EMPLOYEE_NAME);
        var employeeAdid = FirstNotWhiteSpace(local?.CHR_EMPLOYEE_ADID, agent?.CHR_EMPLOYEE_ADID);
        var employeeMail = FirstNotWhiteSpace(local?.CHR_EMPLOYEE_MAIL, agent?.CHR_EMPLOYEE_MAIL);
        var agentSectionCode = FirstNotWhiteSpace(agent?.CHR_SEC_CODE,
            string.IsNullOrWhiteSpace(agent?.CHR_SECTION)
                ? null
                : agent.CHR_SECTION.Split(':', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim());
        var departmentCode = FirstNotWhiteSpace(local?.CHR_CODE_SEC, agentSectionCode);
        var departmentMainCode = FirstNotWhiteSpace(local?.CHR_CODE_DEPT, agent?.CHR_DEPT_CODE,
            string.IsNullOrWhiteSpace(agent?.CHR_DEPT)
                ? null
                : agent.CHR_DEPT.Split(':', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim());
        var groupCode = FirstNotWhiteSpace(local?.CHR_CODE_GROUP, agent?.CHR_DEPT_GROUP);
        var position = FirstNotWhiteSpace(local?.CHR_POSITION, agent?.CHR_POSITION_NAME);
        var positionGroup = FirstNotWhiteSpace(local?.CHR_POSITION_GROUP, agent?.CHR_POSITION_GROUP);
        var phone = FirstNotWhiteSpace(local?.CHR_PHONE, agent?.CHR_PHONE_NO, agent?.CHR_PERMANENT_TEL_NO);
        var city = FirstNotWhiteSpace(local?.NVCHR_CITY, agent?.CHR_PROVINCE);
        var suggestedPickupFromBivn = local?.NVCHR_SUGGESTED_PICKUP_FROM_BIVN;
        var addressDetail = FirstNotWhiteSpace(local?.NVCHR_ADDRESS_DETAIL, agent?.CHR_TEMP_ADDRESS, agent?.CHR_PERMANENT_ADDRESS);
        var suggestedAddress = local?.NVCHR_SUGGESTED_ADDRESS;
        var birthday = FirstNotNull(local?.DTM_BIRTHDAY, agent?.DTM_BIRTH_DATE);
        var address = FirstNotWhiteSpace(local?.CHR_ADDRESS, agent?.CHR_PERMANENT_ADDRESS, agent?.CHR_TEMP_ADDRESS);

        var sectionsResp = await _sectionService.GetAllAsync();
        var sections = sectionsResp?.Data ?? new List<TM_SECTIONDTO>();

        if (!IsQltcPosition(positionGroup, position))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            var normalized = departmentCode.Trim();

            var codeMatched = sections.Any(x =>
                !string.IsNullOrWhiteSpace(x.CHR_CODE)
                && string.Equals(x.CHR_CODE.Trim(), normalized, StringComparison.OrdinalIgnoreCase));

            if (!codeMatched)
            {
                var matchedByName = sections.FirstOrDefault(x =>
                    (!string.IsNullOrWhiteSpace(x.CHR_NAME) && string.Equals(x.CHR_NAME.Trim(), normalized, StringComparison.OrdinalIgnoreCase))
                    || (!string.IsNullOrWhiteSpace(x.CHR_SECTION_NAME) && string.Equals(x.CHR_SECTION_NAME.Trim(), normalized, StringComparison.OrdinalIgnoreCase)));

                if (!string.IsNullOrWhiteSpace(matchedByName?.CHR_CODE))
                {
                    departmentCode = matchedByName.CHR_CODE.Trim();
                }
            }
        }

        if (string.IsNullOrWhiteSpace(employeeName))
        {
            return null;
        }

        return new QltcSnapshot
        {
            EmployeeCode = employeeCode,
            EmployeeName = employeeName,
            EmployeeAdid = employeeAdid ?? string.Empty,
            EmployeeMail = employeeMail ?? string.Empty,
            DepartmentCode = departmentCode ?? string.Empty,
            DepartmentMainCode = departmentMainCode ?? string.Empty,
            DepartmentName = ResolveDepartmentName(departmentCode, sections),
            GroupCode = groupCode ?? string.Empty,
            Position = position ?? positionGroup ?? string.Empty,
            PositionGroup = NormalizeQltcPositionGroup(positionGroup, position),
            Phone = phone ?? string.Empty,
            Birthday = birthday,
            Address = address ?? string.Empty,
            City = city ?? string.Empty,
            SuggestedPickupFromBivn = suggestedPickupFromBivn ?? string.Empty,
            AddressDetail = addressDetail ?? string.Empty,
            SuggestedAddress = suggestedAddress ?? string.Empty
        };
    }

    private async Task<bool> AddOrUpdateQltcByCodeAsync(string employeeCode, string actor, string? suggestedAddress, string? suggestedPickupFromBivn = null)
    {
        var snapshot = await ResolveQltcSnapshotAsync(employeeCode);
        if (snapshot == null)
        {
            return false;
        }
        var local = await _employeeService.GetByEmployeeCodeAsync(employeeCode);

        if (local == null)
        {
            var addResp = await _tmEmployeeService.AddAsyncV2(new TM_EMPLOYEE
            {
                CHR_EMPLOYEE_ID = snapshot.EmployeeCode,
                CHR_EMPLOYEE_ADID = snapshot.EmployeeAdid,
                CHR_EMPLOYEE_MAIL = snapshot.EmployeeMail,
                NVCHR_EMPLOYEE_NAME = snapshot.EmployeeName,
                CHR_CODE_DEPT = snapshot.DepartmentMainCode,
                CHR_CODE_SEC = snapshot.DepartmentCode,
                CHR_CODE_GROUP = snapshot.GroupCode,
                CHR_POSITION = snapshot.Position,
                CHR_POSITION_GROUP = snapshot.PositionGroup,
                CHR_PHONE = snapshot.Phone,
                DTM_BIRTHDAY = snapshot.Birthday,
                CHR_ADDRESS = snapshot.Address,
                NVCHR_CITY = snapshot.City,
                NVCHR_SUGGESTED_PICKUP_FROM_BIVN = string.IsNullOrWhiteSpace(suggestedPickupFromBivn) ? snapshot.SuggestedPickupFromBivn : suggestedPickupFromBivn.Trim(),
                NVCHR_ADDRESS_DETAIL = snapshot.AddressDetail,
                NVCHR_SUGGESTED_ADDRESS = string.IsNullOrWhiteSpace(suggestedAddress) ? snapshot.SuggestedAddress : suggestedAddress.Trim(),
                BIT_IS_ACTIVE = true,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            });

            return addResp.Success;
        }

        var updateResp = await _tmEmployeeService.UpdateAsync(new TM_EMPLOYEE
        {
            ID = local.ID,
            CHR_USER_CREATE = local.CHR_USER_CREATE,
            DTM_CREATE = local.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = snapshot.EmployeeCode,
            CHR_EMPLOYEE_ADID = snapshot.EmployeeAdid,
            CHR_EMPLOYEE_MAIL = snapshot.EmployeeMail,
            NVCHR_EMPLOYEE_NAME = snapshot.EmployeeName,
            INT_BUS_ROUTE_ASSIGN_ID = local.INT_BUS_ROUTE_ASSIGN_ID,
            CHR_SHIFT_CODE = local.CHR_SHIFT_CODE,
            CHR_CODE_DEPT = snapshot.DepartmentMainCode,
            CHR_CODE_SEC = snapshot.DepartmentCode,
            CHR_CODE_GROUP = snapshot.GroupCode,
            CHR_POSITION = snapshot.Position,
            CHR_POSITION_GROUP = snapshot.PositionGroup,
            CHR_PHONE = snapshot.Phone,
            DTM_BIRTHDAY = snapshot.Birthday,
            CHR_ADDRESS = snapshot.Address,
            NVCHR_CITY = snapshot.City,
            NVCHR_WARD = local.NVCHR_WARD,
            NVCHR_SUGGESTED_PICKUP_FROM_BIVN = string.IsNullOrWhiteSpace(suggestedPickupFromBivn) ? snapshot.SuggestedPickupFromBivn : suggestedPickupFromBivn.Trim(),
            NVCHR_ADDRESS_DETAIL = snapshot.AddressDetail,
            NVCHR_SUGGESTED_ADDRESS = string.IsNullOrWhiteSpace(suggestedAddress) ? snapshot.SuggestedAddress : suggestedAddress.Trim(),
            BIT_GENDER = local.BIT_GENDER,
            BIT_IS_USE_BUS = local.BIT_IS_USE_BUS,
            BIT_IS_ACTIVE = true,
            CHR_NOTE = local.CHR_NOTE
        });

        return updateResp.Success;
    }

    private static bool IsQltcPosition(string? positionGroup, string? position)
    {
        var values = new[] { positionGroup, position }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim());

        return values.Any(v => QltcPositionGroups.Any(x => string.Equals(v, x, StringComparison.OrdinalIgnoreCase)));
    }

    private static string NormalizeQltcPositionGroup(string? positionGroup, string? position)
    {
        if (string.Equals(positionGroup?.Trim(), QltcPositionGroups[1], StringComparison.OrdinalIgnoreCase)
            || string.Equals(position?.Trim(), QltcPositionGroups[1], StringComparison.OrdinalIgnoreCase))
        {
            return QltcPositionGroups[1];
        }

        return QltcPositionGroups[0];
    }

    private static string? FirstNotWhiteSpace(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private static DateTime? FirstNotNull(params DateTime?[] values)
    {
        foreach (var value in values)
        {
            if (value.HasValue)
            {
                return value.Value;
            }
        }

        return null;
    }

    private static string ResolveDepartmentName(string? departmentCode, IEnumerable<TM_SECTIONDTO> sections)
    {
        if (string.IsNullOrWhiteSpace(departmentCode))
        {
            return string.Empty;
        }

        var section = sections.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(x.CHR_CODE)
            && string.Equals(x.CHR_CODE.Trim(), departmentCode.Trim(), StringComparison.OrdinalIgnoreCase));

        return section?.CHR_NAME
            ?? section?.CHR_SECTION_NAME
            ?? departmentCode;
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

    private class QltcSnapshot
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string EmployeeAdid { get; set; } = string.Empty;
        public string EmployeeMail { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string DepartmentMainCode { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string PositionGroup { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? Birthday { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string SuggestedPickupFromBivn { get; set; } = string.Empty;
        public string AddressDetail { get; set; } = string.Empty;
        public string SuggestedAddress { get; set; } = string.Empty;
    }
}
