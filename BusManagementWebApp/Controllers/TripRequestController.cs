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

namespace BusManagement.Controllers;

public class TripRequestController : Controller
{
    private const int TripRequestTypeGroup = 2;

    private readonly ITmTripRequestService _tmTripRequestService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly IEmployeeService _employeeService;
    private readonly ITmUserService _tmUserService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ISectionService _sectionService;
    private readonly ITmApprovalHistoryService _tmApprovalHistoryService;
    private readonly ITmMasterMailService _tmMasterMailService;
    private readonly ITmRequestTypeService _tmRequestTypeService;

    public TripRequestController(
        ITmTripRequestService tmTripRequestService,
        ITmEmployeeService tmEmployeeService,
        IEmployeeService employeeService,
        ITmUserService tmUserService,
        ITmRoleService tmRoleService,
        ISectionService sectionService,
        ITmApprovalHistoryService tmApprovalHistoryService,
        ITmMasterMailService tmMasterMailService,
        ITmRequestTypeService tmRequestTypeService)
    {
        _tmTripRequestService = tmTripRequestService;
        _tmEmployeeService = tmEmployeeService;
        _employeeService = employeeService;
        _tmUserService = tmUserService;
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
        string? purpose,
        string? tripType,
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
            new() { Field = nameof(TM_TRIP_REQUEST.CHR_TRIP_TYPE), Operator = "!=", Value = OtherTripRequestConstants.TripTypeCode }
        };

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.CHR_EMPLOYEE_ID), Operator = "LIKE", Value = $"%{employeeCode.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(purpose))
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.NVCHR_PURPOSE), Operator = "LIKE", Value = $"%{purpose.Trim()}%" });
        }

        if (!string.IsNullOrWhiteSpace(tripType))
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.CHR_TRIP_TYPE), Operator = "=", Value = tripType.Trim() });
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
        var allowedTripTypeNames = await LoadRequestTypeNamesByGroupAsync(TripRequestTypeGroup);
        if (allowedTripTypeNames.Any())
        {
            requests = requests
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_TRIP_TYPE)
                    && allowedTripTypeNames.Contains(x.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase))
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

        var sectionResp = await _sectionService.GetAllAsync();
        var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
        var sectionMap = sections
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

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
                PassengerCount = x.INT_PASSENGER_COUNT,
                TripType = x.CHR_TRIP_TYPE,
                PickupGo = x.DTM_PICKUP_GO,
                FromGo = x.NVCHR_FROM_LOCATION_GO,
                ToGo = x.NVCHR_TO_LOCATION_GO,
                PickupReturn = x.DTM_PICKUP_RETURN,
                FromReturn = x.NVCHR_FROM_LOCATION_RETURN,
                ToReturn = x.NVCHR_TO_LOCATION_RETURN,
                Purpose = x.NVCHR_PURPOSE,
                QuotePath = x.CHR_QUOTE_PATH,
                QuoteConfirm = x.BIT_QUOTE_CONFIRM,
                DriverInfo = x.NVCHR_DRIVER_INFO,
                QlscApprove = x.CHR_QLSC_APPROVE,
                QltcApprove = x.CHR_QLTC_APPROVE,
                Status = x.INT_STATUS,
                StatusText = TripRequestStatusUiHelper.GetStatusText(x.INT_STATUS),
                StatusRowClass = TripRequestStatusUiHelper.GetRowClass(x.INT_STATUS),
                StatusBadgeClass = TripRequestStatusUiHelper.GetBadgeClass(x.INT_STATUS),
                CanEdit = isAdmin ||
                    (string.Equals(x.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase)
                     && TripRequestStatusUiHelper.CanEditByOwner(x.INT_STATUS)),
                CanDelete =(string.Equals(x.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase)
                     && TripRequestStatusUiHelper.CanSoftDeleteByOwner(x.INT_STATUS))
            };
        }).ToList();

        var usersResp = await _tmUserService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var users = usersResp?.Data?.Data?.Where(x => x.BIT_IS_ACTIVE != false).ToList() ?? new List<TM_USERDTO>();

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
            Purpose = purpose,
            TripType = tripType,
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
            Sections = sections.ToList(),
            QlscUsers = users,
            QltcUsers = users
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
        if (!canManageAll && !string.Equals(input.EmployeeCode?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["TripRequestError"] = "Bạn chỉ có thể tạo đơn cho chính mình.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.EmployeeCode)
            || string.IsNullOrWhiteSpace(input.TripType)
            || !input.PickupGoDate.HasValue
            || string.IsNullOrWhiteSpace(input.PickupGoTime)
            || string.IsNullOrWhiteSpace(input.FromGo)
            || string.IsNullOrWhiteSpace(input.ToGo)
            || string.IsNullOrWhiteSpace(input.QlscApprove)
            || string.IsNullOrWhiteSpace(input.QltcApprove))
        {
            TempData["TripRequestError"] = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
            return RedirectToIndex(input);
        }

        await EnsureEmployeeAsync(input);

        var goPickup = MergeDateTime(input.PickupGoDate, input.PickupGoTime);
        var returnPickup = string.Equals(input.TripType, "TWO_WAY", StringComparison.OrdinalIgnoreCase)
            ? MergeDateTime(input.PickupReturnDate, input.PickupReturnTime)
            : null;

        var allowedTripTypeNames = await LoadRequestTypeNamesByGroupAsync(TripRequestTypeGroup);
        var tripTypeName = ResolveTripTypeNameForDb(input.TripType, allowedTripTypeNames);
        if (allowedTripTypeNames.Any() && string.IsNullOrWhiteSpace(tripTypeName))
        {
            TempData["TripRequestError"] = "Loại chuyến không hợp lệ theo cấu hình loại đơn (GROUP=2).";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var entity = new TM_TRIP_REQUEST
        {
            CHR_EMPLOYEE_ID = input.EmployeeCode?.Trim(),
            CHR_TRIP_TYPE = input.TripType?.Trim(),
            NVCHR_TRIP_TYPE = tripTypeName,
            DTM_PICKUP_GO = goPickup,
            DTM_PICKUP_RETURN = returnPickup,
            NVCHR_FROM_LOCATION_GO = input.FromGo?.Trim(),
            NVCHR_TO_LOCATION_GO = input.ToGo?.Trim(),
            NVCHR_FROM_LOCATION_RETURN = input.FromReturn?.Trim(),
            NVCHR_TO_LOCATION_RETURN = input.ToReturn?.Trim(),
            NVCHR_PURPOSE = input.Purpose?.Trim(),
            INT_PASSENGER_COUNT = input.PassengerCount,
            CHR_QLSC_APPROVE = input.QlscApprove?.Trim(),
            CHR_QLTC_APPROVE = input.QltcApprove?.Trim(),
            INT_STATUS = TripRequestStatuses.PendingQlsc,
            CHR_STATUS = TripRequestStatusUiHelper.GetStatusText(TripRequestStatuses.PendingQlsc),
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        var resp = await _tmTripRequestService.AddAsyncV2(entity);
        if (!resp.Success)
        {
            TempData["TripRequestError"] = resp.Message ?? "Không thể tạo đơn.";
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
                CHR_ACTION = "CREATE_TRIP_REQUEST",
                DTM_ACTION = DateTime.Now
            });
        }

        TempData["TripRequestSuccess"] = "Tạo đơn thành công.";
        TempData["TripRequestCelebrate"] = "1";
        await SendApprovalMailToQlscAsync(entity, input);
        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Update(TripRequestUpsertInputModel input)
    {
        if (!input.Id.HasValue || input.Id.Value <= 0)
        {
            TempData["TripRequestError"] = "Không xác định được đơn cần cập nhật.";
            return RedirectToIndex(input);
        }

        var currentResp = await _tmTripRequestService.GetByIdAsync(input.Id.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["TripRequestError"] = currentResp.Message ?? "Không tìm thấy đơn.";
            return RedirectToIndex(input);
        }

        if (string.Equals(current.CHR_TRIP_TYPE, OtherTripRequestConstants.TripTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["TripRequestError"] = "Đơn thuộc màn hình Đăng ký xe khác, vui lòng xử lý tại màn hình tương ứng.";
            return RedirectToIndex(input);
        }

        var isAdmin = await IsAdminAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        if (!isAdmin && !string.Equals(current.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["TripRequestError"] = "Bạn chỉ có thể sửa đơn của chính mình.";
            return RedirectToIndex(input);
        }

        if (!isAdmin && !TripRequestStatusUiHelper.CanEditByOwner(current.INT_STATUS))
        {
            TempData["TripRequestError"] = "Đơn đã được phê duyệt, không thể sửa.";
            return RedirectToIndex(input);
        }

        await EnsureEmployeeAsync(input);

        var allowedTripTypeNames = await LoadRequestTypeNamesByGroupAsync(TripRequestTypeGroup);
        var tripTypeName = ResolveTripTypeNameForDb(input.TripType, allowedTripTypeNames);
        if (allowedTripTypeNames.Any() && string.IsNullOrWhiteSpace(tripTypeName))
        {
            TempData["TripRequestError"] = "Loại chuyến không hợp lệ theo cấu hình loại đơn (GROUP=2).";
            return RedirectToIndex(input);
        }

        var actor = GetCurrentActor();
        var goPickup = MergeDateTime(input.PickupGoDate, input.PickupGoTime);
        var returnPickup = string.Equals(input.TripType, "TWO_WAY", StringComparison.OrdinalIgnoreCase)
            ? MergeDateTime(input.PickupReturnDate, input.PickupReturnTime)
            : null;

        var resubmit = current.INT_STATUS == TripRequestStatuses.QlscRejected
                       || current.INT_STATUS == TripRequestStatuses.QltcRejected
                       || current.INT_STATUS == TripRequestStatuses.GaRejected
                       || current.INT_STATUS == TripRequestStatuses.EmployeeRejectedQuote;

        var entity = new TM_TRIP_REQUEST
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = input.EmployeeCode?.Trim(),
            CHR_TRIP_TYPE = input.TripType?.Trim(),
            NVCHR_TRIP_TYPE = tripTypeName,
            DTM_PICKUP_GO = goPickup,
            DTM_PICKUP_RETURN = returnPickup,
            NVCHR_FROM_LOCATION_GO = input.FromGo?.Trim(),
            NVCHR_TO_LOCATION_GO = input.ToGo?.Trim(),
            NVCHR_FROM_LOCATION_RETURN = input.FromReturn?.Trim(),
            NVCHR_TO_LOCATION_RETURN = input.ToReturn?.Trim(),
            NVCHR_PURPOSE = input.Purpose?.Trim(),
            INT_PASSENGER_COUNT = input.PassengerCount,
            CHR_QLSC_APPROVE = input.QlscApprove?.Trim(),
            CHR_QLTC_APPROVE = input.QltcApprove?.Trim(),
            CHR_GA_CONFIRM = resubmit ? null : current.CHR_GA_CONFIRM,
            CHR_QUOTE_PATH = resubmit ? null : current.CHR_QUOTE_PATH,
            BIT_QUOTE_CONFIRM = resubmit ? null : current.BIT_QUOTE_CONFIRM,
            NVCHR_DRIVER_INFO = resubmit ? null : current.NVCHR_DRIVER_INFO,
            INT_STATUS = resubmit ? TripRequestStatuses.PendingQlsc : current.INT_STATUS,
            CHR_STATUS = TripRequestStatusUiHelper.GetStatusText(resubmit ? TripRequestStatuses.PendingQlsc : current.INT_STATUS)
        };

        var resp = await _tmTripRequestService.UpdateAsync(entity);
        if (!resp.Success)
        {
            TempData["TripRequestError"] = resp.Message ?? "Không thể cập nhật đơn.";
            return RedirectToIndex(input);
        }

        if (resubmit)
        {
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_TRIP_REQEST_ID = entity.ID,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = TripRequestStatuses.PendingQlsc,
                BIT_ACTION = true,
                CHR_ACTION = "RESUBMIT_TRIP_REQUEST",
                DTM_ACTION = DateTime.Now
            });
            TempData["TripRequestSuccess"] = "Cập nhật đơn và gửi phê duyệt lại thành công.";
            TempData["TripRequestCelebrate"] = "1";
            await SendApprovalMailToQlscAsync(entity, input);
        }
        else
        {
            TempData["TripRequestSuccess"] = "Cập nhật đơn thành công.";
            TempData["TripRequestCelebrate"] = "1";
        }

        return RedirectToIndex(input);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, TripRequestUpsertInputModel input)
    {
        var currentResp = await _tmTripRequestService.GetByIdAsync(id);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["TripRequestError"] = currentResp.Message ?? "Không tìm thấy đơn.";
            return RedirectToIndex(input);
        }

        if (string.Equals(current.CHR_TRIP_TYPE, OtherTripRequestConstants.TripTypeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["TripRequestError"] = "Đơn thuộc màn hình Đăng ký xe khác, vui lòng xử lý tại màn hình tương ứng.";
            return RedirectToIndex(input);
        }

        var allowedTripTypeNames = await LoadRequestTypeNamesByGroupAsync(TripRequestTypeGroup);
        if (allowedTripTypeNames.Any()
            && (string.IsNullOrWhiteSpace(current.NVCHR_TRIP_TYPE)
                || !allowedTripTypeNames.Contains(current.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase)))
        {
            TempData["TripRequestError"] = "Đơn không thuộc nhóm loại đơn GROUP=2.";
            return RedirectToIndex(input);
        }

        var isAdmin = await IsAdminAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        if (!isAdmin && !string.Equals(current.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["TripRequestError"] = "Bạn không có quyền xóa đơn này.";
            return RedirectToIndex(input);
        }

        if (!isAdmin && !TripRequestStatusUiHelper.CanSoftDeleteByOwner(current.INT_STATUS))
        {
            TempData["TripRequestError"] = "Đơn đã qua bước GA, không thể xóa.";
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
            NVCHR_PURPOSE = current.NVCHR_PURPOSE,
            INT_PASSENGER_COUNT = current.INT_PASSENGER_COUNT,
            CHR_QLSC_APPROVE = current.CHR_QLSC_APPROVE,
            CHR_QLTC_APPROVE = current.CHR_QLTC_APPROVE,
            CHR_GA_CONFIRM = current.CHR_GA_CONFIRM,
            CHR_QUOTE_PATH = current.CHR_QUOTE_PATH,
            BIT_QUOTE_CONFIRM = current.BIT_QUOTE_CONFIRM,
            NVCHR_DRIVER_INFO = current.NVCHR_DRIVER_INFO,
            INT_STATUS = TripRequestStatuses.Deleted,
            CHR_STATUS = "DELETED"
        };

        var resp = await _tmTripRequestService.UpdateAsync(entity);
        TempData[resp.Success ? "TripRequestSuccess" : "TripRequestError"] = resp.Success
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
                phone = phone ?? string.Empty
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? department, string? employeeCode, string? employeeName, string? phone, string? purpose, string? tripType, int? status, DateTime? dateFrom, DateTime? dateTo)
    {
        var result = await Index(department, employeeCode, employeeName, phone, purpose, tripType, status, dateFrom, dateTo, 1, 100000);
        if (result is not ViewResult vr || vr.Model is not TripRequestPageViewModel vm)
        {
            return RedirectToAction(nameof(Index));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("TripRequests");
        sheet.Cell(1, 1).Value = "Thao tác";
        sheet.Cell(1, 2).Value = "STT";
        sheet.Cell(1, 3).Value = "Trạng thái";
        sheet.Cell(1, 4).Value = "Phòng ban";
        sheet.Cell(1, 5).Value = "Họ tên";
        sheet.Cell(1, 6).Value = "MNV";
        sheet.Cell(1, 7).Value = "SĐT";
        sheet.Cell(1, 8).Value = "SL người";
        sheet.Cell(1, 9).Value = "Loại chuyến";
        sheet.Cell(1, 10).Value = "Chiều đi";
        sheet.Cell(1, 14).Value = "Chiều về";
        sheet.Cell(1, 18).Value = "Mục đích";
        sheet.Cell(1, 19).Value = "Báo giá";
        sheet.Cell(1, 20).Value = "Lái xe";
        sheet.Cell(1, 21).Value = "Phản hồi";

        sheet.Cell(2, 10).Value = "Ngày đưa";
        sheet.Cell(2, 11).Value = "Giờ đưa";
        sheet.Cell(2, 12).Value = "Điểm đón";
        sheet.Cell(2, 13).Value = "Điểm đến";
        sheet.Cell(2, 14).Value = "Ngày đưa";
        sheet.Cell(2, 15).Value = "Giờ đưa";
        sheet.Cell(2, 16).Value = "Điểm đón";
        sheet.Cell(2, 17).Value = "Điểm đến";

        foreach (var col in new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 18, 19, 20, 21 })
        {
            sheet.Range(1, col, 2, col).Merge();
        }
        sheet.Range(1, 10, 1, 13).Merge();
        sheet.Range(1, 14, 1, 17).Merge();

        var headerRange = sheet.Range(1, 1, 2, 21);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9ECEF");

        var row = 3;
        var stt = 1;
        foreach (var item in vm.Requests)
        {
            sheet.Cell(row, 1).Value = string.Empty;
            sheet.Cell(row, 2).Value = stt;
            sheet.Cell(row, 3).Value = item.StatusText;
            sheet.Cell(row, 4).Value = item.DepartmentName;
            sheet.Cell(row, 5).Value = item.EmployeeName;
            sheet.Cell(row, 6).Value = item.EmployeeCode;
            sheet.Cell(row, 7).Value = item.Phone;
            sheet.Cell(row, 8).Value = item.PassengerCount;
            sheet.Cell(row, 9).Value = string.Equals(item.TripType, "TWO_WAY", StringComparison.OrdinalIgnoreCase) ? "2 chiều" : "1 chiều";
            sheet.Cell(row, 10).Value = item.PickupGo?.ToString("dd/MM/yyyy");
            sheet.Cell(row, 11).Value = item.PickupGo?.ToString("HH:mm");
            sheet.Cell(row, 12).Value = item.FromGo;
            sheet.Cell(row, 13).Value = item.ToGo;
            sheet.Cell(row, 14).Value = item.PickupReturn?.ToString("dd/MM/yyyy");
            sheet.Cell(row, 15).Value = item.PickupReturn?.ToString("HH:mm");
            sheet.Cell(row, 16).Value = item.FromReturn;
            sheet.Cell(row, 17).Value = item.ToReturn;
            sheet.Cell(row, 18).Value = item.Purpose;
            sheet.Cell(row, 19).Value = item.QuotePath;
            sheet.Cell(row, 20).Value = item.DriverInfo;
            sheet.Cell(row, 21).Value = item.QuoteConfirm == true ? "Đồng ý" : item.QuoteConfirm == false ? "Từ chối" : string.Empty;
            row++; stt++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TripRequests_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
    }

    [HttpPost]
    public async Task<IActionResult> Import(IFormFile? importFile, string? department, string? employeeCode, string? employeeName, string? phone, string? purpose, string? tripType, int? status, DateTime? dateFrom, DateTime? dateTo, int page = 1, int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["TripRequestError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { department, employeeCode, employeeName, phone, purpose, tripType, status, dateFrom, dateTo, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;
        var allowedTripTypeNames = await LoadRequestTypeNamesByGroupAsync(TripRequestTypeGroup);

        async Task<bool> TryImportRowAsync(List<string> cells)
        {
            if (cells.Count < 16) return false;

            var offset = int.TryParse(cells[0], out _) ? 1 : 0;
            var empCode = cells.ElementAtOrDefault(offset + 1)?.Trim();
            if (string.IsNullOrWhiteSpace(empCode)) return false;

            var tripTypeValue = cells.ElementAtOrDefault(offset + 5)?.Trim();
            var typeCode = string.Equals(tripTypeValue, "2 chiều", StringComparison.OrdinalIgnoreCase)
                ? "TWO_WAY"
                : "ONE_WAY";
            var mappedTripTypeName = ResolveTripTypeNameForDb(typeCode, allowedTripTypeNames);
            if (allowedTripTypeNames.Any() && string.IsNullOrWhiteSpace(mappedTripTypeName))
            {
                return false;
            }

            DateTime? goDate = DateTime.TryParse(cells.ElementAtOrDefault(offset + 6), out var gd) ? gd : null;
            var goTime = cells.ElementAtOrDefault(offset + 7)?.Trim();
            var goPickup = MergeDateTime(goDate, goTime);

            DateTime? returnDate = DateTime.TryParse(cells.ElementAtOrDefault(offset + 10), out var rd) ? rd : null;
            var returnTime = cells.ElementAtOrDefault(offset + 11)?.Trim();
            var returnPickup = MergeDateTime(returnDate, returnTime);

            var entity = new TM_TRIP_REQUEST
            {
                CHR_EMPLOYEE_ID = empCode,
                CHR_TRIP_TYPE = typeCode,
                NVCHR_TRIP_TYPE = mappedTripTypeName,
                DTM_PICKUP_GO = goPickup,
                DTM_PICKUP_RETURN = returnPickup,
                NVCHR_FROM_LOCATION_GO = cells.ElementAtOrDefault(offset + 8)?.Trim(),
                NVCHR_TO_LOCATION_GO = cells.ElementAtOrDefault(offset + 9)?.Trim(),
                NVCHR_FROM_LOCATION_RETURN = cells.ElementAtOrDefault(offset + 12)?.Trim(),
                NVCHR_TO_LOCATION_RETURN = cells.ElementAtOrDefault(offset + 13)?.Trim(),
                NVCHR_PURPOSE = cells.ElementAtOrDefault(offset + 14)?.Trim(),
                INT_PASSENGER_COUNT = int.TryParse(cells.ElementAtOrDefault(offset + 4), out var p) ? p : null,
                INT_STATUS = TripRequestStatuses.PendingQlsc,
                CHR_STATUS = TripRequestStatusUiHelper.GetStatusText(TripRequestStatuses.PendingQlsc),
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            var addResp = await _tmTripRequestService.AddAsyncV2(entity);
            return addResp.Success;
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
                for (var c = 1; c <= 17; c++) cells.Add(sheet!.Cell(r, c).GetString());
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

        TempData[failed == 0 ? "TripRequestSuccess" : "TripRequestError"] = failed == 0
            ? $"Import thành công {success} đơn."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Index), new { department, employeeCode, employeeName, phone, purpose, tripType, status, dateFrom, dateTo, page, pageSize });
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

    private IActionResult RedirectToIndex(TripRequestUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            department = input.SearchDepartment,
            employeeCode = input.SearchEmployeeCode,
            employeeName = input.SearchEmployeeName,
            phone = input.SearchPhone,
            purpose = input.SearchPurpose,
            tripType = input.SearchTripType,
            status = input.SearchStatus,
            dateFrom = input.SearchDateFrom,
            dateTo = input.SearchDateTo,
            page = input.Page,
            pageSize = input.PageSize
        });
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
        return User?.Claims?.FirstOrDefault(c => c.Type == "employee_code")?.Value ?? string.Empty;
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

    private async Task SendApprovalMailToQlscAsync(TM_TRIP_REQUEST entity, TripRequestUpsertInputModel input)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(entity.CHR_QLSC_APPROVE)) return;

            var userResp = await _tmUserService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_ADID", Operator = "=", Value = entity.CHR_QLSC_APPROVE }
                }
            });
            var qlsc = userResp?.Data?.Data?.FirstOrDefault();

            var toEmail = ToCompanyEmail(qlsc?.CHR_ADID ?? entity.CHR_QLSC_APPROVE);
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
            var mailCode = "TRIP_REQUEST_QLSC_APPROVAL";

            var template = await GetMailTemplateByCodeAsync(mailCode);
            var sectionName = await ResolveSectionNameAsync(input.DepartmentCode);
            var approveLink = BuildTripRequestApproveLink();

            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{To}}"] = qlsc?.NVCHR_NAME ?? qlsc?.CHR_ADID ?? entity.CHR_QLSC_APPROVE ?? string.Empty,
                ["{{EmployeeCode}}"] = input.EmployeeCode ?? entity.CHR_EMPLOYEE_ID ?? string.Empty,
                ["{{EmployeeName}}"] = input.EmployeeName ?? string.Empty,
                ["{{EmployeeSection}}"] = sectionName,
                ["{{Purpose}}"] = input.Purpose ?? string.Empty,
                ["{{ApproveLink}}"] = approveLink
            };

            var title = ReplaceTemplateTokens(template?.CHR_SUBJECT, tokens);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "[TEST][BusManagement] Đơn đăng ký xe công tác/khác cần QLSC phê duyệt";
            }

            var contentMessage = """
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="utf-8" />
                    <title>Yêu cầu xác nhận xe công ty</title>
                </head>
                <body style="margin:0;padding:0;background-color:#ffffff;font-family:'Times New Roman', Times, serif;">

                    <div style="width:100%;margin:0 auto;">

                        <!-- Header -->
                        <p style="color:red;font-size:24px;font-weight:bold;margin:0 0 30px 0;">
                            Đây là mail tự động, vui lòng không Reply lại mail!
                        </p>

                        <!-- Nội dung -->
                        <p style="font-size:20px;margin:0 0 30px 0;">
                            Gửi anh/chị {{To}},
                        </p>

                        <p style="font-size:20px;margin:0 0 30px 0;">
                            Phía dưới là thông tin yêu cầu xe Công ty cần anh chị xác nhận :
                        </p>

                        <p style="font-size:20px;line-height:1.5;margin:0 0 30px 0;">
                            Loại đơn : Xe công tác<br />
                            CNV : {{EmployeeCode}} - {{EmployeeName}} ({{EmployeeSection}})<br />
                            Mục đích: {{Purpose}}
                        </p>

                        <p style="font-size:20px;margin:0 0 10px 0;">
                            Vui lòng vào hệ thống xác nhận đơn cho người yêu cầu !
                        </p>

                        <p style="font-size:20px;margin:0 0 10px 0;">
                            Hoặc truy cập :
                            <a href="{{ApproveLink}}">LINK</a>
                            </a>
                        </p>

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

            var bodyTemplate = string.IsNullOrWhiteSpace(template?.CHR_CONTENT)
                ? contentMessage
                : template?.CHR_CONTENT ?? string.Empty;
            var body = ReplaceTemplateTokens(bodyTemplate, tokens);

            var fromEmail = ReplaceTemplateTokens(template?.CHR_FROM, tokens);
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = "zzpsvvmg@brother-bivn.com.vn";
            }

            var toTemplate = ReplaceTemplateTokens(template?.CHR_TO, tokens);
            var to = CombineEmails(toEmail, toTemplate);
            var cc = ReplaceTemplateTokens(template?.CHR_CC, tokens);
            var bcc = ReplaceTemplateTokens(template?.CHR_BCC, tokens);
            var priority = template?.BIT_HIGHT == true ? 1 : 0;

            _tmMasterMailService.sendEmailNotify(title, fromEmail, to, cc ?? string.Empty, bcc ?? string.Empty, body, priority);
        }
        catch
        {
            // ignore
        }
    }

    private static string ToCompanyEmail(string? adid)
    {
        if (string.IsNullOrWhiteSpace(adid)) return string.Empty;
        var v = adid.Trim();
        return v.Contains('@') ? v : $"{v}@brothergroup.net";
    }

    private async Task<TM_MASTER_MAILDTO?> GetMailTemplateByCodeAsync(string code)
    {
        var resp = await _tmMasterMailService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_MASTER_MAIL.CHR_MAIL_CODE), Operator = "=", Value = code }
            }
        });

        return resp?.Data?.Data?.FirstOrDefault();
    }

    private string BuildTripRequestApproveLink()
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var baseUrl = configuration?["SystemLinks:BusManagementWebApp"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = Request?.Host.HasValue == true
                ? $"{Request.Scheme}://{Request.Host.Value}"
                : string.Empty;
        }

        var relativePath = Url.Action("Index", "TripRequestApproval", new { stage = "qlsc" }) ?? "/TripRequestApproval?stage=qlsc";
        if (string.IsNullOrWhiteSpace(baseUrl)) return relativePath;
        return $"{baseUrl.TrimEnd('/')}{relativePath}";
    }

    private static string ReplaceTemplateTokens(string? template, IReadOnlyDictionary<string, string> tokens)
    {
        if (string.IsNullOrWhiteSpace(template)) return string.Empty;

        var result = template;
        foreach (var token in tokens)
        {
            result = result.Replace(token.Key, token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }

    private static string CombineEmails(params string?[] emailGroups)
    {
        return string.Join(";",
            emailGroups
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(x => x!.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private async Task<List<string>> LoadRequestTypeNamesByGroupAsync(int group)
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

        return (resp?.Data?.Data ?? Enumerable.Empty<TM_REQUEST_TYPEDTO>())
            .Select(x => x.NVCHR_REQUEST_TYPE?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveTripTypeNameForDb(string? tripTypeCode, List<string> configuredTypeNames)
    {
        var defaultName = string.Equals(tripTypeCode, "TWO_WAY", StringComparison.OrdinalIgnoreCase)
            ? "2 chiều"
            : "1 chiều";

        if (!configuredTypeNames.Any())
        {
            return defaultName;
        }

        if (string.Equals(tripTypeCode, "TWO_WAY", StringComparison.OrdinalIgnoreCase))
        {
            return configuredTypeNames.FirstOrDefault(x => x.Contains("2", StringComparison.OrdinalIgnoreCase))
                ?? configuredTypeNames.FirstOrDefault(x => x.Contains("hai", StringComparison.OrdinalIgnoreCase))
                ?? string.Empty;
        }

        return configuredTypeNames.FirstOrDefault(x => x.Contains("1", StringComparison.OrdinalIgnoreCase))
            ?? configuredTypeNames.FirstOrDefault(x => x.Contains("một", StringComparison.OrdinalIgnoreCase))
            ?? string.Empty;
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
