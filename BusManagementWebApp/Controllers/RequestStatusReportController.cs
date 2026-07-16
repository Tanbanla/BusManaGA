using Azure.Core;
using BusManagement.Model.Common;
using BusManagement.Model.Common.Enum;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Implementations;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.RequestStatusReport;
using BusManagementWebApp.Helpers;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace BusManagementWebApp.Controllers
{
    public class RequestStatusReportController : Controller
    {
        private readonly ITmBusRequestService _tmBusRequestService;
        private readonly ITmTripRequestService _tmTripRequestService;
        private readonly ITmRequestTypeService _tmRequestTypeService;
        private readonly ITmEmployeeService _tmEmployeeService;
        private readonly ISectionService _sectionService;
        private readonly ITmMasterMailService _tmMasterMailService;
        private readonly ITmApprovalHistoryService _tmApprovalHistoryService;
        private readonly ITmUserService _tmUserService;

        public RequestStatusReportController(
            ITmBusRequestService tmBusRequestService,
            ITmTripRequestService tmTripRequestService,
            ITmRequestTypeService tmRequestTypeService,
            ITmEmployeeService tmEmployeeService,
            ISectionService sectionService,
            ITmMasterMailService tmMasterMailService,
            ITmApprovalHistoryService tmApprovalHistoryService,
            ITmUserService tmUserService)
        {
            _tmBusRequestService = tmBusRequestService;
            _tmTripRequestService = tmTripRequestService;
            _tmRequestTypeService = tmRequestTypeService;
            _tmEmployeeService = tmEmployeeService;
            _sectionService = sectionService;
            _tmMasterMailService = tmMasterMailService;
            _tmApprovalHistoryService = tmApprovalHistoryService;
            _tmUserService = tmUserService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? requestTypeIds,
            string? employeeCode,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page = 1,
            int pageSize = 10)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 10 : pageSize;

            // Default to 1 year ago if no date filter
            var today = DateTime.Now.Date;
            if (!dateFrom.HasValue && !dateTo.HasValue)
            {
                dateFrom = today.AddYears(-1);
                dateTo = today;
            }

            var selectedRequestTypeIds = ParseCsvInts(requestTypeIds);

            // Load request types (group 1 and 2)
            var requestTypesResp = await _tmRequestTypeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_REQUEST_TYPE.INT_GROUP), Operator = "=", Value = 1, LogicType = null },
                    new() { Field = nameof(TM_REQUEST_TYPE.INT_GROUP), Operator = "=", Value = 2, LogicType = "OR" }
                }
            });

            var requestTypes = requestTypesResp?.Data?.Data?.ToList() ?? new List<TM_REQUEST_TYPEDTO>();

            // Build filters for request types
            var typeFilters = new List<FilterOption>();
            if (selectedRequestTypeIds.Any())
            {
                for (var i = 0; i < selectedRequestTypeIds.Count; i++)
                {
                    typeFilters.Add(new FilterOption
                    {
                        Field = nameof(TM_BUS_REQUEST.INT_REQUEST_TYPE),
                        Operator = "=",
                        Value = selectedRequestTypeIds[i],
                        LogicType = i == 0 ? null : "OR"
                    });
                }
            }

            // Build combined filter list
            var filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_BUS_REQUEST.DTM_CREATE), Operator = ">=", Value = dateFrom?.Date },
                new() { Field = nameof(TM_BUS_REQUEST.DTM_CREATE), Operator = "<=", Value = dateTo?.Date.AddDays(1).AddTicks(-1) }
            };

            if (!string.IsNullOrWhiteSpace(employeeCode))
            {
                filters.Add(new FilterOption
                {
                    Field = nameof(TM_BUS_REQUEST.CHR_EMPLOYEE_ID),
                    Operator = "LIKE",
                    Value = $"%{employeeCode.Trim()}%"
                });
            }

            filters.AddRange(typeFilters);

            // Get Bus Requests
            var busRequestResp = await _tmBusRequestService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = filters,
                SortOptions = new()
                {
                    new SortOption { Field = nameof(TM_BUS_REQUEST.DTM_CREATE), SortDirection = "DESC" }
                }
            });

            var busRequests = busRequestResp?.Data?.Data?.ToList() ?? new List<TM_BUS_REQUESTDTO>();

            // Get Trip Requests with same filters
            var tripFilters = new List<FilterOption>
            {
                new() { Field = nameof(TM_TRIP_REQUEST.DTM_CREATE), Operator = ">=", Value = dateFrom?.Date },
                new() { Field = nameof(TM_TRIP_REQUEST.DTM_CREATE), Operator = "<=", Value = dateTo?.Date.AddDays(1).AddTicks(-1) },
                new() { Field = nameof(TM_TRIP_REQUEST.CHR_TRIP_TYPE), Operator = "<>", Value = "OTHER_CAR" },
                new() { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "!=", Value = -1 } // Exclude deleted
            };

            if (!string.IsNullOrWhiteSpace(employeeCode))
            {
                tripFilters.Add(new FilterOption
                {
                    Field = nameof(TM_TRIP_REQUEST.CHR_EMPLOYEE_ID),
                    Operator = "LIKE",
                    Value = $"%{employeeCode.Trim()}%"
                });
            }

            // Add trip request type filters if needed (loại chuyến công tác group = 2)
            var tripTypeFilters = selectedRequestTypeIds.Any()
                ? selectedRequestTypeIds
                    .Select(id => requestTypes.FirstOrDefault(x => x.ID.ToString() == id)?.NVCHR_REQUEST_TYPE)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList()
                : new List<string>();

            var tripRequestResp = await _tmTripRequestService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = -1,
                PageSize = 100000,
                Filters = tripFilters,
                SortOptions = new()
                {
                    new SortOption { Field = nameof(TM_TRIP_REQUEST.DTM_CREATE), SortDirection = "DESC" }
                }
            });

            var tripRequests = tripRequestResp?.Data?.Data?.ToList() ?? new List<TM_TRIP_REQUESTDTO>();

            // Filter trip requests by request type if needed
            if (tripTypeFilters.Any())
            {
                tripRequests = tripRequests
                    .Where(x => !string.IsNullOrWhiteSpace(x.NVCHR_TRIP_TYPE)
                        && tripTypeFilters.Contains(x.NVCHR_TRIP_TYPE.Trim(), StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }

            // Build combined rows
            var allRows = new List<RequestStatusRowViewModel>();
            allRows.AddRange(await BuildBusRequestRows(busRequests, requestTypes, today));
            allRows.AddRange(await BuildTripRequestRows(tripRequests, requestTypes, today));

            // Sort by created date descending
            allRows = allRows.OrderByDescending(x => x.CreatedDate).ToList();

            // Paginate
            var totalCount = allRows.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var pagedRows = allRows
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new RequestStatusReportPageViewModel
            {
                SelectedRequestTypeIds = requestTypeIds,
                EmployeeCode = employeeCode,
                DateFrom = dateFrom,
                DateTo = dateTo,
                SelectedRequestTypeIdList = selectedRequestTypeIds
                .Select(x => int.TryParse(x, out var id) ? (int?)id : null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .Distinct()
                .ToList(),
                PageNumber = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Requests = pagedRows,
                RequestTypes = requestTypes
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendReminder(string? ids, string? requestTypeIds, string? employeeCode, DateTime? dateFrom, DateTime? dateTo)
        {
            var selectedIds = ParseCsvStrings(ids);
            if (!selectedIds.Any())
            {
                TempData["RequestStatusReportError"] = "Vui lòng chọn ít nhất 1 bản ghi.";
                return RedirectToIndex(requestTypeIds, employeeCode, dateFrom, dateTo);
            }

                var remindersSent = 0;
            var uniqueApprovers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // email => name
            var gaApproverRequired = false; // if approver is GA (group approval), use template.To
            var approvalUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var today = DateTime.Now.Date;

            foreach (var id in selectedIds)
            {
                if (!id.StartsWith("bus_") && !id.StartsWith("trip_")) continue;

                var isBusRequest = id.StartsWith("bus_");
                var recordId = int.TryParse(id.Split("_").LastOrDefault(), out var parsed) ? parsed : 0;
                if (recordId <= 0) continue;

                string? approverEmail = null;
                string? approverName = null;

                if (isBusRequest)
                {
                    var busResp = await _tmBusRequestService.GetByIdAsync(recordId);
                    var busReq = busResp?.Data;
                    if (busReq != null)
                    {
                        // Determine current approver identifier based on status (may be email, ADID or employeeId)
                        var approverIdentifier = DetermineApproverEmailForBusRequest(busReq);
                        // Resolve canonical email and display name separately
                        approverEmail = await ResolveApproverEmailAsync(approverIdentifier);
                        approverName = await ResolveApproverDisplayNameAsync(approverIdentifier);

                        // Determine approval url if this request is delayed
                        var delayInfo = await IsRequestDelayed(busReq.DTM_CREATE, today, busReq.INT_STATUS, "BusRequest", busReq.ID);
                        if (delayInfo.IsDelayed && !string.IsNullOrWhiteSpace(delayInfo.ApprovalUrl))
                        {
                            approvalUrls.Add(delayInfo.ApprovalUrl!);
                        }
                    }
                }
                else
                {
                    var tripResp = await _tmTripRequestService.GetByIdAsync(recordId);
                    var tripReq = tripResp?.Data;
                    if (tripReq != null)
                    {
                        var approverIdentifier = DetermineApproverEmailForTripRequest(tripReq);
                        approverEmail = await ResolveApproverEmailAsync(approverIdentifier);
                        approverName = await ResolveApproverDisplayNameAsync(
                            tripReq.INT_STATUS == TripRequestStatuses.PendingQlsc ? tripReq.CHR_QLSC_APPROVE :
                            tripReq.INT_STATUS == TripRequestStatuses.QlscApproved ? tripReq.CHR_QLTC_APPROVE :
                            tripReq.INT_STATUS == TripRequestStatuses.QuoteSentByGa ? tripReq.CHR_EMPLOYEE_ID :
                            tripReq.INT_STATUS == TripRequestStatuses.QltcApproved ? null : null);

                        // Determine approval url if this request is delayed
                        var delayInfo = await IsRequestDelayed(tripReq.DTM_CREATE, today, tripReq.INT_STATUS, "TripRequest", tripReq.ID);
                        if (delayInfo.IsDelayed && !string.IsNullOrWhiteSpace(delayInfo.ApprovalUrl))
                        {
                            approvalUrls.Add(delayInfo.ApprovalUrl!);
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(approverEmail))
                {
                    if (!uniqueApprovers.ContainsKey(approverEmail))
                    {
                        uniqueApprovers[approverEmail] = approverName ?? approverEmail;
                    }
                }
                else
                {
                    // No individual approver found -> likely a GA/group approval step. Mark so we use template.To
                    gaApproverRequired = true;
                }
            }

            if (uniqueApprovers.Any() || gaApproverRequired)
            {
                var template = await GetMailTemplateByCodeAsync("REQUEST_REMINDER_DELAYED");

                // If GA step is required, use the To field from the template. Otherwise use individual approver emails.
                var toEmails = gaApproverRequired
                    ? (template?.CHR_TO ?? string.Empty)
                    : string.Join(";", uniqueApprovers.Keys);

                var approverNames = gaApproverRequired
                    ? (template?.CHR_TO ?? "nhóm quản lý xe Công ty")
                    : string.Join(", ", uniqueApprovers.Values.Distinct(StringComparer.OrdinalIgnoreCase));

                var approveLink = approvalUrls.Any() ? approvalUrls.First() : (template?.CHR_TO ?? string.Empty);
                var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["{{To}}"] = approverNames,
                    ["{{RecordCount}}"] = selectedIds.Count.ToString(),
                    ["{{ApproveLink}}"] = approveLink
                };

                var title = ReplaceTemplateTokens(template?.CHR_SUBJECT, tokens)
                    ?? "[TEST][BusManagement] Nhắc nhở: Có đơn quá hạn";

                var defaultBody = """
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset="utf-8" />
                        <title>Nhắc nhở phê duyệt đơn</title>
                    </head>
                    <body style="margin:0;padding:0;background-color:#ffffff;font-family:'Times New Roman', Times, serif;">
                        <div style="width:100%;margin:0 auto;">
                            <p style="color:red;font-size:24px;font-weight:bold;margin:0 0 30px 0;">
                                Đây là mail tự động, vui lòng không Reply lại mail!
                            </p>
                            <p style="font-size:20px;margin:0 0 30px 0;">Gửi anh/chị {{To}},</p>
                            <p style="font-size:20px;margin:0 0 30px 0;">
                                Bạn có <b>{{RecordCount}}</b> đơn yêu cầu chờ phê duyệt quá hạn. 
                                Vui lòng vào hệ thống để xử lý sớm nhất.
                            </p>
                          <p style="font-size:20px;margin:0 0 30px 0;">Truy cập tại: <a href="{{ApproveLink}}">LINK</a></p>
                            <p style="font-size:20px;margin:0 0 10px 0;">Xin cảm ơn!</p>
                            <p style="font-size:20px;font-style:italic;margin:0;">Hệ thống quản lý xe Công ty-BIVN</p>
                        </div>
                    </body>
                    </html>
                    """;

                var bodyTemplate = string.IsNullOrWhiteSpace(template?.CHR_CONTENT) ? defaultBody : template.CHR_CONTENT;
                var body = ReplaceTemplateTokens(bodyTemplate, tokens);

                var from = template?.CHR_FROM ?? "zzpsvvmg@brother-bivn.com.vn";
                var cc = template?.CHR_CC ?? string.Empty;
                var bcc = template?.CHR_BCC ?? string.Empty;

                _tmMasterMailService.sendEmailNotify(title, from, toEmails, cc, bcc, body, 0);
                remindersSent++;
            }

            TempData["RequestStatusReportSuccess"] = remindersSent > 0
                ? $"Đã gửi nhắc nhở cho {uniqueApprovers.Count} người phê duyệt."
                : "Không tìm thấy người phê duyệt để gửi nhắc nhở.";

            return RedirectToIndex(requestTypeIds, employeeCode, dateFrom, dateTo);
        }

        private async Task<string?> ResolveApproverEmailAsync(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;
            var id = identifier.Trim();

            // If already an email, return as-is
            if (id.Contains("@")) return id;

            // Try find by ADID
            var resp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ADID), Operator = "=", Value = id }
                }
            });
            var emp = resp?.Data?.Data?.FirstOrDefault();
            if (emp != null)
            {
                if (!string.IsNullOrWhiteSpace(emp.CHR_EMPLOYEE_MAIL)) return emp.CHR_EMPLOYEE_MAIL;
                if (!string.IsNullOrWhiteSpace(emp.CHR_EMPLOYEE_ADID)) return ToCompanyEmail(emp.CHR_EMPLOYEE_ADID);
            }

            // Try find by employee id
            var respByCode = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = id }
                }
            });
            var empByCode = respByCode?.Data?.Data?.FirstOrDefault();
            if (empByCode != null)
            {
                if (!string.IsNullOrWhiteSpace(empByCode.CHR_EMPLOYEE_MAIL)) return empByCode.CHR_EMPLOYEE_MAIL;
                if (!string.IsNullOrWhiteSpace(empByCode.CHR_EMPLOYEE_ADID)) return ToCompanyEmail(empByCode.CHR_EMPLOYEE_ADID);
            }

            // Try user service by employee id
            var userResp = await _tmUserService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_EMPLOYEE_ID", Operator = "=", Value = id }
                }
            });
            var userData = userResp?.Data?.Data?.FirstOrDefault();
            if (userData != null && !string.IsNullOrWhiteSpace(userData.CHR_ADID))
            {
                return ToCompanyEmail(userData.CHR_ADID);
            }

            // Try user service by ADID
            var userRespByAd = await _tmUserService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_ADID", Operator = "=", Value = id }
                }
            });
            var userData1 = userRespByAd?.Data?.Data?.FirstOrDefault();
            if (userData1 != null && !string.IsNullOrWhiteSpace(userData1.CHR_ADID))
            {
                return ToCompanyEmail(userData1.CHR_ADID);
            }

            // Fallback: assume id is ADID and convert
            return ToCompanyEmail(id);
        }

        private async Task<string?> ResolveApproverDisplayNameAsync(string? identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return null;
            var id = identifier.Trim();

            // Try ADID -> return employee name
            var resp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ADID), Operator = "=", Value = id }
                }
            });
            var emp = resp?.Data?.Data?.FirstOrDefault();
            if (emp != null && !string.IsNullOrWhiteSpace(emp.NVCHR_EMPLOYEE_NAME)) return emp.NVCHR_EMPLOYEE_NAME;

            // Try employee id
            var respByCode = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = id }
                }
            });
            var empByCode = respByCode?.Data?.Data?.FirstOrDefault();
            if (empByCode != null && !string.IsNullOrWhiteSpace(empByCode.NVCHR_EMPLOYEE_NAME)) return empByCode.NVCHR_EMPLOYEE_NAME;

            // If no friendly name, fallback to resolved email
            var email = await ResolveApproverEmailAsync(id);
            if (!string.IsNullOrWhiteSpace(email)) return email;

            // final fallback to identifier
            return id;
        }

        [HttpGet]
        public async Task<IActionResult> Export(
            string? requestTypeIds,
            string? employeeCode,
            DateTime? dateFrom,
            DateTime? dateTo)
        {
            var result = await Index(requestTypeIds, employeeCode, dateFrom, dateTo, 1, 100000);
            if (result is not ViewResult vr || vr.Model is not RequestStatusReportPageViewModel vm)
            {
                return RedirectToAction(nameof(Index));
            }

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("RequestStatusReport");

            var headers = new[] { "No", "Loại đơn", "Mã nhân viên", "Họ và tên", "Phòng ban", "Ngày tạo đơn", "Tình trạng", "Chậm xử lý" };
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
            foreach (var item in vm.Requests)
            {
                sheet.Cell(row, 1).Value = stt;
                sheet.Cell(row, 2).Value = item.RequestType;
                sheet.Cell(row, 3).Value = item.EmployeeCode;
                sheet.Cell(row, 4).Value = item.EmployeeName;
                sheet.Cell(row, 5).Value = item.DepartmentName;
                sheet.Cell(row, 6).Value = item.CreatedDate.ToString("dd/MM/yyyy HH:mm");
                sheet.Cell(row, 7).Value = item.StatusText;
                sheet.Cell(row, 8).Value = item.IsDelayed ? "Có" : "Không";
                row++;
                stt++;
            }

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var fileName = $"RequestStatusReport_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private async Task<List<RequestStatusRowViewModel>> BuildBusRequestRows(
            List<TM_BUS_REQUESTDTO> requests,
            List<TM_REQUEST_TYPEDTO> allRequestTypes,
            DateTime today)
        {
            var rows = new List<RequestStatusRowViewModel>();
            var employeeCodes = requests
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
                .Select(x => x.CHR_EMPLOYEE_ID!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var employeeMap = await LoadEmployeeMapAsync(employeeCodes);
            var sectionMap = await LoadSectionMapAsync();
            var requestTypeMap = allRequestTypes.ToDictionary(x => x.ID, x => x.NVCHR_REQUEST_TYPE ?? string.Empty);

            foreach (var req in requests)
            {
                employeeMap.TryGetValue(req.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var emp);
                var deptCode = emp?.CHR_CODE_SEC?.Trim() ?? string.Empty;
                sectionMap.TryGetValue(deptCode, out var section);

                var requestTypeName = req.INT_REQUEST_TYPE.HasValue && requestTypeMap.TryGetValue(req.INT_REQUEST_TYPE.Value, out var rtName)
                    ? rtName
                    : string.Empty;

                // Determine delay and approval navigation info based on status and source
                var delayInfo = await IsRequestDelayed(req.DTM_CREATE, today, req.INT_STATUS, "BusRequest", req.ID);
                var isDelayed = delayInfo.IsDelayed;
                var currentApprover = DetermineBusRequestCurrentApprover(req);

                rows.Add(new RequestStatusRowViewModel
                {
                    Id = req.ID,
                    RequestType = requestTypeName,
                    EmployeeCode = req.CHR_EMPLOYEE_ID,
                    EmployeeName = emp?.NVCHR_EMPLOYEE_NAME,
                    DepartmentName = section?.CHR_NAME ?? section?.CHR_SECTION_NAME ?? deptCode,
                    CreatedDate = req.DTM_CREATE,
                    StatusCode = req.INT_STATUS ?? 0,
                    StatusText = BusRequestStatusUiHelper.GetStatusText(req.INT_STATUS),
                    StatusBadgeClass = BusRequestStatusUiHelper.GetBadgeClass(req.INT_STATUS),
                    IsDelayed = isDelayed,
                    CurrentApprover = currentApprover,
                    DelayedApproverEmail = isDelayed ? DetermineApproverEmailForBusRequest(req) : null,
                    ApprovalStage = delayInfo.ApprovalStage,
                    ApprovalUrl = delayInfo.ApprovalUrl,
                    IsGaStep = delayInfo.IsGaStep,
                    RequestSourceType = "BusRequest",
                    CanViewHistory = req.INT_STATUS == (int)BusRequestStatus.QlscRejected || req.INT_STATUS == (int)BusRequestStatus.GaRejected,
                    HistoryUrl = $"/BusRequestApproval/History?id={req.ID}"
                });
            }

            return rows;
        }

        private async Task<List<RequestStatusRowViewModel>> BuildTripRequestRows(
            List<TM_TRIP_REQUESTDTO> requests,
            List<TM_REQUEST_TYPEDTO> allRequestTypes,
            DateTime today)
        {
            var rows = new List<RequestStatusRowViewModel>();
            var employeeCodes = requests
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
                .Select(x => x.CHR_EMPLOYEE_ID!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var employeeMap = await LoadEmployeeMapAsync(employeeCodes);
            var sectionMap = await LoadSectionMapAsync();

            foreach (var req in requests)
            {
                employeeMap.TryGetValue(req.CHR_EMPLOYEE_ID?.Trim() ?? string.Empty, out var emp);
                var deptCode = emp?.CHR_CODE_SEC?.Trim() ?? string.Empty;
                sectionMap.TryGetValue(deptCode, out var section);

                var requestTypeName = !string.IsNullOrWhiteSpace(req.NVCHR_TRIP_TYPE) ? req.NVCHR_TRIP_TYPE : "Đăng ký xe đi công tác";

                var delayInfo = await IsRequestDelayed(req.DTM_CREATE, today, req.INT_STATUS, "TripRequest", req.ID);
                var isDelayed = delayInfo.IsDelayed;
                var currentApprover = DetermineTripRequestCurrentApprover(req);

                rows.Add(new RequestStatusRowViewModel
                {
                    Id = req.ID,
                    RequestType = requestTypeName,
                    EmployeeCode = req.CHR_EMPLOYEE_ID,
                    EmployeeName = emp?.NVCHR_EMPLOYEE_NAME,
                    DepartmentName = section?.CHR_NAME ?? section?.CHR_SECTION_NAME ?? deptCode,
                    CreatedDate = req.DTM_CREATE,
                    StatusCode = req.INT_STATUS ?? 0,
                    StatusText = TripRequestStatusUiHelper.GetStatusText(req.INT_STATUS),
                    StatusBadgeClass = TripRequestStatusUiHelper.GetBadgeClass(req.INT_STATUS),
                    IsDelayed = isDelayed,
                    CurrentApprover = currentApprover,
                    DelayedApproverEmail = isDelayed ? DetermineApproverEmailForTripRequest(req) : null,
                    ApprovalStage = delayInfo.ApprovalStage,
                    ApprovalUrl = delayInfo.ApprovalUrl,
                    IsGaStep = delayInfo.IsGaStep,
                    RequestSourceType = "TripRequest",
                    CanViewHistory = req.INT_STATUS == TripRequestStatuses.QlscRejected || req.INT_STATUS == TripRequestStatuses.GaRejected,
                    HistoryUrl = $"/TripRequestApproval/History?id={req.ID}"
                });
            }

            return rows;
        }

        private record RequestDelayInfo(bool IsDelayed, string? ApprovalStage, string? ApprovalUrl, bool IsGaStep);

        private async Task<RequestDelayInfo> IsRequestDelayed(DateTime createdDate, DateTime today, int? status = null, string? requestSource = null, int? requestId = null)
        {
            var yesterday = today.AddDays(-1);

            // If trip is already completed, not delayed
            if (string.Equals(requestSource, "TripRequest", StringComparison.OrdinalIgnoreCase)
                && status == TripRequestStatuses.Completed)
            {
                return new RequestDelayInfo(false, null, null, false);
            }
            // If bus is already completed, not delayed
            if (string.Equals(requestSource, "BusRequest", StringComparison.OrdinalIgnoreCase)
                && status == (int)BusRequestStatus.GaApproved)
            {
                return new RequestDelayInfo(false, null, null, false);
            }

            DateTime submissionTime = createdDate;

            // Determine previous action(s) that represent submission to current step
            string[]? previousActions = null;
            if (string.Equals(requestSource, "BusRequest", StringComparison.OrdinalIgnoreCase))
            {
                previousActions = status switch
                {
                    (int)BusRequestStatus.PendingQlscApproval => new[] { "CREATE_REQUEST" },
                    (int)BusRequestStatus.QlscApproved => new[] { "QLSC_APPROVE" },
                    (int)BusRequestStatus.QlscRejected => new[] { "QLSC_REJECT" },
                    (int)BusRequestStatus.GaApproved => new[] { "QLSC_APPROVE" },
                    (int)BusRequestStatus.GaRejected => new[] { "GA_REJECT" },
                    _ => null
                };
            }
            else if (string.Equals(requestSource, "TripRequest", StringComparison.OrdinalIgnoreCase))
            {
                previousActions = status switch
                {
                    TripRequestStatuses.PendingQlsc => new[] { "CREATE_TRIP_REQUEST", "RESUBMIT_TRIP_REQUEST" },
                    TripRequestStatuses.QlscApproved => new[] { "TRIP_QLSC_APPROVE" },
                    TripRequestStatuses.QltcApproved => new[] { "TRIP_QLTC_APPROVE" },
                    TripRequestStatuses.QuoteSentByGa => new[] { "TRIP_GA_SENT_QUOTE" },
                    TripRequestStatuses.EmployeeAcceptedQuote => new[] { "TRIP_EMPLOYEE_ACCEPT_QUOTE" },
                    TripRequestStatuses.QlscRejected => new[] { "TRIP_QLSC_REJECT" },
                    TripRequestStatuses.GaRejected => new[] { "TRIP_GA_REJECT" },
                    _ => null
                };
            }

            if (requestId.HasValue && previousActions != null && previousActions.Length > 0)
            {
                var filters = new List<FilterOption>
                {
                    new() { Field = string.Equals(requestSource, "TripRequest", StringComparison.OrdinalIgnoreCase) ? nameof(TM_APPROVAL_HISTORY.INT_TRIP_REQEST_ID) : nameof(TM_APPROVAL_HISTORY.INT_BUS_REQEST_ID), Operator = "=", Value = requestId.Value }
                };

                var historyResp = await _tmApprovalHistoryService.SearchByConditionAsync(new SearchOptions
                {
                    PageNumber = -1,
                    PageSize = 100000,
                    Filters = filters,
                    SortOptions = new()
                    {
                        new SortOption { Field = nameof(TM_APPROVAL_HISTORY.DTM_ACTION), SortDirection = "DESC" },
                        new SortOption { Field = nameof(TM_APPROVAL_HISTORY.ID), SortDirection = "DESC" }
                    }
                });

                var histories = historyResp?.Data?.Data?.ToList() ?? new List<TM_APPROVAL_HISTORYDTO>();
                var found = histories.FirstOrDefault(h => previousActions.Any(a => string.Equals(a, h.CHR_ACTION, StringComparison.OrdinalIgnoreCase)));
                if (found != null && found.DTM_ACTION.HasValue)
                {
                    submissionTime = found.DTM_ACTION.Value;
                }
            }

            var isDelayed = submissionTime.Date <= yesterday;
            if (!isDelayed)
            {
                return new RequestDelayInfo(false, null, null, false);
            }

            string? stage = null;
            string? url = null;
            var isGa = false;

            if (string.Equals(requestSource, "BusRequest", StringComparison.OrdinalIgnoreCase))
            {
                switch (status)
                {
                    case (int)BusRequestStatus.PendingQlscApproval:
                        stage = "qlsc";
                        url = BuildApprovalUrl("BusRequestApproval", "qlsc");
                        break;
                    case (int)BusRequestStatus.QlscApproved:
                        stage = "ga";
                        url = BuildApprovalUrl("BusRequestApproval", "ga");
                        isGa = true;
                        break;
                    default:
                        break;
                }
            }
            else if (string.Equals(requestSource, "TripRequest", StringComparison.OrdinalIgnoreCase))
            {
                switch (status)
                {
                    case TripRequestStatuses.PendingQlsc:
                        stage = "qlsc";
                        url = BuildApprovalUrl("TripRequestApproval", "qlsc");
                        break;
                    case TripRequestStatuses.QlscApproved:
                        stage = "qltc";
                        url = BuildApprovalUrl("TripRequestApproval", "qltc");
                        break;
                    case TripRequestStatuses.QltcApproved:
                        stage = "ga";
                        url = BuildApprovalUrl("TripRequestApproval", "ga");
                        isGa = true;
                        break;
                    case TripRequestStatuses.QuoteSentByGa:
                        stage = "employee";
                        url = BuildApprovalUrl("TripRequestApproval", "employee");
                        break;
                    case TripRequestStatuses.EmployeeAcceptedQuote:
                        stage = "ga-driver";
                        url = BuildApprovalUrl("TripRequestApproval", "ga-driver");
                        isGa = true;
                        break;
                    default:
                        break;
                }
            }

            return new RequestDelayInfo(true, stage, url, isGa);
        }

        private string BuildApprovalUrl(string controllerName, string stage)
        {
            var configuration = HttpContext?.RequestServices?.GetService(typeof(IConfiguration)) as IConfiguration;
            var baseUrl = configuration?[
                "SystemLinks:BusManagementWebApp"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = Request?.Host.HasValue == true
                    ? $"{Request.Scheme}://{Request.Host.Value}"
                    : string.Empty;
            }

            var relativePath = Url.Action(nameof(Index), controllerName, new { stage }) ?? $"/{controllerName}?stage={stage}";
            if (string.IsNullOrWhiteSpace(baseUrl)) return relativePath;
            return $"{baseUrl.TrimEnd('/')}{relativePath}";
        }

        private static string? DetermineBusRequestCurrentApprover(TM_BUS_REQUESTDTO req)
        {
            return req.INT_STATUS switch
            {
                (int)BusRequestStatus.PendingQlscApproval => "QLSC",
                (int)BusRequestStatus.QlscApproved => "GA",
                _ => null
            };
        }

        private static string? DetermineTripRequestCurrentApprover(TM_TRIP_REQUESTDTO req)
        {
            return req.INT_STATUS switch
            {
                TripRequestStatuses.PendingQlsc => "QLSC",
                TripRequestStatuses.QlscApproved => "QLTC",
                TripRequestStatuses.QltcApproved => "GA",
                _ => null
            };
        }

        private static string? DetermineApproverEmailForBusRequest(TM_BUS_REQUESTDTO req)
        {
            if (req.INT_STATUS == (int)BusRequestStatus.PendingQlscApproval && !string.IsNullOrWhiteSpace(req.CHR_QLSC_APPROVE))
            {
                return ToCompanyEmail(req.CHR_QLSC_APPROVE);
            }
            if (req.INT_STATUS == (int)BusRequestStatus.QlscApproved)
            {
                return "BIVNgabus@brother-bivn.com.vn"; // GA group email
            }
            return null;
        }

        private static string? DetermineApproverEmailForTripRequest(TM_TRIP_REQUESTDTO req)
        {
            if (req.INT_STATUS == TripRequestStatuses.PendingQlsc && !string.IsNullOrWhiteSpace(req.CHR_QLSC_APPROVE))
            {
                return ToCompanyEmail(req.CHR_QLSC_APPROVE);
            }
            if (req.INT_STATUS == TripRequestStatuses.QlscApproved && !string.IsNullOrWhiteSpace(req.CHR_QLTC_APPROVE))
            {
                return ToCompanyEmail(req.CHR_QLTC_APPROVE);
            }
            if (req.INT_STATUS == TripRequestStatuses.QltcApproved)
            {
                return "BIVNgabus@brother-bivn.com.vn"; // GA group email
            }
            if (req.INT_STATUS == TripRequestStatuses.QuoteSentByGa)
            {
                return ToCompanyEmail(req.CHR_EMPLOYEE_ID);
            }
            return null;
        }

        private async Task<Dictionary<string, TM_EMPLOYEEDTO>> LoadEmployeeMapAsync(List<string> employeeCodes)
        {
            if (!employeeCodes.Any()) return new Dictionary<string, TM_EMPLOYEEDTO>(StringComparer.OrdinalIgnoreCase);

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

            return (resp?.Data?.Data ?? new List<TM_EMPLOYEEDTO>())
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_EMPLOYEE_ID))
                .GroupBy(x => x.CHR_EMPLOYEE_ID!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        private async Task<Dictionary<string, TM_SECTIONDTO>> LoadSectionMapAsync()
        {
            var resp = await _sectionService.GetAllAsync();
            return (resp?.Data ?? new List<TM_SECTIONDTO>())
                .Where(x => !string.IsNullOrWhiteSpace(x.CHR_CODE))
                .GroupBy(x => x.CHR_CODE!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        private async Task<string?> ResolveApproverNameAsync(string? adid)
        {
            if (string.IsNullOrWhiteSpace(adid)) return null;

            var resp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
                {
                    new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ADID), Operator = "=", Value = adid.Trim() }
                }
            });
            if (resp != null && resp.Data != null && resp.Data.Data != null && resp.Data.Data.Count > 0)
            {
                return resp?.Data?.Data?.FirstOrDefault()?.NVCHR_EMPLOYEE_NAME ?? adid;
            }

            // Case employee code
            var employeeResp = await _tmEmployeeService.SearchByConditionAsync(new SearchOptions
            {
                PageNumber = 1,
                PageSize = 1,
                Filters = new List<FilterOption>
            {
                new() { Field = nameof(TM_EMPLOYEE.CHR_EMPLOYEE_ID), Operator = "=", Value = adid.Trim() }
            }
            });

            var emp = employeeResp?.Data?.Data?.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(emp?.CHR_EMPLOYEE_MAIL)) return emp.CHR_EMPLOYEE_MAIL!;
            if (!string.IsNullOrWhiteSpace(emp?.CHR_EMPLOYEE_ADID)) return ToCompanyEmail(emp.CHR_EMPLOYEE_ADID);

            if (emp == null || string.IsNullOrWhiteSpace(emp?.CHR_EMPLOYEE_ADID))
            {
                // Fall back to user service if available EMPLOYEE_ID
                var userResp = await _tmUserService.SearchByConditionAsync(new SearchOptions
                {
                    PageNumber = 1,
                    PageSize = 1,
                    Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_EMPLOYEE_ID", Operator = "=", Value = adid }
                }
                });
                var userData = userResp?.Data?.Data?.FirstOrDefault();
                if (userData != null)
                {
                    return ToCompanyEmail(userData.CHR_ADID);
                }

                // Fall back ADID
                var userResp1 = await _tmUserService.SearchByConditionAsync(new SearchOptions
                {
                    PageNumber = 1,
                    PageSize = 1,
                    Filters = new List<FilterOption>
                {
                    new() { Field = "CHR_ADID", Operator = "=", Value = adid }
                }
                });
                var userData1 = userResp?.Data?.Data?.FirstOrDefault();
                if (userData1 != null)
                {
                    return ToCompanyEmail(userData1.CHR_ADID);
                }

            }
            return ToCompanyEmail(adid);
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

        private static string? ReplaceTemplateTokens(string? template, IReadOnlyDictionary<string, string> tokens)
        {
            if (string.IsNullOrWhiteSpace(template)) return null;

            var result = template;
            foreach (var token in tokens)
            {
                result = result.Replace(token.Key, token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }

        private static string ToCompanyEmail(string? adid)
        {
            if (string.IsNullOrWhiteSpace(adid)) return string.Empty;
            var value = adid.Trim();
            return value.Contains('@') ? value : $"{value}@brothergroup.net";
        }


        private static List<string> ParseCsvInts(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
            return csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
        }

        //private static List<int> ParseCsvInts(string? csv)
        //{
        //    if (string.IsNullOrWhiteSpace(csv))
        //        return new List<int>();

        //    return csv
        //        .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //        .Select(x => x.Trim())
        //        .Select(x => int.TryParse(x, out var n) ? (int?)n : null)
        //        .Where(x => x.HasValue)
        //        .Select(x => x!.Value)
        //        .Distinct()
        //        .ToList();
        //}

        private static List<string> ParseCsvStrings(string? csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return new List<string>();
            return csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
        }

        private IActionResult RedirectToIndex(string? requestTypeIds, string? employeeCode, DateTime? dateFrom, DateTime? dateTo)
        {
            return RedirectToAction(nameof(Index), new { requestTypeIds, employeeCode, dateFrom, dateTo });
        }
    }
}
