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

public class TripRequestApprovalController : Controller
{
    private const int TripRequestTypeGroup = 2;

    private readonly ITmTripRequestService _tmTripRequestService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly ITmApprovalHistoryService _tmApprovalHistoryService;
    private readonly ITmRoleService _tmRoleService;
    private readonly ISectionService _sectionService;
    private readonly IWebHostEnvironment _environment;
    private readonly ITmMasterMailService _tmMasterMailService;
    private readonly IActiveDirectoryService _activeDirectoryService;
    private readonly ITmRequestTypeService _tmRequestTypeService;

    public TripRequestApprovalController(
        ITmTripRequestService tmTripRequestService,
        ITmEmployeeService tmEmployeeService,
        ITmApprovalHistoryService tmApprovalHistoryService,
        ITmRoleService tmRoleService,
        ISectionService sectionService,
        IWebHostEnvironment environment,
        ITmMasterMailService tmMasterMailService,
        IActiveDirectoryService activeDirectoryService,
        ITmRequestTypeService tmRequestTypeService)
    {
        _tmTripRequestService = tmTripRequestService;
        _tmEmployeeService = tmEmployeeService;
        _tmApprovalHistoryService = tmApprovalHistoryService;
        _tmRoleService = tmRoleService;
        _sectionService = sectionService;
        _environment = environment;
        _tmMasterMailService = tmMasterMailService;
        _activeDirectoryService = activeDirectoryService;
        _tmRequestTypeService = tmRequestTypeService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? stage)
    {
        var normalizedStage = NormalizeStage(stage);
        var vm = new TripRequestApprovalPageViewModel
        {
            Stage = normalizedStage,
            StageTitle = ResolveStageTitle(normalizedStage),
            CanProcess = true
        };

        var currentAdid = GetCurrentAdid();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        if (string.IsNullOrWhiteSpace(currentAdid) && !string.Equals(normalizedStage, "employee", StringComparison.OrdinalIgnoreCase))
        {
            vm.CanProcess = false;
            vm.AccessDeniedMessage = "Không xác định được tài khoản đăng nhập.";
            return View(vm);
        }

        if ((normalizedStage == "ga-quote" || normalizedStage == "ga-driver")
            && !await CanCurrentUserProcessTripGaAsync(normalizedStage))
        {
            vm.CanProcess = false;
            vm.AccessDeniedMessage = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return View(vm);
        }

        vm.Requests = await LoadPendingRowsAsync(normalizedStage, currentAdid, currentEmployeeCode);
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
        var stage = NormalizeStage(input.Stage);
        var ids = ParseIds(input.SelectedIds);
        if (!ids.Any())
        {
            TempData["TripRequestApprovalError"] = "Vui lòng chọn ít nhất 1 bản ghi.";
            return RedirectToAction(nameof(Index), new { stage });
        }

        var currentAdid = GetCurrentAdid();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        if ((stage == "ga-driver") && !await CanCurrentUserProcessTripGaAsync(stage))
        {
            TempData["TripRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return RedirectToAction(nameof(Index), new { stage });
        }

        var requests = await GetProcessableRequestsAsync(ids, stage, currentAdid, currentEmployeeCode);
        if (!requests.Any())
        {
            TempData["TripRequestApprovalError"] = "Không có bản ghi hợp lệ để xử lý.";
            return RedirectToAction(nameof(Index), new { stage });
        }

        var success = 0;
        foreach (var req in requests)
        {
            var entity = BuildUpdateEntity(req, currentAdid);
            var action = string.Empty;
            var actor = string.IsNullOrWhiteSpace(currentAdid) ? currentEmployeeCode : currentAdid;

            if (stage == "qlsc")
            {
                entity.INT_STATUS = TripRequestStatuses.QlscApproved;
                action = "TRIP_QLSC_APPROVE";
            }
            else if (stage == "qltc")
            {
                entity.INT_STATUS = TripRequestStatuses.QltcApproved;
                action = "TRIP_QLTC_APPROVE";
            }
            else if (stage == "employee")
            {
                entity.INT_STATUS = TripRequestStatuses.EmployeeAcceptedQuote;
                entity.BIT_QUOTE_CONFIRM = true;
                action = "TRIP_EMPLOYEE_ACCEPT_QUOTE";
            }
            else if (stage == "ga-driver")
            {
                entity.INT_STATUS = TripRequestStatuses.Completed;
                entity.NVCHR_DRIVER_INFO = input.DriverInfo?.Trim();
                action = "TRIP_GA_DRIVER_FILLED";
            }
            else
            {
                continue;
            }

            entity.CHR_STATUS = TripRequestStatusUiHelper.GetStatusText(entity.INT_STATUS);

            var updateResp = await _tmTripRequestService.UpdateAsync(entity);
            if (!updateResp.Success) continue;

            success++;
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_TRIP_REQEST_ID = req.ID,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = entity.INT_STATUS,
                BIT_ACTION = true,
                CHR_ACTION = action,
                DTM_ACTION = DateTime.Now
            });

            await SendNextStageMailAsync(stage, req, entity, input.DriverInfo);
        }

        TempData[success > 0 ? "TripRequestApprovalSuccess" : "TripRequestApprovalError"] = success > 0
            ? $"Xử lý thành công {success} bản ghi."
            : "Không thể xử lý bản ghi đã chọn.";
        if (success > 0)
        {
            TempData["TripRequestApprovalCelebrate"] = "1";
        }

        return RedirectToAction(nameof(Index), new { stage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(TripRequestApprovalActionInputModel input)
    {
        var stage = NormalizeStage(input.Stage);
        var ids = ParseIds(input.SelectedIds);
        if (!ids.Any())
        {
            TempData["TripRequestApprovalError"] = "Vui lòng chọn ít nhất 1 bản ghi.";
            return RedirectToAction(nameof(Index), new { stage });
        }

        if (string.IsNullOrWhiteSpace(input.RejectReason))
        {
            TempData["TripRequestApprovalError"] = "Vui lòng nhập lý do từ chối.";
            return RedirectToAction(nameof(Index), new { stage });
        }

        var currentAdid = GetCurrentAdid();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        if ((stage == "ga-quote" || stage == "ga-driver") && !await CanCurrentUserProcessTripGaAsync(stage))
        {
            TempData["TripRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return RedirectToAction(nameof(Index), new { stage });
        }

        var requests = await GetProcessableRequestsAsync(ids, stage, currentAdid, currentEmployeeCode);
        if (!requests.Any())
        {
            TempData["TripRequestApprovalError"] = "Không có bản ghi hợp lệ để xử lý.";
            return RedirectToAction(nameof(Index), new { stage });
        }

        var success = 0;
        foreach (var req in requests)
        {
            var entity = BuildUpdateEntity(req, currentAdid);
            var action = string.Empty;
            var actor = string.IsNullOrWhiteSpace(currentAdid) ? currentEmployeeCode : currentAdid;

            if (stage == "qlsc")
            {
                entity.INT_STATUS = TripRequestStatuses.QlscRejected;
                action = "TRIP_QLSC_REJECT";
            }
            else if (stage == "qltc")
            {
                entity.INT_STATUS = TripRequestStatuses.QltcRejected;
                action = "TRIP_QLTC_REJECT";
            }
            else if (stage == "ga-quote" || stage == "ga-driver")
            {
                entity.INT_STATUS = TripRequestStatuses.GaRejected;
                action = "TRIP_GA_REJECT";
            }
            else if (stage == "employee")
            {
                entity.INT_STATUS = TripRequestStatuses.EmployeeRejectedQuote;
                action = "TRIP_EMPLOYEE_REJECT_QUOTE";
            }
            else
            {
                continue;
            }

            entity.CHR_STATUS = TripRequestStatusUiHelper.GetStatusText(entity.INT_STATUS);
            entity.BIT_QUOTE_CONFIRM = stage == "employee" ? false : entity.BIT_QUOTE_CONFIRM;
            var updateResp = await _tmTripRequestService.UpdateAsync(entity);
            if (!updateResp.Success) continue;

            success++;
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_TRIP_REQEST_ID = req.ID,
                CHR_CONFIRMER_APPROVER = actor,
                INT_ACTION = entity.INT_STATUS,
                BIT_ACTION = false,
                CHR_ACTION = action,
                DTM_ACTION = DateTime.Now,
                NVCHR_REASON_REJECT = input.RejectReason?.Trim()
            });

            if (stage == "employee")
            {
                await SendMailToGaGroupAsync(
                    "TRIP_REQUEST_GA_REQUOTE",
                    "[TEST][BusManagement] CNV từ chối báo giá",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["{{RequestId}}"] = req.ID.ToString(),
                        ["{{RejectReason}}"] = input.RejectReason?.Trim() ?? string.Empty,
                        ["{{ApproveLink}}"] = BuildTripRequestApprovalLink("ga-quote")
                    },
                    """
                    <p style="font-size:20px;margin:0 0 30px 0;">Đơn #{{RequestId}} bị CNV từ chối báo giá.</p>
                    <p style="font-size:20px;margin:0 0 30px 0;">Lý do: {{RejectReason}}</p>
                    <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng nhập lại báo giá: <a href="{{ApproveLink}}">LINK</a></p>
                    """);
            }
            else
            {
                await SendRejectMailToEmployeeAsync(req, input.RejectReason?.Trim() ?? string.Empty, stage);
            }
        }

        TempData[success > 0 ? "TripRequestApprovalSuccess" : "TripRequestApprovalError"] = success > 0
            ? $"Từ chối thành công {success} bản ghi."
            : "Không thể xử lý bản ghi đã chọn.";

        return RedirectToAction(nameof(Index), new { stage });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitQuote(IFormFile? quoteFile, string stage, string selectedIds)
    {
        var normalizedStage = NormalizeStage(stage);
        if (normalizedStage != "ga-quote")
        {
            TempData["TripRequestApprovalError"] = "Bước xử lý không hợp lệ.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }
        var ids = ParseIds(selectedIds);
        if (!ids.Any())
        {
            TempData["TripRequestApprovalError"] = "Vui lòng chọn ít nhất 1 bản ghi.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        if (quoteFile == null || quoteFile.Length == 0)
        {
            TempData["TripRequestApprovalError"] = "Vui lòng chọn file báo giá.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var currentAdid = GetCurrentAdid();
        if (!await CanCurrentUserProcessTripGaAsync(normalizedStage))
        {
            TempData["TripRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var requests = await GetProcessableRequestsAsync(ids, normalizedStage, currentAdid, GetCurrentEmployeeCode());
        if (!requests.Any())
        {
            TempData["TripRequestApprovalError"] = "Không có bản ghi hợp lệ để xử lý.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var quotePath = await SaveQuoteFileAsync(quoteFile);
        var success = 0;
        foreach (var req in requests)
        {
            var entity = BuildUpdateEntity(req, currentAdid);
            entity.INT_STATUS = TripRequestStatuses.QuoteSentByGa;
            entity.CHR_STATUS = TripRequestStatusUiHelper.GetStatusText(entity.INT_STATUS);
            entity.CHR_QUOTE_PATH = quotePath;
            entity.CHR_GA_CONFIRM = currentAdid;

            var updateResp = await _tmTripRequestService.UpdateAsync(entity);
            if (!updateResp.Success) continue;

            success++;
            await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
            {
                INT_TRIP_REQEST_ID = req.ID,
                CHR_CONFIRMER_APPROVER = currentAdid,
                INT_ACTION = entity.INT_STATUS,
                BIT_ACTION = true,
                CHR_ACTION = "TRIP_GA_SENT_QUOTE",
                DTM_ACTION = DateTime.Now
            });

            await SendQuoteMailToEmployeeAsync(req, quotePath);
        }

        TempData[success > 0 ? "TripRequestApprovalSuccess" : "TripRequestApprovalError"] = success > 0
            ? $"Đã gửi báo giá cho {success} bản ghi."
            : "Không thể gửi báo giá.";
        if (success > 0)
        {
            TempData["TripRequestApprovalCelebrate"] = "1";
        }

        return RedirectToAction(nameof(Index), new { stage = normalizedStage });
    }

    [HttpGet]
    public async Task<IActionResult> DownloadImportTemplate(string stage, string? selectedIds)
    {
        var normalizedStage = NormalizeStage(stage);
        if (normalizedStage != "ga-quote" && normalizedStage != "ga-driver")
        {
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        if (!await CanCurrentUserProcessTripGaAsync(normalizedStage))
        {
            TempData["TripRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var ids = ParseIds(selectedIds);
        if (!ids.Any())
        {
            TempData["TripRequestApprovalError"] = "Vui lòng tick chọn ít nhất 1 bản ghi để tải mẫu import.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var currentAdid = GetCurrentAdid();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        var requests = await GetProcessableRequestsAsync(ids, normalizedStage, currentAdid, currentEmployeeCode);
        if (!requests.Any())
        {
            TempData["TripRequestApprovalError"] = "Không có bản ghi hợp lệ để tạo mẫu import.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var rows = await BuildRowsFromRequestsAsync(requests);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Template");

        sheet.Cell(1, 1).Value = "RequestId";
        sheet.Cell(1, 2).Value = "STT";
        sheet.Cell(1, 3).Value = "Trạng thái";
        sheet.Cell(1, 4).Value = "Phòng ban";
        sheet.Cell(1, 5).Value = "MNV";
        sheet.Cell(1, 6).Value = "Họ tên";
        sheet.Cell(1, 7).Value = "SĐT";
        sheet.Cell(1, 8).Value = "Loại chuyến";
        sheet.Cell(1, 9).Value = "Chiều đi";
        sheet.Cell(1, 10).Value = "Chiều về";
        sheet.Cell(1, 11).Value = "Mục đích";
        sheet.Cell(1, 12).Value = normalizedStage == "ga-quote" ? "QuoteFileLink" : "DriverInfo";
        sheet.Cell(1, 13).Value = "ImportResult";

        var rowIndex = 2;
        foreach (var item in rows.OrderBy(x => x.Id))
        {
            sheet.Cell(rowIndex, 1).Value = item.Id;
            sheet.Cell(rowIndex, 2).Value = rowIndex - 1;
            sheet.Cell(rowIndex, 3).Value = item.StatusText ?? string.Empty;
            sheet.Cell(rowIndex, 4).Value = item.DepartmentName ?? string.Empty;
            sheet.Cell(rowIndex, 5).Value = item.EmployeeCode ?? string.Empty;
            sheet.Cell(rowIndex, 6).Value = item.EmployeeName ?? string.Empty;
            sheet.Cell(rowIndex, 7).Value = item.Phone ?? string.Empty;
            sheet.Cell(rowIndex, 8).Value = item.TripType ?? string.Empty;
            sheet.Cell(rowIndex, 9).Value = $"{item.PickupGo:dd/MM/yyyy HH:mm} | {item.FromGo} -> {item.ToGo}";
            sheet.Cell(rowIndex, 10).Value = item.PickupReturn.HasValue
                ? $"{item.PickupReturn:dd/MM/yyyy HH:mm} | {item.FromReturn} -> {item.ToReturn}"
                : string.Empty;
            sheet.Cell(rowIndex, 11).Value = item.Purpose ?? string.Empty;
            sheet.Cell(rowIndex, 14).Value = normalizedStage == "ga-driver" ? (item.DriverInfo ?? string.Empty) : string.Empty;
            rowIndex++;
        }

        var headerRange = sheet.Range(1, 1, 1, 13);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.Column(1).Hide();
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileName = normalizedStage == "ga-quote"
            ? $"TripRequest_GaQuote_Template_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
            : $"TripRequest_GaDriver_Template_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportGaBatch(IFormFile? importFile, string stage)
    {
        var normalizedStage = NormalizeStage(stage);
        if (normalizedStage != "ga-quote" && normalizedStage != "ga-driver")
        {
            TempData["TripRequestApprovalError"] = "Bước xử lý không hợp lệ.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        if (importFile == null || importFile.Length == 0)
        {
            TempData["TripRequestApprovalError"] = "Vui lòng chọn file Excel để import.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        if (!await CanCurrentUserProcessTripGaAsync(normalizedStage))
        {
            TempData["TripRequestApprovalError"] = "Bạn không thuộc nhóm mail GA được phép xử lý bước này.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var currentAdid = GetCurrentAdid();
        var currentEmployeeCode = GetCurrentEmployeeCode();
        var actor = string.IsNullOrWhiteSpace(currentAdid) ? currentEmployeeCode : currentAdid;

        await using var inputStream = importFile.OpenReadStream();
        using var workbook = new XLWorkbook(inputStream);
        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
        {
            TempData["TripRequestApprovalError"] = "Không đọc được sheet dữ liệu.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        if (lastRow < 2)
        {
            TempData["TripRequestApprovalError"] = "File import không có dữ liệu.";
            return RedirectToAction(nameof(Index), new { stage = normalizedStage });
        }

        var rows = new List<(int RowNo, int RequestId, string InputValue)>();
        for (var r = 2; r <= lastRow; r++)
        {
            var idText = sheet.Cell(r, 1).GetString()?.Trim();
            var valueText = sheet.Cell(r, 12).GetString()?.Trim() ?? string.Empty;
            var requestId = int.TryParse(idText, out var parsedId) ? parsedId : 0;
            rows.Add((r, requestId, valueText));
        }

        var ids = rows.Select(x => x.RequestId).Where(x => x > 0).Distinct().ToList();
        var processable = await GetProcessableRequestsAsync(ids, normalizedStage, currentAdid, currentEmployeeCode);
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

            try
            {
                if (normalizedStage == "ga-quote")
                {
                    if (string.IsNullOrWhiteSpace(row.InputValue))
                    {
                        errorMap[row.RowNo] = "Thiếu đường dẫn file báo giá.";
                        continue;
                    }

                    var quotePath = await SaveQuoteFileFromSourceAsync(row.InputValue);
                    var entity = BuildUpdateEntity(req, currentAdid);
                    entity.INT_STATUS = TripRequestStatuses.QuoteSentByGa;
                    entity.CHR_STATUS = TripRequestStatusUiHelper.GetStatusText(entity.INT_STATUS);
                    entity.CHR_QUOTE_PATH = quotePath;
                    entity.CHR_GA_CONFIRM = currentAdid;

                    var updateResp = await _tmTripRequestService.UpdateAsync(entity);
                    if (!updateResp.Success)
                    {
                        errorMap[row.RowNo] = updateResp.Message ?? "Không thể cập nhật báo giá.";
                        continue;
                    }

                    await _tmApprovalHistoryService.AddAsync(new TM_APPROVAL_HISTORY
                    {
                        INT_TRIP_REQEST_ID = req.ID,
                        CHR_CONFIRMER_APPROVER = actor,
                        INT_ACTION = entity.INT_STATUS,
                        BIT_ACTION = true,
                        CHR_ACTION = "TRIP_GA_SENT_QUOTE",
                        DTM_ACTION = DateTime.Now
                    });

                    await SendQuoteMailToEmployeeAsync(req, quotePath);
                    success++;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(row.InputValue))
                    {
                        errorMap[row.RowNo] = "Thiếu thông tin lái xe.";
                        continue;
                    }

                    var entity = BuildUpdateEntity(req, currentAdid);
                    entity.INT_STATUS = TripRequestStatuses.Completed;
                    entity.CHR_STATUS = TripRequestStatusUiHelper.GetStatusText(entity.INT_STATUS);
                    entity.NVCHR_DRIVER_INFO = row.InputValue.Trim();

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
                        CHR_ACTION = "TRIP_GA_DRIVER_FILLED",
                        DTM_ACTION = DateTime.Now
                    });

                    await SendNextStageMailAsync("ga-driver", req, entity, entity.NVCHR_DRIVER_INFO);
                    success++;
                }
            }
            catch (Exception ex)
            {
                errorMap[row.RowNo] = ex.Message;
            }
        }

        sheet.Cell(1, 13).Value = "ImportResult";
        sheet.Cell(1, 13).Style.Font.Bold = true;

        foreach (var row in rows)
        {
            if (errorMap.TryGetValue(row.RowNo, out var err))
            {
                sheet.Cell(row.RowNo, 13).Value = err;
                sheet.Cell(row.RowNo, 13).Style.Font.FontColor = XLColor.Red;
            }
            else
            {
                sheet.Cell(row.RowNo, 13).Value = "OK";
                sheet.Cell(row.RowNo, 13).Style.Font.FontColor = XLColor.Green;
            }
        }
        sheet.Columns().AdjustToContents();

        if (errorMap.Any())
        {
            using var errorStream = new MemoryStream();
            workbook.SaveAs(errorStream);
            var errorFileName = $"TripRequest_ImportErrors_{normalizedStage}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(errorStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", errorFileName);
        }

        TempData["TripRequestApprovalSuccess"] = normalizedStage == "ga-quote"
            ? $"Đã gửi báo giá cho {success} bản ghi."
            : $"Đã cập nhật thông tin lái xe cho {success} bản ghi.";
        TempData["TripRequestApprovalCelebrate"] = "1";
        return RedirectToAction(nameof(Index), new { stage = normalizedStage });
    }

    private async Task<List<TM_TRIP_REQUESTDTO>> GetProcessableRequestsAsync(List<int> ids, string stage, string currentAdid, string currentEmployeeCode)
    {
        var resp = await _tmTripRequestService.GetByIdsAsync(ids);
        var requests = resp?.Data?.ToList() ?? new List<TM_TRIP_REQUESTDTO>();
        var allowedTripTypeNames = await LoadRequestTypeNamesByGroupAsync(TripRequestTypeGroup);
        if (allowedTripTypeNames.Any())
        {
            requests = requests
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_TRIP_TYPE)
                    && allowedTripTypeNames.Contains(x.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        return requests.Where(x =>
        {
            if (x.INT_STATUS == TripRequestStatuses.Deleted) return false;
            if (string.Equals(x.CHR_TRIP_TYPE, OtherTripRequestConstants.TripTypeCode, StringComparison.OrdinalIgnoreCase)) return false;

            if (stage == "qlsc")
            {
                return x.INT_STATUS == TripRequestStatuses.PendingQlsc
                       && string.Equals(x.CHR_QLSC_APPROVE?.Trim(), currentAdid, StringComparison.OrdinalIgnoreCase);
            }

            if (stage == "qltc")
            {
                return x.INT_STATUS == TripRequestStatuses.QlscApproved
                       && string.Equals(x.CHR_QLTC_APPROVE?.Trim(), currentAdid, StringComparison.OrdinalIgnoreCase);
            }

            if (stage == "ga-quote")
            {
                return x.INT_STATUS == TripRequestStatuses.QltcApproved
                       || x.INT_STATUS == TripRequestStatuses.EmployeeRejectedQuote;
            }

            if (stage == "employee")
            {
                return x.INT_STATUS == TripRequestStatuses.QuoteSentByGa
                       && string.Equals(x.CHR_EMPLOYEE_ID?.Trim(), currentEmployeeCode, StringComparison.OrdinalIgnoreCase);
            }

            if (stage == "ga-driver")
            {
                return x.INT_STATUS == TripRequestStatuses.EmployeeAcceptedQuote;
            }

            return false;
        }).ToList();
    }

    private async Task<List<TripRequestRowViewModel>> LoadPendingRowsAsync(string stage, string currentAdid, string currentEmployeeCode)
    {
        var filters = new List<FilterOption>
        {
            new() { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "!=", Value = TripRequestStatuses.Deleted },
            new() { Field = nameof(TM_TRIP_REQUEST.CHR_TRIP_TYPE), Operator = "!=", Value = OtherTripRequestConstants.TripTypeCode }
        };

        if (stage == "qlsc")
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "=", Value = TripRequestStatuses.PendingQlsc });
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.CHR_QLSC_APPROVE), Operator = "=", Value = currentAdid });
        }
        else if (stage == "qltc")
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "=", Value = TripRequestStatuses.QlscApproved });
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.CHR_QLTC_APPROVE), Operator = "=", Value = currentAdid });
        }
        else if (stage == "ga-quote")
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "=", Value = TripRequestStatuses.QltcApproved });
            filters.Add(new FilterOption
            {
                Field = nameof(TM_TRIP_REQUEST.INT_STATUS),
                Operator = "=",
                Value = TripRequestStatuses.EmployeeRejectedQuote,
                LogicType = "OR"
            });
        }
        else if (stage == "employee")
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "=", Value = TripRequestStatuses.QuoteSentByGa });
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.CHR_EMPLOYEE_ID), Operator = "=", Value = currentEmployeeCode });
        }
        else if (stage == "ga-driver")
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "=", Value = TripRequestStatuses.EmployeeAcceptedQuote });
        }

        var resp = await _tmTripRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = -1,
            PageSize = 100000,
            Filters = filters,
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), SortDirection = "ASC" },
                new SortOption { Field = nameof(TM_TRIP_REQUEST.DTM_CREATE), SortDirection = "DESC" }
            }
        });


        var requests = resp?.Data?.Data?.ToList() ?? new List<TM_TRIP_REQUESTDTO>();
        var allowedTripTypeNames = await LoadRequestTypeNamesByGroupAsync(TripRequestTypeGroup);
        if (allowedTripTypeNames.Any())
        {
            requests = requests
                .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_TRIP_TYPE)
                    && allowedTripTypeNames.Contains(x.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase))
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
                PassengerCount = req.INT_PASSENGER_COUNT,
                TripType = req.NVCHR_TRIP_TYPE,
                PickupGo = req.DTM_PICKUP_GO,
                FromGo = req.NVCHR_FROM_LOCATION_GO,
                ToGo = req.NVCHR_TO_LOCATION_GO,
                PickupReturn = req.DTM_PICKUP_RETURN,
                FromReturn = req.NVCHR_FROM_LOCATION_RETURN,
                ToReturn = req.NVCHR_TO_LOCATION_RETURN,
                Purpose = req.NVCHR_PURPOSE,
                QuotePath = req.CHR_QUOTE_PATH,
                QuoteConfirm = req.BIT_QUOTE_CONFIRM,
                DriverInfo = req.NVCHR_DRIVER_INFO,
                QlscApprove = req.CHR_QLSC_APPROVE,
                QltcApprove = req.CHR_QLTC_APPROVE,
                Status = req.INT_STATUS,
                StatusText = TripRequestStatusUiHelper.GetStatusText(req.INT_STATUS),
                StatusRowClass = TripRequestStatusUiHelper.GetRowClass(req.INT_STATUS),
                StatusBadgeClass = TripRequestStatusUiHelper.GetBadgeClass(req.INT_STATUS)
            };
        }).ToList();
    }

    private async Task<TripRequestApprovalHistoryPageViewModel?> BuildHistoryViewModelAsync(int id)
    {
        var requestResp = await _tmTripRequestService.GetByIdAsync(id);
        var request = requestResp?.Data;
        if (request == null || string.Equals(request.CHR_TRIP_TYPE, OtherTripRequestConstants.TripTypeCode, StringComparison.OrdinalIgnoreCase)) return null;

        var allowedTripTypeNames = await LoadRequestTypeNamesByGroupAsync(TripRequestTypeGroup);
        if (allowedTripTypeNames.Any()
            && (string.IsNullOrWhiteSpace(request.NVCHR_TRIP_TYPE)
                || !allowedTripTypeNames.Contains(request.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase)))
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
            PassengerCount = request.INT_PASSENGER_COUNT,
            TripType = request.NVCHR_TRIP_TYPE,
            PickupGo = request.DTM_PICKUP_GO,
            FromGo = request.NVCHR_FROM_LOCATION_GO,
            ToGo = request.NVCHR_TO_LOCATION_GO,
            PickupReturn = request.DTM_PICKUP_RETURN,
            FromReturn = request.NVCHR_FROM_LOCATION_RETURN,
            ToReturn = request.NVCHR_TO_LOCATION_RETURN,
            Purpose = request.NVCHR_PURPOSE,
            QuotePath = request.CHR_QUOTE_PATH,
            DriverInfo = request.NVCHR_DRIVER_INFO,
            QuoteConfirm = request.BIT_QUOTE_CONFIRM,
            Status = request.INT_STATUS,
            StatusText = TripRequestStatusUiHelper.GetStatusText(request.INT_STATUS),
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

            var rejected = action.BIT_ACTION == false;
            return new TripRequestWorkflowStepViewModel
            {
                StepName = step,
                StateText = rejected ? "Bị từ chối" : "Đã xử lý",
                CssClass = rejected ? "workflow-step-reject" : "workflow-step-done",
                Actor = action.CHR_CONFIRMER_APPROVER,
                ActionTime = action.DTM_ACTION,
                Reason = action.NVCHR_REASON_REJECT
            };
        }

        return new List<TripRequestWorkflowStepViewModel>
        {
            Build("CNV đăng ký", "CREATE_TRIP_REQUEST", "RESUBMIT_TRIP_REQUEST"),
            Build("QLSC phê duyệt", "TRIP_QLSC_APPROVE", "TRIP_QLSC_REJECT"),
            Build("QLTC phê duyệt", "TRIP_QLTC_APPROVE", "TRIP_QLTC_REJECT"),
            Build("GA báo giá", "TRIP_GA_SENT_QUOTE", "TRIP_GA_REJECT"),
            Build("CNV phản hồi báo giá", "TRIP_EMPLOYEE_ACCEPT_QUOTE", "TRIP_EMPLOYEE_REJECT_QUOTE"),
            Build("GA điền thông tin lái xe", "TRIP_GA_DRIVER_FILLED")
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
            NVCHR_PURPOSE = dto.NVCHR_PURPOSE,
            INT_PASSENGER_COUNT = dto.INT_PASSENGER_COUNT,
            CHR_QLSC_APPROVE = dto.CHR_QLSC_APPROVE,
            CHR_QLTC_APPROVE = dto.CHR_QLTC_APPROVE,
            CHR_GA_CONFIRM = dto.CHR_GA_CONFIRM,
            INT_GROUP_BATCH_APPLICATIONS = dto.INT_GROUP_BATCH_APPLICATIONS,
            CHR_QUOTE_PATH = dto.CHR_QUOTE_PATH,
            BIT_QUOTE_CONFIRM = dto.BIT_QUOTE_CONFIRM,
            NVCHR_DRIVER_INFO = dto.NVCHR_DRIVER_INFO,
            INT_STATUS = dto.INT_STATUS,
            CHR_STATUS = dto.CHR_STATUS
        };
    }

    [HttpGet("TripRequestApproval/QuoteFile/{y}/{m}/{d}/{fileName}")]
    public IActionResult QuoteFile(string y, string m, string d, string fileName)
    {
        try
        {
            var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
            var uploadCfg = configuration?["Files:UploadPath"] ?? "uploads/trip-quotes";
            var contentRoot = _environment.ContentRootPath ?? Directory.GetCurrentDirectory();
            var physicalUploadPath = Path.IsPathRooted(uploadCfg)
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
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".csv" => "text/csv",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            return PhysicalFile(physicalPath, contentType);
        }
        catch
        {
            return NotFound();
        }
    }

    private async Task<string> SaveQuoteFileAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return await SaveQuoteFileBytesAsync(ms.ToArray(), file.FileName);
    }

    private async Task<string> SaveQuoteFileFromSourceAsync(string source)
    {
        var value = source?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Đường dẫn file báo giá không hợp lệ.");
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Không thể tải file từ link: {(int)response.StatusCode}.");
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var fileName = Path.GetFileName(uri.LocalPath);
            return await SaveQuoteFileBytesAsync(bytes, fileName);
        }

        var filePath = value;
        if (Uri.TryCreate(value, UriKind.Absolute, out var fileUri) && fileUri.Scheme == Uri.UriSchemeFile)
        {
            filePath = fileUri.LocalPath;
        }

        if (!System.IO.File.Exists(filePath))
        {
            throw new FileNotFoundException("Không tìm thấy file báo giá theo đường dẫn đã nhập.", filePath);
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        var localFileName = Path.GetFileName(filePath);
        return await SaveQuoteFileBytesAsync(fileBytes, localFileName);
    }

    private async Task<string> SaveQuoteFileBytesAsync(byte[] fileBytes, string? originalFileName)
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var uploadCfg = configuration?["Files:UploadPath"] ?? "uploads/trip-quotes";
        var contentRoot = _environment.ContentRootPath ?? Directory.GetCurrentDirectory();
        var physicalUploadPath = Path.IsPathRooted(uploadCfg)
            ? uploadCfg
            : Path.Combine(contentRoot, uploadCfg.TrimStart('~', '/', '\\'));

        var allowedExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".png", ".jpg", ".jpeg", ".gif"
        };

        var ext = Path.GetExtension(originalFileName ?? string.Empty);
        if (string.IsNullOrWhiteSpace(ext) || !allowedExts.Contains(ext))
        {
            ext = ".pdf";
        }

        var now = DateTime.Now;
        var y = now.Year.ToString("0000");
        var mm = now.Month.ToString("00");
        var dd = now.Day.ToString("00");

        var subFolder = Path.Combine(physicalUploadPath, y, mm, dd);
        Directory.CreateDirectory(subFolder);

        var safeName = Guid.NewGuid().ToString("N") + ext.ToLowerInvariant();
        var fullPath = Path.Combine(subFolder, safeName);
        await System.IO.File.WriteAllBytesAsync(fullPath, fileBytes);

        return $"/TripRequestApproval/QuoteFile/{y}/{mm}/{dd}/{safeName}";
    }

    private static string NormalizeStage(string? stage)
    {
        var value = stage?.Trim().ToLowerInvariant();
        return value switch
        {
            "qltc" => "qltc",
            "ga-quote" => "ga-quote",
            "employee" => "employee",
            "ga-driver" => "ga-driver",
            _ => "qlsc"
        };
    }

    private static string ResolveStageTitle(string stage)
    {
        return stage switch
        {
            "qltc" => "QLTC phòng ban xác nhận phê duyệt",
            "ga-quote" => "GA nhập báo giá và gửi lại CNV",
            "employee" => "CNV xác nhận báo giá",
            "ga-driver" => "GA điền thông tin lái xe",
            _ => "QLSC phòng ban xác nhận phê duyệt"
        };
    }

    private async Task SendNextStageMailAsync(string stage, TM_TRIP_REQUESTDTO original, TM_TRIP_REQUEST updated, string? driverInfo)
    {
        try
        {
            if (stage == "qlsc")
            {
                var toEmail = ToCompanyEmail(updated.CHR_QLTC_APPROVE);
                if (string.IsNullOrWhiteSpace(toEmail)) return;

                var toName = await ResolveApproverNameAsync(updated.CHR_QLTC_APPROVE);
                var link = BuildTripRequestApprovalLink("qltc");
                var employeeInfo = await ResolveEmployeeInfoAsync(updated.CHR_EMPLOYEE_ID);
                var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["{{To}}"] = toName,
                    ["{{ToEmail}}"] = toEmail,
                    ["{{RequestId}}"] = updated.ID.ToString(),
                    ["{{ApproveLink}}"] = link,
                    ["{{EmployeeCode}}"] = employeeInfo.EmployeeCode,
                    ["{{EmployeeName}}"] = employeeInfo.EmployeeName,
                    ["{{EmployeeSection}}"] = employeeInfo.EmployeeSection,
                    ["{{Purpose}}"] = updated.NVCHR_PURPOSE ?? string.Empty
                };

                await SendTemplatedMailAsync("TRIP_REQUEST_QLTC_APPROVAL", tokens,
                    "[TEST][BusManagement] Đơn chờ QLTC phê duyệt",
                    """
                    <p style="font-size:20px;margin:0 0 30px 0;">Đơn xe công tác #{{RequestId}} đã được QLSC duyệt.</p>
                    <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng vào hệ thống để xác nhận!</p>
                    <p style="font-size:20px;margin:0 0 10px 0;">Hoặc truy cập: <a href="{{ApproveLink}}">LINK</a></p>
                    """,
                    toEmail);
            }
            else if (stage == "qltc")
            {
                var employeeInfo = await ResolveEmployeeInfoAsync(updated.CHR_EMPLOYEE_ID);
                await SendMailToGaGroupAsync(
                    "TRIP_REQUEST_GA_QUOTE",
                    "[TEST][BusManagement] Đơn đăng ký xe công tác chờ GA nhập báo giá",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["{{RequestId}}"] = updated.ID.ToString(),
                        ["{{ApproveLink}}"] = BuildTripRequestApprovalLink("ga-quote"),
                        ["{{EmployeeCode}}"] = employeeInfo.EmployeeCode,
                        ["{{EmployeeName}}"] = employeeInfo.EmployeeName,
                        ["{{EmployeeSection}}"] = employeeInfo.EmployeeSection,
                        ["{{Purpose}}"] = updated.NVCHR_PURPOSE ?? string.Empty
                    },
                    """
                    <p style="font-size:20px;margin:0 0 30px 0;">Đơn đăng ký xe công tác #{{RequestId}} đã được QLSC, QLTC duyệt.</p>
                    <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng nhập báo giá và gửi lại cho CNV.</p>
                    <p style="font-size:20px;margin:0 0 10px 0;">Truy cập tại: <a href="{{ApproveLink}}">LINK</a></p>
                    """);
            }
            else if (stage == "employee")
            {
                await SendMailToGaGroupAsync(
                    "TRIP_REQUEST_GA_DRIVER_INFO",
                    "[TEST][BusManagement] Đơn đăng ký xe công tác đã được CNV xác nhận đồng ý báo giá",
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["{{RequestId}}"] = updated.ID.ToString(),
                        ["{{ApproveLink}}"] = BuildTripRequestApprovalLink("ga-driver")
                    },
                    """
                    <p style="font-size:20px;margin:0 0 30px 0;">Đơn đăng ký xe công tác #{{RequestId}}: CNV đã phản hồi đồng ý báo giá.</p>
                    <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng điền thông tin xe/lái xe.</p>
                    <p style="font-size:20px;margin:0 0 10px 0;">Truy cập tại: <a href="{{ApproveLink}}">LINK</a></p>
                    """);
            }
            else if (stage == "ga-driver")
            {
                var employeeMail = await ResolveEmployeeMailAsync(updated.CHR_EMPLOYEE_ID);
                if (!string.IsNullOrWhiteSpace(employeeMail))
                {
                    var employeeName = await ResolveEmployeeNameByCodeAsync(updated.CHR_EMPLOYEE_ID);
                    var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["{{To}}"] = employeeName,
                        ["{{ToEmail}}"] = employeeMail,
                        ["{{RequestId}}"] = updated.ID.ToString(),
                        ["{{DriverInfo}}"] = driverInfo ?? string.Empty,
                        ["{{ApproveLink}}"] = BuildTripRequestListLink()
                    };

                    await SendTemplatedMailAsync("TRIP_REQUEST_COMPLETED", tokens,
                        "[TEST][BusManagement] Đơn đăng ký xe công tác đã hoàn thành",
                        """
                        <p style="font-size:20px;margin:0 0 30px 0;">Đơn đăng ký xe công tác #{{RequestId}} đã hoàn thành.</p>
                        <p style="font-size:20px;margin:0 0 30px 0;">Thông tin lái xe: {{DriverInfo}}</p>
                        <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng kiểm tra chi tiết trên hệ thống: <a href="{{ApproveLink}}">LINK</a></p>
                        """,
                        employeeMail);
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task SendRejectMailToEmployeeAsync(TM_TRIP_REQUESTDTO request, string reason, string stage)
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
                ["{{RejectStage}}"] = ResolveStageTitle(stage),
                ["{{RejectReason}}"] = reason,
                ["{{ApproveLink}}"] = BuildTripRequestListLink()
            };

            await SendTemplatedMailAsync("TRIP_REQUEST_REJECT", tokens,
                "[TEST][BusManagement] Đơn đăng ký xe công tác bị từ chối",
                """
                <p style="font-size:20px;margin:0 0 30px 0;">Đơn đăng ký xe công tác #{{RequestId}} đã bị từ chối tại bước <b>{{RejectStage}}</b>.</p>
                <p style="font-size:20px;margin:0 0 30px 0;">Lý do: {{RejectReason}}</p>
                <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng kiểm tra trên hệ thống: <a href="{{ApproveLink}}">LINK</a></p>
                """,
                employeeMail);
        }
        catch
        {
            // ignore
        }
    }

    private async Task SendQuoteMailToEmployeeAsync(TM_TRIP_REQUESTDTO request, string quotePath)
    {
        try
        {
            var employeeMail = await ResolveEmployeeMailAsync(request.CHR_EMPLOYEE_ID);
            if (string.IsNullOrWhiteSpace(employeeMail)) return;
            var link = Request?.Host.HasValue == true ? $"{Request.Scheme}://{Request.Host}{quotePath}" : quotePath;
            var employeeName = await ResolveEmployeeNameByCodeAsync(request.CHR_EMPLOYEE_ID);
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["{{To}}"] = employeeName,
                ["{{ToEmail}}"] = employeeMail,
                ["{{RequestId}}"] = request.ID.ToString(),
                ["{{QuoteLink}}"] = link,
                ["{{ApproveLink}}"] = BuildTripRequestApprovalLink("employee")
            };

            await SendTemplatedMailAsync("TRIP_REQUEST_QUOTE_TO_EMPLOYEE", tokens,
                "[TEST][BusManagement] GA đã gửi báo giá",
                """
                <p style="font-size:20px;margin:0 0 30px 0;">Đơn đăng ký xe công tác #{{RequestId}} đã có báo giá.</p>
                <p style="font-size:20px;margin:0 0 10px 0;">Vui lòng kiểm tra file: <a href="{{QuoteLink}}">Báo giá</a></p>
                <p style="font-size:20px;margin:0 0 10px 0;">Và phản hồi trên hệ thống: <a href="{{ApproveLink}}">LINK</a></p>
                """,
                employeeMail);
        }
        catch
        {
            // ignore
        }
    }

    private async Task SendMailToGaGroupAsync(string templateCode, string fallbackSubject, Dictionary<string, string> tokens, string fallbackMessage)
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var to = string.Empty;
        if (string.IsNullOrWhiteSpace(to))
        {
            var templateResp = await _tmMasterMailService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_MASTER_MAIL.CHR_MAIL_CODE), Operator = "=", Value = "TRIP_REQUEST_GA_QUOTE" }
                }
            });
            to = templateResp?.Data?.Data?.FirstOrDefault()?.CHR_TO;
        }

        if (string.IsNullOrWhiteSpace(to)) to = "BIVNgabus@brother-bivn.com.vn"; // Fallback;

        if (!tokens.ContainsKey("{{To}}")) tokens["{{To}}"] = "nhóm quản lý xe Công ty";
        if (!tokens.ContainsKey("{{ToEmail}}")) tokens["{{ToEmail}}"] = to;

        await SendTemplatedMailAsync(templateCode, tokens, fallbackSubject, fallbackMessage, to);
    }

    private async Task SendTemplatedMailAsync(string templateCode, Dictionary<string, string> tokens, string fallbackSubject, string fallbackMessageHtml, string toEmail)
    {
        var template = await GetMailTemplateByCodeAsync(templateCode);

        var title = ReplaceTemplateTokens(template?.CHR_SUBJECT, tokens);
        if (string.IsNullOrWhiteSpace(title)) title = fallbackSubject;

        var bodyTemplate = template == null
            ? BuildDefaultMailBody(fallbackMessageHtml)
            : template.CHR_CONTENT ?? string.Empty;
        var body = ReplaceTemplateTokens(bodyTemplate, tokens);

        var from = ReplaceTemplateTokens(template?.CHR_FROM, tokens);
        if (string.IsNullOrWhiteSpace(from)) from = "zzpsvvmg@brother-bivn.com.vn";

        var toTemplate = ReplaceTemplateTokens(template?.CHR_TO, tokens);
        var to = CombineEmails(toEmail, toTemplate);
        if (string.IsNullOrWhiteSpace(to)) return;

        var cc = ReplaceTemplateTokens(template?.CHR_CC, tokens);
        var bcc = ReplaceTemplateTokens(template?.CHR_BCC, tokens);
        var priority = template?.BIT_HIGHT == true ? 1 : 0;

        _tmMasterMailService.sendEmailNotify(title, from, to, cc ?? string.Empty, bcc ?? string.Empty, body, priority);
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

    private string BuildTripRequestApprovalLink(string stage)
    {
        var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
        var baseUrl = configuration?["SystemLinks:BusManagementWebApp"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = Request?.Host.HasValue == true
                ? $"{Request.Scheme}://{Request.Host.Value}"
                : string.Empty;
        }

        var relativePath = Url.Action(nameof(Index), "TripRequestApproval", new { stage }) ?? $"/TripRequestApproval?stage={stage}";
        if (string.IsNullOrWhiteSpace(baseUrl)) return relativePath;
        return $"{baseUrl.TrimEnd('/')}{relativePath}";
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

        var relativePath = Url.Action("Index", "TripRequest") ?? "/TripRequest";
        if (string.IsNullOrWhiteSpace(baseUrl)) return relativePath;
        return $"{baseUrl.TrimEnd('/')}{relativePath}";
    }

    private async Task<string> ResolveApproverNameAsync(string? adid)
    {
        if (string.IsNullOrWhiteSpace(adid)) return string.Empty;
        var resp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ADID), Operator = "=", Value = adid.Trim() }
            }
        });

        var emp = resp?.Data?.Data?.FirstOrDefault();
        return emp?.NVCHR_EMPLOYEE_NAME ?? adid;
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

    private async Task<(string EmployeeCode, string EmployeeName, string EmployeeSection)> ResolveEmployeeInfoAsync(string? employeeCode)
    {
        if (string.IsNullOrWhiteSpace(employeeCode)) return (string.Empty, string.Empty, string.Empty);

        var empResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = 1,
            PageSize = 1,
            Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = employeeCode.Trim() }
            }
        });

        var emp = empResp?.Data?.Data?.FirstOrDefault();
        var sectionCode = emp?.CHR_CODE_SEC?.Trim();
        var sectionName = sectionCode ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(sectionCode))
        {
            var sectionResp = await _sectionService.GetAllAsync();
            var sections = sectionResp?.Data ?? new List<TM_SECTIONDTO>();
            var sec = sections.FirstOrDefault(x => string.Equals(x.CHR_CODE?.Trim(), sectionCode, StringComparison.OrdinalIgnoreCase));
            sectionName = sec?.CHR_NAME ?? sec?.CHR_SECTION_NAME ?? sectionCode;
        }

        return (
            employeeCode.Trim(),
            emp?.NVCHR_EMPLOYEE_NAME ?? employeeCode.Trim(),
            sectionName
        );
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
            "CREATE_TRIP_REQUEST" => "Tạo đơn",
            "RESUBMIT_TRIP_REQUEST" => "Gửi lại phê duyệt",
            "TRIP_QLSC_APPROVE" => "QLSC phê duyệt",
            "TRIP_QLSC_REJECT" => "QLSC từ chối",
            "TRIP_QLTC_APPROVE" => "QLTC phê duyệt",
            "TRIP_QLTC_REJECT" => "QLTC từ chối",
            "TRIP_GA_SENT_QUOTE" => "GA gửi báo giá",
            "TRIP_EMPLOYEE_ACCEPT_QUOTE" => "CNV đồng ý báo giá",
            "TRIP_EMPLOYEE_REJECT_QUOTE" => "CNV từ chối báo giá",
            "TRIP_GA_DRIVER_FILLED" => "GA điền thông tin lái xe",
            "TRIP_GA_REJECT" => "GA từ chối",
            _ => action ?? string.Empty
        };
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

    private async Task<bool> CanCurrentUserProcessTripGaAsync(string stage)
    {
        var mailCode = stage == "ga-driver" ? "TRIP_REQUEST_GA_DRIVER_INFO" : "TRIP_REQUEST_GA_QUOTE";
        var template = await GetMailTemplateByCodeAsync(mailCode);
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
