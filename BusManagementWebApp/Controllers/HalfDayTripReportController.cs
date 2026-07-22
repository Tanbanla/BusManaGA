using BusManagement.Model.Common;
using BusManagement.Model.Common.Enum;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using BusManagement.Service.Services.Interfaces;
using BusManagement.ViewModels.TripRequest;
using BusManagementWebApp.Helpers;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TM_EMPLOYEE = BusManagement.Model.Entity.TM_EMPLOYEE;

namespace BusManagement.Controllers;

public class HalfDayTripReportController : Controller
{
    private const string HalfDayTripTypeName = "Xe đi về nửa ngày";

    private readonly ITmTripRequestService _tmTripRequestService;
    private readonly ITmEmployeeService _tmEmployeeService;
    private readonly ISectionService _sectionService;

    public HalfDayTripReportController(
        ITmTripRequestService tmTripRequestService,
        ITmEmployeeService tmEmployeeService,
        ISectionService sectionService)
    {
        _tmTripRequestService = tmTripRequestService ?? throw new ArgumentNullException(nameof(tmTripRequestService));
        _tmEmployeeService = tmEmployeeService ?? throw new ArgumentNullException(nameof(tmEmployeeService));
        _sectionService = sectionService ?? throw new ArgumentNullException(nameof(sectionService));
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? department,
        string? employeeCode,
        string? employeeName,
        string? phone,
        string? driverInfo,
        DateTime? pickupGoFrom,
        DateTime? pickupGoTo,
        DateTime? pickupReturnFrom,
        DateTime? pickupReturnTo,
        int page = 1,
        int pageSize = 10)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var joinOptions = BuildRouteAssignJoinOptions(employeeName, phone, department);

        var filters = new List<FilterOption>
        {
            new() { Field = nameof(TM_TRIP_REQUEST.INT_STATUS), Operator = "!=", Value = TripRequestStatuses.Deleted },
            new() { Field = nameof(TM_TRIP_REQUEST.NVCHR_TRIP_TYPE), Operator = "=", Value = HalfDayTripTypeName }
        };

        if (!string.IsNullOrWhiteSpace(employeeName))
        {
            filters.Add(new FilterOption { Field = $"e.{nameof(TM_EMPLOYEE.NVCHR_EMPLOYEE_NAME)}", Operator = "LIKE", Value = $"{employeeName.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            filters.Add(new FilterOption { Field = $"e.{nameof(TM_EMPLOYEE.CHR_PHONE)}", Operator = "LIKE", Value = $"{phone.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            filters.Add(new FilterOption { Field = $"e.{nameof(TM_EMPLOYEE.CHR_CODE_SEC)}", Operator = "LIKE", Value = $"{department.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(employeeCode))
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.CHR_EMPLOYEE_ID), Operator = "LIKE", Value = $"{employeeCode.Trim()}" });
        }

        if (!string.IsNullOrWhiteSpace(driverInfo))
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.NVCHR_DRIVER_INFO), Operator = "LIKE", Value = $"{driverInfo.Trim()}" });
        }

        if (pickupGoFrom.HasValue)
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.DTM_PICKUP_GO), Operator = ">=", Value = pickupGoFrom.Value.Date });
        }

        if (pickupGoTo.HasValue)
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.DTM_PICKUP_GO), Operator = "<=", Value = pickupGoTo.Value.Date.AddDays(1).AddTicks(-1) });
        }

        if (pickupReturnFrom.HasValue)
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.DTM_PICKUP_RETURN), Operator = ">=", Value = pickupReturnFrom.Value.Date });
        }

        if (pickupReturnTo.HasValue)
        {
            filters.Add(new FilterOption { Field = nameof(TM_TRIP_REQUEST.DTM_PICKUP_RETURN), Operator = "<=", Value = pickupReturnTo.Value.Date.AddDays(1).AddTicks(-1) });
        }

        var tripResp = await _tmTripRequestService.SearchByConditionAsync(new SearchOptions
        {
            PageNumber = page,
            PageSize = pageSize,
            Filters = filters,
            JoinOptions = joinOptions,
            SortOptions = new()
            {
                new SortOption { Field = nameof(TM_TRIP_REQUEST.DTM_PICKUP_GO), SortDirection = "ASC" },
                new SortOption { Field = nameof(TM_TRIP_REQUEST.DTM_CREATE), SortDirection = "DESC" }
            }
        });

        var requests = tripResp?.Data?.Data?.ToList() ?? new List<TM_TRIP_REQUESTDTO>();

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
                StatusText = x.INT_STATUS.HasValue ? (x.INT_STATUS == TripRequestStatuses.Completed ? "Hoàn thành" : "Chờ xử lý") : "",
                StatusRowClass = TripRequestStatusUiHelper.GetRowClass(x.INT_STATUS),
                StatusBadgeClass = TripRequestStatusUiHelper.GetBadgeClass(x.INT_STATUS)
            };
        }).ToList();

        var vm = new TripRequestPageViewModel
        {
            Department = department,
            EmployeeCode = employeeCode,
            EmployeeName = employeeName,
            Phone = phone,
            PageNumber = page,
            PageSize = pageSize,
            TotalCount = (int)(tripResp?.Data?.TotalCount ?? rows.Count),
            TotalPages = (int)(tripResp?.Data?.TotalPages ?? 1),
            Requests = rows,
            Sections = sections.ToList(),
            RequestTypeNames = new List<string> { HalfDayTripTypeName }
        };

        if (vm.TotalPages <= 0)
        {
            vm.TotalPages = vm.TotalCount > 0 ? (int)Math.Ceiling(vm.TotalCount / (double)pageSize) : 1;
        }

        ViewBag.DriverInfo = driverInfo;
        ViewBag.PickupGoFrom = pickupGoFrom;
        ViewBag.PickupGoTo = pickupGoTo;
        ViewBag.PickupReturnFrom = pickupReturnFrom;
        ViewBag.PickupReturnTo = pickupReturnTo;

        return View("~/Views/Report/HalfDayTrip/Index.cshtml", vm);
    }

    private static List<JoinOption> BuildRouteAssignJoinOptions(string? employeeName, string? phone, string? department)
    {
        var joins = new List<JoinOption>();

        if (!string.IsNullOrWhiteSpace(employeeName) || !string.IsNullOrWhiteSpace(phone) || !string.IsNullOrWhiteSpace(department))
        {
            joins.Add(new JoinOption { JoinType = "LEFT JOIN", Table = nameof(TM_EMPLOYEE), Alias = "e", OnCondition = $"{nameof(TM_TRIP_REQUEST)}.{nameof(TM_TRIP_REQUEST.CHR_EMPLOYEE_ID)} = e.CHR_EMPLOYEE_ID" });
        }

        return joins;
    }

    [HttpGet]
    public async Task<IActionResult> Export(string? department, string? employeeCode, string? employeeName, string? phone, string? driverInfo, DateTime? pickupGoFrom, DateTime? pickupGoTo, DateTime? pickupReturnFrom, DateTime? pickupReturnTo)
    {
        var result = await Index(department, employeeCode, employeeName, phone, driverInfo, pickupGoFrom, pickupGoTo, pickupReturnFrom, pickupReturnTo, 1, 100000);
        if (result is not ViewResult vr || vr.Model is not TripRequestPageViewModel vm)
        {
            return RedirectToAction(nameof(Index));
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("HalfDayTrips");
        sheet.Cell(1, 1).Value = "STT";
        sheet.Cell(1, 2).Value = "MNV";
        sheet.Cell(1, 3).Value = "Phòng ban";
        sheet.Cell(1, 4).Value = "Họ tên";
        sheet.Cell(1, 5).Value = "SĐT";
        sheet.Cell(1, 6).Value = "Ngày đón đi";
        sheet.Cell(1, 7).Value = "Giờ đón đi";
        sheet.Cell(1, 8).Value = "Đón từ điểm đi";
        sheet.Cell(1, 9).Value = "Ngày đón về";
        sheet.Cell(1, 10).Value = "Giờ đón về";
        sheet.Cell(1, 11).Value = "Đón từ điểm về";
        sheet.Cell(1, 12).Value = "Thông tin lái xe";

        var row = 2;
        var stt = 1;
        foreach (var item in vm.Requests)
        {
            sheet.Cell(row, 1).Value = stt;
            sheet.Cell(row, 2).Value = item.EmployeeCode;
            sheet.Cell(row, 3).Value = item.DepartmentName;
            sheet.Cell(row, 4).Value = item.EmployeeName;
            sheet.Cell(row, 5).Value = item.Phone;
            sheet.Cell(row, 6).Value = item.PickupGo?.ToString("dd/MM/yyyy");
            sheet.Cell(row, 7).Value = item.PickupGo?.ToString("HH:mm");
            sheet.Cell(row, 8).Value = item.FromGo;
            sheet.Cell(row, 9).Value = item.PickupReturn?.ToString("dd/MM/yyyy");
            sheet.Cell(row, 10).Value = item.PickupReturn?.ToString("HH:mm");
            sheet.Cell(row, 11).Value = item.FromReturn;
            sheet.Cell(row, 12).Value = item.DriverInfo;
            row++; stt++;
        }

        sheet.Columns().AdjustToContents();
        sheet.Column(12).Style.Alignment.WrapText = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"HalfDayTrips_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
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
}
