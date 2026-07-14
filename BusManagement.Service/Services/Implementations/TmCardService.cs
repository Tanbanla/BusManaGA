using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;
using ClosedXML.Excel;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BusManagement.Service.Services.Implementations;

public class TmCardService : BaseService<TM_CARD, int, TM_CARDDTO>, ITmCardService
{
    private readonly ITmCardRepository _repo;
    private readonly IHostEnvironment _hostEnvironment;

    public TmCardService(ITmCardRepository repository, IMapper mapper, IHostEnvironment hostEnvironment) : base(repository, mapper)
    {
        _repo = repository;
        _hostEnvironment = hostEnvironment;
    }
    // Search for cards by card taxi
    public async Task<GenericResponse<List<dynamic>>> GetCardByTaxiAsync(SearchCard serach)
    {
        var result = new GenericResponse<List<dynamic>>();
        try
        {
            result.Data = await _repo.GetCardByTaxiAsync(serach);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Status = 500;
            result.Message = ex.Message;
        }

        return result;
    }
    // Delete soft card Taxi by IDs
    public async Task<GenericResponse<bool>> DeleteCardByTaxiAsync(List<int> ids)
    {
        var result = new GenericResponse<bool>();
        try
        {
            result.Data = await _repo.DeleteCardByTaxiAsync(ids);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Status = 500;
            result.Message = ex.Message;
        }

        return result;
    }
    // Tải Excel dữ liệu thẻ taxi
    public async Task<GenericResponse<byte[]>> DownloadExcelTaxiAsync(SearchCard serach)
    {
        var result = new GenericResponse<byte[]>();
        try
        {
            serach.PageIndex = 0;
            serach.PageSize = 0;

            var dataExportAsync = await GetCardByTaxiAsync(serach);

            if (!dataExportAsync.Success)
            {
                result.Success = false;
                result.Status = dataExportAsync.Status ?? 500;
                result.Message = "Lỗi khi lấy dữ liệu: " + dataExportAsync.Message;
                return result;
            }

            var root = _hostEnvironment.ContentRootPath;
            var templatePath = Path.Combine(root, "Template", "TemplateCardTaxi.xlsx");
            if (!System.IO.File.Exists(templatePath))
            {
                result.Success = false;
                result.Status = 404;
                result.Message = "Không tìm thấy file template: TemplateCardTaxi.xlsx";
                return result;
            }

            using var fs = System.IO.File.OpenRead(templatePath);
            using var workbook = new XLWorkbook(fs);
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws == null)
            {
                result.Success = false;
                result.Status = 500;
                result.Message = "Không tìm thấy worksheet trong template";
                return result;
            }

            int rowStart = 2;

            foreach (var item in dataExportAsync.Data ?? Enumerable.Empty<dynamic>())
            {
                int i = 1;
                ws.Cell(rowStart, i++).Value = rowStart - 1;
                ws.Cell(rowStart, i++).Value = item.NVCHR_BRAND ?? "";
                ws.Cell(rowStart, i++).Value = item.CHR_CARD_CODE ?? "";
                ws.Cell(rowStart, i++).Value = item.CHR_EMPLOYEE_ID ?? "";
                ws.Cell(rowStart, i++).Value = item.NVCHR_EMPLOYEE_NAME ?? "";
                ws.Cell(rowStart, i++).Value = item.CHR_CODE_SEC ?? "";
                ws.Cell(rowStart, i++).Value = item.DTM_BORROW_TIME ?? "";
                ws.Cell(rowStart, i++).Value = item.NVCHR_NOTE ?? "";
                ws.Cell(rowStart, i++).Value = item.DTM_RETURN_TIME ?? "";
                ws.Cell(rowStart, i++).Value = item.DTM_USE_DATE ?? "";
                ws.Cell(rowStart, i++).Value = item.NVCHR_FROM_ADDRESS ?? "";
                ws.Cell(rowStart, i++).Value = item.NVCHR_TO_ADDRESS ?? "";
                ws.Cell(rowStart, i++).Value = item.CHR_TRAN_CODE ?? "";
                ws.Cell(rowStart, i++).Value = item.INT_NUMBER_MOVE ?? "";
                ws.Cell(rowStart, i++).Value = item.INT_AMOUNT ?? "";
                ws.Cell(rowStart, i++).Value = item.CHR_SEC_COST ?? "";


                rowStart++;
            }


            using var outStream = new MemoryStream();
            workbook.SaveAs(outStream);
            result.Data = outStream.ToArray();
            result.Success = true;
            result.Status = 200;
            result.Message = "Xuất file Excel thành công.";

            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Status = 500;
            result.Message = ex.Message;
        }
        return result;
    }

    //List card taxi need usable
    public async Task<GenericResponse<List<TM_CARD>>> GetListCardTaxiUsableAsync()
    {
        var result = new GenericResponse<List<TM_CARD>>();
        try
        {
            result.Data = await _repo.GetListCardTaxiUsableAsync();
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Status = 500;
            result.Message = ex.Message;
        }
        return result;
    }
}