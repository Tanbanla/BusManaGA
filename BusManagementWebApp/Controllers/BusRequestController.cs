using BusManagement.Model.Common;
using BusManagement.Model.Common.Enum;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.BusRequest;
using BusManagementWebApp.Helpers;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace BusManagement.Controllers;

public class BusRequestController : Controller
{
    private const int BusRequestTypeGroup = 1;

    private readonly ITmBusRequestService _tmBusRequestService;
    private readonly ITmRequestTypeService _tmRequestTypeService;
    private readonly ITmBusRouteAssignService _tmBusRouteAssignService;
    private readonly ITmBusService _tmBusService;
    private readonly ITmBusRouteService _tmBusRouteService;
    private readonly ITmBusStopService _tmBusStopService;
    private readonly ITmUserService _tmUserService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly IEmployeeService _employeeService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ISectionService _sectionService;
    private readonly ITmMasterMailService _tmMasterMailService;
    private readonly ITmApprovalHistoryService _tmApprovalHistoryService;

    private static readonly Dictionary<string, List<string>> VehicleAreaConfig = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Nhân viên"] = new() { "Hà Nội", "Hải Phòng" },
        ["Công nhân"] = new() { "Hưng Yên", "Hải Phòng", "Thái Bình", "Bắc Ninh" }
    };

    public BusRequestController(
        ITmBusRequestService tmBusRequestService,
        ITmRequestTypeService tmRequestTypeService,
        ITmBusRouteAssignService tmBusRouteAssignService,
        ITmBusService tmBusService,
        ITmBusRouteService tmBusRouteService,
        ITmBusStopService tmBusStopService,
        ITmUserService tmUserService,
        ITmEmployeeService tmEmployeeService,
        IEmployeeService employeeService,
        ITmRoleService tmRoleService,
        ISectionService sectionService,
        ITmMasterMailService tmMasterMailService,
        ITmApprovalHistoryService tmApprovalHistoryService)
    {
        _tmBusRequestService = tmBusRequestService;
        _tmRequestTypeService = tmRequestTypeService;
        _tmBusRouteAssignService = tmBusRouteAssignService;
        _tmBusService = tmBusService;
        _tmBusRouteService = tmBusRouteService;
        _tmBusStopService = tmBusStopService;
        _tmUserService = tmUserService;
        _tmEmployeeService = tmEmployeeService;
        _employeeService = employeeService;
        _tmRoleService = tmRoleService;
        _sectionService = sectionService;
        _tmMasterMailService = tmMasterMailService;
        _tmApprovalHistoryService = tmApprovalHistoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? department,
        string? departments,
        string? employeeCode,
        string? employeeName,
        string? position,
        string? address,
        int? requestTypeId,
        string? requestTypeIds,
        string? routeKeyword,
        int? busStopId,
        string? busStopIds,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? note,
        int? status,
        int page = 1,
        int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var canManageAll = await CanManageAllAsync();
        var isAdmin = await IsAdminAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        var currentDeptCode = GetCurrentDepartmentCode();

        var selectedDepartments = ParseCsvStrings(departments);
        if (!string.IsNullOrWhiteSpace(department)) selectedDepartments.Add(department.Trim());
        selectedDepartments = selectedDepartments.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var selectedRequestTypeIds = ParseCsvInts(requestTypeIds);
        if (requestTypeId.HasValue) selectedRequestTypeIds.Add(requestTypeId.Value);
        selectedRequestTypeIds = selectedRequestTypeIds.Distinct().ToList();

        var selectedBusStopIds = ParseCsvInts(busStopIds);
        if (busStopId.HasValue) selectedBusStopIds.Add(busStopId.Value);
        selectedBusStopIds = selectedBusStopIds.Distinct().ToList();

        var allowedRequestTypes = await LoadRequestTypesByGroupAsync(BusRequestTypeGroup);
        var allowedRequestTypeIds = allowedRequestTypes.Select(x => x.ID).Distinct().ToList();

        if (selectedRequestTypeIds.Any())
        {
            selectedRequestTypeIds = selectedRequestTypeIds
                .Where(x => allowedRequestTypeIds.Contains(x))
                .Distinct()
                .ToList();
        }

        var filters = BuildBusRequestFilters(selectedRequestTypeIds, selectedBusStopIds, employeeCode, note, dateFrom, dateTo);
        if (status.HasValue)
        {
            filters.Add(new FilterOption
            {
                Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.INT_STATUS)}",
                Operator = "=",
                Value = status.Value
            });
        }
        var joins = BuildBusRequestJoinOptions(selectedDepartments, employeeName, position, address, routeKeyword, canManageAll ? null : currentDeptCode);

        if (selectedDepartments.Any())
        {
            for (var i = 0; i < selectedDepartments.Count; i++)
            {
                filters.Add(new FilterOption
                {
                    Field = $"e.{nameof(TM_EMPLOYEE.CHR_CODE_SEC)}",
                    Operator = "=",
                    Value = selectedDepartments[i],
                    LogicType = i == 0 ? null : "OR"
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            filters.Add(new FilterOption
            {
                Field = $"e.{nameof(TM_EMPLOYEE.NVCHR_EMPLOYEE_NAME)}",
                Operator = "LIKE",
                Value = $"%{employeeName.Trim()}%"
            });
        }

        if (!string.IsNullOrWhiteSpace(position))
        {
            filters.Add(new FilterOption
            {
                Field = $"e.{nameof(TM_EMPLOYEE.CHR_POSITION)}",
                Operator = "LIKE",
                Value = $"%{position.Trim()}%"
            });
        }

        if (!string.IsNullOrWhiteSpace(address))
        {
            filters.Add(new FilterOption
            {
                Field = $"e.{nameof(TM_EMPLOYEE.NVCHR_ADDRESS_DETAIL)}",
                Operator = "LIKE",
                Value = $"%{address.Trim()}%"
            });
        }

        if (!canManageAll && !string.IsNullOrWhiteSpace(currentDeptCode))
        {
            filters.Add(new FilterOption
            {
                Field = $"e.{nameof(TM_EMPLOYEE.CHR_CODE_SEC)}",
                Operator = "=",
                Value = currentDeptCode.Trim()
            });
        }

        if (!string.IsNullOrWhiteSpace(routeKeyword))
        {
            filters.Add(new FilterOption
            {
                Field = $"r.{nameof(TM_BUS_ROUTE.NVCHR_ROUTE_NAME)}",
                Operator = "LIKE",
                Value = $"%{routeKeyword.Trim()}%"
            });
        }

        var requestPageResp = await _tmBusRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = filters,
            JoinOptions = joins,
            SortOptions = new()
            {
                new SortOption { Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.INT_STATUS)}", SortDirection = "ASC" },
                new SortOption { Field = $"{nameof(TM_BUS_REQUEST)}.DTM_CREATE", SortDirection = "DESC" }
            }
        });

        var requestTypesResp = await _tmRequestTypeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_REQUEST_TYPE.INT_GROUP), Operator = "=", Value = BusRequestTypeGroup }
            }
        });
        var usersResp = await _tmUserService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var sectionResp = await _sectionService.GetAllAsync();
        var requestTypes = requestTypesResp?.Data?.Data?.ToList() ?? allowedRequestTypes;
        var users = usersResp?.Data?.Data?.Where(x => x.BIT_IS_ACTIVE != false).ToList() ?? new List<TM_USERDTO>();
        var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();

        var pagedRequests = requestPageResp?.Data?.Data?.ToList() ?? new List<TM_BUS_REQUESTDTO>();
        if (allowedRequestTypeIds.Any())
        {
            pagedRequests = pagedRequests
                .Where(x => x.INT_REQUEST_TYPE.HasValue && allowedRequestTypeIds.Contains(x.INT_REQUEST_TYPE.Value))
                .ToList();
        }
        var totalCount = (int)(requestPageResp?.Data?.TotalCount ?? 0);
        var totalPages = (int)(requestPageResp?.Data?.TotalPages ?? 0);
        if (totalPages <= 0)
        {
            totalPages = totalCount > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 1;
        }

        var requestTypeIdsToLoad = pagedRequests.Where(x => x.INT_REQUEST_TYPE.HasValue).Select(x => x.INT_REQUEST_TYPE!.Value).Distinct().ToList();
        var busStopIdsToLoad = pagedRequests.Where(x => x.INT_BUS_STOP_ID.HasValue).Select(x => x.INT_BUS_STOP_ID!.Value).Distinct().ToList();
        var routeAssignIdsToLoad = pagedRequests.Where(x => x.INT_BUS_ROUTE_ASSIGN_ID.HasValue).Select(x => x.INT_BUS_ROUTE_ASSIGN_ID!.Value).Distinct().ToList();
        var employeeCodesToLoad = pagedRequests.Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID)).Select(x => x.CHR_EMPLOYEE_ID!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var requestTypeMap = (requestTypeIdsToLoad.Any()
            ? (await _tmRequestTypeService.GetByIdsAsync(requestTypeIdsToLoad))?.Data ?? Enumerable.Empty<TM_REQUEST_TYPEDTO>()
            : Enumerable.Empty<TM_REQUEST_TYPEDTO>())
            .Where(x => x.INT_GROUP == BusRequestTypeGroup)
            .ToDictionary(x => x.ID, x => x.NVCHR_REQUEST_TYPE ?? string.Empty);

        var busStopMap = (busStopIdsToLoad.Any()
            ? (await _tmBusStopService.GetByIdsAsync(busStopIdsToLoad))?.Data ?? Enumerable.Empty<TM_BUS_STOPDTO>()
            : Enumerable.Empty<TM_BUS_STOPDTO>())
            .ToDictionary(x => x.ID, x => x);

        var routeAssigns = (routeAssignIdsToLoad.Any()
            ? (await _tmBusRouteAssignService.GetByIdsAsync(routeAssignIdsToLoad))?.Data ?? Enumerable.Empty<TM_BUS_ROUTE_ASSIGNDTO>()
            : Enumerable.Empty<TM_BUS_ROUTE_ASSIGNDTO>())
            .ToList();

        var routeAssignLookups = (await BuildRouteDisplayMap(routeAssigns)).ToList();
        var routeAssignMap = routeAssignLookups.ToDictionary(x => x.Id, x => x);

        var employees = new List<TM_EMPLOYEEDTO>();
        if (employeeCodesToLoad.Any())
        {
            var empFilters = new List<FilterOption>();
            for (var i = 0; i < employeeCodesToLoad.Count; i++)
            {
                empFilters.Add(new FilterOption
                {
                    Field = "CHR_EMPLOYEE_ID",
                    Operator = "=",
                    Value = employeeCodesToLoad[i],
                    LogicType = i == 0 ? null : "OR"
                });
            }

            var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = empFilters
            });

            employees = employeeResp?.Data?.Data?.ToList() ?? new List<TM_EMPLOYEEDTO>();
        }

        var employeeMap = employees
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
            .GroupBy(x => x.CHR_EMPLOYEE_ID!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var sectionMap = sections
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var pagedRows = new List<BusRequestRowViewModel>();
        foreach (var req in pagedRequests)
        {
            employeeMap.TryGetValue(req.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var emp);
            routeAssignMap.TryGetValue(req.INT_BUS_ROUTE_ASSIGN_ID ?? 0, out var routeAssignLookup);
            busStopMap.TryGetValue(req.INT_BUS_STOP_ID ?? 0, out var stop);

            var deptCode = emp?.CHR_CODE_SEC?.Trim();
            sectionMap.TryGetValue(deptCode ?? string.Empty, out var sec);
            var deptName = sec?.CHR_NAME ?? sec?.CHR_SECTION_NAME ?? deptCode;
            var useOrCancelDate = req.DTM_CANCEL_DATE ?? req.DTM_START_DATE;

            pagedRows.Add(new BusRequestRowViewModel
            {
                Id = req.ID,
                DepartmentCode = deptCode,
                DepartmentName = deptName,
                EmployeeCode = req.CHR_EMPLOYEE_ID,
                EmployeeName = emp?.NVCHR_EMPLOYEE_NAME,
                Position = emp?.CHR_POSITION,
                //Address = BuildAddress(emp),
                Address = emp?.NVCHR_ADDRESS_DETAIL,
                RequestTypeId = req.INT_REQUEST_TYPE,
                RequestTypeName = req.INT_REQUEST_TYPE.HasValue && requestTypeMap.ContainsKey(req.INT_REQUEST_TYPE.Value)
                    ? requestTypeMap[req.INT_REQUEST_TYPE.Value]
                    : string.Empty,
                BusRouteAssignId = req.INT_BUS_ROUTE_ASSIGN_ID,
                RouteDisplay = routeAssignLookup?.DisplayName,
                BusStopId = req.INT_BUS_STOP_ID,
                BusStopDisplay = stop?.NVCHR_STOP_NAME,
                UseOrCancelDate = useOrCancelDate,
                Note = req.CHR_NOTE,
                QlscApprove = req.CHR_QLSC_APPROVE,
                Status = req.INT_STATUS,
                StatusText = BusRequestStatusUiHelper.GetStatusText(req.INT_STATUS),
                StatusRowClass = BusRequestStatusUiHelper.GetRowClass(req.INT_STATUS),
                StatusBadgeClass = BusRequestStatusUiHelper.GetBadgeClass(req.INT_STATUS),
                CanEdit = isAdmin ||
                    ((canManageAll || string.Equals(req.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
                        && BusRequestStatusUiHelper.IsEditableStatus(req.INT_STATUS))
            });
        }

        var stopResp = await _tmBusStopService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var stops = stopResp?.Data?.Data?.ToList() ?? new List<TM_BUS_STOPDTO>();

        TM_EMPLOYEEDTO? currentEmployee = null;
        if (!string.IsNullOrWhiteSpace(currentEmployeeCode))
        {
            if (!employeeMap.TryGetValue(currentEmployeeCode, out currentEmployee))
            {
                var currentEmpResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
                {
                    PageNumber = 1,
                    PageSize = 1,
                    Filters = new List<FilterOption> { new() { Field = "CHR_EMPLOYEE_ID", Operator = "=", Value = currentEmployeeCode } }
                });
                currentEmployee = currentEmpResp?.Data?.Data?.FirstOrDefault();
            }
        }

        var vm = new BusRequestPageViewModel
        {
            Department = department,
            Departments = string.Join(',', selectedDepartments),
            EmployeeCode = employeeCode,
            EmployeeName = employeeName,
            Position = position,
            Address = address,
            RequestTypeId = requestTypeId,
            RequestTypeIds = string.Join(',', selectedRequestTypeIds),
            RouteKeyword = routeKeyword,
            BusStopId = busStopId,
            BusStopIds = string.Join(',', selectedBusStopIds),
            DateFrom = dateFrom,
            DateTo = dateTo,
            Note = note,
            Status = status,
            SelectedDepartments = selectedDepartments,
            SelectedRequestTypeIds = selectedRequestTypeIds,
            SelectedBusStopIds = selectedBusStopIds,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            CanManageAll = canManageAll,
            CurrentEmployeeCode = currentEmployeeCode,
            CurrentEmployeeName = currentEmployee?.NVCHR_EMPLOYEE_NAME,
            CurrentDepartmentCode = currentEmployee?.CHR_CODE_SEC,
            Requests = pagedRows,
            RequestTypes = requestTypes,
            Sections = sections
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
                .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(x => x.CHR_CODE)
                .ToList(),
            QlscUsers = users,
            BusStops = stops,
            RouteAssignOptions = routeAssignLookups,
            VehicleAreas = VehicleAreaConfig
        };

        return View(vm);
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

    [HttpPost]
    public async Task<IActionResult> Create(BusRequestUpsertInputModel input)
    {
        var canManageAll = await CanManageAllAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        if (!canManageAll && !string.Equals(input.EmployeeCode?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["BusRequestError"] = "Bạn chỉ có thể tạo đơn cho chính mình.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.EmployeeCode)
            || !input.RequestTypeId.HasValue
            || !input.BusRouteAssignId.HasValue
            || !input.BusStopId.HasValue
            || !input.UseOrCancelDate.HasValue)
        {
            TempData["BusRequestError"] = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
            return RedirectToIndex(input);
        }

        var hasDuplicateCreate = await HasDuplicateRequestAsync(
            input.EmployeeCode?.Trim(),
            input.RequestTypeId.Value,
            excludeRequestId: null);
        if (hasDuplicateCreate)
        {
            TempData["BusRequestError"] = "Đã tồn tại đơn cùng mã nhân viên và loại đơn đang chờ xử lý. Vui lòng kiểm tra lại.";
            return RedirectToIndex(input);
        }

        var requestTypes = await LoadRequestTypesByGroupAsync(BusRequestTypeGroup);
        var requestType = requestTypes
            .FirstOrDefault(x => x.ID == input.RequestTypeId.Value);
        if (requestType == null)
        {
            TempData["BusRequestError"] = "Loại đơn không hợp lệ.";
            return RedirectToIndex(input);
        }
        var requestTypeName = requestType?.NVCHR_REQUEST_TYPE ?? string.Empty;

        await EnsureEmployeeForBusRequestAsync(input);

        var actor = GetCurrentActor();
        var entity = new TM_BUS_REQUEST
        {
            CHR_EMPLOYEE_ID = input.EmployeeCode?.Trim(),
            INT_BUS_ROUTE_ASSIGN_ID = input.BusRouteAssignId,
            INT_BUS_STOP_ID = input.BusStopId,
            INT_REQUEST_TYPE = input.RequestTypeId,
            INT_STATUS = (int)BusRequestStatus.PendingQlscApproval,
            CHR_QLSC_APPROVE = input.QlscApprove?.Trim(),
            CHR_NOTE = input.Note?.Trim(),
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        };

        if (IsCancelRequest(requestTypeName))
        {
            entity.DTM_CANCEL_DATE = input.UseOrCancelDate;
        }
        else
        {
            entity.DTM_START_DATE = input.UseOrCancelDate;
        }

        var resp = await _tmBusRequestService.AddAsyncV2(entity);
        if (!resp.Success)
        {
            TempData["BusRequestError"] = resp.Message ?? "Không thể tạo đơn.";
            return RedirectToIndex(input);
        }

        var requestId = resp.Data?.ID ?? entity.ID;
        if (requestId > 0)
        {
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_BUS_REQEST_ID = requestId,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = (int)BusRequestStatus.PendingQlscApproval,
                BIT_ACTION = true,
                CHR_ACTION = "CREATE_REQUEST",
                DTM_ACTION = DateTime.Now
            });
        }

        await SendApprovalMailAsync(entity, input, requestTypeName);
        TempData["BusRequestSuccess"] = "Tạo đơn thành công. Hệ thống đã gửi mail cho QLSC phê duyệt.";
        TempData["BusRequestCelebrate"] = "1";
        return RedirectToIndex(input);
    }

    private async Task EnsureEmployeeForBusRequestAsync(BusRequestUpsertInputModel input)
    {
        var employeeCode = input.EmployeeCode?.Trim();
        if (string.IsNullOrWhiteSpace(employeeCode)) return;

        var localEmployee = await _employeeService.GetByEmployeeCodeAsync(employeeCode);
        if (localEmployee == null)
        {
            var agentEmployee = await _employeeService.GetAgentEmployeeByEmployeeIdAsync(employeeCode);
            if (agentEmployee != null)
            {
                localEmployee = await _employeeService.EnsureLocalEmployeeFromAgentAsync(agentEmployee);
            }
        }

        if (localEmployee == null) return;

        var hasChanged = false;

        //if (input.BusStopId.HasValue && localEmployee.INT_BUS_STOP_ID != input.BusStopId)
        //{
        //    localEmployee.INT_BUS_STOP_ID = input.BusStopId;
        //    hasChanged = true;
        //}

        //if (localEmployee.BIT_IS_USE_BUS != true)
        //{
        //    localEmployee.BIT_IS_USE_BUS = true;
        //    hasChanged = true;
        //}

        if (!string.IsNullOrWhiteSpace(input.EmployeeName)
            && !string.Equals(localEmployee.NVCHR_EMPLOYEE_NAME?.Trim(), input.EmployeeName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            localEmployee.NVCHR_EMPLOYEE_NAME = input.EmployeeName.Trim();
            hasChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(input.DepartmentCode)
            && !string.Equals(localEmployee.CHR_CODE_SEC?.Trim(), input.DepartmentCode.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            localEmployee.CHR_CODE_SEC = input.DepartmentCode.Trim();
            hasChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(input.Address)
            && !string.Equals(localEmployee.NVCHR_ADDRESS_DETAIL?.Trim(), input.Address.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            localEmployee.NVCHR_ADDRESS_DETAIL = input.Address.Trim();
            hasChanged = true;
        }

        if (!hasChanged) return;

        localEmployee.CHR_USER_UPDATE = GetCurrentActor();
        localEmployee.DTM_UPDATE = DateTime.Now;
        await _tmEmployeeService.UpdateAsync(localEmployee);
    }

    [HttpPost]
    public async Task<IActionResult> Update(BusRequestUpsertInputModel input)
    {
        if (!input.Id.HasValue || input.Id.Value <= 0)
        {
            TempData["BusRequestError"] = "Không xác định được đơn cần cập nhật.";
            return RedirectToIndex(input);
        }

        var canManageAll = await CanManageAllAsync();
        var currentEmployeeCode = GetCurrentEmployeeCode();

        var currentResp = await _tmBusRequestService.GetByIdAsync(input.Id.Value);
        var current = currentResp.Data;
        if (!currentResp.Success || current == null)
        {
            TempData["BusRequestError"] = currentResp.Message ?? "Không tìm thấy đơn yêu cầu.";
            return RedirectToIndex(input);
        }

        if (!canManageAll && !string.Equals(current.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase))
        {
            TempData["BusRequestError"] = "Bạn chỉ có thể sửa đơn do chính mình đăng ký.";
            return RedirectToIndex(input);
        }

        if (string.IsNullOrWhiteSpace(input.EmployeeCode)
            || !input.RequestTypeId.HasValue
            || !input.BusRouteAssignId.HasValue
            || !input.BusStopId.HasValue
            || !input.UseOrCancelDate.HasValue)
        {
            TempData["BusRequestError"] = "Vui lòng nhập đầy đủ thông tin bắt buộc.";
            return RedirectToIndex(input);
        }

        if (!await IsAdminAsync() && !BusRequestStatusUiHelper.IsEditableStatus(current.INT_STATUS))
        {
            TempData["BusRequestError"] = "Đơn đã được xác nhận phê duyệt, không thể chỉnh sửa.";
            return RedirectToIndex(input);
        }

        var hasDuplicateUpdate = await HasDuplicateRequestAsync(
            input.EmployeeCode?.Trim(),
            input.RequestTypeId.Value,
            excludeRequestId: input.Id);
        if (hasDuplicateUpdate)
        {
            TempData["BusRequestError"] = "Đã tồn tại đơn cùng mã nhân viên và loại đơn đang chờ xử lý. Vui lòng kiểm tra lại.";
            return RedirectToIndex(input);
        }

        var requestTypes = await LoadRequestTypesByGroupAsync(BusRequestTypeGroup);
        var requestType = requestTypes
            .FirstOrDefault(x => x.ID == input.RequestTypeId.Value);
        if (requestType == null)
        {
            TempData["BusRequestError"] = "Loại đơn không hợp lệ.";
            return RedirectToIndex(input);
        }
        var requestTypeName = requestType?.NVCHR_REQUEST_TYPE ?? string.Empty;

        await EnsureEmployeeForBusRequestAsync(input);

        var wasRejected = current.INT_STATUS == (int)BusRequestStatus.QlscRejected
            || current.INT_STATUS == (int)BusRequestStatus.GaRejected;

        var actor = GetCurrentActor();
        var entity = new TM_BUS_REQUEST
        {
            ID = current.ID,
            CHR_USER_CREATE = current.CHR_USER_CREATE,
            DTM_CREATE = current.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = input.EmployeeCode?.Trim(),
            INT_BUS_ROUTE_ASSIGN_ID = input.BusRouteAssignId,
            INT_BUS_STOP_ID = input.BusStopId,
            INT_REQUEST_TYPE = input.RequestTypeId,
            INT_STATUS = wasRejected ? (int)BusRequestStatus.PendingQlscApproval : current.INT_STATUS,
            CHR_QLSC_APPROVE = input.QlscApprove?.Trim(),
            CHR_NOTE = input.Note?.Trim(),
            CHR_GA_CONFIRM = wasRejected ? null : current.CHR_GA_CONFIRM,
        };

        if (IsCancelRequest(requestTypeName))
        {
            entity.DTM_CANCEL_DATE = input.UseOrCancelDate;
            entity.DTM_START_DATE = null;
        }
        else
        {
            entity.DTM_START_DATE = input.UseOrCancelDate;
            entity.DTM_CANCEL_DATE = null;
        }

        var resp = await _tmBusRequestService.UpdateAsync(entity);
        if (resp.Success && wasRejected)
        {
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_BUS_REQEST_ID = entity.ID,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = (int)BusRequestStatus.PendingQlscApproval,
                BIT_ACTION = true,
                CHR_ACTION = "RESUBMIT_REQUEST",
                DTM_ACTION = DateTime.Now
            });

            await SendApprovalMailAsync(entity, input, requestTypeName);
            TempData["BusRequestSuccess"] = "Cập nhật đơn thành công và đã gửi phê duyệt lại cho QLSC.";
            TempData["BusRequestCelebrate"] = "1";
            return RedirectToIndex(input);
        }

        TempData[resp.Success ? "BusRequestSuccess" : "BusRequestError"] = resp.Success
            ? "Cập nhật đơn thành công."
            : (resp.Message ?? "Không thể cập nhật đơn.");
        if (resp.Success)
        {
            TempData["BusRequestCelebrate"] = "1";
        }

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
                new() { Field = "CHR_EMPLOYEE_ID", Operator = "=", Value = code }
            }
        });

        var local = localResp?.Data?.Data?.FirstOrDefault();
        string? employeeName = local?.NVCHR_EMPLOYEE_NAME;
        string? departmentCode = local?.CHR_CODE_SEC;
        string? address = local?.NVCHR_ADDRESS_DETAIL;

        if (string.IsNullOrWhiteSpace(employeeName) || string.IsNullOrWhiteSpace(departmentCode) || string.IsNullOrWhiteSpace(address))
        {
            var agent = await _employeeService.GetAgentEmployeeByEmployeeIdAsync(code);
            if (agent != null)
            {
                employeeName ??= agent.CHR_EMPLOYEE_NAME;
                departmentCode ??= agent.CHR_SEC_CODE ?? agent.CHR_SECTION;
                address ??= agent.CHR_TEMP_ADDRESS ?? agent.CHR_PERMANENT_ADDRESS;
            }
        }

        return Json(new
        {
            success = true,
            data = new
            {
                employeeName = employeeName ?? string.Empty,
                departmentCode = departmentCode ?? string.Empty,
                address = address ?? string.Empty
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? department,
        string? departments,
        string? employeeCode,
        string? employeeName,
        string? position,
        string? address,
        int? requestTypeId,
        string? requestTypeIds,
        string? routeKeyword,
        int? busStopId,
        string? busStopIds,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? note,
        int? status)
    {
        var result = await Index(department, departments, employeeCode, employeeName, position, address, requestTypeId, requestTypeIds, routeKeyword, busStopId, busStopIds, dateFrom, dateTo, note, status, 1, 100000);
        if (result is not ViewResult vr || vr.Model is not BusRequestPageViewModel vm)
        {
            return RedirectToAction(nameof(Index));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BusRequests");
        var headers = new[] { "STT", "Phòng ban", "Mã NV", "Họ tên", "Chức vụ", "Địa chỉ", "Loại đơn", "Loại xe", "Khu vực", "Tuyến xe", "Điểm đón", "Ngày sử dụng/Hủy xe", "Chọn QLSC phê duyệt", "Ghi chú" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(1, i + 1).Value = headers[i];

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#166fe5");

        var requestTypeSheet = workbook.Worksheets.Add("_ReqTypeLookup");
        for (var i = 0; i < vm.RequestTypes.Count; i++) requestTypeSheet.Cell(i + 1, 1).Value = vm.RequestTypes[i].NVCHR_REQUEST_TYPE ?? string.Empty;
        requestTypeSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var vehicleTypeSheet = workbook.Worksheets.Add("_VehicleTypeLookup");
        var vehicleTypes = vm.VehicleAreas.Keys.ToList();
        for (var i = 0; i < vehicleTypes.Count; i++) vehicleTypeSheet.Cell(i + 1, 1).Value = vehicleTypes[i];
        vehicleTypeSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var routeSheet = workbook.Worksheets.Add("_RouteAssignLookup");
        for (var i = 0; i < vm.RouteAssignOptions.Count; i++) routeSheet.Cell(i + 1, 1).Value = vm.RouteAssignOptions[i].DisplayName ?? string.Empty;
        routeSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var stopSheet = workbook.Worksheets.Add("_BusStopLookup");
        for (var i = 0; i < vm.BusStops.Count; i++) stopSheet.Cell(i + 1, 1).Value = vm.BusStops[i].NVCHR_STOP_NAME ?? string.Empty;
        stopSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var qlscSheet = workbook.Worksheets.Add("_QlscLookup");
        for (var i = 0; i < vm.QlscUsers.Count; i++) qlscSheet.Cell(i + 1, 1).Value = vm.QlscUsers[i].CHR_ADID ?? string.Empty;
        qlscSheet.Visibility = XLWorksheetVisibility.VeryHidden;

        var row = 2;
        var stt = 1;
        foreach (var item in vm.Requests)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.DepartmentName;
            sheet.Cell(row, 3).Value = item.EmployeeCode;
            sheet.Cell(row, 4).Value = item.EmployeeName;
            sheet.Cell(row, 5).Value = item.Position;
            sheet.Cell(row, 6).Value = item.Address;
            sheet.Cell(row, 7).Value = item.RequestTypeName;
            sheet.Cell(row, 8).Value = ResolveVehicleType(item.RouteDisplay);
            sheet.Cell(row, 9).Value = ResolveArea(item.RouteDisplay);
            sheet.Cell(row, 10).Value = item.RouteDisplay;
            sheet.Cell(row, 11).Value = item.BusStopDisplay;
            sheet.Cell(row, 12).Value = item.UseOrCancelDate?.ToString("dd/MM/yyyy") ?? string.Empty;
            sheet.Cell(row, 13).Value = item.QlscApprove;
            sheet.Cell(row, 14).Value = item.Note;
            row++; stt++;
        }

        var validationEndRow = Math.Max(row + 100, 200);
        for (var r = 2; r <= validationEndRow; r++)
        {
            if (vm.RequestTypes.Count > 0) sheet.Range(r, 7, r, 7).SetDataValidation().List(requestTypeSheet.Range(1, 1, vm.RequestTypes.Count, 1));
            if (vehicleTypes.Count > 0) sheet.Range(r, 8, r, 8).SetDataValidation().List(vehicleTypeSheet.Range(1, 1, vehicleTypes.Count, 1));
            if (vm.RouteAssignOptions.Count > 0) sheet.Range(r, 10, r, 10).SetDataValidation().List(routeSheet.Range(1, 1, vm.RouteAssignOptions.Count, 1));
            if (vm.BusStops.Count > 0) sheet.Range(r, 11, r, 11).SetDataValidation().List(stopSheet.Range(1, 1, vm.BusStops.Count, 1));
            if (vm.QlscUsers.Count > 0) sheet.Range(r, 13, r, 13).SetDataValidation().List(qlscSheet.Range(1, 1, vm.QlscUsers.Count, 1));
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"DangKyHuyXe_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    public async Task<IActionResult> Import(
        IFormFile? importFile,
        string? department,
        string? departments,
        string? employeeCode,
        string? employeeName,
        string? position,
        string? address,
        int? requestTypeId,
        string? requestTypeIds,
        string? routeKeyword,
        int? busStopId,
        string? busStopIds,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? note,
        int page = 1,
        int pageSize = 10)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["BusRequestError"] = "Vui lòng chọn file để import.";
            return RedirectToAction(nameof(Index), new { department, departments, employeeCode, employeeName, position, address, requestTypeId, requestTypeIds, routeKeyword, busStopId, busStopIds, dateFrom, dateTo, note, page, pageSize });
        }

        var canManageAll = await CanManageAllAsync();
        if (!canManageAll)
        {
            TempData["BusRequestError"] = "Bạn không có quyền import dữ liệu.";
            return RedirectToAction(nameof(Index), new { department, departments, employeeCode, employeeName, position, address, requestTypeId, requestTypeIds, routeKeyword, busStopId, busStopIds, dateFrom, dateTo, note, page, pageSize });
        }

        var actor = GetCurrentActor();
        var success = 0;
        var failed = 0;

        var requestTypesResp = await _tmRequestTypeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_REQUEST_TYPE.INT_GROUP), Operator = "=", Value = BusRequestTypeGroup }
            }
        });
        var routeAssignResp = await _tmBusRouteAssignService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var stopResp = await _tmBusStopService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });

        var requestTypes = requestTypesResp?.Data?.Data?.ToList() ?? new List<TM_REQUEST_TYPEDTO>();
        var routeAssigns = routeAssignResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_ASSIGNDTO>();
        var stops = stopResp?.Data?.Data?.ToList() ?? new List<TM_BUS_STOPDTO>();

        var routeLookup = await BuildRouteDisplayMap(routeAssigns);

        async Task<bool> TryImportRowAsync(List<string> cells)
        {
            if (cells.Count < 11) return false;

            var offset = 0;
            if (int.TryParse(cells.ElementAtOrDefault(0)?.Trim(), out _)) offset = 1;

            var empCode = cells.ElementAtOrDefault(offset + 1)?.Trim();
            var requestTypeText = cells.ElementAtOrDefault(offset + 5)?.Trim();
            var routeDisplay = cells.ElementAtOrDefault(offset + 8)?.Trim();
            var stopName = cells.ElementAtOrDefault(offset + 9)?.Trim();
            var dateText = cells.ElementAtOrDefault(offset + 10)?.Trim();
            var qlscApprove = cells.ElementAtOrDefault(offset + 11)?.Trim();
            var rowNote = cells.ElementAtOrDefault(offset + 12)?.Trim();

            if (string.IsNullOrWhiteSpace(empCode) || string.IsNullOrWhiteSpace(requestTypeText) || string.IsNullOrWhiteSpace(routeDisplay) || string.IsNullOrWhiteSpace(stopName))
            {
                return false;
            }

            var requestType = requestTypes.FirstOrDefault(x => string.Equals(x.NVCHR_REQUEST_TYPE, requestTypeText, StringComparison.OrdinalIgnoreCase));
            var routeItem = routeLookup.FirstOrDefault(x => string.Equals(x.DisplayName, routeDisplay, StringComparison.OrdinalIgnoreCase));
            var stop = stops.FirstOrDefault(x => string.Equals(x.NVCHR_STOP_NAME, stopName, StringComparison.OrdinalIgnoreCase));

            if (requestType == null || routeItem == null || stop == null) return false;

            DateTime? useDate = null;
            if (DateTime.TryParse(dateText, out var parsedDate)) useDate = parsedDate;
            if (!useDate.HasValue) useDate = DateTime.Now;

            var entity = new TM_BUS_REQUEST
            {
                CHR_EMPLOYEE_ID = empCode,
                INT_REQUEST_TYPE = requestType.ID,
                INT_BUS_ROUTE_ASSIGN_ID = routeItem.Id,
                INT_BUS_STOP_ID = stop.ID,
                INT_STATUS = (int)BusRequestStatus.PendingQlscApproval,
                CHR_QLSC_APPROVE = qlscApprove,
                CHR_NOTE = rowNote,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            };

            if (IsCancelRequest(requestType.NVCHR_REQUEST_TYPE)) entity.DTM_CANCEL_DATE = useDate; else entity.DTM_START_DATE = useDate;

            var resp = await _tmBusRequestService.AddAsyncV2(entity);
            return resp.Success;
        }

        var ext = Path.GetExtension(importFile.FileName)?.ToLowerInvariant();
        if (ext == ".xlsx")
        {
            using var stream = importFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault(x => !x.Name.StartsWith("_"));
            if (sheet == null)
            {
                TempData["BusRequestError"] = "Không đọc được sheet dữ liệu trong file Excel.";
                return RedirectToAction(nameof(Index), new { department, departments, employeeCode, employeeName, position, address, requestTypeId, requestTypeIds, routeKeyword, busStopId, busStopIds, dateFrom, dateTo, note, page, pageSize });
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
            for (var r = 2; r <= lastRow; r++)
            {
                var cells = new List<string>();
                for (var c = 1; c <= 14; c++) cells.Add(sheet.Cell(r, c).GetString());
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
                if (row == 1 && (line.Contains("Mã NV", StringComparison.OrdinalIgnoreCase) || line.Contains("EmployeeCode", StringComparison.OrdinalIgnoreCase))) continue;
                var cells = ParseCsvLine(line);
                if (await TryImportRowAsync(cells)) success++; else failed++;
            }
        }

        TempData[failed == 0 ? "BusRequestSuccess" : "BusRequestError"] = failed == 0
            ? $"Import thành công {success} đơn."
            : $"Import hoàn tất: Thành công {success}, lỗi {failed}.";

        return RedirectToAction(nameof(Index), new { department, departments, employeeCode, employeeName, position, address, requestTypeId, requestTypeIds, routeKeyword, busStopId, busStopIds, dateFrom, dateTo, note, page, pageSize });
    }

    [HttpGet]
    public async Task<IActionResult> GetRouteAssignOptions(string? vehicleType, string? area)
    {
        var assignResp = await _tmBusRouteAssignService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var assigns = assignResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_ASSIGNDTO>();
        var routeOptions = await BuildRouteDisplayMap(assigns);

        if (!string.IsNullOrWhiteSpace(vehicleType))
        {
            routeOptions = routeOptions
                .Where(x => string.Equals(x.VehicleType, vehicleType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(area))
        {
            routeOptions = routeOptions
                .Where(x => string.Equals(x.Area, area, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return Json(routeOptions.Select(x => new { id = x.Id, displayName = x.DisplayName, busStopId = x.BusStopId }));
    }

    [HttpGet]
    public async Task<IActionResult> GetStopsByAssign(int assignId)
    {
        var assignResp = await _tmBusRouteAssignService.GetByIdAsync(assignId);
        var assign = assignResp.Data;
        if (assign == null || !assign.INT_ROUTE_PICKUP_ID.HasValue)
        {
            return Json(new List<object>());
        }

        var routeStopResp = await _tmBusRouteService.GetByIdAsync(assign.INT_ROUTE_PICKUP_ID.Value);
        if (!routeStopResp.Success || routeStopResp.Data == null)
        {
            return Json(new List<object>());
        }

        var rsResp = await _tmBusRouteAssignService.GetByIdAsync(assignId);
        if (!rsResp.Success)
        {
            return Json(new List<object>());
        }

        var routeStopsResp = await _tmBusStopService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var stops = routeStopsResp?.Data?.Data?.ToList() ?? new List<TM_BUS_STOPDTO>();

        return Json(stops.Select(x => new { id = x.ID, name = x.NVCHR_STOP_NAME }));
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveRouteCapacities()
    {
        var assignResp = await _tmBusRouteAssignService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_ROUTE_ASSIGN.BIT_IS_ACTIVE), Operator = "=", Value = true }
            }
        });

        var assigns = assignResp?.Data?.Data?.ToList() ?? new List<TM_BUS_ROUTE_ASSIGNDTO>();
        if (!assigns.Any())
        {
            return Json(Array.Empty<object>());
        }

        var routeLookups = await BuildRouteDisplayMap(assigns);
        var routeMap = routeLookups.ToDictionary(x => x.Id, x => x.DisplayName ?? string.Empty);

        var result = assigns
            .Select(x =>
            {
                var capacity = Math.Max(0, x.INT_CAPACITY ?? 0);
                var currentUser = Math.Max(0, x.INT_CURRENT_USER ?? 0);
                var percentRaw = capacity > 0 ? (currentUser * 100.0 / capacity) : 0;
                var percentBar = Math.Min(100, Math.Max(0, percentRaw));

                return new
                {
                    id = x.ID,
                    routeDisplay = routeMap.TryGetValue(x.ID, out var display) ? display : string.Empty,
                    currentUser,
                    capacity,
                    percentRaw,
                    percentBar,
                    level = ResolveCapacityLevel(percentRaw, capacity)
                };
            })
            .OrderByDescending(x => x.percentRaw)
            .ThenBy(x => x.routeDisplay)
            .ToList();

        return Json(result);
    }

    private async Task<List<BusRouteAssignLookupItem>> BuildRouteDisplayMap(List<TM_BUS_ROUTE_ASSIGNDTO> assigns)
    {
        var busesResp = await _tmBusService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var routesResp = await _tmBusRouteService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });

        var buses = (busesResp?.Data?.Data ?? new List<TM_BUSDTO>()).ToDictionary(x => x.ID, x => x);
        var routes = (routesResp?.Data?.Data ?? new List<TM_BUS_ROUTEDTO>()).ToDictionary(x => x.ID, x => x);

        var result = new List<BusRouteAssignLookupItem>();
        foreach (var a in assigns)
        {
            buses.TryGetValue(a.INT_BUS_ID ?? 0, out var bus);
            routes.TryGetValue(a.INT_ROUTE_PICKUP_ID ?? 0, out var route);
            var routeName = route?.NVCHR_ROUTE_NAME;
            if (string.IsNullOrWhiteSpace(routeName)) continue;

            var vehicleType = string.Equals(a.CHR_BRA_TYPE, "Worker", StringComparison.OrdinalIgnoreCase) ? "Công nhân" : "Nhân viên";
            var area = ResolveArea(routeName);

            result.Add(new BusRouteAssignLookupItem
            {
                Id = a.ID,
                VehicleType = vehicleType,
                Area = area,
                DisplayName = $"{bus?.CHR_BUS_CODE} - {routeName}",
                BusStopId = null
            });
        }

        return result
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.DisplayName)
            .ToList();
    }

    private static List<FilterOption> BuildBusRequestFilters(
        List<int> requestTypeIds,
        List<int> busStopIds,
        string? employeeCode,
        string? note,
        DateTime? dateFrom,
        DateTime? dateTo)
    {
        var filters = new List<FilterOption>();

        if (requestTypeIds.Any())
        {
            for (var i = 0; i < requestTypeIds.Count; i++)
            {
                filters.Add(new FilterOption
                {
                    Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.INT_REQUEST_TYPE)}",
                    Operator = "=",
                    Value = requestTypeIds[i],
                    LogicType = i == 0 ? null : "OR"
                });
            }
        }

        if (busStopIds.Any())
        {
            for (var i = 0; i < busStopIds.Count; i++)
            {
                filters.Add(new FilterOption
                {
                    Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.INT_BUS_STOP_ID)}",
                    Operator = "=",
                    Value = busStopIds[i],
                    LogicType = i == 0 ? null : "OR"
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            filters.Add(new FilterOption
            {
                Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.CHR_EMPLOYEE_ID)}",
                Operator = "LIKE",
                Value = $"%{employeeCode.Trim()}%"
            });
        }

        if (!string.IsNullOrWhiteSpace(note))
        {
            filters.Add(new FilterOption
            {
                Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.CHR_NOTE)}",
                Operator = "LIKE",
                Value = $"%{note.Trim()}%"
            });
        }

        if (dateFrom.HasValue)
        {
            filters.Add(new FilterOption
            {
                Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.DTM_START_DATE)}",
                Operator = ">=",
                Value = dateFrom.Value.Date
            });
        }

        if (dateTo.HasValue)
        {
            filters.Add(new FilterOption
            {
                Field = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.DTM_START_DATE)}",
                Operator = "<=",
                Value = dateTo.Value.Date.AddDays(1).AddTicks(-1)
            });
        }

        return filters;
    }

    private static List<JoinOption> BuildBusRequestJoinOptions(
        List<string> departments,
        string? employeeName,
        string? position,
        string? address,
        string? routeKeyword,
        string? forcedDepartment)
    {
        var joins = new List<JoinOption>();

        var needEmployeeJoin = departments.Any()
            || !string.IsNullOrWhiteSpace(employeeName)
            || !string.IsNullOrWhiteSpace(position)
            || !string.IsNullOrWhiteSpace(address)
            || !string.IsNullOrWhiteSpace(forcedDepartment);

        if (needEmployeeJoin)
        {
            joins.Add(new JoinOption
            {
                JoinType = "LEFT JOIN",
                Table = nameof(TM_EMPLOYEE),
                Alias = "e",
                OnCondition = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.CHR_EMPLOYEE_ID)} = e.{nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID)}"
            });
        }

        if (!string.IsNullOrWhiteSpace(routeKeyword))
        {
            joins.Add(new JoinOption
            {
                JoinType = "LEFT JOIN",
                Table = nameof(TM_BUS_ROUTE_ASSIGN),
                Alias = "bra",
                OnCondition = $"{nameof(TM_BUS_REQUEST)}.{nameof(TM_BUS_REQUEST.INT_BUS_ROUTE_ASSIGN_ID)} = bra.ID"
            });

            joins.Add(new JoinOption
            {
                JoinType = "LEFT JOIN",
                Table = nameof(TM_BUS_ROUTE),
                Alias = "r",
                OnCondition = $"bra.{nameof(TM_BUS_ROUTE_ASSIGN.INT_ROUTE_PICKUP_ID)} = r.ID"
            });
        }

        return joins;
    }

    private static List<BusRequestRowViewModel> ApplyFilters(
        List<BusRequestRowViewModel> rows,
        List<string> departments,
        string? employeeCode,
        string? employeeName,
        string? position,
        string? address,
        List<int> requestTypeIds,
        string? routeKeyword,
        List<int> busStopIds,
        DateTime? dateFrom,
        DateTime? dateTo,
        string? note)
    {
        var query = rows.AsEnumerable();

        if (departments.Any()) query = query.Where(x => departments.Any(d => string.Equals(x.DepartmentCode, d, StringComparison.OrdinalIgnoreCase) || string.Equals(x.DepartmentName, d, StringComparison.OrdinalIgnoreCase)));
        if (!string.IsNullOrWhiteSpace(employeeCode)) query = query.Where(x => ContainsText(x.EmployeeCode, employeeCode));
        if (!string.IsNullOrWhiteSpace(employeeName)) query = query.Where(x => ContainsText(x.EmployeeName, employeeName));
        if (!string.IsNullOrWhiteSpace(position)) query = query.Where(x => ContainsText(x.Position, position));
        if (!string.IsNullOrWhiteSpace(address)) query = query.Where(x => ContainsText(x.Address, address));
        if (requestTypeIds.Any()) query = query.Where(x => x.RequestTypeId.HasValue && requestTypeIds.Contains(x.RequestTypeId.Value));
        if (!string.IsNullOrWhiteSpace(routeKeyword)) query = query.Where(x => ContainsText(x.RouteDisplay, routeKeyword));
        if (busStopIds.Any()) query = query.Where(x => x.BusStopId.HasValue && busStopIds.Contains(x.BusStopId.Value));
        if (dateFrom.HasValue) query = query.Where(x => x.UseOrCancelDate.HasValue && x.UseOrCancelDate.Value.Date >= dateFrom.Value.Date);
        if (dateTo.HasValue) query = query.Where(x => x.UseOrCancelDate.HasValue && x.UseOrCancelDate.Value.Date <= dateTo.Value.Date);
        if (!string.IsNullOrWhiteSpace(note)) query = query.Where(x => ContainsText(x.Note, note));

        return query.OrderByDescending(x => x.Id).ToList();
    }

    private static bool ContainsText(string? source, string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return true;
        return !string.IsNullOrWhiteSpace(source)
            && source.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildAddress(TM_EMPLOYEEDTO? emp)
    {
        if (emp == null) return string.Empty;
        var parts = new[] { emp.NVCHR_ADDRESS_DETAIL, emp.NVCHR_WARD, emp.NVCHR_CITY }
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(", ", parts);
    }

    private static bool IsCancelRequest(string? requestTypeName)
    {
        return !string.IsNullOrWhiteSpace(requestTypeName)
            && (requestTypeName.Contains("hủy", StringComparison.OrdinalIgnoreCase)
                || requestTypeName.Contains("huy", StringComparison.OrdinalIgnoreCase)
                || requestTypeName.Contains("cancel", StringComparison.OrdinalIgnoreCase));
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

    private string GetCurrentEmployeeCode()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "employee_code")?.Value
            ?? string.Empty;
    }

    private string GetCurrentDepartmentCode()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "sec_code")?.Value
            ?? string.Empty;
    }

    private string GetCurrentActor()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value
            ?? User?.Claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value
            ?? "system";
    }

    private static string ResolveVehicleType(string? routeDisplay)
    {
        if (string.IsNullOrWhiteSpace(routeDisplay)) return string.Empty;
        if (routeDisplay.Contains("CN", StringComparison.OrdinalIgnoreCase)
            || routeDisplay.Contains("công nhân", StringComparison.OrdinalIgnoreCase))
        {
            return "Công nhân";
        }

        return "Nhân viên";
    }

    private static string ResolveArea(string? routeName)
    {
        if (string.IsNullOrWhiteSpace(routeName)) return string.Empty;
        foreach (var area in VehicleAreaConfig.SelectMany(x => x.Value).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (routeName.Contains(area, StringComparison.OrdinalIgnoreCase)) return area;
        }
        return string.Empty;
    }

    private static string ResolveCapacityLevel(double percent, int capacity)
    {
        if (capacity <= 0) return "secondary";
        if (percent >= 100) return "danger";
        if (percent >= 80) return "warning";
        return "success";
    }

    private IActionResult RedirectToIndex(BusRequestUpsertInputModel input)
    {
        return RedirectToAction(nameof(Index), new
        {
            department = input.SearchDepartment,
            departments = input.SearchDepartments,
            employeeCode = input.SearchEmployeeCode,
            employeeName = input.SearchEmployeeName,
            position = input.SearchPosition,
            address = input.SearchAddress,
            requestTypeId = input.SearchRequestTypeId,
            requestTypeIds = input.SearchRequestTypeIds,
            routeKeyword = input.SearchRoute,
            busStopId = input.SearchBusStopId,
            busStopIds = input.SearchBusStopIds,
            dateFrom = input.SearchDateFrom,
            dateTo = input.SearchDateTo,
            note = input.SearchNote,
            status = input.SearchStatus,
            page = input.Page,
            pageSize = input.PageSize
        });
    }

    private static List<string> ParseCsvStrings(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<int> ParseCsvInts(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<int>();
        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var parsed) ? parsed : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToList();
    }

    private async Task<bool> HasDuplicateRequestAsync(string? employeeCode, int requestTypeId, int? excludeRequestId)
    {
        if (string.IsNullOrWhiteSpace(employeeCode) || requestTypeId <= 0) return false;

        var duplicateResp = await _tmBusRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_REQUEST.CHR_EMPLOYEE_ID), Operator = "=", Value = employeeCode.Trim() },
                new() { Field = nameof(TM_BUS_REQUEST.INT_REQUEST_TYPE), Operator = "=", Value = requestTypeId }
            }
        });

        var requests = duplicateResp?.Data?.Data ?? Enumerable.Empty<TM_BUS_REQUESTDTO>();

        return requests.Any(x =>
            (!excludeRequestId.HasValue || x.ID != excludeRequestId.Value)
            && x.INT_STATUS != (int)BusRequestStatus.QlscRejected
            && x.INT_STATUS != (int)BusRequestStatus.GaRejected);
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

    private async Task SendApprovalMailAsync(TM_BUS_REQUEST entity, BusRequestUpsertInputModel input, string? requestTypeName)
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
            if (qlsc == null || string.IsNullOrWhiteSpace(qlsc.CHR_EMPLOYEE_ID)) return;

            var toEmail = qlsc.CHR_ADID + "@brothergroup.net";
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
            var mailCode = "BUS_REQUEST_APPROVAL";

            var mailTemplateResp = await _tmMasterMailService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_MASTER_MAIL.CHR_MAIL_CODE), Operator = "=", Value = mailCode }
                }
            });
            var mailTemplate = mailTemplateResp?.Data?.Data?.FirstOrDefault();

            var approveLink = BuildBusRequestApproveLink();
            var sectionName = await ResolveSectionNameAsync(input.DepartmentCode);

            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{To}}"] = qlsc.NVCHR_NAME ?? qlsc.CHR_ADID ?? string.Empty,
                ["{{ToEmail}}"] = toEmail,
                ["{{RequestType}}"] = requestTypeName ?? string.Empty,
                ["{{EmployeeCode}}"] = entity.CHR_EMPLOYEE_ID ?? input.EmployeeCode ?? string.Empty,
                ["{{EmployeeName}}"] = input.EmployeeName ?? string.Empty,
                ["{{EmployeeSection}}"] = sectionName,
                ["{{ApproveLink}}"] = approveLink
            };

            var title = ReplaceTemplateTokens(mailTemplate?.CHR_SUBJECT, tokens);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "[Bus] Yêu cầu đăng ký/hủy xe đi làm cần QLSC phê duyệt";
            }

            var defaultBodyTemplate = """
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="utf-8" />
                    <title>Yêu cầu xác nhận xe công ty</title>
                </head>
                <body style="margin:0;padding:0;background-color:#ffffff;font-family:'Times New Roman', Times, serif;">
                    <div style="width:100%;max-width:800px;margin:0 auto;">
                        <p style="color:red;font-size:24px;font-weight:bold;margin:0 0 30px 0;">
                            Đây là mail tự động, vui lòng không Reply lại mail!
                        </p>
                        <p style="font-size:20px;margin:0 0 30px 0;">Gửi anh/chị {{To}},</p>
                        <p style="font-size:20px;margin:0 0 30px 0;">Phía dưới là thông tin yêu cầu xe Công ty cần anh chị xác nhận :</p>
                        <p style="font-size:20px;line-height:1.5;margin:0 0 30px 0;">
                            Loại đơn : {{RequestType}}<br />
                            CNV : {{EmployeeCode}} - {{EmployeeName}} ({{EmployeeSection}})
                        </p>
                        <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng vào hệ thống xác nhận đơn cho người yêu cầu !</p>
                        <p style="font-size:20px;margin:0 0 10px 0;">
                            Hoặc truy cập :
                            <a href="{{ApproveLink}}">LINK</a>
                        </p>
                        <p style="font-size:20px;margin:0 0 10px 0;">Xin cảm ơn!</p>
                        <p style="font-size:20px;font-style:italic;margin:0;">Hệ thống quản lý xe Công ty-BIVN</p>
                    </div>
                </body>
                </html>
                """;

            var bodyTemplate = string.IsNullOrWhiteSpace(mailTemplate?.CHR_CONTENT)
                ? defaultBodyTemplate
                : mailTemplate.CHR_CONTENT ?? string.Empty;
            var body = ReplaceTemplateTokens(bodyTemplate, tokens);

            var fromEmail = ReplaceTemplateTokens(mailTemplate?.CHR_FROM, tokens);
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = "zzpsvvmg@brother-bivn.com.vn";
            }

            var toFromTemplate = ReplaceTemplateTokens(mailTemplate?.CHR_TO, tokens);
            var to = CombineEmails(toEmail, toFromTemplate);
            var cc = ReplaceTemplateTokens(mailTemplate?.CHR_CC, tokens);
            var bcc = ReplaceTemplateTokens(mailTemplate?.CHR_BCC, tokens);
            var priority = mailTemplate?.BIT_HIGHT == true ? 1 : 0;

            _tmMasterMailService.sendEmailNotify(title, fromEmail, to, cc ?? string.Empty, bcc ?? string.Empty, body, priority);
        }
        catch
        {
            // ignore mail exception to avoid blocking request creation
        }
    }

    private async Task<string> ResolveSectionNameAsync(string? sectionCode)
    {
        if (string.IsNullOrWhiteSpace(sectionCode)) return string.Empty;

        var sectionResp = await _sectionService.GetAllAsync();
        var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
        var match = sections.FirstOrDefault(x =>
            string.Equals(x.CHR_CODE?.Trim(), sectionCode.Trim(), StringComparison.OrdinalIgnoreCase));

        return match?.CHR_NAME ?? match?.CHR_SECTION_NAME ?? sectionCode;
    }

    private string BuildBusRequestApproveLink()
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var baseUrl = configuration?["SystemLinks:BusManagementWebApp"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = Request?.Host.HasValue == true
                ? $"{Request.Scheme}://{Request.Host.Value}"
                : string.Empty;
        }

        var relativePath = Url.Action("Index", "BusRequestApproval", new { stage = "qlsc" }) ?? "/BusRequestApproval?stage=qlsc";
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
