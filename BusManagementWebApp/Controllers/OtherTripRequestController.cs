using System.Globalization;
using System.Security.Claims;
using BusManagement.Model.Common;
using BusManagement.Model.Common.Enum;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.TripRequest;
using BusManagementWebApp.Helpers;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace BusManagement.Controllers;

public class OtherTripRequestController : Controller
{
    private const int OtherTripRequestTypeGroup = 3;

    private readonly ITmTripRequestService _tmTripRequestService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly IEmployeeService _employeeService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ISectionService _sectionService;
    private readonly ITmApprovalHistoryService _tmApprovalHistoryService;
    private readonly ITmMasterMailService _tmMasterMailService;
    private readonly ITmRequestTypeService _tmRequestTypeService;

    public OtherTripRequestController(
        ITmTripRequestService tmTripRequestService,
        ITmEmployeeService tmEmployeeService,
        IEmployeeService employeeService,
        ITmRoleService tmRoleService,
        ISectionService sectionService,
        ITmApprovalHistoryService tmApprovalHistoryService,
        ITmMasterMailService tmMasterMailService,
        ITmRequestTypeService tmRequestTypeService)
    {
        _tmTripRequestService = tmTripRequestService;
        _tmEmployeeService = tmEmployeeService;
        _employeeService = employeeService;
        _tmRoleService = tmRoleService;
        _sectionService = sectionService;
        _tmApprovalHistoryService = tmApprovalHistoryService;
        _tmMasterMailService = tmMasterMailService;
        _tmRequestTypeService = tmRequestTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? department,
        string? employeeCode,
        string? employeeName,
        string? phone,
        int? status,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page = 1,
        int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var canManageAll = await CanManageAllAsync();
        var isAdmin = await IsAdminAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();

        var filters = new List<FilterOption>
        {
            new() { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "!=", Value = TripRequestStatuses.Deleted },
            new() { Field = nameof(TM_TRIP_REQUEST.CHR_TRIP_TYPE), Operator = "=", Value = OtherTripRequestConstants.TripTypeCode }
        };

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.CHR_EMPLOYEE_ID), Operator = "LIKE", Value = $"%{employeeCode.Trim()}%" });
        }

        if (status.HasValue)
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "=", Value = status.Value });
        }

        if (!canManageAll && !string.IsNullOrWhiteSpace(currentEmployeeCode))
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.CHR_EMPLOYEE_ID), Operator = "=", Value = currentEmployeeCode.Trim() });
        }

        if (dateFrom.HasValue)
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.DTM_PICKUP_GO), Operator = ">=", Value = dateFrom.Value.Date });
        }

        if (dateTo.HasValue)
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.DTM_PICKUP_GO), Operator = "<=", Value = dateTo.Value.Date.AddDays(1).AddTicks(-1) });
        }

        var tripResp = await _tmTripRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), SortDirection = "ASC" },
                new SortOption { Field = nameof(TM_TRIP_REQUEST.DTM_CREATE), SortDirection = "DESC" }
            }
        });

        var requests = tripResp?.Data?.Data?.ToList() ?? new List<TM_TRIP_REQUESTDTO>();
        var requestTypeItems = await LoadRequestTypesByGroupAsync(OtherTripRequestTypeGroup);
        var allowedOtherTypes = requestTypeItems
            .Select(x => x.NVCHR_REQUEST_TYPE?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (allowedOtherTypes.Any())
        {
            requests = requests
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_TRIP_TYPE)
                    && allowedOtherTypes.Contains(x.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        var employeeCodes = requests
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
            .Select(x => x.CHR_EMPLOYEE_ID!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var employees = await LoadEmployeesByCodesAsync(employeeCodes);
        var employeeMap = employees
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
            .GroupBy(x => x.CHR_EMPLOYEE_ID!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var sectionResp = await _sectionService.GetAllAsync();
        var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
        var sectionMap = sections
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(employeeName) || !string.IsNullOrWhiteSpace(phone) || !string.IsNullOrWhiteSpace(department))
        {
            requests = requests.Where(x =>
            {
                employeeMap.TryGetValue(x.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var emp);
                var matchedName = string.IsNullOrWhiteSpace(employeeName)
                                  || (!string.IsNullOrWhiteSpace(emp?.NVCHR_EMPLOYEE_NAME) && emp.NVCHR_EMPLOYEE_NAME.Contains(employeeName.Trim(), StringComparison.OrdinalIgnoreCase));
                var matchedPhone = string.IsNullOrWhiteSpace(phone)
                                   || (!string.IsNullOrWhiteSpace(emp?.CHR_PHONE) && emp.CHR_PHONE.Contains(phone.Trim(), StringComparison.OrdinalIgnoreCase));
                var matchedDept = string.IsNullOrWhiteSpace(department)
                                  || string.Equals(emp?.CHR_CODE_SEC?.Trim(), department.Trim(), StringComparison.OrdinalIgnoreCase);
                return matchedName && matchedPhone && matchedDept;
            }).ToList();
        }

        var rows = requests.Select(x =>
        {
            employeeMap.TryGetValue(x.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var emp);
            var deptCode = emp?.CHR_CODE_SEC?.Trim();
            sectionMap.TryGetValue(deptCode ?? string.Empty, out var sec);
            return new TripRequestRowViewModel
            {
                Id = x.ID,
                DepartmentCode = deptCode,
                DepartmentName = sec?.CHR_NAME ?? sec?.CHR_SECTION_NAME ?? deptCode,
                EmployeeCode = x.CHR_EMPLOYEE_ID,
                EmployeeName = emp?.NVCHR_EMPLOYEE_NAME,
                Phone = emp?.CHR_PHONE,
                TripType = x.NVCHR_TRIP_TYPE,
                PickupGo = x.DTM_PICKUP_GO,
                FromGo = x.NVCHR_FROM_LOCATION_GO,
                ToGo = x.NVCHR_TO_LOCATION_GO,
                PickupReturn = x.DTM_PICKUP_RETURN,
                FromReturn = x.NVCHR_FROM_LOCATION_RETURN,
                ToReturn = x.NVCHR_TO_LOCATION_RETURN,
                DriverInfo = x.NVCHR_DRIVER_INFO,
                Status = x.INT_STATUS,
                StatusText = ResolveStatusText(x.INT_STATUS),
                StatusRowClass = TripRequestStatusUiHelper.GetRowClass(x.INT_STATUS),
                StatusBadgeClass = TripRequestStatusUiHelper.GetBadgeClass(x.INT_STATUS),
                CanEdit = isAdmin || (string.Equals(x.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase)
                         && x.INT_STATUS == TripRequestStatuses.PendingQlsc),
                CanDelete = isAdmin || (string.Equals(x.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase)
                         && x.INT_STATUS == TripRequestStatuses.PendingQlsc)
            };
        }).ToList();

        TM_EMPLOYEEDTO? currentEmployee = null;
        if (!string.IsNullOrWhiteSpace(currentEmployeeCode))
        {
            employeeMap.TryGetValue(currentEmployeeCode.Trim(), out currentEmployee);
            if (currentEmployee == null)
            {
                var currentResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
                {
                    PageNumber = 1,
                    PageSize = 1,
                    Filters = new List<FilterOption> { new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = currentEmployeeCode.Trim() } }
                });
                currentEmployee = currentResp?.Data?.Data?.FirstOrDefault();
            }
        }

        var vm = new TripRequestPageViewModel
        {
            Department = department,
            EmployeeCode = employeeCode,
            EmployeeName = employeeName,
            Phone = phone,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = (int)(tripResp?.Data?.TotalCount ?? rows.Count),
            TotalPages = (int)(tripResp?.Data?.TotalPages ?? 1),
            CanManageAll = canManageAll,
            CurrentEmployeeCode = currentEmployeeCode,
            CurrentEmployeeName = currentEmployee?.NVCHR_EMPLOYEE_NAME,
            CurrentDepartmentCode = currentEmployee?.CHR_CODE_SEC,
            Requests = rows,
            RequestTypeNames = allowedOtherTypes,
            Sections = sections.ToList()
        };

        if (vm.TotalPages <= 0)
        {
            vm.TotalPages = vm.TotalCount > 0 ? (int)Math.Ceiling(vm.TotalCount / (double)pageSize) : 1;
        }

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TripRequestUpsertInputModel input)
    {
        var canManageAll = await CanManageAllAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        //if (!canManageAll && !string.Equals(input.EmployeeCode?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
        //{
        //    TempData["OtherTripRequestError"] = "Bạn chỉ có thể tạo đơn cho chính mình.";
        //    return RedirectToIndex(input);
        //}

        // Chỉ được đăng ký đơn trước 15h hàng ngày
        if (DateTime.Now.TimeOfDay > new TimeSpan(15, 0, 0))
        {
            TempData["OtherTripRequestError"] = "Quá 15:00, không thể đăng ký xe khác trong ngày.";
            return RedirectToIndex(input);
        }

        var allowedOtherTypes = await LoadRequestTypeNamesByGroupAsync(OtherTripRequestTypeGroup);
        var requestType = NormalizeOtherRequestType(input.TripType, allowedOtherTypes);

        if (string.IsNullOrWhiteSpace(input.EmployeeCode)
            || string.IsNullOrWhiteSpace(requestType)
            || !input.PickupGoDate.HasValue
            || string.IsNullOrWhiteSpace(input.PickupGoTime)
            || string.IsNullOrWhiteSpace(input.FromGo)
            || string.IsNullOrWhiteSpace(input.ToGo))
        {
            TempData["OtherTripRequestError"] = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
            return RedirectToIndex(input);
        }

        await EnsureEmployeeAsync(input);

        var goPickup = MergeDateTime(input.PickupGoDate, input.PickupGoTime);
        var returnPickup = MergeDateTime(input.PickupReturnDate, input.PickupReturnTime);

        var actor = GetCurrentActor();
        var entity = new TM_TRIP_REQUEST
        {
            CHR_EMPLOYEE_ID = input.EmployeeCode?.Trim(),
            CHR_TRIP_TYPE = OtherTripRequestConstants.TripTypeCode,
            NVCHR_TRIP_TYPE = requestType,
            DTM_PICKUP_GO = goPickup,
            DTM_PICKUP_RETURN = returnPickup,
            NVCHR_FROM_LOCATION_GO = input.FromGo?.Trim(),
            NVCHR_TO_LOCATION_GO = input.ToGo?.Trim(),
            NVCHR_FROM_LOCATION_RETURN = input.FromReturn?.Trim(),
            NVCHR_TO_LOCATION_RETURN = input.ToReturn?.Trim(),
            INT_STATUS = TripRequestStatuses.PendingQlsc,
            CHR_STATUS = ResolveStatusText(TripRequestStatuses.PendingQlsc),
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmTripRequestService.AddAsyncV2(entity);
        if (!resp.Success)
        {
            TempData["OtherTripRequestError"] = resp.Message ?? "Không thể tạo đơn.";
            return RedirectToIndex(input);
        }

        var requestId = resp.Data?.ID ?? entity.ID;
        if (requestId > 0)
        {
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_TRIP_REQEST_ID = requestId,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = TripRequestStatuses.PendingQlsc,
                BIT_ACTION = true,
                CHR_ACTION = "CREATE_OTHER_TRIP_REQUEST",
                DTM_ACTION = DateTime.Now
            });
        }

        TempData["OtherTripRequestSuccess"] = "Tạo đơn thành công.";
        TempData["OtherTripRequestCelebrate"] = "1";
        await SendCreateMailToGaAsync(entity);
        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(TripRequestUpsertInputModel input)
    {
        if (!input.Id.HasValue || input.Id.Value <= 0)
        {
            TempData["OtherTripRequestError"] = "Không xác định được đơn cần cập nhật.";
            return RedirectToIndex(input);
        }

        var currentResp = await _tmTripRequestService.GetByIdAsync(input.Id.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null || !string.Equals(current.CHR_TRIP_TYPE, OtherTripRequestConstants.TripTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["OtherTripRequestError"] = currentResp.Message ?? "Không tìm thấy đơn.";
            return RedirectToIndex(input);
        }

        var isAdmin = await IsAdminAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        if (!isAdmin && !string.Equals(current.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["OtherTripRequestError"] = "Bạn chỉ có thể sửa đơn của chính mình.";
            return RedirectToIndex(input);
        }

        if (!isAdmin && current.INT_STATUS != TripRequestStatuses.PendingQlsc)
        {
            TempData["OtherTripRequestError"] = "Đơn đã được xử lý, không thể sửa.";
            return RedirectToIndex(input);
        }

        await EnsureEmployeeAsync(input);

        var allowedOtherTypes = await LoadRequestTypeNamesByGroupAsync(OtherTripRequestTypeGroup);
        var requestType = NormalizeOtherRequestType(input.TripType, allowedOtherTypes)
            ?? current.NVCHR_TRIP_TYPE
            ?? allowedOtherTypes.FirstOrDefault()
            ?? OtherTripRequestConstants.TripTypeName;

        var actor = GetCurrentActor();
        var entity = new TM_TRIP_REQUEST
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = input.EmployeeCode?.Trim(),
            CHR_TRIP_TYPE = OtherTripRequestConstants.TripTypeCode,
            NVCHR_TRIP_TYPE = requestType,
            DTM_PICKUP_GO = MergeDateTime(input.PickupGoDate, input.PickupGoTime),
            DTM_PICKUP_RETURN = MergeDateTime(input.PickupReturnDate, input.PickupReturnTime),
            NVCHR_FROM_LOCATION_GO = input.FromGo?.Trim(),
            NVCHR_TO_LOCATION_GO = input.ToGo?.Trim(),
            NVCHR_FROM_LOCATION_RETURN = input.FromReturn?.Trim(),
            NVCHR_TO_LOCATION_RETURN = input.ToReturn?.Trim(),
            NVCHR_DRIVER_INFO = current.NVCHR_DRIVER_INFO,
            INT_STATUS = current.INT_STATUS,
            CHR_STATUS = ResolveStatusText(current.INT_STATUS)
        };

        var resp = await _tmTripRequestService.UpdateAsync(entity);
        TempData[resp.Success ? "OtherTripRequestSuccess" : "OtherTripRequestError"] = resp.Success
            ? "Cập nhật đơn thành công."
            : (resp.Message ?? "Không thể cập nhật đơn.");
        if (resp.Success)
        {
            TempData["OtherTripRequestCelebrate"] = "1";
        }

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, TripRequestUpsertInputModel input)
    {
        var currentResp = await _tmTripRequestService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null || !string.Equals(current.CHR_TRIP_TYPE, OtherTripRequestConstants.TripTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["OtherTripRequestError"] = currentResp.Message ?? "Không tìm thấy đơn.";
            return RedirectToIndex(input);
        }

        var isAdmin = await IsAdminAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        if (!isAdmin && !string.Equals(current.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["OtherTripRequestError"] = "Bạn không có quyền xóa đơn này.";
            return RedirectToIndex(input);
        }

        if (!isAdmin && current.INT_STATUS != TripRequestStatuses.PendingQlsc)
        {
            TempData["OtherTripRequestError"] = "Đơn đã được xử lý, không thể xóa.";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_TRIP_REQUEST
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = current.CHR_EMPLOYEE_ID,
            CHR_TRIP_TYPE = current.CHR_TRIP_TYPE,
            NVCHR_TRIP_TYPE = current.NVCHR_TRIP_TYPE,
            DTM_PICKUP_GO = current.DTM_PICKUP_GO,
            DTM_PICKUP_RETURN = current.DTM_PICKUP_RETURN,
            NVCHR_FROM_LOCATION_GO = current.NVCHR_FROM_LOCATION_GO,
            NVCHR_TO_LOCATION_GO = current.NVCHR_TO_LOCATION_GO,
            NVCHR_FROM_LOCATION_RETURN = current.NVCHR_FROM_LOCATION_RETURN,
            NVCHR_TO_LOCATION_RETURN = current.NVCHR_TO_LOCATION_RETURN,
            NVCHR_DRIVER_INFO = current.NVCHR_DRIVER_INFO,
            INT_STATUS = TripRequestStatuses.Deleted,
            CHR_STATUS = "DELETED"
        };

        var resp = await _tmTripRequestService.UpdateAsync(entity);
        TempData[resp.Success ? "OtherTripRequestSuccess" : "OtherTripRequestError"] = resp.Success
            ? "Xóa đơn thành công."
            : (resp.Message ?? "Không thể xóa đơn.");

        return RedirectToIndex(input);
    }

    [HttpGet]
    public async Task<IActionResult> ResolveEmployeeByCode(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return Json(new { success = false, message = "Mã nhân viên không hợp lệ." });
        }

        var code = employeeCode.Trim();
        var localResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = code }
            }
        });

        var local = localResp?.Data?.Data?.FirstOrDefault();
        var employeeName = local?.NVCHR_EMPLOYEE_NAME;
        var departmentCode = local?.CHR_CODE_SEC;
        var phone = local?.CHR_PHONE;
        var suggestedPickupFromBivn = local?.NVCHR_SUGGESTED_PICKUP_FROM_BIVN;
        var suggestedAddress = local?.NVCHR_SUGGESTED_ADDRESS;

        if (string.IsNullOrWhiteSpace(employeeName) || string.IsNullOrWhiteSpace(departmentCode) || string.IsNullOrWhiteSpace(phone))
        {
            var agent = await _employeeService.GetAgentEmployeeByEmployeeIdAsync(code);
            if (agent != null)
            {
                employeeName ??= agent.CHR_EMPLOYEE_NAME;
                departmentCode ??= agent.CHR_SEC_CODE ?? agent.CHR_SECTION;
                phone ??= agent.CHR_PHONE_NO;
            }
        }

        var departmentName = await ResolveSectionNameAsync(departmentCode);
        return Json(new
        {
            success = true,
            data = new
            {
                employeeName = employeeName ?? string.Empty,
                departmentCode = departmentCode ?? string.Empty,
                departmentName = departmentName ?? string.Empty,
                phone = phone ?? string.Empty,
                suggestedPickupFromBivn = suggestedPickupFromBivn ?? string.Empty,
                suggestedAddress = suggestedAddress ?? string.Empty
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? department, string? employeeCode, string? employeeName, string? phone, int? status, DateTime? dateFrom, DateTime? dateTo)
    {
        var result = await Index(department, employeeCode, employeeName, phone, status, dateFrom, dateTo, 1, 100000);
        if (result is not ViewResult vr || vr.Model is not TripRequestPageViewModel vm)
        {
            return RedirectToAction(nameof(Index));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("OtherTripRequests");
        sheet.Cell(1, 1).Value = "STT";
        sheet.Cell(1, 2).Value = "MNV";
        sheet.Cell(1, 3).Value = "Phòng ban";
        sheet.Cell(1, 4).Value = "Họ tên";
        sheet.Cell(1, 5).Value = "SĐT";
        sheet.Cell(1, 6).Value = "Trạng thái";
        sheet.Cell(1, 7).Value = "Ngày đón đi";
        sheet.Cell(1, 8).Value = "Giờ đón đi";
        sheet.Cell(1, 9).Value = "Đón từ điểm đi";
        sheet.Cell(1, 10).Value = "Tới điểm đi";
        sheet.Cell(1, 11).Value = "Ngày đón về";
        sheet.Cell(1, 12).Value = "Giờ đón về";
        sheet.Cell(1, 13).Value = "Đón từ điểm về";
        sheet.Cell(1, 14).Value = "Tới điểm về";
        sheet.Cell(1, 15).Value = "Thông tin lái xe";

        var row = 2;
        var stt = 1;
        foreach (var item in vm.Requests)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.EmployeeCode;
            sheet.Cell(row, 3).Value = item.DepartmentName;
            sheet.Cell(row, 4).Value = item.EmployeeName;
            sheet.Cell(row, 5).Value = item.Phone;
            sheet.Cell(row, 6).Value = item.StatusText;
            sheet.Cell(row, 7).Value = item.PickupGo?.ToString("dd/MM/yyyy");
            sheet.Cell(row, 8).Value = item.PickupGo?.ToString("HH:mm");
            sheet.Cell(row, 9).Value = item.FromGo;
            sheet.Cell(row, 10).Value = item.ToGo;
            sheet.Cell(row, 11).Value = item.PickupReturn?.ToString("dd/MM/yyyy");
            sheet.Cell(row, 12).Value = item.PickupReturn?.ToString("HH:mm");
            sheet.Cell(row, 13).Value = item.FromReturn;
            sheet.Cell(row, 14).Value = item.ToReturn;
            sheet.Cell(row, 15).Value = item.DriverInfo;
            row++; stt++;
        }

        sheet.Columns().AdjustToContents();
        sheet.Column(15).Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"OtherTripRequests_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile? importFile, string? department, string? employeeCode, string? employeeName, string? phone, int? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["OtherTripRequestError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { department, employeeCode, employeeName, phone, status, dateFrom, dateTo, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;
        var allowedOtherTypes = await LoadRequestTypeNamesByGroupAsync(OtherTripRequestTypeGroup);
        var defaultOtherType = allowedOtherTypes.FirstOrDefault() ?? OtherTripRequestConstants.TripTypeName;

        async Task<bool> TryImportRowAsync(List<string> cells)
        {
            if (cells.Count < 12) return false;

            var offset = int.TryParse(cells[0], out _) ? 1 : 0;
            var empCode = cells.ElementAtOrDefault(offset)?.Trim();
            if (string.IsNullOrWhiteSpace(empCode)) return false;

            DateTime? goDate = DateTime.TryParse(cells.ElementAtOrDefault(offset + 4), out var gd) ? gd : null;
            var goTime = cells.ElementAtOrDefault(offset + 5)?.Trim();
            var goPickup = MergeDateTime(goDate, goTime);
            if (!goPickup.HasValue) return false;

            DateTime? returnDate = DateTime.TryParse(cells.ElementAtOrDefault(offset + 8), out var rd) ? rd : null;
            var returnTime = cells.ElementAtOrDefault(offset + 9)?.Trim();

            var entity = new TM_TRIP_REQUEST
            {
                CHR_EMPLOYEE_ID = empCode,
                CHR_TRIP_TYPE = OtherTripRequestConstants.TripTypeCode,
                NVCHR_TRIP_TYPE = defaultOtherType,
                DTM_PICKUP_GO = goPickup,
                DTM_PICKUP_RETURN = MergeDateTime(returnDate, returnTime),
                NVCHR_FROM_LOCATION_GO = cells.ElementAtOrDefault(offset + 6)?.Trim(),
                NVCHR_TO_LOCATION_GO = cells.ElementAtOrDefault(offset + 7)?.Trim(),
                NVCHR_FROM_LOCATION_RETURN = cells.ElementAtOrDefault(offset + 10)?.Trim(),
                NVCHR_TO_LOCATION_RETURN = cells.ElementAtOrDefault(offset + 11)?.Trim(),
                INT_STATUS = TripRequestStatuses.PendingQlsc,
                CHR_STATUS = ResolveStatusText(TripRequestStatuses.PendingQlsc),
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmTripRequestService.AddAsyncV2(entity);
            if (!addResp.Success) return false;

            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_TRIP_REQEST_ID = addResp.Data?.ID ?? entity.ID,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = TripRequestStatuses.PendingQlsc,
                BIT_ACTION = true,
                CHR_ACTION = "CREATE_OTHER_TRIP_REQUEST",
                DTM_ACTION = DateTime.Now
            });

            return true;
        }

        if (Path.GetExtension(importFile.FileName)?.Equals(".xlsx", StringComparison.OrdinalIgnoreCase) == true)
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            var lastRow = sheet?.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>();
                for (var c = 1; c <= 13; c++) cells.Add(sheet!.Cell(r, c).GetString());
                if (await TryImportRowAsync(cells)) success++; else failed++;
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
                if (row == 1 && line.Contains("MNV", StringComparison.OrdinalIgnoreCase)) continue;
                var cells = ParseCsvLine(line);
                if (await TryImportRowAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "OtherTripRequestSuccess" : "OtherTripRequestError"] = failed == 0
            ? $"Import thành công {success} đơn."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Index), new { department, employeeCode, employeeName, phone, status, dateFrom, dateTo, page, pageSize });
    }

    private IActionResult RedirectToIndex(TripRequestUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            department = input.SearchDepartment,
            employeeCode = input.SearchEmployeeCode,
            employeeName = input.SearchEmployeeName,
            phone = input.SearchPhone,
            status = input.SearchStatus,
            dateFrom = input.SearchDateFrom,
            dateTo = input.SearchDateTo,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private async Task EnsureEmployeeAsync(TripRequestUpsertInputModel input)
    {
        var code = input.EmployeeCode?.Trim();
        if (string.IsNullOrWhiteSpace(code)) return;

        var employee = await _employeeService.GetByEmployeeCodeAsync(code);
        if (employee == null)
        {
            var agent = await _employeeService.GetAgentEmployeeByEmployeeIdAsync(code);
            if (agent != null)
            {
                employee = await _employeeService.EnsureLocalEmployeeFromAgentAsync(agent);
            }
        }

        if (employee == null) return;

        var changed = false;
        if (!string.IsNullOrWhiteSpace(input.EmployeeName)
            && !string.Equals(employee.NVCHR_EMPLOYEE_NAME?.Trim(), input.EmployeeName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            employee.NVCHR_EMPLOYEE_NAME = input.EmployeeName.Trim();
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(input.DepartmentCode)
            && !string.Equals(employee.CHR_CODE_SEC?.Trim(), input.DepartmentCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            employee.CHR_CODE_SEC = input.DepartmentCode.Trim();
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(input.Phone)
            && !string.Equals(employee.CHR_PHONE?.Trim(), input.Phone.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            employee.CHR_PHONE = input.Phone.Trim();
            changed = true;
        }

        if (!changed) return;

        employee.CHR_USER_UPDATE = GetCurrentActor();
        employee.DTM_UPDATE = DateTime.Now;
        await _tmEmployeeService.UpdateAsync(employee);
    }

    private static DateTime? MergeDateTime(DateTime? date, string? time)
    {
        if (!date.HasValue) return null;
        if (string.IsNullOrWhiteSpace(time)) return date.Value.Date;

        if (TimeSpan.TryParseExact(time.Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out var ts)
            || TimeSpan.TryParse(time.Trim(), out ts))
        {
            return date.Value.Date.Add(ts);
        }

        return date.Value.Date;
    }

    private async Task<List<TM_EMPLOYEEDTO>> LoadEmployeesByCodesAsync(List<string> employeeCodes)
    {
        if (!employeeCodes.Any()) return new List<TM_EMPLOYEEDTO>();

        var filters = new List<FilterOption>();
        for (var i = 0; i < employeeCodes.Count; i++)
        {
            filters.Add(new FilterOption
            {
                Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID),
                Operator = "=",
                Value = employeeCodes[i],
                LogicType = i == 0 ? null : "OR"
            });
        }

        var resp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters
        });

        return resp?.Data?.Data?.ToList() ?? new List<TM_EMPLOYEEDTO>();
    }

    private async Task<bool> CanManageAllAsync()
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

    private async Task<bool> IsAdminAsync()
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
        return string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase);
    }

    private string GetCurrentEmployeeCode()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "employee_code")?.Value?.Trim() ?? string.Empty;
    }

    private string GetCurrentActor()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value
            ?? User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? "system";
    }

    private async Task<string> ResolveSectionNameAsync(string? sectionCode)
    {
        if (string.IsNullOrWhiteSpace(sectionCode)) return string.Empty;
        var sectionResp = await _sectionService.GetAllAsync();
        var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
        var sec = sections.FirstOrDefault(x => string.Equals(x.CHR_CODE?.Trim(), sectionCode.Trim(), StringComparison.OrdinalIgnoreCase));
        return sec?.CHR_NAME ?? sec?.CHR_SECTION_NAME ?? sectionCode;
    }

    private static string ResolveStatusText(int? status)
    {
        return status switch
        {
            TripRequestStatuses.PendingQlsc => "Chờ GA điền thông tin lái xe",
            TripRequestStatuses.Completed => "Hoàn thành",
            _ => "Chờ xử lý"
        };
    }

    private async Task SendCreateMailToGaAsync(TM_TRIP_REQUEST entity)
    {
        try
        {
            var templateResp = await _tmMasterMailService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_MASTER_MAIL.CHR_MAIL_CODE), Operator = "=", Value = "OTHER_TRIP_REQUEST_GA_DRIVER_INFO" }
                }
            });

            var to = templateResp?.Data?.Data?.FirstOrDefault()?.CHR_TO;
            if (string.IsNullOrWhiteSpace(to))
            {
                to = "BIVNgabus@brother-bivn.com.vn";
            }

            var link = BuildApprovalLink();
            var requestType = string.IsNullOrWhiteSpace(entity.NVCHR_TRIP_TYPE) ? "Xe khác" : entity.NVCHR_TRIP_TYPE;
            var title = $"[TEST][BusManagement] Đơn đăng ký {requestType} mới";
            var body = """
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="utf-8" />
                    <title>Yêu cầu xác nhận xe công ty</title>
                </head>
                <body style="margin:0;padding:0;background-color:#ffffff;font-family:'Times New Roman', Times, serif;">
                
                    <div style="width:100%;margin:0 auto;">
                
                        <p style="color:red;font-size:24px;font-weight:bold;margin:0 0 30px 0;">
                            Đây là mail tự động, vui lòng không Reply lại mail!
                        </p>
                
                        <p style="font-size:20px;margin:0 0 30px 0;">
                            Gửi nhóm quản lý xe Công ty,
                        </p>
                
                        <p style="font-size:20px;margin:0 0 30px 0;">Có đơn đăng ký {{RequestType}} mới.</p>
                        <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng điền thông tin xe/lái xe.</p>
                        <p style="font-size:20px;margin:0 0 10px 0;">Truy cập tại: <a href="{{ApproveLink}}">LINK</a></p>
                
                        <p style="font-size:20px;margin:0 0 10px 0;">
                            Xin cảm ơn!
                        </p>
                
                        <p style="font-size:20px;font-style:italic;margin:0;">
                            Hệ thống quản lý xe Công ty-BIVN
                        </p>
                
                    </div>
                
                </body>
                </html>
                """;
            body = body.Replace("{{RequestType}}", requestType, StringComparison.OrdinalIgnoreCase)
                       .Replace("{{ApproveLink}}", link, StringComparison.OrdinalIgnoreCase);
            _tmMasterMailService.sendEmailNotify(title, "zzpsvvmg@brother-bivn.com.vn", to, string.Empty, string.Empty, body, 0);
        }
        catch
        {
            // ignore
        }
    }

    private static string? NormalizeOtherRequestType(string? value, List<string> allowedTypes)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        if (!allowedTypes.Any()) return normalized;
        return allowedTypes.FirstOrDefault(x => string.Equals(x, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<TM_REQUEST_TYPEDTO>> LoadRequestTypesByGroupAsync(int group)
    {
        var resp = await _tmRequestTypeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_REQUEST_TYPE.INT_GROUP), Operator = "=", Value = group }
            }
        });

        return resp?.Data?.Data?.ToList() ?? new List<TM_REQUEST_TYPEDTO>();
    }

    private async Task<List<string>> LoadRequestTypeNamesByGroupAsync(int group)
    {
        var types = await LoadRequestTypesByGroupAsync(group);

        return types
            .Select(x => x.NVCHR_REQUEST_TYPE?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private string BuildApprovalLink()
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var baseUrl = configuration?["SystemLinks:BusManagementWebApp"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = Request?.Host.HasValue == true ? $"{Request.Scheme}://{Request.Host.Value}" : string.Empty;
        }

        var relativePath = Url.Action("Index", "OtherTripRequestApproval") ?? "/OtherTripRequestApproval";
        if (string.IsNullOrWhiteSpace(baseUrl)) return relativePath;
        return $"{baseUrl.TrimEnd('/')}{relativePath}";
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
