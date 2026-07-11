using BusManagement.Model.Models;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Data.Repositories.Interfaces;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using BusManagement.Model.Common;
using System.Text;

namespace BusManagement.Data.Repositories.Implementations;

public class CenterRepository : ICenterRepository
{
    private readonly BusManagementDBContext _context;
    private readonly string _connectionString;
    private const string TableName = "TM_CENTER";
    private const string KeyColumn = "CHR_CODE_CENTER"; // primary key column

    public CenterRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration)
    {
        _context = context;
        // Use Agent DB connection for CenterRepository
        _connectionString = options.Value.AgentConnection;
    }

    #region Get All / Count
    public async Task<IEnumerable<TM_CENTER>> GetAllAsync()
    {
        // _context.TM_CENTERs is not available in trimmed DbContext; use Dapper instead
        return await GetAllWithDapperAsync();
    }

    public async Task<IEnumerable<TM_CENTER>> GetAllWithDapperAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = $"SELECT * FROM {TableName}";
        return await conn.QueryAsync<TM_CENTER>(sql);
    }

    public async Task<long> GetCountAllAsync()
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = $"SELECT COUNT(*) FROM {TableName}";
        return await conn.ExecuteScalarAsync<long>(sql);
    }
    #endregion

    #region Search / Paging
    public async Task<long> GetCountByConditionAsync(SearchOptions options)
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = new StringBuilder($"SELECT COUNT(*) FROM {TableName} WHERE 1=1");
        var parameters = new DynamicParameters();
        AppendSearchClause(sql, parameters, options);
        AppendFilterClause(sql, parameters, options);
        return await conn.ExecuteScalarAsync<long>(sql.ToString(), parameters);
    }

    public async Task<PagedList<TM_CENTER>> SearchByConditionAsync(SearchOptions options)
    {
        using var conn = new SqlConnection(_connectionString);
        var sqlSelect = new StringBuilder($"SELECT * FROM {TableName} WHERE 1=1");
        var sqlTotal = new StringBuilder($"SELECT COUNT(*) FROM {TableName}");
        var sqlFiltered = new StringBuilder($"SELECT COUNT(*) FROM {TableName} WHERE 1=1");
        var parameters = new DynamicParameters();

        AppendSearchClause(sqlSelect, parameters, options, sqlFiltered);
        AppendFilterClause(sqlSelect, parameters, options, sqlFiltered);

        // Sort
        if (options.SortOptions?.Any() == true)
        {
            sqlSelect.Append(" ORDER BY ");
            for (int i = 0; i < options.SortOptions.Count; i++)
            {
                var s = options.SortOptions[i];
                var dir = s.SortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";
                if (i > 0) sqlSelect.Append(", ");
                sqlSelect.Append($"{s.Field} {dir}");
            }
        }
        else
        {
            sqlSelect.Append($" ORDER BY {KeyColumn} ASC");
        }

        // Paging (PageNumber = -1 => no paging)
        if (options.PageNumber != -1)
        {
            sqlSelect.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
            parameters.Add("@Offset", (options.PageNumber - 1) * options.PageSize);
            parameters.Add("@PageSize", options.PageSize);
        }

        var finalSql = $"{sqlSelect}; {sqlTotal}; {sqlFiltered};";
        using var multi = await conn.QueryMultipleAsync(finalSql, parameters);
        var data = (await multi.ReadAsync<TM_CENTER>()).ToList();
        var total = await multi.ReadFirstAsync<long>();
        var totalFiltered = await multi.ReadFirstAsync<long>();

        return new PagedList<TM_CENTER>
        {
            Data = data,
            PageIndex = options.PageNumber,
            PageSize = options.PageSize,
            TotalCount = total,
            TotalFilter = (int)totalFiltered,
            TotalPages = options.PageNumber == -1 ? 1 : (int)Math.Ceiling((decimal)totalFiltered / options.PageSize)
        };
    }

    private static void AppendSearchClause(StringBuilder target, DynamicParameters parameters, SearchOptions options, StringBuilder? also = null)
    {
        if (!string.IsNullOrEmpty(options.SearchTerm) && options.SearchFields?.Any() == true)
        {
            var conds = new List<string>();
            for (int i = 0; i < options.SearchFields.Count; i++)
            {
                var field = options.SearchFields[i];
                var param = $"@SearchTerm{i}";
                conds.Add($"{field} COLLATE Latin1_General_CI_AI LIKE {param}");
                parameters.Add(param, $"%{options.SearchTerm}%");
            }
            var clause = " AND (" + string.Join(" OR ", conds) + ")";
            target.Append(clause);
            also?.Append(clause);
        }
    }

    private static void AppendFilterClause(StringBuilder target, DynamicParameters parameters, SearchOptions options, StringBuilder? also = null)
    {
        if (options.Filters?.Any() == true)
        {
            var fragments = new List<string>();
            for (int i = 0; i < options.Filters.Count; i++)
            {
                var f = options.Filters[i];
                var logic = i == 0 ? string.Empty : $" {f.LogicType?.ToUpper() ?? "AND"} ";
                var op = f.Operator?.ToUpper() ?? "=";
                var field = f.Field;
                var pname = $"@Filter_{i}";
                switch (op)
                {
                    case "IN":
                        if (f.Value is IEnumerable<object> list)
                        {
                            var inParams = new List<string>();
                            int j = 0;
                            foreach (var item in list)
                            {
                                var p = $"{pname}_{j}";
                                inParams.Add(p);
                                parameters.Add(p, item);
                                j++;
                            }
                            fragments.Add($"{logic}{field} IN (" + string.Join(",", inParams) + ")");
                        }
                        break;
                    case "IS NULL":
                    case "IS NOT NULL":
                        fragments.Add($"{logic}{field} {op}");
                        break;
                    case "LIKE":
                        parameters.Add(pname, $"%{f.Value}%");
                        fragments.Add($"{logic}{field} COLLATE Latin1_General_CI_AI LIKE {pname}");
                        break;
                    default:
                        parameters.Add(pname, f.Value);
                        fragments.Add($"{logic}{field} {op} {pname}");
                        break;
                }
            }
            if (fragments.Count > 0)
            {
                var clause = " AND (" + string.Join(" ", fragments) + ")";
                target.Append(clause);
                also?.Append(clause);
            }
        }
    }
    #endregion

    #region CRUD (using CHR_CODE_CENTER)
    public async Task<TM_CENTER> GetByIdAsync(string id)
    {
        // Use Dapper to fetch by id because DbContext no longer exposes TM_CENTERs
        using var conn = new SqlConnection(_connectionString);
        var sql = $"SELECT TOP 1 * FROM {TableName} WHERE CHR_CODE_CENTER = @Id";
        return await conn.QueryFirstOrDefaultAsync<TM_CENTER>(sql, new { Id = id });
    }

    public async Task<IEnumerable<TM_CENTER>> GetByIdsAsync(IEnumerable<string> ids)
    {
        // Use Dapper to fetch multiple centers by ids
        using var conn = new SqlConnection(_connectionString);
        var sql = $"SELECT * FROM {TableName} WHERE CHR_CODE_CENTER IN @Ids";
        var list = await conn.QueryAsync<TM_CENTER>(sql, new { Ids = ids.ToList() });
        return list;
    }

    public async Task<string> AddAsync(TM_CENTER entity)
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = $@"INSERT INTO {TableName} (CHR_CODE_CENTER, CHR_CODE_DEPT, CHR_CODE_SEC, NVCHR_CENTER, NVCHR_CENTER_SHORT, CHR_CRT_USERID, DTM_CREATE, CHR_UPD_USERID, DTM_UPDATE)
                     VALUES (@CHR_CODE_CENTER, @CHR_CODE_DEPT, @CHR_CODE_SEC, @NVCHR_CENTER, @NVCHR_CENTER_SHORT, @CHR_CRT_USERID, @DTM_CREATE, @CHR_UPD_USERID, @DTM_UPDATE);";
        await conn.ExecuteAsync(sql, entity);
        return entity.CHR_CODE_CENTER;
    }

    public async Task<TM_CENTER> AddAsyncV2(TM_CENTER entity)
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = $@"INSERT INTO {TableName} (CHR_CODE_CENTER, CHR_CODE_DEPT, CHR_CODE_SEC, NVCHR_CENTER, NVCHR_CENTER_SHORT, CHR_CRT_USERID, DTM_CREATE, CHR_UPD_USERID, DTM_UPDATE)
                     OUTPUT Inserted.*
                     VALUES (@CHR_CODE_CENTER, @CHR_CODE_DEPT, @CHR_CODE_SEC, @NVCHR_CENTER, @NVCHR_CENTER_SHORT, @CHR_CRT_USERID, @DTM_CREATE, @CHR_UPD_USERID, @DTM_UPDATE);";
        return await conn.QuerySingleAsync<TM_CENTER>(sql, entity);
    }

    public async Task<int> AddMultiAsync(IEnumerable<TM_CENTER> entities)
    {
        if (entities == null || !entities.Any()) return 0;
        using var conn = new SqlConnection(_connectionString);
        var sql = $@"INSERT INTO {TableName} (CHR_CODE_CENTER, CHR_CODE_DEPT, CHR_CODE_SEC, NVCHR_CENTER, NVCHR_CENTER_SHORT, CHR_CRT_USERID, DTM_CREATE, CHR_UPD_USERID, DTM_UPDATE)
                     VALUES (@CHR_CODE_CENTER, @CHR_CODE_DEPT, @CHR_CODE_SEC, @NVCHR_CENTER, @NVCHR_CENTER_SHORT, @CHR_CRT_USERID, @DTM_CREATE, @CHR_UPD_USERID, @DTM_UPDATE);";
        return await conn.ExecuteAsync(sql, entities);
    }

    public async Task<List<TM_CENTER>> AddMultiAsyncV2(IEnumerable<TM_CENTER> entities)
    {
        var list = new List<TM_CENTER>();
        if (entities == null || !entities.Any()) return list;
        using var conn = new SqlConnection(_connectionString);
        var sql = $@"INSERT INTO {TableName} (CHR_CODE_CENTER, CHR_CODE_DEPT, CHR_CODE_SEC, NVCHR_CENTER, NVCHR_CENTER_SHORT, CHR_CRT_USERID, DTM_CREATE, CHR_UPD_USERID, DTM_UPDATE)
                     OUTPUT Inserted.*
                     VALUES (@CHR_CODE_CENTER, @CHR_CODE_DEPT, @CHR_CODE_SEC, @NVCHR_CENTER, @NVCHR_CENTER_SHORT, @CHR_CRT_USERID, @DTM_CREATE, @CHR_UPD_USERID, @DTM_UPDATE);";
        foreach (var e in entities)
        {
            list.Add(await conn.QuerySingleAsync<TM_CENTER>(sql, e));
        }
        return list;
    }

    public async Task<bool> IsExistsAsync(string fieldName, object value)
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = $"SELECT COUNT(1) FROM {TableName} WHERE {fieldName} = @Value";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { Value = value });
        return count > 0;
    }

    public async Task<int> UpdateAsync(TM_CENTER entity)
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = $@"UPDATE {TableName} SET 
                        CHR_CODE_DEPT = @CHR_CODE_DEPT,
                        CHR_CODE_SEC = @CHR_CODE_SEC,
                        NVCHR_CENTER = @NVCHR_CENTER,
                        NVCHR_CENTER_SHORT = @NVCHR_CENTER_SHORT,
                        CHR_UPD_USERID = @CHR_UPD_USERID,
                        DTM_UPDATE = @DTM_UPDATE
                     WHERE {KeyColumn} = @CHR_CODE_CENTER";
        return await conn.ExecuteAsync(sql, entity);
    }

    public async Task<int> UpdateMultiAsync(IEnumerable<TM_CENTER> entities)
    {
        if (entities == null || !entities.Any()) return 0;
        int affected = 0;
        foreach (var e in entities)
        {
            affected += await UpdateAsync(e);
        }
        return affected;
    }

    public async Task<int> DeleteAsync(string id)
    {
        using var conn = new SqlConnection(_connectionString);
        var sql = $"DELETE FROM {TableName} WHERE {KeyColumn} = @Id";
        return await conn.ExecuteAsync(sql, new { Id = id });
    }

    public Task<int> DeleteLogicAsync(string id) => DeleteAsync(id); // no logical delete column

    public Task DeleteLogicMultiAsync(IEnumerable<string> ids) => DeleteRangeAsync(ids);

    public async Task DeleteRangeAsync(IEnumerable<string> ids)
    {
        if (ids == null || !ids.Any()) return;
        using var conn = new SqlConnection(_connectionString);
        var sql = $"DELETE FROM {TableName} WHERE {KeyColumn} IN @Ids";
        await conn.ExecuteAsync(sql, new { Ids = ids.ToList() });
    }
    #endregion

    public Task SaveChangesAsync() => _context.SaveChangesAsync();

    // DbContext does not expose TM_CENTERs in this trimmed configuration, return empty queryable
    public Task<IQueryable<TM_CENTER>> GetQueryableAsync() => Task.FromResult(Enumerable.Empty<TM_CENTER>().AsQueryable());
}
