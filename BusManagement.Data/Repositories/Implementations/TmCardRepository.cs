using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmCardRepository : BaseRepository<TM_CARD, int>, ITmCardRepository
{
    private readonly BusManagementDBContext _context;

    public TmCardRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
        _context = context;
    }
    // Search for cards by card taxi
    public async Task<List<dynamic>> GetCardByTaxiAsync(SearchCard serach)
    {
        var sql = @"SELECT
                cu.*,

                c.INT_CARD_TYPE,
                c.NVCHR_CARD_TYPE,
                c.INT_CARD_SCOPE,
                c.CHR_CARD_CODE,

                t.NVCHR_CATEGORY,
                t.NVCHR_BRAND,

                e.NVCHR_EMPLOYEE_NAME,
                e.CHR_CODE_SEC,

                cb.DTM_BORROW_TIME,
                cb.DTM_RETURN_TIME

            FROM TM_CARD_USAGE cu
            INNER JOIN TM_CARD c
                ON cu.INT_CARD_ID = c.ID

            LEFT JOIN TM_TAXI t
                ON c.INT_TAXI_ID = t.ID

            LEFT JOIN TM_EMPLOYEE e
                ON cu.CHR_EMPLOYEE_ID = e.CHR_EMPLOYEE_ID

            LEFT JOIN TM_CARD_BORROW cb
                ON cb.INT_CARD_ID = cu.INT_CARD_ID
                AND cb.CHR_EMPLOYEE_ID = cu.CHR_EMPLOYEE_ID

            WHERE ISNULL(cu.BIT_DELETE, 0) = 0
        ";

        var parameters = new DynamicParameters();


        // Thêm cấc điều kiện tìm kiếm vào câu truy vấn SQL
        if (!string.IsNullOrEmpty(serach.EmployeeCode))
        {
            sql += " AND cu.CHR_EMPLOYEE_ID = @EmployeeCode";
            parameters.Add("@EmployeeCode", serach.EmployeeCode);
        }

        if (!string.IsNullOrEmpty(serach.EmployeeName))
        {
            sql += " AND e.NVCHR_EMPLOYEE_NAME like @EmployeeName";
            parameters.Add("@EmployeeName", $"%{serach.EmployeeName}%");
        }

        if (!string.IsNullOrEmpty(serach.SectionCode))
        {
            sql += " AND e.NVCHR_SECTION_CODE = @SectionCode";
            parameters.Add("@SectionCode", serach.SectionCode);
        }

        if (!string.IsNullOrEmpty(serach.TaxiCardNumber))
        {
            sql += " AND c.CHR_CARD_CODE = @TaxiCardNumber";
            parameters.Add("@TaxiCardNumber", serach.TaxiCardNumber);
        }

        if (!string.IsNullOrEmpty(serach.TaxiCompany))
        {
            sql += " AND t.NVCHR_BRAND like @TaxiCompany";
            parameters.Add("@TaxiCompany", $"%{serach.TaxiCompany}%");
        }

        if (!string.IsNullOrEmpty(serach.AddressTo))
        {
            sql += " AND cu.NVCHR_TO_ADDRESS like @AddressTo";
            parameters.Add("@AddressTo", $"%{serach.AddressTo}%");
        }

        if (!string.IsNullOrEmpty(serach.AddressFrom))
        {
            sql += " AND cu.NVCHR_FROM_ADDRESS like @AddressFrom";
            parameters.Add("@AddressFrom", $"%{serach.AddressFrom}%");
        }

        if (serach.DateBorow.HasValue)
        {
            sql += " AND cb.DTM_BORROW_TIME >= @DateBorow";
            parameters.Add("@DateBorow", serach.DateBorow.Value);
        }

        if (serach.DateReturn.HasValue)
        {
            sql += " AND cb.DTM_RETURN_TIME <= @DateReturn";
            parameters.Add("@DateReturn", serach.DateReturn.Value);
        }

        if (serach.Number.HasValue)
        {
            sql += " AND c.INT_NUMBER_MOVE = @Number";
            parameters.Add("@Number", serach.Number.Value);
        }

        if (!string.IsNullOrEmpty(serach.CostSum))
        {
            sql += " AND cu.INT_AMOUNT = @CostSum";
            parameters.Add("@CostSum", serach.CostSum);
        }

        // Thêm ORDER BY và phân trang
        if (serach.PageSize > 0 && serach.PageIndex > 0)
        {
            sql += @"
                    ORDER BY cu.DTM_CREATE
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY
                ";
            parameters.Add("Offset", (serach.PageIndex - 1) * serach.PageSize);
            parameters.Add("PageSize", serach.PageSize);
        }
        else
        {
            sql += @"
                    ORDER BY cu.DTM_CREATE
                ";
        }

        var result = (await _conn.QueryAsync<dynamic>(sql, parameters)).ToList();

        return result;
    }
    // Delete soft card Taxi by IDs
    public async Task<bool> DeleteCardByTaxiAsync(List<int> ids)
    {
        var sql = @"UPDATE TM_CARD_USAGE
                    SET BIT_DELETE = 1
                    WHERE ID IN @Ids";
        var parameters = new DynamicParameters();
        parameters.Add("@Ids", ids);

        var result = await _conn.ExecuteAsync(sql, parameters);
        return result > 0;
    }
}