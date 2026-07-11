using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.BusRequest;
using Microsoft.AspNetCore.Mvc;
using BusManagement.Model.Common.Enum;
using Microsoft.Extensions.Configuration;
using BusManagementWebApp.Helpers;
using System.Globalization;
using System.Text;

namespace BusManagement.Controllers;

public class BusRequestApprovalController : Controller
{
    private const int BusRequestTypeGroup = 1;

    private readonly ITmBusRequestService _tmBusRequestService;
    private readonly ITmRequestTypeService _tmRequestTypeService;
    private readonly ITmBusRouteAssignService _tmBusRouteAssignService;
    private readonly ITmBusService _tmBusService;
    private readonly ITmBusRouteService _tmBusRouteService;
    private readonly ITmBusStopService _tmBusStopService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly ITmEmployeeRouteService _tmEmployeeRouteService;
    private readonly ITmEmployeeRouteHistoryService _tmEmployeeRouteHistoryService;
    private readonly ITmCardPrintQueueService _tmCardPrintQueueService;
    private readonly IEmployeeService _employeeService;
    private readonly ISectionService _sectionService;
    private readonly ITmMasterMailService _tmMasterMailService;
    private readonly ITmApprovalHistoryService _tmApprovalHistoryService;
    private readonly IActiveDirectoryService _activeDirectoryService;

    public BusRequestApprovalController(
        ITmBusRequestService tmBusRequestService,
        ITmRequestTypeService tmRequestTypeService,
        ITmBusRouteAssignService tmBusRouteAssignService,
        ITmBusService tmBusService,
        ITmBusRouteService tmBusRouteService,
        ITmBusStopService tmBusStopService,
        ITmEmployeeService tmEmployeeService,
        ITmEmployeeRouteService tmEmployeeRouteService,
        ITmEmployeeRouteHistoryService tmEmployeeRouteHistoryService,
        ITmCardPrintQueueService tmCardPrintQueueService,
        IEmployeeService employeeService,
        ISectionService sectionService,
        ITmMasterMailService tmMasterMailService,
        ITmApprovalHistoryService tmApprovalHistoryService,
        IActiveDirectoryService activeDirectoryService)
    {
        _tmBusRequestService = tmBusRequestService;
        _tmRequestTypeService = tmRequestTypeService;
        _tmBusRouteAssignService = tmBusRouteAssignService;
        _tmBusService = tmBusService;
        _tmBusRouteService = tmBusRouteService;
        _tmBusStopService = tmBusStopService;
        _tmEmployeeService = tmEmployeeService;
        _tmEmployeeRouteService = tmEmployeeRouteService;
        _tmEmployeeRouteHistoryService = tmEmployeeRouteHistoryService;
        _tmCardPrintQueueService = tmCardPrintQueueService;
        _employeeService = employeeService;
        _sectionService = sectionService;
        _tmMasterMailService = tmMasterMailService;
        _tmApprovalHistoryService = tmApprovalHistoryService;
        _activeDirectoryService = activeDirectoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? stage)
    {
        var normalizedStage = NormalizeStage(stage);
        var stageTitle = normalizedStage == "ga"
            ? "Nhóm quản lý xe Công ty xác nhận"
            : "QLSC phòng ban duyệt đơn";

        var vm = new BusRequestApprovalPageViewModel
        {
            Stage = normalizedStage,
            StageTitle = stageTitle,
            CanProcess = true
        };

        var currentAdid = GetCurrentAdid();
        if (string.IsNullOrWhiteSpace(currentAdid))
        {
            vm.CanProcess = false;
            vm.AccessDeniedMessage = "Không xác định được tài khoản đăng nhập.";
            return View(vm);
        }

        if (normalizedStage == "ga")
        {
            var canGaProcess = await CanCurrentUserProcessGaAsync();
            if (!canGaProcess)
            {
                vm.CanProcess = false;
                vm.AccessDeniedMessage = "Bạn không thuộc nhóm mail GA được phép xác nhận.";
                return View(vm);
            }
        }

        vm.Requests = await LoadPendingRowsAsync(normalizedStage, currentAdid);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> History(int id, bool modal = false)
    {
        if (id <= 0)
        {
            return RedirectToAction(nameof(Index), new { stage = "qlsc" });
        }

        var vm = await BuildHistoryViewModelAsync(id);
        if (vm == null)
        {
            TempData["BusRequestApprovalError"] = "Không tìm thấy đơn yêu cầu.";
            return RedirectToAction(nameof(Index), new { stage = "qlsc" });
        }

        if (modal)
        {
            return PartialView("_HistoryContent", vm);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(BusRequestApprovalActionInputModel input)
    {
        var normalizedStage = NormalizeStage(input.Stage);
        var ids = ParseIds(input.SelectedIds);
        if (!ids.Any())
        {
            TempData["BusRequestApprovalError"] = "Vui lòng chọn ít nhất 1 bản ghi.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var currentAdid = GetCurrentAdid();
        if (string.IsNullOrWhiteSpace(currentAdid))
        {
            TempData["BusRequestApprovalError"] = "Không xác định được tài khoản đăng nhập.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        if (normalizedStage == "ga" && !await CanCurrentUserProcessGaAsync())
        {
            TempData["BusRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xác nhận.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var requests = await GetProcessableRequestsAsync(ids, normalizedStage, currentAdid);
        if (!requests.Any())
        {
            TempData["BusRequestApprovalError"] = "Không có bản ghi hợp lệ để xác nhận.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var successIds = new List<int>();
        var requestTypeMap = await LoadRequestTypeMapAsync(requests);
        foreach (var req in requests)
        {
            var updateEntity = BuildUpdateEntity(req, currentAdid);
            if (normalizedStage == "ga")
            {
                updateEntity.INT_STATUS = (int)BusRequestStatus.GaApproved;
                updateEntity.CHR_GA_CONFIRM = currentAdid;
            }
            else
            {
                updateEntity.INT_STATUS = (int)BusRequestStatus.QlscApproved;
            }

            var updateResp = await _tmBusRequestService.UpdateAsync(updateEntity);
            if (!updateResp.Success)
            {
                continue;
            }

            successIds.Add(req.ID);

            if (normalizedStage == "ga")
            {
                requestTypeMap.TryGetValue(req.INT_REQUEST_TYPE ?? 0, out var requestTypeName);
                await ApplyEmployeeBusUsageByCompletedRequestAsync(req, requestTypeName, currentAdid);
            }

            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_BUS_REQEST_ID = req.ID,
                CHR_CONFIRMER_APPROVER = currentAdid,
                INT_ACTION = updateEntity.INT_STATUS,
                BIT_ACTION = true,
                CHR_ACTION = normalizedStage == "ga" ? "GA_APPROVE" : "QLSC_APPROVE",
                DTM_ACTION = DateTime.Now
            });
        }

        if (!successIds.Any())
        {
            TempData["BusRequestApprovalError"] = "Không thể xác nhận các bản ghi đã chọn.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var successRequests = requests.Where(x => successIds.Contains(x.ID)).ToList();
        var successRows = await BuildRowsFromRequestsAsync(successRequests);

        if (normalizedStage == "qlsc")
        {
            await SendGaApprovalMailAsync(successRows);
        }
        else
        {
            await SendCompletedMailAsync(successRows);
        }

        TempData["BusRequestApprovalSuccess"] = $"Xác nhận thành công {successIds.Count} bản ghi.";
        TempData["BusRequestApprovalCelebrate"] = "1";
        return RedirectToAction(nameof(Index), new { stage = normalizedStage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(BusRequestApprovalActionInputModel input)
    {
        var normalizedStage = NormalizeStage(input.Stage);
        var ids = ParseIds(input.SelectedIds);
        if (!ids.Any())
        {
            TempData["BusRequestApprovalError"] = "Vui lòng chọn ít nhất 1 bản ghi.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        if (string.IsNullOrWhiteSpace(input.RejectReason))
        {
            TempData["BusRequestApprovalError"] = "Vui lòng nhập lý do từ chối.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var currentAdid = GetCurrentAdid();
        if (string.IsNullOrWhiteSpace(currentAdid))
        {
            TempData["BusRequestApprovalError"] = "Không xác định được tài khoản đăng nhập.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        if (normalizedStage == "ga" && !await CanCurrentUserProcessGaAsync())
        {
            TempData["BusRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xác nhận.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var requests = await GetProcessableRequestsAsync(ids, normalizedStage, currentAdid);
        if (!requests.Any())
        {
            TempData["BusRequestApprovalError"] = "Không có bản ghi hợp lệ để từ chối.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var successIds = new List<int>();
        foreach (var req in requests)
        {
            var updateEntity = BuildUpdateEntity(req, currentAdid);
            if (normalizedStage == "ga")
            {
                updateEntity.INT_STATUS = (int)BusRequestStatus.GaRejected;
                updateEntity.CHR_GA_CONFIRM = currentAdid;
            }
            else
            {
                updateEntity.INT_STATUS = (int)BusRequestStatus.QlscRejected;
            }

            var updateResp = await _tmBusRequestService.UpdateAsync(updateEntity);
            if (!updateResp.Success)
            {
                continue;
            }

            successIds.Add(req.ID);
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_BUS_REQEST_ID = req.ID,
                CHR_CONFIRMER_APPROVER = currentAdid,
                INT_ACTION = updateEntity.INT_STATUS,
                BIT_ACTION = false,
                CHR_ACTION = normalizedStage == "ga" ? "GA_REJECT" : "QLSC_REJECT",
                DTM_ACTION = DateTime.Now,
                NVCHR_REASON_REJECT = input.RejectReason?.Trim()
            });
        }

        if (!successIds.Any())
        {
            TempData["BusRequestApprovalError"] = "Không thể từ chối các bản ghi đã chọn.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var successRequests = requests.Where(x => successIds.Contains(x.ID)).ToList();
        var successRows = await BuildRowsFromRequestsAsync(successRequests);
        await SendRejectMailAsync(successRows, normalizedStage == "ga" ? "GA" : "QLSC", input.RejectReason?.Trim() ?? string.Empty);

        TempData["BusRequestApprovalSuccess"] = $"Từ chối thành công {successIds.Count} bản ghi.";
        return RedirectToAction(nameof(Index), new { stage = normalizedStage });
    }

    private async Task<List<TM_BUS_REQUESTDTO>> GetProcessableRequestsAsync(List<int> ids, string stage, string currentAdid)
    {
        var resp = await _tmBusRequestService.GetByIdsAsync(ids);
        var requests = resp?.Data?.ToList() ?? new List<TM_BUS_REQUESTDTO>();
        var allowedRequestTypeIds = await LoadRequestTypeIdsByGroupAsync(BusRequestTypeGroup);
        if (allowedRequestTypeIds.Any())
        {
            requests = requests
                .Where(x => x.INT_REQUEST_TYPE.HasValue && allowedRequestTypeIds.Contains(x.INT_REQUEST_TYPE.Value))
                .ToList();
        }

        return requests.Where(x =>
        {
            if (stage == "ga")
            {
                return x.INT_STATUS == (int)BusRequestStatus.QlscApproved;
            }

            return x.INT_STATUS == (int)BusRequestStatus.PendingQlscApproval
                && string.Equals(x.CHR_QLSC_APPROVE?.Trim(), currentAdid, StringComparison.OrdinalIgnoreCase);
        }).ToList();
    }

    private async Task<List<BusRequestApprovalRowViewModel>> LoadPendingRowsAsync(string stage, string currentAdid)
    {
        var filters = new List<FilterOption>();

        if (stage == "ga")
        {
            filters.Add(new FilterOption
            {
                Field = nameof(TM_BUS_REQUEST.INT_STATUS),
                Operator = "=",
                Value = (int)BusRequestStatus.QlscApproved
            });
        }
        else
        {
            filters.Add(new FilterOption
            {
                Field = nameof(TM_BUS_REQUEST.INT_STATUS),
                Operator = "=",
                Value = (int)BusRequestStatus.PendingQlscApproval
            });
            filters.Add(new FilterOption
            {
                Field = nameof(TM_BUS_REQUEST.CHR_QLSC_APPROVE),
                Operator = "=",
                Value = currentAdid
            });
        }

        var requestResp = await _tmBusRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new() { new SortOption { Field = nameof(TM_BUS_REQUEST.DTM_CREATE), SortDirection = "DESC" } }
        });

        var requests = requestResp?.Data?.Data?.ToList() ?? new List<TM_BUS_REQUESTDTO>();
        var allowedRequestTypeIds = await LoadRequestTypeIdsByGroupAsync(BusRequestTypeGroup);
        if (allowedRequestTypeIds.Any())
        {
            requests = requests
                .Where(x => x.INT_REQUEST_TYPE.HasValue && allowedRequestTypeIds.Contains(x.INT_REQUEST_TYPE.Value))
                .ToList();
        }
        return await BuildRowsFromRequestsAsync(requests);
    }

    private async Task<List<BusRequestApprovalRowViewModel>> BuildRowsFromRequestsAsync(List<TM_BUS_REQUESTDTO> requests)
    {
        var requestTypeIds = requests.Where(x => x.INT_REQUEST_TYPE.HasValue).Select(x => x.INT_REQUEST_TYPE!.Value).Distinct().ToList();
        var busStopIds = requests.Where(x => x.INT_BUS_STOP_ID.HasValue).Select(x => x.INT_BUS_STOP_ID!.Value).Distinct().ToList();
        var routeAssignIds = requests.Where(x => x.INT_BUS_ROUTE_ASSIGN_ID.HasValue).Select(x => x.INT_BUS_ROUTE_ASSIGN_ID!.Value).Distinct().ToList();
        var employeeCodes = requests.Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID)).Select(x => x.CHR_EMPLOYEE_ID!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var requestTypeMap = (requestTypeIds.Any()
            ? (await _tmRequestTypeService.GetByIdsAsync(requestTypeIds))?.Data ?? Enumerable.Empty<TM_REQUEST_TYPEDTO>()
            : Enumerable.Empty<TM_REQUEST_TYPEDTO>())
            .Where(x => x.INT_GROUP == BusRequestTypeGroup)
            .ToDictionary(x => x.ID, x => x.NVCHR_REQUEST_TYPE ?? string.Empty);

        var busStopMap = (busStopIds.Any()
            ? (await _tmBusStopService.GetByIdsAsync(busStopIds))?.Data ?? Enumerable.Empty<TM_BUS_STOPDTO>()
            : Enumerable.Empty<TM_BUS_STOPDTO>())
            .ToDictionary(x => x.ID, x => x.NVCHR_STOP_NAME ?? string.Empty);

        var routeAssigns = (routeAssignIds.Any()
            ? (await _tmBusRouteAssignService.GetByIdsAsync(routeAssignIds))?.Data ?? Enumerable.Empty<TM_BUS_ROUTE_ASSIGNDTO>()
            : Enumerable.Empty<TM_BUS_ROUTE_ASSIGNDTO>())
            .ToList();
        var routeAssignMap = (await BuildRouteDisplayMap(routeAssigns)).ToDictionary(x => x.Id, x => x.DisplayName ?? string.Empty);

        var employees = new List<TM_EMPLOYEEDTO>();
        if (employeeCodes.Any())
        {
            var empFilters = new List<FilterOption>();
            for (var i = 0; i < employeeCodes.Count; i++)
            {
                empFilters.Add(new FilterOption
                {
                    Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID),
                    Operator = "=",
                    Value = employeeCodes[i],
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

        var sectionResp = await _sectionService.GetAllAsync();
        var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
        var sectionMap = sections
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var rows = new List<BusRequestApprovalRowViewModel>();
        foreach (var req in requests.OrderByDescending(x => x.DTM_CREATE).ThenByDescending(x => x.ID))
        {
            employeeMap.TryGetValue(req.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var emp);
            var deptCode = emp?.CHR_CODE_SEC?.Trim() ?? string.Empty;
            sectionMap.TryGetValue(deptCode, out var sec);

            rows.Add(new BusRequestApprovalRowViewModel
            {
                Id = req.ID,
                DepartmentName = sec?.CHR_NAME ?? sec?.CHR_SECTION_NAME ?? deptCode,
                EmployeeCode = req.CHR_EMPLOYEE_ID,
                EmployeeName = emp?.NVCHR_EMPLOYEE_NAME,
                Position = emp?.CHR_POSITION,
                Address = BuildAddress(emp),
                RequestTypeName = req.INT_REQUEST_TYPE.HasValue && requestTypeMap.TryGetValue(req.INT_REQUEST_TYPE.Value, out var requestTypeName)
                    ? requestTypeName
                    : string.Empty,
                RouteDisplay = req.INT_BUS_ROUTE_ASSIGN_ID.HasValue && routeAssignMap.TryGetValue(req.INT_BUS_ROUTE_ASSIGN_ID.Value, out var routeDisplay)
                    ? routeDisplay
                    : string.Empty,
                BusStopDisplay = req.INT_BUS_STOP_ID.HasValue && busStopMap.TryGetValue(req.INT_BUS_STOP_ID.Value, out var stopName)
                    ? stopName
                    : string.Empty,
                UseOrCancelDate = req.DTM_CANCEL_DATE ?? req.DTM_START_DATE,
                Note = req.CHR_NOTE,
                CreatorAdid = req.CHR_USER_CREATE,
                Status = req.INT_STATUS,
                StatusText = BusRequestStatusUiHelper.GetStatusText(req.INT_STATUS),
                StatusRowClass = BusRequestStatusUiHelper.GetRowClass(req.INT_STATUS),
                StatusBadgeClass = BusRequestStatusUiHelper.GetBadgeClass(req.INT_STATUS)
            });
        }

        return rows;
    }

    private async Task<Dictionary<int, string>> LoadRequestTypeMapAsync(List<TM_BUS_REQUESTDTO> requests)
    {
        var requestTypeIds = requests
            .Where(x => x.INT_REQUEST_TYPE.HasValue)
            .Select(x => x.INT_REQUEST_TYPE!.Value)
            .Distinct()
            .ToList();

        if (!requestTypeIds.Any())
        {
            return new Dictionary<int, string>();
        }

        var response = await _tmRequestTypeService.GetByIdsAsync(requestTypeIds);
        var items = (response?.Data ?? Enumerable.Empty<TM_REQUEST_TYPEDTO>())
            .Where(x => x.INT_GROUP == BusRequestTypeGroup);
        return items.ToDictionary(x => x.ID, x => x.NVCHR_REQUEST_TYPE ?? string.Empty);
    }

    private async Task<BusRequestApprovalHistoryPageViewModel?> BuildHistoryViewModelAsync(int id)
    {
        var requestResp = await _tmBusRequestService.GetByIdAsync(id);
        var request = requestResp?.Data;
        if (request == null)
        {
            return null;
        }

        var requestTypeName = string.Empty;
        if (request.INT_REQUEST_TYPE.HasValue)
        {
            var requestTypeResp = await _tmRequestTypeService.GetByIdAsync(request.INT_REQUEST_TYPE.Value);
            if (requestTypeResp?.Data?.INT_GROUP == BusRequestTypeGroup)
            {
                requestTypeName = requestTypeResp.Data.NVCHR_REQUEST_TYPE ?? string.Empty;
            }
        }

        TM_EMPLOYEEDTO? employee = null;
        var employeeName = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.CHR_EMPLOYEE_ID))
        {
            var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = request.CHR_EMPLOYEE_ID.Trim() }
                }
            });
            employee = employeeResp?.Data?.Data?.FirstOrDefault();
            employeeName = employee?.NVCHR_EMPLOYEE_NAME ?? string.Empty;
        }

        var sectionName = string.Empty;
        var deptCode = employee?.CHR_CODE_SEC?.Trim();
        if (!string.IsNullOrWhiteSpace(deptCode))
        {
            var sectionResp = await _sectionService.GetAllAsync();
            var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
            var section = sections.FirstOrDefault(x => string.Equals(x.CHR_CODE?.Trim(), deptCode, StringComparison.OrdinalIgnoreCase));
            sectionName = section?.CHR_NAME ?? section?.CHR_SECTION_NAME ?? deptCode;
        }

        var routeDisplay = string.Empty;
        if (request.INT_BUS_ROUTE_ASSIGN_ID.HasValue)
        {
            var routeAssignResp = await _tmBusRouteAssignService.GetByIdAsync(request.INT_BUS_ROUTE_ASSIGN_ID.Value);
            var routeAssign = routeAssignResp?.Data;
            if (routeAssign != null)
            {
                routeDisplay = (await BuildRouteDisplayMap(new List<TM_BUS_ROUTE_ASSIGNDTO> { routeAssign }))
                    .FirstOrDefault().DisplayName ?? string.Empty;
            }
        }

        var busStopDisplay = string.Empty;
        if (request.INT_BUS_STOP_ID.HasValue)
        {
            var stopResp = await _tmBusStopService.GetByIdAsync(request.INT_BUS_STOP_ID.Value);
            busStopDisplay = stopResp?.Data?.NVCHR_STOP_NAME ?? string.Empty;
        }

        var useOrCancelDate = request.DTM_CANCEL_DATE ?? request.DTM_START_DATE;

        var historyResp = await _tmApprovalHistoryService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_APPROVAL_HISTORY.INT_BUS_REQEST_ID), Operator = "=", Value = id }
            },
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_APPROVAL_HISTORY.DTM_ACTION), SortDirection = "ASC" },
                new SortOption { Field = nameof(TM_APPROVAL_HISTORY.ID), SortDirection = "ASC" }
            }
        });

        var histories = historyResp?.Data?.Data?.ToList() ?? new List<TM_APPROVAL_HISTORYDTO>();
        return new BusRequestApprovalHistoryPageViewModel
        {
            RequestId = request.ID,
            DepartmentName = sectionName,
            EmployeeCode = request.CHR_EMPLOYEE_ID,
            EmployeeName = employeeName,
            Position = employee?.CHR_POSITION,
            Address = BuildAddress(employee),
            RequestTypeName = requestTypeName,
            RouteDisplay = routeDisplay,
            BusStopDisplay = busStopDisplay,
            UseOrCancelDate = useOrCancelDate,
            Note = request.CHR_NOTE,
            Status = request.INT_STATUS,
            StatusText = BusRequestStatusUiHelper.GetStatusText(request.INT_STATUS),
            StatusBadgeClass = BusRequestStatusUiHelper.GetBadgeClass(request.INT_STATUS),
            Histories = histories.Select(x => new BusRequestApprovalHistoryItemViewModel
            {
                Action = TranslateHistoryAction(x.CHR_ACTION),
                Actor = x.CHR_CONFIRMER_APPROVER,
                ActionTime = x.DTM_ACTION,
                Reason = x.NVCHR_REASON_REJECT,
                Result = x.BIT_ACTION
            }).ToList(),
            Steps = BuildWorkflowSteps(request, histories)
        };
    }

    private async Task ApplyEmployeeBusUsageByCompletedRequestAsync(TM_BUS_REQUESTDTO request, string? requestTypeName, string actor)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CHR_EMPLOYEE_ID)) return;

            var isRegisterType = IsRegisterBusUsageType(requestTypeName);
            var isChangeType = IsChangeBusUsageType(requestTypeName);
            var isEndType = IsEndBusUsageType(requestTypeName);
            var isReissueCardType = IsReissueBusCardType(requestTypeName);

            var employee = await _employeeService.GetByEmployeeCodeAsync(request.CHR_EMPLOYEE_ID.Trim());
            if (employee == null) return;

            var changed = false;
            if (isRegisterType)
            {
                if (employee.BIT_IS_USE_BUS != true)
                {
                    employee.BIT_IS_USE_BUS = true;
                    changed = true;
                }
                if (employee.BIT_IS_ACTIVE != true)
                {
                    employee.BIT_IS_ACTIVE = true;
                    changed = true;
                }
                if (employee.INT_BUS_STOP_ID != request.INT_BUS_STOP_ID)
                {
                    employee.INT_BUS_STOP_ID = request.INT_BUS_STOP_ID;
                    changed = true;
                }
                if (employee.INT_BUS_ROUTE_ASSIGN_ID != request.INT_BUS_ROUTE_ASSIGN_ID)
                {
                    employee.INT_BUS_ROUTE_ASSIGN_ID = request.INT_BUS_ROUTE_ASSIGN_ID;
                    changed = true;
                }
            }
            else if (isChangeType)
            {
                if (employee.INT_BUS_STOP_ID != request.INT_BUS_STOP_ID)
                {
                    employee.INT_BUS_STOP_ID = request.INT_BUS_STOP_ID;
                    changed = true;
                }
                if (employee.INT_BUS_ROUTE_ASSIGN_ID != request.INT_BUS_ROUTE_ASSIGN_ID)
                {
                    employee.INT_BUS_ROUTE_ASSIGN_ID = request.INT_BUS_ROUTE_ASSIGN_ID;
                    changed = true;
                }
            }
            else if (isEndType)
            {
                if (employee.BIT_IS_USE_BUS != false)
                {
                    employee.BIT_IS_USE_BUS = false;
                    changed = true;
                }
            }

            if (changed)
            {
                employee.CHR_USER_UPDATE = actor;
                employee.DTM_UPDATE = DateTime.Now;
                await _tmEmployeeService.UpdateAsync(employee);
            }

            var employeeRouteId = await SyncEmployeeRouteAndCapacityAsync(request, requestTypeName, employee.ID, actor, isRegisterType, isChangeType, isEndType);
            if (isRegisterType || isChangeType || isReissueCardType)
            {
                await AddCardPrintQueueIfNeededAsync(request, requestTypeName, employeeRouteId, actor);
            }
        }
        catch
        {
            // ignore employee update error to avoid blocking approval flow
        }
    }

    private async Task<int?> SyncEmployeeRouteAndCapacityAsync(
        TM_BUS_REQUESTDTO request,
        string? requestTypeName,
        int employeeId,
        string actor,
        bool isRegisterType,
        bool isChangeType,
        bool isEndType)
    {
        if (!isRegisterType && !isChangeType && !isEndType)
        {
            return await GetCurrentActiveEmployeeRouteIdAsync(request.CHR_EMPLOYEE_ID);
        }

        if (string.IsNullOrWhiteSpace(request.CHR_EMPLOYEE_ID))
        {
            return null;
        }

        var employeeCode = request.CHR_EMPLOYEE_ID.Trim();
        var activeRoute = await GetCurrentActiveEmployeeRouteAsync(employeeCode);

        var oldRouteAssignId = activeRoute?.INT_ROUTE_ASSIGN_ID;
        var oldPickupStopId = activeRoute?.INT_PICKUP_STOP_ID;
        var oldReturnStopId = activeRoute?.INT_RETURN_STOP_ID;

        var effectiveFrom = request.DTM_START_DATE
            ?? request.DTM_CANCEL_DATE
            ?? DateTime.Now;

        var newRouteAssignId = isEndType ? (int?)null : request.INT_BUS_ROUTE_ASSIGN_ID;
        var newPickupStopId = isEndType ? (int?)null : request.INT_BUS_STOP_ID;
        var newReturnStopId = isEndType ? (int?)null : request.INT_BUS_STOP_ID;

        if (!isEndType && (!newRouteAssignId.HasValue || !newPickupStopId.HasValue))
        {
            return activeRoute?.ID;
        }

        var actionType = ResolveEmployeeRouteActionType(
            requestTypeName,
            oldRouteAssignId,
            newRouteAssignId,
            oldPickupStopId,
            newPickupStopId,
            activeRoute == null);

        await _tmEmployeeRouteHistoryService.AddAsync(new TM_EMPLOYEE_ROUTE_HISTORY
        {
            INT_EMPLOYEE_ID = employeeId,
            CHR_EMPLOYEE_ID = employeeCode,
            INT_REQUEST_ID = request.ID,
            INT_OLD_ROUTE_ASSIGN_ID = oldRouteAssignId,
            INT_NEW_ROUTE_ASSIGN_ID = newRouteAssignId,
            INT_OLD_PICKUP_STOP_ID = oldPickupStopId,
            INT_NEW_PICKUP_STOP_ID = newPickupStopId,
            INT_OLD_RETURN_STOP_ID = oldReturnStopId,
            INT_NEW_RETURN_STOP_ID = newReturnStopId,
            DTM_NEW_EFFECTIVE_FROM = effectiveFrom,
            NVCHR_ACTION_TYPE = actionType,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        });

        if (isEndType)
        {
            if (activeRoute != null && activeRoute.BIT_IS_ACTIVE)
            {
                await _tmEmployeeRouteService.UpdateAsync(new TM_EMPLOYEE_ROUTE
                {
                    ID = activeRoute.ID,
                    CHR_USER_CREATE = activeRoute.CHR_USER_CREATE,
                    DTM_CREATE = activeRoute.DTM_CREATE,
                    CHR_USER_UPDATE = actor,
                    DTM_UPDATE = DateTime.Now,
                    INT_EMPLOYEE_ID = activeRoute.INT_EMPLOYEE_ID,
                    CHR_EMPLOYEE_ID = activeRoute.CHR_EMPLOYEE_ID,
                    INT_ROUTE_ASSIGN_ID = activeRoute.INT_ROUTE_ASSIGN_ID,
                    INT_PICKUP_STOP_ID = activeRoute.INT_PICKUP_STOP_ID,
                    INT_RETURN_STOP_ID = activeRoute.INT_RETURN_STOP_ID,
                    DTM_EFFECTIVE_FROM = activeRoute.DTM_EFFECTIVE_FROM,
                    DTM_EFFECTIVE_TO = request.DTM_CANCEL_DATE ?? DateTime.Now,
                    BIT_IS_ACTIVE = false
                });

                await AdjustRouteAssignCurrentUserAsync(activeRoute.INT_ROUTE_ASSIGN_ID, -1, actor);
            }

            return null;
        }

        int? resultRouteId;
        if (activeRoute == null)
        {
            var addResp = await _tmEmployeeRouteService.AddAsyncV2(new TM_EMPLOYEE_ROUTE
            {
                INT_EMPLOYEE_ID = employeeId,
                CHR_EMPLOYEE_ID = employeeCode,
                INT_ROUTE_ASSIGN_ID = newRouteAssignId!.Value,
                INT_PICKUP_STOP_ID = newPickupStopId!.Value,
                INT_RETURN_STOP_ID = newReturnStopId ?? newPickupStopId.Value,
                DTM_EFFECTIVE_FROM = effectiveFrom,
                DTM_EFFECTIVE_TO = null,
                BIT_IS_ACTIVE = true,
                CHR_USER_CREATE = actor,
                DTM_CREATE = DateTime.Now
            });

            resultRouteId = addResp.Data?.ID;
            await AdjustRouteAssignCurrentUserAsync(newRouteAssignId.Value, +1, actor);
        }
        else
        {
            var sameRoute = activeRoute.INT_ROUTE_ASSIGN_ID == newRouteAssignId
                && activeRoute.INT_PICKUP_STOP_ID == newPickupStopId
                && activeRoute.INT_RETURN_STOP_ID == (newReturnStopId ?? newPickupStopId);

            await _tmEmployeeRouteService.UpdateAsync(new TM_EMPLOYEE_ROUTE
            {
                ID = activeRoute.ID,
                CHR_USER_CREATE = activeRoute.CHR_USER_CREATE,
                DTM_CREATE = activeRoute.DTM_CREATE,
                CHR_USER_UPDATE = actor,
                DTM_UPDATE = DateTime.Now,
                INT_EMPLOYEE_ID = activeRoute.INT_EMPLOYEE_ID,
                CHR_EMPLOYEE_ID = activeRoute.CHR_EMPLOYEE_ID,
                INT_ROUTE_ASSIGN_ID = newRouteAssignId!.Value,
                INT_PICKUP_STOP_ID = newPickupStopId!.Value,
                INT_RETURN_STOP_ID = newReturnStopId ?? newPickupStopId.Value,
                DTM_EFFECTIVE_FROM = effectiveFrom,
                DTM_EFFECTIVE_TO = null,
                BIT_IS_ACTIVE = true
            });

            resultRouteId = activeRoute.ID;
            if (!sameRoute)
            {
                if (oldRouteAssignId.HasValue)
                {
                    await AdjustRouteAssignCurrentUserAsync(oldRouteAssignId.Value, -1, actor);
                }

                await AdjustRouteAssignCurrentUserAsync(newRouteAssignId.Value, +1, actor);
            }
        }

        return resultRouteId;
    }

    private async Task AddCardPrintQueueIfNeededAsync(TM_BUS_REQUESTDTO request, string? requestTypeName, int? employeeRouteId, string actor)
    {
        if (!employeeRouteId.HasValue || employeeRouteId.Value <= 0 || string.IsNullOrWhiteSpace(request.CHR_EMPLOYEE_ID))
        {
            return;
        }

        var existingQueueResp = await _tmCardPrintQueueService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_CARD_PRINT_QUEUE.INT_BUS_REQUEST_ID), Operator = "=", Value = request.ID }
            }
        });

        if ((existingQueueResp?.Data?.Data?.Any() ?? false))
        {
            return;
        }

        await _tmCardPrintQueueService.AddAsyncV2(new TM_CARD_PRINT_QUEUE
        {
            CHR_EMPLOYEE_ID = request.CHR_EMPLOYEE_ID.Trim(),
            INT_EMPLOYEE_ROUTE_ID = employeeRouteId.Value,
            INT_BUS_REQUEST_ID = request.ID,
            INT_PRINT_TYPE = ResolveCardPrintType(requestTypeName),
            INT_STATUS = 0,
            DTM_REQUEST_DATE = DateTime.Now,
            NVCHR_REASON = request.CHR_NOTE,
            CHR_USER_CREATE = actor,
            DTM_CREATE = DateTime.Now
        });
    }

    private async Task<TM_EMPLOYEE_ROUTEDTO?> GetCurrentActiveEmployeeRouteAsync(string employeeCode)
    {
        var routeResp = await _tmEmployeeRouteService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE_ROUTE.CHR_EMPLOYEE_ID), Operator = "=", Value = employeeCode },
                new() { Field = nameof(TM_EMPLOYEE_ROUTE.BIT_IS_ACTIVE), Operator = "=", Value = true }
            },
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_EMPLOYEE_ROUTE.DTM_EFFECTIVE_FROM), SortDirection = "DESC" },
                new SortOption { Field = nameof(TM_EMPLOYEE_ROUTE.ID), SortDirection = "DESC" }
            }
        });

        return routeResp?.Data?.Data?.FirstOrDefault();
    }

    private async Task<int?> GetCurrentActiveEmployeeRouteIdAsync(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode))
        {
            return null;
        }

        var activeRoute = await GetCurrentActiveEmployeeRouteAsync(employeeCode.Trim());
        return activeRoute?.ID;
    }

    private async Task AdjustRouteAssignCurrentUserAsync(int routeAssignId, int delta, string actor)
    {
        if (routeAssignId <= 0 || delta == 0)
        {
            return;
        }

        var routeAssignResp = await _tmBusRouteAssignService.GetByIdAsync(routeAssignId);
        var routeAssign = routeAssignResp?.Data;
        if (routeAssign == null)
        {
            return;
        }

        var currentUser = routeAssign.INT_CURRENT_USER ?? 0;
        var nextValue = Math.Max(0, currentUser + delta);
        if (nextValue == currentUser)
        {
            return;
        }

        await _tmBusRouteAssignService.UpdateAsync(new TM_BUS_ROUTE_ASSIGN
        {
            ID = routeAssign.ID,
            CHR_USER_CREATE = routeAssign.CHR_USER_CREATE,
            DTM_CREATE = routeAssign.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            INT_ROUTE_PICKUP_ID = routeAssign.INT_ROUTE_PICKUP_ID,
            INT_ROUTE_RETURN_ID = routeAssign.INT_ROUTE_RETURN_ID,
            INT_BUS_ID = routeAssign.INT_BUS_ID,
            INT_DRIVER_ID = routeAssign.INT_DRIVER_ID,
            INT_CURRENT_USER = nextValue,
            CHR_BRA_TYPE = routeAssign.CHR_BRA_TYPE,
            INT_CAPACITY = routeAssign.INT_CAPACITY,
            DTM_EFFECTIVE_FROM = routeAssign.DTM_EFFECTIVE_FROM,
            DTM_EFFECTIVE_TO = routeAssign.DTM_EFFECTIVE_TO,
            BIT_IS_ACTIVE = routeAssign.BIT_IS_ACTIVE
        });
    }

    private static string ResolveEmployeeRouteActionType(
        string? requestTypeName,
        int? oldRouteAssignId,
        int? newRouteAssignId,
        int? oldPickupStopId,
        int? newPickupStopId,
        bool isNew)
    {
        if (IsEndBusUsageType(requestTypeName))
        {
            return "CANCEL_ROUTE";
        }

        if (isNew || IsRegisterBusUsageType(requestTypeName))
        {
            return "NEW_REGISTER";
        }

        if (oldRouteAssignId != newRouteAssignId)
        {
            return "CHANGE_ROUTE";
        }

        if (oldPickupStopId != newPickupStopId)
        {
            return "CHANGE_STOP";
        }

        return "CHANGE_ROUTE";
    }

    private static int ResolveCardPrintType(string? requestTypeName)
    {
        if (IsReissueBusCardType(requestTypeName)) return 3;
        if (IsChangeBusUsageType(requestTypeName)) return 2;
        return 1;
    }

    private static bool IsRegisterBusUsageType(string? requestTypeName)
    {
        var normalized = NormalizeVietnamese(requestTypeName);
        return normalized.Contains("dang ky su dung xe buyt", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("dang ky", StringComparison.OrdinalIgnoreCase) && normalized.Contains("xe buyt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsChangeBusUsageType(string? requestTypeName)
    {
        var normalized = NormalizeVietnamese(requestTypeName);
        return normalized.Contains("thay doi su dung xe buyt", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("thay doi", StringComparison.OrdinalIgnoreCase) && normalized.Contains("xe buyt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEndBusUsageType(string? requestTypeName)
    {
        var normalized = NormalizeVietnamese(requestTypeName);
        return normalized.Contains("ket thuc su dung xe buyt", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("ket thuc", StringComparison.OrdinalIgnoreCase) && normalized.Contains("xe buyt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsReissueBusCardType(string? requestTypeName)
    {
        var normalized = NormalizeVietnamese(requestTypeName);
        return normalized.Contains("cap lai the su dung xe buyt", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("cap lai the", StringComparison.OrdinalIgnoreCase) && normalized.Contains("xe buyt", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeVietnamese(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        var normalized = input.Trim().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch == 'đ' ? 'd' : ch == 'Đ' ? 'D' : ch);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private async Task<List<(int Id, string? DisplayName)>> BuildRouteDisplayMap(List<TM_BUS_ROUTE_ASSIGNDTO> assigns)
    {
        var busesResp = await _tmBusService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });
        var routesResp = await _tmBusRouteService.SearchByConditionAsync(new SearchOptions { PageNumber = -1, PageSize = 100000 });

        var buses = (busesResp?.Data?.Data ?? new List<TM_BUSDTO>()).ToDictionary(x => x.ID, x => x);
        var routes = (routesResp?.Data?.Data ?? new List<TM_BUS_ROUTEDTO>()).ToDictionary(x => x.ID, x => x);

        return assigns
            .Select(a =>
            {
                buses.TryGetValue(a.INT_BUS_ID ?? 0, out var bus);
                routes.TryGetValue(a.INT_ROUTE_PICKUP_ID ?? 0, out var route);
                if (string.IsNullOrWhiteSpace(route?.NVCHR_ROUTE_NAME))
                {
                    return (a.ID, DisplayName: (string?)null);
                }

                return (a.ID, DisplayName: $"{bus?.CHR_BUS_CODE} - {route.NVCHR_ROUTE_NAME}");
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.DisplayName))
            .GroupBy(x => x.ID)
            .Select(x => x.First())
            .ToList();
    }

    private static string BuildAddress(TM_EMPLOYEEDTO? emp)
    {
        if (emp == null) return string.Empty;
        var parts = new[] { emp.NVCHR_ADDRESS_DETAIL, emp.NVCHR_WARD, emp.NVCHR_CITY }
            .Where(x => !string.IsNullOrWhiteSpace(x));
        return string.Join(", ", parts);
    }

    private async Task<bool> CanCurrentUserProcessGaAsync()
    {
        var template = await GetMailTemplateByCodeAsync("BUS_REQUEST_GA_APPROVAL");
        var configuredTo = template?.CHR_TO;
        if (string.IsNullOrWhiteSpace(configuredTo))
        {
            return false;
        }

        var currentEmail = GetCurrentEmail();
        if (string.IsNullOrWhiteSpace(currentEmail))
        {
            return false;
        }

        var recipients = configuredTo
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var recipient in recipients)
        {
            if (await _activeDirectoryService.CanUserProcessConfirmationAsync(currentEmail, recipient))
            {
                return true;
            }
        }

        return false;
    }

    private async Task SendCompletedMailAsync(List<BusRequestApprovalRowViewModel> rows)
    {
        try
        {
            if (!rows.Any()) return;

            var template = await GetMailTemplateByCodeAsync("BUS_REQUEST_COMPLETED");
            var tableHtml = BuildRequestTableHtml(rows);
            var link = BuildBusRequestLink();
            var creatorEmails = rows
                .Select(x => ToCompanyEmail(x.CreatorAdid))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
            var recipientDisplayNames = await ResolveRecipientDisplayNamesAsync(rows);

            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{To}}"] = recipientDisplayNames,
                ["{{RequestCount}}"] = rows.Count.ToString(),
                ["{{RequestTable}}"] = tableHtml,
                ["{{ApproveLink}}"] = link
            };

            var title = ReplaceTemplateTokens(template?.CHR_SUBJECT, tokens);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "[BusManagement] Đơn đăng ký/hủy xe đi làm đã hoàn thành phê duyệt";
            }

            var defaultBody = """
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset="utf-8" />
                    <title>Thông báo hoàn thành phê duyệt đơn xe</title>
                </head>
                <body style="margin:0;padding:0;background-color:#ffffff;font-family:'Times New Roman', Times, serif;">

                    <div style="width:100%;margin:0 auto;">

                        <p style="color:red;font-size:24px;font-weight:bold;margin:0 0 30px 0;">
                            Đây là mail tự động, vui lòng không Reply lại mail!
                        </p>

                        <p style="font-size:20px;margin:0 0 30px 0;">
                            Gửi anh/chị {{To}},
                        </p>

                        <p style="font-size:20px;margin:0 0 30px 0;">
                            Đơn đăng ký/hủy xe đi làm của anh/chị đã hoàn thành xác nhận phê duyệt.
                        </p>

                        <p style="font-size:20px;margin:0 0 30px 0;">Tổng số đơn hoàn thành: <b>{{RequestCount}}</b></p>

                        {{RequestTable}}

                        <br><br>

                        <p style="font-size:20px;margin:0 0 10px 0;">
                            Vui lòng vào hệ thống để kiểm tra chi tiết!
                        </p>

                        <p style="font-size:20px;margin:0 0 10px 0;">
                            Hoặc truy cập :
                            <a href="{{ApproveLink}}">LINK</a>
                        </p>

                        <p style="font-size:20px;margin:0 0 10px 0;">Xin cảm ơn!</p>

                        <p style="font-size:20px;font-style:italic;margin:0;">
                            Hệ thống quản lý xe Công ty-BIVN
                        </p>

                    </div>

                </body>
                </html>
                """;

            var bodyTemplate = string.IsNullOrWhiteSpace(template?.CHR_CONTENT)
                ? defaultBody
                : template?.CHR_CONTENT ?? string.Empty;
            var body = ReplaceTemplateTokens(bodyTemplate, tokens);

            var fromEmail = ReplaceTemplateTokens(template?.CHR_FROM, tokens);
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = "zzpsvvmg@brother-bivn.com.vn";
            }

            var toTemplate = ReplaceTemplateTokens(template?.CHR_TO, tokens);
            var to = CombineEmails(string.Join(';', creatorEmails), toTemplate);
            if (string.IsNullOrWhiteSpace(to)) return;

            var cc = ReplaceTemplateTokens(template?.CHR_CC, tokens);
            var bcc = ReplaceTemplateTokens(template?.CHR_BCC, tokens);
            var priority = template?.BIT_HIGHT == true ? 1 : 0;

            _tmMasterMailService.sendEmailNotify(title, fromEmail, to, cc ?? string.Empty, bcc ?? string.Empty, body, priority);
        }
        catch
        {
            // ignore mail exception
        }
    }

    private async Task SendGaApprovalMailAsync(List<BusRequestApprovalRowViewModel> rows)
    {
        try
        {
            if (!rows.Any()) return;

            var template = await GetMailTemplateByCodeAsync("BUS_REQUEST_GA_APPROVAL");
            var tableHtml = BuildRequestTableHtml(rows);
            var approveLink = BuildApprovalLink("ga");

            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{RequestCount}}"] = rows.Count.ToString(),
                ["{{RequestTable}}"] = tableHtml,
                ["{{ApproveLink}}"] = approveLink
            };

            var title = ReplaceTemplateTokens(template?.CHR_SUBJECT, tokens);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "[BusManagement] Nhóm quản lý xe Công ty xác nhận yêu cầu đăng ký/hủy xe đi làm";
            }

            var defaultBody = """
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
                        <p style="font-size:20px;margin:0 0 30px 0;">Gửi nhóm quản lý xe Công ty,</p>
                        <p style="font-size:20px;margin:0 0 30px 0;">Phía dưới là thông tin yêu cầu xe Công ty cần nhóm xác nhận :</p>
                        <p style="font-size:20px;margin:0 0 30px 0;">Có <b>{{RequestCount}}</b> đơn đã được QLSC duyệt và chờ GA xác nhận.</p>
                        {{RequestTable}}
                        <br> <br>
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

            var bodyTemplate = string.IsNullOrWhiteSpace(template?.CHR_CONTENT)
                ? defaultBody
                : template?.CHR_CONTENT ?? string.Empty;
            var body = ReplaceTemplateTokens(bodyTemplate, tokens);

            var fromEmail = ReplaceTemplateTokens(template?.CHR_FROM, tokens);
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = "zzpsvvmg@brother-bivn.com.vn";
            }

            var to = ReplaceTemplateTokens(template?.CHR_TO, tokens);
            if (string.IsNullOrWhiteSpace(to)) return;

            var cc = ReplaceTemplateTokens(template?.CHR_CC, tokens);
            var bcc = ReplaceTemplateTokens(template?.CHR_BCC, tokens);
            var priority = template?.BIT_HIGHT == true ? 1 : 0;

            _tmMasterMailService.sendEmailNotify(title, fromEmail, to, cc ?? string.Empty, bcc ?? string.Empty, body, priority);
        }
        catch
        {
            // ignore mail exception
        }
    }

    private async Task SendRejectMailAsync(List<BusRequestApprovalRowViewModel> rows, string stageName, string reason)
    {
        try
        {
            if (!rows.Any()) return;

            var template = await GetMailTemplateByCodeAsync("BUS_REQUEST_REJECT");
            var tableHtml = BuildRequestTableHtml(rows);
            var link = BuildBusRequestLink();
            var creatorEmails = rows
                .Select(x => ToCompanyEmail(x.CreatorAdid))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray();
            var recipientDisplayNames = await ResolveRecipientDisplayNamesAsync(rows);

            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{To}}"] = recipientDisplayNames,
                ["{{RejectStage}}"] = stageName,
                ["{{RejectReason}}"] = reason,
                ["{{RequestCount}}"] = rows.Count.ToString(),
                ["{{RequestTable}}"] = tableHtml,
                ["{{ApproveLink}}"] = link
            };

            var title = ReplaceTemplateTokens(template?.CHR_SUBJECT, tokens);
            if (string.IsNullOrWhiteSpace(title))
            {
                title = "[BusManagement] Đơn đăng ký/hủy xe đi làm của bạn đã bị từ chối";
            }

            var defaultBody = """
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

                        <p style="font-size:20px;margin:0 0 30px 0;">Đơn yêu cầu của anh/chị đã bị từ chối tại bước <b>{{RejectStage}}</b>.</p>

                        <p  style="font-size:20px;margin:0 0 30px 0;">Lý do: {{RejectReason}}</p>

                        <p style="font-size:20px;margin:0 0 10px 0;">
                            Vui lòng vào hệ thống để kiểm tra !
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
                ? defaultBody
                : template?.CHR_CONTENT ?? string.Empty;
            var body = ReplaceTemplateTokens(bodyTemplate, tokens);

            var fromEmail = ReplaceTemplateTokens(template?.CHR_FROM, tokens);
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                fromEmail = "zzpsvvmg@brother-bivn.com.vn";
            }

            var toTemplate = ReplaceTemplateTokens(template?.CHR_TO, tokens);
            var to = CombineEmails(string.Join(';', creatorEmails), toTemplate);
            if (string.IsNullOrWhiteSpace(to)) return;

            var cc = ReplaceTemplateTokens(template?.CHR_CC, tokens);
            var bcc = ReplaceTemplateTokens(template?.CHR_BCC, tokens);
            var priority = template?.BIT_HIGHT == true ? 1 : 0;

            _tmMasterMailService.sendEmailNotify(title, fromEmail, to, cc ?? string.Empty, bcc ?? string.Empty, body, priority);
        }
        catch
        {
            // ignore mail exception
        }
    }

    private async Task<string> ResolveRecipientDisplayNamesAsync(List<BusRequestApprovalRowViewModel> rows)
    {
        var namesByEmployeeCode = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeCode) && !string.IsNullOrWhiteSpace(x.EmployeeName))
            .GroupBy(x => x.EmployeeCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.EmployeeName?.Trim()).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

        var employeeCodes = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeCode))
            .Select(x => x.EmployeeCode!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (employeeCodes.Any())
        {
            var codeFilters = new List<FilterOption>();
            for (var i = 0; i < employeeCodes.Count; i++)
            {
                codeFilters.Add(new FilterOption
                {
                    Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID),
                    Operator = "=",
                    Value = employeeCodes[i],
                    LogicType = i == 0 ? null : "OR"
                });
            }

            var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = codeFilters
            });

            foreach (var emp in employeeResp?.Data?.Data ?? Enumerable.Empty<TM_EMPLOYEEDTO>())
            {
                var code = emp.CHR_EMPLOYEE_ID?.Trim();
                var name = emp.NVCHR_EMPLOYEE_NAME?.Trim();
                if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name)) continue;
                namesByEmployeeCode[code] = name;
            }
        }

        var namesByAdid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var adids = rows
            .Where(x => !string.IsNullOrWhiteSpace(x.CreatorAdid))
            .Select(x => x.CreatorAdid!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (adids.Any())
        {
            var adidFilters = new List<FilterOption>();
            for (var i = 0; i < adids.Count; i++)
            {
                adidFilters.Add(new FilterOption
                {
                    Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ADID),
                    Operator = "=",
                    Value = adids[i],
                    LogicType = i == 0 ? null : "OR"
                });
            }

            var employeeByAdidResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = adidFilters
            });

            foreach (var emp in employeeByAdidResp?.Data?.Data ?? Enumerable.Empty<TM_EMPLOYEEDTO>())
            {
                var adid = emp.CHR_EMPLOYEE_ADID?.Trim();
                var name = emp.NVCHR_EMPLOYEE_NAME?.Trim();
                if (string.IsNullOrWhiteSpace(adid) || string.IsNullOrWhiteSpace(name)) continue;
                namesByAdid[adid] = name;
            }
        }

        var resolvedNames = rows
            .Select(x =>
            {
                if (!string.IsNullOrWhiteSpace(x.EmployeeCode)
                    && namesByEmployeeCode.TryGetValue(x.EmployeeCode.Trim(), out var byCode)
                    && !string.IsNullOrWhiteSpace(byCode))
                {
                    return byCode;
                }

                if (!string.IsNullOrWhiteSpace(x.CreatorAdid)
                    && namesByAdid.TryGetValue(x.CreatorAdid.Trim(), out var byAdid)
                    && !string.IsNullOrWhiteSpace(byAdid))
                {
                    return byAdid;
                }

                return x.CreatorAdid?.Trim() ?? string.Empty;
            })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return string.Join(", ", resolvedNames);
    }

    private async Task<TM_MASTER_MAILDTO?> GetMailTemplateByCodeAsync(string code)
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;

        var mailTemplateResp = await _tmMasterMailService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_MASTER_MAIL.CHR_MAIL_CODE), Operator = "=", Value = code }
            }
        });

        return mailTemplateResp?.Data?.Data?.FirstOrDefault();
    }

    private string BuildApprovalLink(string stage)
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var baseUrl = configuration?["SystemLinks:BusManagementWebApp"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = Request?.Host.HasValue == true
                ? $"{Request.Scheme}://{Request.Host.Value}"
                : string.Empty;
        }

        var relativePath = Url.Action(nameof(Index), "BusRequestApproval", new { stage }) ?? $"/BusRequestApproval?stage={stage}";
        if (string.IsNullOrWhiteSpace(baseUrl)) return relativePath;
        return $"{baseUrl.TrimEnd('/')}{relativePath}";
    }

    private string BuildBusRequestLink()
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var baseUrl = configuration?["SystemLinks:BusManagementWebApp"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = Request?.Host.HasValue == true
                ? $"{Request.Scheme}://{Request.Host.Value}"
                : string.Empty;
        }

        var relativePath = Url.Action(nameof(Index), "BusRequest") ?? $"/BusRequest";
        if (string.IsNullOrWhiteSpace(baseUrl)) return relativePath;
        return $"{baseUrl.TrimEnd('/')}{relativePath}";
    }

    private static string BuildRequestTableHtml(List<BusRequestApprovalRowViewModel> rows)
    {
        var tableRows = string.Join(string.Empty, rows.Select((x, idx) =>
            $"<tr><td>{idx + 1}</td><td>{x.DepartmentName}</td><td>{x.EmployeeCode}</td><td>{x.EmployeeName}</td><td>{x.RequestTypeName}</td><td>{x.RouteDisplay}</td><td>{x.BusStopDisplay}</td><td>{x.UseOrCancelDate:dd/MM/yyyy}</td><td>{x.Note}</td></tr>"));

        return $"<table border='1' cellpadding='6' cellspacing='0' style='border-collapse:collapse;width:100%;'><thead><tr><th>STT</th><th>Phòng ban</th><th>Mã NV</th><th>Họ tên</th><th>Loại đơn</th><th>Tuyến xe</th><th>Điểm đón</th><th>Ngày</th><th>Ghi chú</th></tr></thead><tbody>{tableRows}</tbody></table>";
    }

    private static string NormalizeStage(string? stage)
    {
        return string.Equals(stage, "ga", StringComparison.OrdinalIgnoreCase) ? "ga" : "qlsc";
    }

    private static List<int> ParseIds(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new List<int>();

        return csv
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x.Trim(), out var parsed) ? parsed : 0)
            .Where(x => x > 0)
            .Distinct()
            .ToList();
    }

    private TM_BUS_REQUEST BuildUpdateEntity(TM_BUS_REQUESTDTO dto, string actor)
    {
        return new TM_BUS_REQUEST
        {
            ID = dto.ID,
            CHR_USER_CREATE = dto.CHR_USER_CREATE,
            DTM_CREATE = dto.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = dto.CHR_EMPLOYEE_ID,
            INT_BUS_ROUTE_ASSIGN_ID = dto.INT_BUS_ROUTE_ASSIGN_ID,
            INT_BUS_STOP_ID = dto.INT_BUS_STOP_ID,
            CHR_NOTE = dto.CHR_NOTE,
            DTM_START_DATE = dto.DTM_START_DATE,
            DTM_CANCEL_DATE = dto.DTM_CANCEL_DATE,
            INT_REQUEST_TYPE = dto.INT_REQUEST_TYPE,
            INT_STATUS = dto.INT_STATUS,
            CHR_QLSC_APPROVE = dto.CHR_QLSC_APPROVE,
            CHR_GA_CONFIRM = dto.CHR_GA_CONFIRM
        };
    }

    private string GetCurrentAdid()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value?.Trim() ?? string.Empty;
    }

    private string GetCurrentEmail()
    {
        var email = User?.Claims?.FirstOrDefault(c => c.Type == "email")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(email)) return email;

        var adid = GetCurrentAdid();
        if (string.IsNullOrWhiteSpace(adid)) return string.Empty;
        return ToCompanyEmail(adid);
    }

    private static string ToCompanyEmail(string? adid)
    {
        if (string.IsNullOrWhiteSpace(adid)) return string.Empty;
        var value = adid.Trim();
        return value.Contains('@') ? value : $"{value}@brothergroup.net";
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

    private static string TranslateHistoryAction(string? action)
    {
        return action?.Trim().ToUpperInvariant() switch
        {
            "CREATE_REQUEST" => "Tạo đơn",
            "RESUBMIT_REQUEST" => "Gửi lại phê duyệt",
            "QLSC_APPROVE" => "QLSC phê duyệt",
            "QLSC_REJECT" => "QLSC từ chối",
            "GA_APPROVE" => "GA phê duyệt",
            "GA_REJECT" => "GA từ chối",
            _ => action ?? string.Empty
        };
    }

    private static List<BusRequestWorkflowStepViewModel> BuildWorkflowSteps(TM_BUS_REQUESTDTO request, List<TM_APPROVAL_HISTORYDTO> histories)
    {
        var create = histories.FirstOrDefault(x => string.Equals(x.CHR_ACTION, "CREATE_REQUEST", StringComparison.OrdinalIgnoreCase));
        var qlsc = histories.FirstOrDefault(x => string.Equals(x.CHR_ACTION, "QLSC_APPROVE", StringComparison.OrdinalIgnoreCase)
                                              || string.Equals(x.CHR_ACTION, "QLSC_REJECT", StringComparison.OrdinalIgnoreCase));
        var ga = histories.FirstOrDefault(x => string.Equals(x.CHR_ACTION, "GA_APPROVE", StringComparison.OrdinalIgnoreCase)
                                            || string.Equals(x.CHR_ACTION, "GA_REJECT", StringComparison.OrdinalIgnoreCase));

        var steps = new List<BusRequestWorkflowStepViewModel>
        {
            new()
            {
                StepName = "Tạo đơn",
                State = "done",
                StateText = "Đã xử lý",
                CssClass = "workflow-step-done",
                Actor = create?.CHR_CONFIRMER_APPROVER ?? request.CHR_USER_CREATE,
                ActionTime = create?.DTM_ACTION ?? request.DTM_CREATE
            },
            BuildQlscStep(request, qlsc),
            BuildGaStep(request, ga)
        };

        return steps;
    }

    private static BusRequestWorkflowStepViewModel BuildQlscStep(TM_BUS_REQUESTDTO request, TM_APPROVAL_HISTORYDTO? qlsc)
    {
        if (qlsc != null)
        {
            var isReject = string.Equals(qlsc.CHR_ACTION, "QLSC_REJECT", StringComparison.OrdinalIgnoreCase)
                           || qlsc.BIT_ACTION == false
                           || request.INT_STATUS == (int)BusRequestStatus.QlscRejected;

            return new BusRequestWorkflowStepViewModel
            {
                StepName = "QLSC phòng ban duyệt đơn",
                State = isReject ? "reject" : "done",
                StateText = isReject ? "Bị từ chối" : "Đã xử lý",
                CssClass = isReject ? "workflow-step-reject" : "workflow-step-done",
                Actor = qlsc.CHR_CONFIRMER_APPROVER,
                ActionTime = qlsc.DTM_ACTION,
                Reason = qlsc.NVCHR_REASON_REJECT
            };
        }

        if (request.INT_STATUS == (int)BusRequestStatus.PendingQlscApproval)
        {
            return new BusRequestWorkflowStepViewModel
            {
                StepName = "QLSC phòng ban duyệt đơn",
                State = "progress",
                StateText = "Đang xử lý",
                CssClass = "workflow-step-progress"
            };
        }

        return new BusRequestWorkflowStepViewModel
        {
            StepName = "QLSC phòng ban duyệt đơn",
            State = "pending",
            StateText = "Chưa xử lý",
            CssClass = "workflow-step-pending"
        };
    }

    private static BusRequestWorkflowStepViewModel BuildGaStep(TM_BUS_REQUESTDTO request, TM_APPROVAL_HISTORYDTO? ga)
    {
        if (ga != null)
        {
            var isReject = string.Equals(ga.CHR_ACTION, "GA_REJECT", StringComparison.OrdinalIgnoreCase)
                           || ga.BIT_ACTION == false
                           || request.INT_STATUS == (int)BusRequestStatus.GaRejected;

            return new BusRequestWorkflowStepViewModel
            {
                StepName = "Nhóm quản lý xe Công ty xác nhận",
                State = isReject ? "reject" : "done",
                StateText = isReject ? "Bị từ chối" : "Đã xử lý",
                CssClass = isReject ? "workflow-step-reject" : "workflow-step-done",
                Actor = ga.CHR_CONFIRMER_APPROVER,
                ActionTime = ga.DTM_ACTION,
                Reason = ga.NVCHR_REASON_REJECT
            };
        }

        if (request.INT_STATUS == (int)BusRequestStatus.QlscApproved)
        {
            return new BusRequestWorkflowStepViewModel
            {
                StepName = "Nhóm quản lý xe Công ty xác nhận",
                State = "progress",
                StateText = "Đang xử lý",
                CssClass = "workflow-step-progress"
            };
        }

        return new BusRequestWorkflowStepViewModel
        {
            StepName = "Nhóm quản lý xe Công ty xác nhận",
            State = "pending",
            StateText = "Chưa xử lý",
            CssClass = "workflow-step-pending"
        };
    }

    private async Task<List<int>> LoadRequestTypeIdsByGroupAsync(int group)
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
            .Select(x => x.ID)
            .Distinct()
            .ToList();
    }
}
