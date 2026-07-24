using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Text;

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

                cb.DTM_BORROW_TIME,
                cb.DTM_RETURN_TIME,
                
                e.CHR_CODE_SEC

            FROM TM_CARD_USAGE cu
            LEFT JOIN TM_CARD_BORROW cb
                ON cb.id  = cu.INT_BORROW_ID
            LEFT JOIN TM_CARD c
                ON cb.INT_CARD_ID = c.ID
            LEFT JOIN TM_TAXI t
                ON c.INT_TAXI_ID = t.ID
            left join TM_EMPLOYEE as e 
				on cu.CHR_EMPLOYEE_ID = e.CHR_EMPLOYEE_ID
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
            sql += " AND cu.NVCHR_EMPLOYEE_NAME like @EmployeeName";
            parameters.Add("@EmployeeName", $"%{serach.EmployeeName}%");
        }

        if (!string.IsNullOrEmpty(serach.SectionCode))
        {
            sql += " AND e.CHR_CODE_SEC = @SectionCode";
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
            sql += " AND CONVERT(DATE, cb.DTM_RETURN_TIME) >=  CONVERT(DATE, @DateBorow)";
            parameters.Add("@DateBorow", serach.DateBorow.Value);
        }

        if (serach.DateReturn.HasValue)
        {
            sql += " AND CONVERT(DATE, cb.DTM_RETURN_TIME) <= CONVERT(DATE, @DateReturn)";
            parameters.Add("@DateReturn", serach.DateReturn.Value);
        }

        if (serach.Number.HasValue)
        {
            sql += " AND cu.INT_NUMBER_MOVE = @Number";
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
    //List card taxi need usable
    public async Task<List<TM_CARD>> GetListCardTaxiUsableAsync()
    {
        var sql = @"SELECT distinct c.*
                    FROM TM_CARD as c
                    WHERE BIT_IS_ACTIVE = 1
					and id not in (SELECT INT_CARD_ID FROM TM_CARD_BORROW where BIT_END = 0)
                    and c.INT_CARD_TYPE = 2
                    ";
        var result = (await _conn.QueryAsync<TM_CARD>(sql)).ToList();
        return result;
    }
    public async Task<UsingCardViewModel> GetCardUsing(string? employeeId, string? department, int page = 1, int pageSize = 10)
    {
        var sql = @"
            SELECT 
                c.*,
                e.CHR_CODE_SEC,
                e.NVCHR_EMPLOYEE_NAME,
                b.CHR_BUS_CODE as BusCode,
                bs.NVCHR_STOP_NAME
            FROM TM_CARD c
            LEFT JOIN TM_EMPLOYEE e ON c.CHR_EMPLOYEE_ID = e.CHR_EMPLOYEE_ID
            LEFT JOIN TM_BUS b ON c.INT_BUS_ID = b.ID
            LEFT JOIN TM_BUS_STOP bs ON c.INT_BUS_STOP_ID = bs.ID
            WHERE c.INT_CARD_TYPE = 1
            AND c.BIT_IS_ACTIVE = 1
        ";

        var parameters = new DynamicParameters();

        #region Filter
        if (!string.IsNullOrEmpty(employeeId))
        {
            sql += " AND e.CHR_EMPLOYEE_ID like @EmployeeId";
            parameters.Add("@EmployeeId", $"%{employeeId}%");
        }

        if (!string.IsNullOrEmpty(department))
        {
            sql += " AND e.CHR_CODE_SEC = @Department";
            parameters.Add("@Department", department);
        }


        #endregion

        // Query tổng record
        var countSql = $@"
            SELECT COUNT(1)
            FROM (
                {sql}
            ) AS CountResult
        ";

        var totalRecords = await _conn.ExecuteScalarAsync<int>(countSql, parameters);


        #region Paging

        int offset = (page - 1) * pageSize;

        sql += @"
            ORDER BY c.ID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
        ";

        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", pageSize);

        #endregion

        var data = (await _conn.QueryAsync<dynamic>(sql, parameters)).ToList();

        return new UsingCardViewModel
        {
            TotalCount = totalRecords,
            PageNumber = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalRecords * 1.0 / pageSize),
            Cards = data
        };
    }
    // Update card buss
    public async Task<bool> UpdateCardBusAsync(TM_CARD card)
    {
        var sql = @"UPDATE TM_CARD
                    SET CHR_BUS_CODE = @BusCode,
                        INT_BUS_ID = @BusId,
                        INT_BUS_STOP_ID = @BusStopId,
                        DTM_UPDATE = GETDATE(),
                        CHR_USER_UPDATE = @UpdateBy   
                    WHERE ID = @Id";
        var parameters = new DynamicParameters();
        parameters.Add("@BusCode", card.CHR_BUS_CODE);
        parameters.Add("@BusId", card.INT_BUS_ID);
        parameters.Add("@BusStopId", card.INT_BUS_STOP_ID);
        parameters.Add("@Id", card.ID);
        parameters.Add("@UpdateBy", card.CHR_USER_UPDATE);

        var result = await _conn.ExecuteAsync(sql, parameters);
        return result > 0;
    }
    // delete
    public async Task<bool> DeleteCardAsync(int id)
    {
        var sql = @"UPDATE TM_CARD SET BIT_IS_ACTIVE = 0 WHERE ID = @Id";
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);

        var result = await _conn.ExecuteAsync(sql, parameters);
        return result > 0;
    }
    public async Task<bool> DeleteCardsAsync(List<int> ids)
    {
        var sql = @"UPDATE TM_CARD SET BIT_IS_ACTIVE = 0 WHERE ID IN @Ids";
        var parameters = new DynamicParameters();
        parameters.Add("@Ids", ids);

        var result = await _conn.ExecuteAsync(sql, parameters);
        return result > 0;
    }
}