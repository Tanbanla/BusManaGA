using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmCardPrintQueueRepository : BaseRepository<TM_CARD_PRINT_QUEUE, int>, ITmCardPrintQueueRepository
{
    private readonly BusManagementDBContext _context;
    public TmCardPrintQueueRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
        _context = context;
    }
    // search card print queue with pagination
    public async Task<NotCardViewModel> SearchCardPrintQueueAsync(SearchCard searchCard)
    {
        var sql = @"
            SELECT
                cp.*,

                e.NVCHR_EMPLOYEE_NAME,
                e.CHR_CODE_SEC,

                er.INT_PICKUP_STOP_ID,
                er.INT_RETURN_STOP_ID,

                pickupStop.NVCHR_STOP_NAME AS PICKUP_STOP_NAME,
                returnStop.NVCHR_STOP_NAME AS RETURN_STOP_NAME,

                er.INT_ROUTE_ASSIGN_PICKUP_ID,
                er.INT_ROUTE_ASSIGN_RETURN_ID,

                pickupRoute.NVCHR_BUS_ROUTE_WORKING_NAME AS PICKUP_ROUTE_NAME,
                returnRoute.NVCHR_BUS_ROUTE_WORKING_NAME AS RETURN_ROUTE_NAME

            FROM TM_CARD_PRINT_QUEUE cp

            LEFT JOIN TM_EMPLOYEE e
                ON cp.CHR_EMPLOYEE_ID = e.CHR_EMPLOYEE_ID

            LEFT JOIN TM_EMPLOYEE_ROUTE er
                ON cp.INT_EMPLOYEE_ROUTE_ID = er.ID

            -- Điểm đón
            LEFT JOIN TM_BUS_STOP pickupStop
                ON er.INT_PICKUP_STOP_ID = pickupStop.ID

            -- Điểm trả
            LEFT JOIN TM_BUS_STOP returnStop
                ON er.INT_RETURN_STOP_ID = returnStop.ID

            -- Tuyến đón
            LEFT JOIN TM_BUS_ROUTE_ASSIGN pickupRoute
                ON er.INT_ROUTE_ASSIGN_PICKUP_ID = pickupRoute.ID

            -- Tuyến về
            LEFT JOIN TM_BUS_ROUTE_ASSIGN returnRoute
                ON er.INT_ROUTE_ASSIGN_RETURN_ID = returnRoute.ID

            WHERE cp.INT_STATUS IN (0, 1)
        ";

        var parameters = new DynamicParameters();

        #region Filter
        if (!string.IsNullOrEmpty(searchCard.EmployeeCode))
        {
            sql += " AND e.CHR_EMPLOYEE_ID like @EmployeeId";
            parameters.Add("@EmployeeId", $"%{searchCard.EmployeeCode}%");
        }

        if (!string.IsNullOrEmpty(searchCard.SectionCode))
        {
            sql += " AND e.CHR_CODE_SEC = @Department";
            parameters.Add("@Department", searchCard.SectionCode);
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

        int offset = (int)((searchCard.PageIndex - 1) * searchCard.PageSize);

        sql += @"
            ORDER BY cp.ID
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY
        ";

        parameters.Add("@Offset", offset);
        parameters.Add("@PageSize", searchCard.PageSize);

        #endregion

        var data = (await _conn.QueryAsync<dynamic>(sql, parameters)).ToList();

        return new NotCardViewModel
        {
            TotalCount = totalRecords,
            PageNumber = (int)(searchCard.PageIndex ?? 1),
            PageSize = (int)(searchCard.PageSize ?? 10),
            TotalPages = (int)Math.Ceiling(totalRecords * 1.0 / (int)(searchCard.PageSize ?? 10)),
            NotCards = data
        };
    }
    // Delete card print queue 
    public async Task<bool> DeleteCardPrintQueuesAsync(List<int> ids)
    {
        var sql = "UPDATE TM_CARD_PRINT_QUEUE SET INT_STATUS = 4 WHERE ID IN @Ids";
        var parameters = new DynamicParameters();
        parameters.Add("@Ids", ids);
        var result = await _conn.ExecuteAsync(sql, parameters);
        return result > 0;
    }
    // Print card queue 
    public async Task<bool> PrintCardQueuesAsync(
        List<int> ids,
        string user)
    {
        var printTable = await _context.TM_CARD_PRINT_QUEUEs
            .Where(x => ids.Contains(x.ID)
                     && x.INT_STATUS != 2)
            .ToListAsync();

        if (!printTable.Any())
        {
            throw new Exception("Không tìm thấy bản ghi để in.");
        }

        var insertTmCards = new List<TM_CARD>();

        foreach (var item in printTable)
        {
            item.INT_STATUS = 2;

            insertTmCards.Add(new TM_CARD
            {
                CHR_EMPLOYEE_ID = item.CHR_EMPLOYEE_ID,
                CHR_CARD_CODE = "",// sinh tự động 
                INT_CARD_TYPE = 1,
                NVCHR_CARD_TYPE = "Thẻ xe bus",
                INT_CARD_SCOPE = null,
                NVCHR_CARD_SCOPE = null,
                CHR_USER_CREATE = user,
                DTM_CREATE = DateTime.Now,
                //CHR_BUS_CODE = item.CHR_BUS_CODE,
                //INT_BUS_ID = item.INT_BUS_ID,
                //INT_TAXI_ID = item.INT_TAXI_ID,
                //INT_BUS_STOP_ID = item.INT_BUS_STOP_ID,
                DTM_ISSUE_DATE = DateTime.Now,
                DTM_EXPRIRATION_DATE = DateTime.Now.AddYears(1),
                BIT_IS_ACTIVE = true
            });
        }

        await _context.TM_CARDs.AddRangeAsync(insertTmCards);

        await _context.SaveChangesAsync();

        return true;
    }
}