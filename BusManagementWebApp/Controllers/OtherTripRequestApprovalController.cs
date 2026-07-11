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

public class OtherTripRequestApprovalController : Controller
{
    private const int OtherTripRequestTypeGroup = 3;

    private readonly ITmTripRequestService _tmTripRequestService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly ITmApprovalHistoryService _tmApprovalHistoryService;
    private readonly ISectionService _sectionService;
    private readonly ITmMasterMailService _tmMasterMailService;
    private readonly IActiveDirectoryService _activeDirectoryService;
    private readonly ITmRequestTypeService _tmRequestTypeService;

    public OtherTripRequestApprovalController(
        ITmTripRequestService tmTripRequestService,
        ITmEmployeeService tmEmployeeService,
        ITmApprovalHistoryService tmApprovalHistoryService,
        ISectionService sectionService,
        ITmMasterMailService tmMasterMailService,
        IActiveDirectoryService activeDirectoryService,
        ITmRequestTypeService tmRequestTypeService)
    {
        _tmTripRequestService = tmTripRequestService;
        _tmEmployeeService = tmEmployeeService;
        _tmApprovalHistoryService = tmApprovalHistoryService;
        _sectionService = sectionService;
        _tmMasterMailService = tmMasterMailService;
        _activeDirectoryService = activeDirectoryService;
        _tmRequestTypeService = tmRequestTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vm = new TripRequestApprovalPageViewModel
        {
            Stage = "ga-driver",
            StageTitle = "GA điền thông tin lái xe - Đơn xe khác",
            CanProcess = true
        };

        if (!await CanCurrentUserProcessAsync())
        {
            vm.CanProcess = false;
            vm.AccessDeniedMessage = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return View(vm);
        }

        vm.Requests = await LoadPendingRowsAsync();
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> History(int id, bool modal = false)
    {
        var vm = await BuildHistoryViewModelAsync(id);
        if (vm == null)
        {
            return NotFound();
        }

        if (modal)
        {
            return PartialView("_HistoryContent", vm);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(TripRequestApprovalActionInputModel input)
    {
        var ids = ParseIds(input.SelectedIds);
        if (!ids.Any())
        {
            TempData["OtherTripRequestApprovalError"] = "Vui lòng chọn ít nhất 1 bản ghi.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(input.DriverInfo))
        {
            TempData["OtherTripRequestApprovalError"] = "Vui lòng nhập thông tin lái xe.";
            return RedirectToAction(nameof(Index));
        }

        if (!await CanCurrentUserProcessAsync())
        {
            TempData["OtherTripRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return RedirectToAction(nameof(Index));
        }

        var currentAdid = GetCurrentAdid();
        var requests = await GetProcessableRequestsAsync(ids, currentAdid, GetCurrentEmployeeCode());
        if (!requests.Any())
        {
            TempData["OtherTripRequestApprovalError"] = "Không có bản ghi hợp lệ để xử lý.";
            return RedirectToAction(nameof(Index));
        }

        var success = 0;
        foreach (var req in requests)
        {
            var entity = BuildUpdateEntity(req, currentAdid);
            entity.INT_STATUS = TripRequestStatuses.Completed;
            entity.CHR_STATUS = ResolveStatusText(entity.INT_STATUS);
            entity.NVCHR_DRIVER_INFO = input.DriverInfo?.Trim();

            var updateResp = await _tmTripRequestService.UpdateAsync(entity);
            if (!updateResp.Success) continue;

            success++;
            var actor = string.IsNullOrWhiteSpace(currentAdid) ? GetCurrentEmployeeCode() : currentAdid;
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_TRIP_REQEST_ID = req.ID,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = entity.INT_STATUS,
                BIT_ACTION = true,
                CHR_ACTION = "OTHER_TRIP_GA_DRIVER_FILLED",
                DTM_ACTION = DateTime.Now
            });

            await SendCompletedMailToEmployeeAsync(req, entity.NVCHR_DRIVER_INFO);
        }

        TempData[success > 0 ? "OtherTripRequestApprovalSuccess" : "OtherTripRequestApprovalError"] = success > 0
            ? $"Đã cập nhật thông tin lái xe cho {success} bản ghi."
            : "Không thể xử lý bản ghi đã chọn.";
        if (success > 0)
        {
            TempData["OtherTripRequestApprovalCelebrate"] = "1";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadImportTemplate(string? selectedIds)
    {
        if (!await CanCurrentUserProcessAsync())
        {
            TempData["OtherTripRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return RedirectToAction(nameof(Index));
        }

        var ids = ParseIds(selectedIds);
        if (!ids.Any())
        {
            TempData["OtherTripRequestApprovalError"] = "Vui lòng tick chọn ít nhất 1 bản ghi để tải mẫu import.";
            return RedirectToAction(nameof(Index));
        }

        var currentAdid = GetCurrentAdid();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        var requests = await GetProcessableRequestsAsync(ids, currentAdid, currentEmployeeCode);
        if (!requests.Any())
        {
            TempData["OtherTripRequestApprovalError"] = "Không có bản ghi hợp lệ để tạo mẫu import.";
            return RedirectToAction(nameof(Index));
        }

        var rows = await BuildRowsFromRequestsAsync(requests);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Template");

        sheet.Cell(1, 1).Value = "RequestId";
        sheet.Cell(1, 2).Value = "STT";
        sheet.Cell(1, 3).Value = "MNV";
        sheet.Cell(1, 4).Value = "Phòng ban";
        sheet.Cell(1, 5).Value = "Họ tên";
        sheet.Cell(1, 6).Value = "SĐT";
        sheet.Cell(1, 7).Value = "Chiều đi";
        sheet.Cell(1, 8).Value = "Chiều về";
        sheet.Cell(1, 9).Value = "DriverInfo";
        sheet.Cell(1, 10).Value = "ImportResult";

        var rowIndex = 2;
        foreach (var item in rows.OrderBy(x => x.Id))
        {
            sheet.Cell(rowIndex, 1).Value = item.Id;
            sheet.Cell(rowIndex, 2).Value = rowIndex - 1;
            sheet.Cell(rowIndex, 3).Value = item.EmployeeCode ?? string.Empty;
            sheet.Cell(rowIndex, 4).Value = item.DepartmentName ?? string.Empty;
            sheet.Cell(rowIndex, 5).Value = item.EmployeeName ?? string.Empty;
            sheet.Cell(rowIndex, 6).Value = item.Phone ?? string.Empty;
            sheet.Cell(rowIndex, 7).Value = $"{item.PickupGo:dd/MM/yyyy HH:mm} | {item.FromGo} -> {item.ToGo}";
            sheet.Cell(rowIndex, 8).Value = item.PickupReturn.HasValue
                ? $"{item.PickupReturn:dd/MM/yyyy HH:mm} | {item.FromReturn} -> {item.ToReturn}"
                : string.Empty;
            sheet.Cell(rowIndex, 9).Value = item.DriverInfo ?? string.Empty;
            rowIndex++;
        }

        var headerRange = sheet.Range(1, 1, 1, 10);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.Column(1).Hide();
        sheet.Column(9).Style.Alignment.WrapText = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = $"OtherTrip_GaDriver_Template_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportGaBatch(IFormFile? importFile)
    {
        if (importFile == null || importFile.Length == 0)
        {
            TempData["OtherTripRequestApprovalError"] = "Vui lòng chọn file Excel để import.";
            return RedirectToAction(nameof(Index));
        }

        if (!await CanCurrentUserProcessAsync())
        {
            TempData["OtherTripRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return RedirectToAction(nameof(Index));
        }

        var currentAdid = GetCurrentAdid();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        var actor = string.IsNullOrWhiteSpace(currentAdid) ? currentEmployeeCode : currentAdid;

        await using var inputStream = importFile.OpenReadStream();
        using var workbook = new XLWorkbook(inputStream);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            TempData["OtherTripRequestApprovalError"] = "Không đọc được sheet dữ liệu.";
            return RedirectToAction(nameof(Index));
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRow < 2)
        {
            TempData["OtherTripRequestApprovalError"] = "File import không có dữ liệu.";
            return RedirectToAction(nameof(Index));
        }

        var rows = new List<(int RowNo, int RequestId, string DriverInfo)>();
        for (var r = 2; r <= lastRow; r++)
        {
            var idText = sheet.Cell(r, 1).GetString()?.Trim();
            var valueText = sheet.Cell(r, 9).GetString() ?? string.Empty;
            var requestId = int.TryParse(idText, out var parsedId) ? parsedId : 0;
            rows.Add((r, requestId, valueText));
        }

        var ids = rows.Select(x => x.RequestId).Where(x => x > 0).Distinct().ToList();
        var processable = await GetProcessableRequestsAsync(ids, currentAdid, currentEmployeeCode);
        var processableMap = processable.ToDictionary(x => x.ID, x => x);

        var errorMap = new Dictionary<int, string>();
        var success = 0;

        foreach (var row in rows)
        {
            if (row.RequestId <= 0)
            {
                errorMap[row.RowNo] = "RequestId không hợp lệ.";
                continue;
            }

            if (!processableMap.TryGetValue(row.RequestId, out var req))
            {
                errorMap[row.RowNo] = "Bản ghi không hợp lệ hoặc không thuộc quyền xử lý.";
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.DriverInfo))
            {
                errorMap[row.RowNo] = "Thiếu thông tin lái xe.";
                continue;
            }

            var entity = BuildUpdateEntity(req, currentAdid);
            entity.INT_STATUS = TripRequestStatuses.Completed;
            entity.CHR_STATUS = ResolveStatusText(entity.INT_STATUS);
            entity.NVCHR_DRIVER_INFO = row.DriverInfo.Trim();

            var updateResp = await _tmTripRequestService.UpdateAsync(entity);
            if (!updateResp.Success)
            {
                errorMap[row.RowNo] = updateResp.Message ?? "Không thể cập nhật thông tin lái xe.";
                continue;
            }

            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_TRIP_REQEST_ID = req.ID,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = entity.INT_STATUS,
                BIT_ACTION = true,
                CHR_ACTION = "OTHER_TRIP_GA_DRIVER_FILLED",
                DTM_ACTION = DateTime.Now
            });

            await SendCompletedMailToEmployeeAsync(req, entity.NVCHR_DRIVER_INFO);
            success++;
        }

        sheet.Cell(1, 10).Value = "ImportResult";
        sheet.Cell(1, 10).Style.Font.Bold = true;

        foreach (var row in rows)
        {
            if (errorMap.TryGetValue(row.RowNo, out var err))
            {
                sheet.Cell(row.RowNo, 10).Value = err;
                sheet.Cell(row.RowNo, 10).Style.Font.FontColor = XLColor.Red;
            }
            else
            {
                sheet.Cell(row.RowNo, 10).Value = "OK";
                sheet.Cell(row.RowNo, 10).Style.Font.FontColor = XLColor.Green;
            }
        }
        sheet.Columns().AdjustToContents();

        if (errorMap.Any())
        {
            using var errorStream = new MemoryStream();
            workbook.SaveAs(errorStream);
            var errorFileName = $"OtherTrip_ImportErrors_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(errorStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", errorFileName);
        }

        TempData["OtherTripRequestApprovalSuccess"] = $"Đã cập nhật thông tin lái xe cho {success} bản ghi.";
        TempData["OtherTripRequestApprovalCelebrate"] = "1";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<TM_TRIP_REQUESTDTO>> GetProcessableRequestsAsync(List<int> ids, string currentAdid, string currentEmployeeCode)
    {
        var resp = await _tmTripRequestService.GetByIdsAsync(ids);
        var requests = resp?.Data?.ToList() ?? new List<TM_TRIP_REQUESTDTO>();
        var allowedOtherTypes = await LoadRequestTypeNamesByGroupAsync(OtherTripRequestTypeGroup);
        if (allowedOtherTypes.Any())
        {
            requests = requests
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_TRIP_TYPE)
                    && allowedOtherTypes.Contains(x.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        return requests.Where(x =>
            x.INT_STATUS == TripRequestStatuses.PendingQlsc
            && string.Equals(x.CHR_TRIP_TYPE, OtherTripRequestConstants.TripTypeCode, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private async Task<List<TripRequestRowViewModel>> LoadPendingRowsAsync()
    {
        var resp = await _tmTripRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "=", Value = TripRequestStatuses.PendingQlsc },
                new() { Field = nameof(TM_TRIP_REQUEST.CHR_TRIP_TYPE), Operator = "=", Value = OtherTripRequestConstants.TripTypeCode }
            },
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_TRIP_REQUEST.DTM_CREATE), SortDirection = "DESC" }
            }
        });

        var requests = resp?.Data?.Data?.ToList() ?? new List<TM_TRIP_REQUESTDTO>();
        var allowedOtherTypes = await LoadRequestTypeNamesByGroupAsync(OtherTripRequestTypeGroup);
        if (allowedOtherTypes.Any())
        {
            requests = requests
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_TRIP_TYPE)
                    && allowedOtherTypes.Contains(x.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
        return await BuildRowsFromRequestsAsync(requests);
    }

    private async Task<List<TripRequestRowViewModel>> BuildRowsFromRequestsAsync(List<TM_TRIP_REQUESTDTO> requests)
    {
        var employeeCodes = requests.Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID)).Select(x => x.CHR_EMPLOYEE_ID!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var employeeMap = new Dictionary<string, TM_EMPLOYEEDTO>(StringComparer.OrdinalIgnoreCase);
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

            employeeMap = (employeeResp?.Data?.Data?.ToList() ?? new List<TM_EMPLOYEEDTO>())
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
                .GroupBy(x => x.CHR_EMPLOYEE_ID!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        var sectionResp = await _sectionService.GetAllAsync();
        var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
        var sectionMap = sections
            .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
            .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        return requests.Select(req =>
        {
            employeeMap.TryGetValue(req.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var emp);
            var deptCode = emp?.CHR_CODE_SEC?.Trim() ?? string.Empty;
            sectionMap.TryGetValue(deptCode, out var sec);

            return new TripRequestRowViewModel
            {
                Id = req.ID,
                DepartmentCode = deptCode,
                DepartmentName = sec?.CHR_NAME ?? sec?.CHR_SECTION_NAME ?? deptCode,
                EmployeeCode = req.CHR_EMPLOYEE_ID,
                EmployeeName = emp?.NVCHR_EMPLOYEE_NAME,
                Phone = emp?.CHR_PHONE,
                TripType = req.NVCHR_TRIP_TYPE,
                PickupGo = req.DTM_PICKUP_GO,
                FromGo = req.NVCHR_FROM_LOCATION_GO,
                ToGo = req.NVCHR_TO_LOCATION_GO,
                PickupReturn = req.DTM_PICKUP_RETURN,
                FromReturn = req.NVCHR_FROM_LOCATION_RETURN,
                ToReturn = req.NVCHR_TO_LOCATION_RETURN,
                DriverInfo = req.NVCHR_DRIVER_INFO,
                Status = req.INT_STATUS,
                StatusText = ResolveStatusText(req.INT_STATUS),
                StatusRowClass = TripRequestStatusUiHelper.GetRowClass(req.INT_STATUS),
                StatusBadgeClass = TripRequestStatusUiHelper.GetBadgeClass(req.INT_STATUS)
            };
        }).ToList();
    }

    private async Task<TripRequestApprovalHistoryPageViewModel?> BuildHistoryViewModelAsync(int id)
    {
        var requestResp = await _tmTripRequestService.GetByIdAsync(id);
        var request = requestResp?.Data;
        if (request == null || !string.Equals(request.CHR_TRIP_TYPE, OtherTripRequestConstants.TripTypeCode, StringComparison.OrdinalIgnoreCase)) return null;

        var allowedOtherTypes = await LoadRequestTypeNamesByGroupAsync(OtherTripRequestTypeGroup);
        if (allowedOtherTypes.Any()
            && (string.IsNullOrWhiteSpace(request.NVCHR_TRIP_TYPE)
                || !allowedOtherTypes.Contains(request.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase)))
        {
            return null;
        }

        TM_EMPLOYEEDTO? employee = null;
        var employeeName = string.Empty;
        var departmentName = string.Empty;
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

            var deptCode = employee?.CHR_CODE_SEC?.Trim();
            if (!string.IsNullOrWhiteSpace(deptCode))
            {
                var sectionResp = await _sectionService.GetAllAsync();
                var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
                var section = sections.FirstOrDefault(x => string.Equals(x.CHR_CODE?.Trim(), deptCode, StringComparison.OrdinalIgnoreCase));
                departmentName = section?.CHR_NAME ?? section?.CHR_SECTION_NAME ?? deptCode;
            }
        }

        var historyResp = await _tmApprovalHistoryService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_APPROVAL_HISTORY.INT_TRIP_REQEST_ID), Operator = "=", Value = id }
            },
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_APPROVAL_HISTORY.DTM_ACTION), SortDirection = "ASC" },
                new SortOption { Field = nameof(TM_APPROVAL_HISTORY.ID), SortDirection = "ASC" }
            }
        });

        var histories = historyResp?.Data?.Data?.ToList() ?? new List<TM_APPROVAL_HISTORYDTO>();
        return new TripRequestApprovalHistoryPageViewModel
        {
            RequestId = request.ID,
            DepartmentName = departmentName,
            EmployeeCode = request.CHR_EMPLOYEE_ID,
            EmployeeName = employeeName,
            Phone = employee?.CHR_PHONE,
            PickupGo = request.DTM_PICKUP_GO,
            FromGo = request.NVCHR_FROM_LOCATION_GO,
            ToGo = request.NVCHR_TO_LOCATION_GO,
            PickupReturn = request.DTM_PICKUP_RETURN,
            FromReturn = request.NVCHR_FROM_LOCATION_RETURN,
            ToReturn = request.NVCHR_TO_LOCATION_RETURN,
            DriverInfo = request.NVCHR_DRIVER_INFO,
            Status = request.INT_STATUS,
            StatusText = ResolveStatusText(request.INT_STATUS),
            StatusBadgeClass = TripRequestStatusUiHelper.GetBadgeClass(request.INT_STATUS),
            Histories = histories.Select(x => new TripRequestApprovalHistoryItemViewModel
            {
                Action = TranslateHistoryAction(x.CHR_ACTION),
                Actor = x.CHR_CONFIRMER_APPROVER,
                ActionTime = x.DTM_ACTION,
                Reason = x.NVCHR_REASON_REJECT,
                Result = x.BIT_ACTION
            }).ToList(),
            Steps = BuildWorkflowSteps(histories)
        };
    }

    private static List<TripRequestWorkflowStepViewModel> BuildWorkflowSteps(List<TM_APPROVAL_HISTORYDTO> histories)
    {
        TripRequestWorkflowStepViewModel Build(string step, params string[] actions)
        {
            var action = histories.LastOrDefault(x => actions.Any(a => string.Equals(a, x.CHR_ACTION, StringComparison.OrdinalIgnoreCase)));
            if (action == null)
            {
                return new TripRequestWorkflowStepViewModel
                {
                    StepName = step,
                    StateText = "Chưa xử lý",
                    CssClass = "workflow-step-pending"
                };
            }

            return new TripRequestWorkflowStepViewModel
            {
                StepName = step,
                StateText = "Đã xử lý",
                CssClass = "workflow-step-done",
                Actor = action.CHR_CONFIRMER_APPROVER,
                ActionTime = action.DTM_ACTION,
                Reason = action.NVCHR_REASON_REJECT
            };
        }

        return new List<TripRequestWorkflowStepViewModel>
        {
            Build("Quản lý trung cấp đăng ký", "CREATE_OTHER_TRIP_REQUEST"),
            Build("GA điền thông tin lái xe", "OTHER_TRIP_GA_DRIVER_FILLED")
        };
    }

    private TM_TRIP_REQUEST BuildUpdateEntity(TM_TRIP_REQUESTDTO dto, string actor)
    {
        return new TM_TRIP_REQUEST
        {
            ID = dto.ID,
            CHR_USER_CREATE = dto.CHR_USER_CREATE,
            DTM_CREATE = dto.DTM_CREATE,
            CHR_USER_UPDATE = actor,
            DTM_UPDATE = DateTime.Now,
            CHR_EMPLOYEE_ID = dto.CHR_EMPLOYEE_ID,
            CHR_TRIP_TYPE = dto.CHR_TRIP_TYPE,
            NVCHR_TRIP_TYPE = dto.NVCHR_TRIP_TYPE,
            DTM_PICKUP_GO = dto.DTM_PICKUP_GO,
            DTM_PICKUP_RETURN = dto.DTM_PICKUP_RETURN,
            NVCHR_FROM_LOCATION_GO = dto.NVCHR_FROM_LOCATION_GO,
            NVCHR_TO_LOCATION_GO = dto.NVCHR_TO_LOCATION_GO,
            NVCHR_FROM_LOCATION_RETURN = dto.NVCHR_FROM_LOCATION_RETURN,
            NVCHR_TO_LOCATION_RETURN = dto.NVCHR_TO_LOCATION_RETURN,
            NVCHR_DRIVER_INFO = dto.NVCHR_DRIVER_INFO,
            INT_STATUS = dto.INT_STATUS,
            CHR_STATUS = dto.CHR_STATUS
        };
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

    private static string TranslateHistoryAction(string? action)
    {
        return action?.Trim().ToUpperInvariant() switch
        {
            "CREATE_OTHER_TRIP_REQUEST" => "Tạo đơn xe khác",
            "OTHER_TRIP_GA_DRIVER_FILLED" => "GA điền thông tin lái xe",
            _ => action ?? string.Empty
        };
    }

    private async Task<bool> CanCurrentUserProcessAsync()
    {
        var template = await GetMailTemplateByCodeAsync("OTHER_TRIP_REQUEST_GA_DRIVER_INFO");
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

    private async Task SendCompletedMailToEmployeeAsync(TM_TRIP_REQUESTDTO request, string? driverInfo)
    {
        try
        {
            var employeeMail = await ResolveEmployeeMailAsync(request.CHR_EMPLOYEE_ID);
            if (string.IsNullOrWhiteSpace(employeeMail)) return;

            var employeeName = await ResolveEmployeeNameByCodeAsync(request.CHR_EMPLOYEE_ID);
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{To}}"] = employeeName,
                ["{{ToEmail}}"] = employeeMail,
                ["{{RequestId}}"] = request.ID.ToString(),
                ["{{DriverInfo}}"] = await BuildDriverInfoTableHtmlAsync(request, driverInfo),
                ["{{ApproveLink}}"] = BuildTripRequestListLink()
            };

            await SendTemplatedMailAsync(
                "OTHER_TRIP_REQUEST_COMPLETED",
                tokens,
                "[TEST][BusManagement] Đơn đăng ký xe đã hoàn thành",
                """
                <p style="font-size:20px;margin:0 0 30px 0;">Nhóm quản lý xe Công ty gửi thông tin xe về nửa ngày theo thông tin đã đăng ký</p>
                <p style="font-size:20px;margin:0 0 10px 0;">Anh/chị lưu ý thời gian đã đăng ký</p>
                <p style="font-size:20px;margin:0 0 10px 0;">Sau khi kết thúc chuyến đi Anh/chị vui lòng ký xác nhận biên bản cho lái xe</p>
                <p style="font-size:20px;margin:0 0 10px 0;">Thông tin chi tiết như sau:</p>
                <p style="font-size:20px;margin:0 0 30px 0;white-space:pre-line;">{{DriverInfo}}</p>
                <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng kiểm tra tại: <a href="{{ApproveLink}}">LINK</a></p>
                """,
                employeeMail);
        }
        catch
        {
            // ignore
        }
    }

    private async Task SendTemplatedMailAsync(string templateCode, Dictionary<string, string> tokens, string fallbackSubject, string fallbackMessageHtml, string toEmail)
    {
        var template = await GetMailTemplateByCodeAsync(templateCode);

        var title = ReplaceTemplateTokens(template?.CHR_SUBJECT, tokens);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = fallbackSubject;
        }

        var bodyTemplate = template == null
            ? ResolveFallbackBodyTemplate(fallbackMessageHtml)
            : template.CHR_CONTENT ?? string.Empty;
        var body = ReplaceTemplateTokens(bodyTemplate, tokens);

        var from = ReplaceTemplateTokens(template?.CHR_FROM, tokens);
        if (string.IsNullOrWhiteSpace(from))
        {
            from = "zzpsvvmg@brother-bivn.com.vn";
        }

        var toTemplate = ReplaceTemplateTokens(template?.CHR_TO, tokens);
        var to = CombineEmails(toEmail, toTemplate);
        if (string.IsNullOrWhiteSpace(to)) return;

        var cc = ReplaceTemplateTokens(template?.CHR_CC, tokens);
        var bcc = ReplaceTemplateTokens(template?.CHR_BCC, tokens);
        var priority = template?.BIT_HIGHT == true ? 1 : 0;

        _tmMasterMailService.sendEmailNotify(title, from, to, cc ?? string.Empty, bcc ?? string.Empty, body, priority);
    }

    private static string ResolveFallbackBodyTemplate(string fallbackMessageHtml)
    {
        var value = fallbackMessageHtml ?? string.Empty;
        return value.Contains("<html", StringComparison.OrdinalIgnoreCase)
            ? value
            : BuildDefaultMailBody(value);
    }

    private static string BuildDefaultMailBody(string contentHtml)
    {
        var template = """
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />
                <title>Thông báo hệ thống xe công ty</title>
            </head>
            <body style="margin:0;padding:0;background-color:#ffffff;font-family:'Times New Roman', Times, serif;">
                <div style="width:100%;max-width:800px;margin:0 auto;">
                    <p style="color:red;font-size:24px;font-weight:bold;margin:0 0 30px 0;">Đây là mail tự động, vui lòng không Reply lại mail!</p>
                    <p style="font-size:20px;margin:0 0 30px 0;">Gửi anh/chị {{To}},</p>
                    __CONTENT__
                    <p style="font-size:20px;margin:0 0 10px 0;">Xin cảm ơn!</p>
                    <p style="font-size:20px;font-style:italic;margin:0;">Hệ thống quản lý xe Công ty-BIVN</p>
                </div>
            </body>
            </html>
            """;

        return template.Replace("__CONTENT__", contentHtml ?? string.Empty, StringComparison.Ordinal);
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

    private async Task<string> BuildDriverInfoTableHtmlAsync(TM_TRIP_REQUESTDTO request, string? driverInfo)
    {
        var (employeeName, phone, departmentName) = await ResolveEmployeeDisplayInfoAsync(request.CHR_EMPLOYEE_ID);
        var safeDriverInfo = FormatDriverInfoForMail(driverInfo);

        return $"""
            <table style="width:100%;border-collapse:collapse;margin:0 0 30px 0;font-size:16px;">
                <thead>
                    <tr>
                        <th style="border:1px solid #333;padding:8px;">No</th>
                        <th style="border:1px solid #333;padding:8px;">MNV</th>
                        <th style="border:1px solid #333;padding:8px;">Phòng ban</th>
                        <th style="border:1px solid #333;padding:8px;">Họ và Tên</th>
                        <th style="border:1px solid #333;padding:8px;">SĐT</th>
                        <th style="border:1px solid #333;padding:8px;">Ngày đón Chiều đi</th>
                        <th style="border:1px solid #333;padding:8px;">Giờ đón Chiều đi</th>
                        <th style="border:1px solid #333;padding:8px;">Điểm đón</th>
                        <th style="border:1px solid #333;padding:8px;">Điểm trả</th>
                        <th style="border:1px solid #333;padding:8px;">Thông tin lái xe</th>
                    </tr>
                </thead>
                <tbody>
                    <tr>
                        <td style="border:1px solid #333;padding:8px;">1</td>
                        <td style="border:1px solid #333;padding:8px;">{System.Net.WebUtility.HtmlEncode(request.CHR_EMPLOYEE_ID ?? string.Empty)}</td>
                        <td style="border:1px solid #333;padding:8px;">{System.Net.WebUtility.HtmlEncode(departmentName)}</td>
                        <td style="border:1px solid #333;padding:8px;">{System.Net.WebUtility.HtmlEncode(employeeName)}</td>
                        <td style="border:1px solid #333;padding:8px;">{System.Net.WebUtility.HtmlEncode(phone)}</td>
                        <td style="border:1px solid #333;padding:8px;">{request.DTM_PICKUP_GO?.ToString("dd/MM/yyyy") ?? string.Empty}</td>
                        <td style="border:1px solid #333;padding:8px;">{request.DTM_PICKUP_GO?.ToString("HH:mm") ?? string.Empty}</td>
                        <td style="border:1px solid #333;padding:8px;">{System.Net.WebUtility.HtmlEncode(request.NVCHR_FROM_LOCATION_GO ?? string.Empty)}</td>
                        <td style="border:1px solid #333;padding:8px;">{System.Net.WebUtility.HtmlEncode(request.NVCHR_TO_LOCATION_GO ?? string.Empty)}</td>
                        <td style="border:1px solid #333;padding:8px;white-space:pre-line;">{safeDriverInfo}</td>
                    </tr>
                </tbody>
            </table>
            """;
    }

    private async Task<(string EmployeeName, string Phone, string DepartmentName)> ResolveEmployeeDisplayInfoAsync(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode)) return (string.Empty, string.Empty, string.Empty);

        var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = employeeCode.Trim() }
            }
        });

        var emp = employeeResp?.Data?.Data?.FirstOrDefault();
        var employeeName = emp?.NVCHR_EMPLOYEE_NAME ?? employeeCode.Trim();
        var phone = emp?.CHR_PHONE ?? string.Empty;
        var departmentCode = emp?.CHR_CODE_SEC?.Trim();
        var departmentName = departmentCode ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(departmentCode))
        {
            var sectionResp = await _sectionService.GetAllAsync();
            var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
            var section = sections.FirstOrDefault(x => string.Equals(x.CHR_CODE?.Trim(), departmentCode, StringComparison.OrdinalIgnoreCase));
            departmentName = section?.CHR_NAME ?? section?.CHR_SECTION_NAME ?? departmentCode;
        }

        return (employeeName, phone, departmentName);
    }

    private static string FormatDriverInfoForMail(string? driverInfo)
    {
        var value = driverInfo ?? string.Empty;
        return System.Net.WebUtility.HtmlEncode(value)
            .Replace("\r\n", "<br />", StringComparison.Ordinal)
            .Replace("\n", "<br />", StringComparison.Ordinal)
            .Replace("\r", "<br />", StringComparison.Ordinal);
    }

    private async Task<string> ResolveEmployeeMailAsync(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode)) return string.Empty;
        var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = employeeCode.Trim() }
            }
        });

        var emp = employeeResp?.Data?.Data?.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(emp?.CHR_EMPLOYEE_MAIL)) return emp.CHR_EMPLOYEE_MAIL!;
        if (!string.IsNullOrWhiteSpace(emp?.CHR_EMPLOYEE_ADID)) return ToCompanyEmail(emp.CHR_EMPLOYEE_ADID);
        return ToCompanyEmail(employeeCode);
    }

    private async Task<string> ResolveEmployeeNameByCodeAsync(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode)) return string.Empty;
        var resp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = employeeCode.Trim() }
            }
        });

        return resp?.Data?.Data?.FirstOrDefault()?.NVCHR_EMPLOYEE_NAME ?? employeeCode;
    }

    private string BuildTripRequestListLink()
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var baseUrl = configuration?["SystemLinks:BusManagementWebApp"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = Request?.Host.HasValue == true
                ? $"{Request.Scheme}://{Request.Host.Value}"
                : string.Empty;
        }

        var relativePath = Url.Action("Index", "OtherTripRequest") ?? "/OtherTripRequest";
        if (string.IsNullOrWhiteSpace(baseUrl)) return relativePath;
        return $"{baseUrl.TrimEnd('/')}{relativePath}";
    }

    private static string ToCompanyEmail(string? adid)
    {
        if (string.IsNullOrWhiteSpace(adid)) return string.Empty;
        var v = adid.Trim();
        return v.Contains('@') ? v : $"{v}@brothergroup.net";
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

    private string GetCurrentAdid()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "adid")?.Value?.Trim() ?? string.Empty;
    }

    private string GetCurrentEmail()
    {
        var email = User?.Claims?.FirstOrDefault(c => c.Type == "email")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(email)) return email;

        var adid = GetCurrentAdid();
        return string.IsNullOrWhiteSpace(adid) ? string.Empty : ToCompanyEmail(adid);
    }

    private string GetCurrentEmployeeCode()
    {
        return User?.Claims?.FirstOrDefault(c => c.Type == "employee_code")?.Value?.Trim() ?? string.Empty;
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
}
