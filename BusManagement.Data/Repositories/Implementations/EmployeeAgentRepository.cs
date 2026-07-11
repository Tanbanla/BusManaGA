using BusManagement.Data.Repositories.Implementations;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Models;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BusManagement.Data.Repositories.Implementations
{
    public class EmployeeAgentRepository : BaseRepository<Model.Models.TM_EMPLOYEE, int>, IEmployeeAgentRepository
    {
        private readonly string _AgentConnectionString;
        // Use Agent DB connection for employee agent repository
        public EmployeeAgentRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration, useAgent: true)
        {
            _AgentConnectionString = configuration.GetConnectionString("AgentConnection");
        }

        public async Task<IEnumerable<Model.Models.TM_EMPLOYEE>> GetByAdidAsync(string adid)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = $"SELECT * FROM {_tableName} WHERE CHR_EMPLOYEE_ADID = @Adid";
            return await connection.QueryAsync<Model.Models.TM_EMPLOYEE>(sql, new { Adid = adid });
        }

        public async Task<Model.Models.TM_EMPLOYEE> GetByEmployeeIdAsync(string employeeId)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = $"SELECT * FROM {_tableName} WHERE CHR_EMPLOYEE_ID = @EmployeeId";
            return await connection.QueryFirstOrDefaultAsync<Model.Models.TM_EMPLOYEE>(sql, new { EmployeeId = employeeId });
        }

        public async Task<PagedList<Model.Models.TM_EMPLOYEE>> SearchByDuplicateAdidAsync(SearchOptions options)
        {
            using var connection = new SqlConnection(_connectionString);
            var adidColumn = "CHR_EMPLOYEE_ADID";
            var duplicatePredicate = $"{adidColumn} IN (SELECT {adidColumn} FROM {_tableName} WHERE {adidColumn} IS NOT NULL AND {adidColumn} <> '' GROUP BY {adidColumn} HAVING COUNT(*) >1)";
            var sqlSelect = new StringBuilder($"SELECT * FROM {_tableName} WHERE {duplicatePredicate}");
            var sqlCount = new StringBuilder($"SELECT COUNT(*) FROM {_tableName} WHERE {duplicatePredicate}");
            var sqlFilteredCount = new StringBuilder($"SELECT COUNT(*) FROM {_tableName} WHERE {duplicatePredicate}");
            var parameters = new DynamicParameters();
            if (!string.IsNullOrEmpty(options.SearchTerm) && options.SearchFields?.Any() == true)
            {
                var searchConditions = new List<string>();
                for (int i =0; i < options.SearchFields.Count; i++)
                {
                    var field = options.SearchFields[i];
                    var paramName = $"@SearchTerm{i}";
                    searchConditions.Add($"{field} COLLATE Latin1_General_CI_AI LIKE {paramName}");
                    parameters.Add(paramName, $"%{options.SearchTerm}%");
                }
                var searchClause = " AND (" + string.Join(" OR ", searchConditions) + ")";
                sqlSelect.Append(searchClause);
                sqlFilteredCount.Append(searchClause);
            }
            if (options.Filters?.Any() == true)
            {
                var filterClauses = new List<string>();
                for (int i =0; i < options.Filters.Count; i++)
                {
                    var filter = options.Filters[i];
                    object value = filter.Value;
                    if (value is JsonElement jsonElement)
                    {
                        switch (jsonElement.ValueKind)
                        {
                            case JsonValueKind.String:
                                value = jsonElement.GetString();
                                break;
                            case JsonValueKind.Number:
                                if (jsonElement.TryGetInt32(out int intValue)) value = intValue; else if (jsonElement.TryGetDouble(out double doubleValue)) value = doubleValue;
                                break;
                            case JsonValueKind.True:
                            case JsonValueKind.False:
                                value = jsonElement.GetBoolean();
                                break;
                            case JsonValueKind.Null:
                                value = DBNull.Value;
                                break;
                            case JsonValueKind.Array:
                                var list = new List<object>();
                                foreach (var item in jsonElement.EnumerateArray())
                                {
                                    list.Add(item.ValueKind switch
                                    {
                                        JsonValueKind.String => item.GetString(),
                                        JsonValueKind.Number => item.TryGetInt32(out var intVal) ? intVal : item.GetDouble(),
                                        JsonValueKind.True => true,
                                        JsonValueKind.False => false,
                                        _ => item.ToString()
                                    });
                                }
                                value = list;
                                break;
                            default:
                                value = jsonElement.ToString();
                                break;
                        }
                    }
                    if (value is string stringValue)
                    {
                        stringValue = Regex.Replace(stringValue, @"\s+", " ").Trim();
                        value = stringValue;
                    }
                    var logic = (i ==0) ? "" : $" {filter.LogicType?.ToUpper() ?? "AND"} ";
                    var op = filter.Operator?.ToUpper() ?? "=";
                    var field = filter.Field;
                    var paramName = $"@Filter_{i}";
                    switch (op)
                    {
                        case "IN":
                            if (value is IEnumerable<object> list2)
                            {
                                var inParams = new List<string>();
                                int j =0;
                                foreach (var item in list2)
                                {
                                    var pName = $"{paramName}_{j}";
                                    inParams.Add(pName);
                                    parameters.Add(pName, item);
                                    j++;
                                }
                                filterClauses.Add($"{logic}{field} IN ({string.Join(",", inParams)})");
                            }
                            break;
                        case "BETWEEN":
                            if (value is IEnumerable<object> range && range.Count() ==2)
                            {
                                var fromParam = $"{paramName}_from";
                                var toParam = $"{paramName}_to";
                                var rangeList = range.ToList();
                                parameters.Add(fromParam, rangeList[0]);
                                parameters.Add(toParam, rangeList[1]);
                                filterClauses.Add($"{logic}{field} BETWEEN {fromParam} AND {toParam}");
                            }
                            break;
                        case "IS NULL":
                        case "IS NOT NULL":
                            filterClauses.Add($"{logic}{field} {op}");
                            break;
                        case "LIKE":
                            filterClauses.Add($"{logic}{field} COLLATE Latin1_General_CI_AI LIKE {paramName}");
                            parameters.Add(paramName, $"%{value}%");
                            break;
                        default:
                            filterClauses.Add($"{logic}{field} {op} {paramName}");
                            parameters.Add(paramName, value);
                            break;
                    }
                }
                var filterClause = " AND (" + string.Join(" ", filterClauses) + ")";
                sqlSelect.Append(filterClause);
                sqlFilteredCount.Append(filterClause);
            }
            if (options.SortOptions?.Any() == true)
            {
                sqlSelect.Append(" ORDER BY ");
                for (int i =0; i < options.SortOptions.Count; i++)
                {
                    var sort = options.SortOptions[i];
                    var direction = sort.SortDirection?.ToUpper() == "DESC" ? "DESC" : "ASC";
                    if (i >0) sqlSelect.Append(", ");
                    sqlSelect.Append($"{sort.Field} {direction}");
                }
            }
            else
            {
                sqlSelect.Append(" ORDER BY (SELECT NULL)");
            }

            if (options.PageNumber != -1)
            {
                sqlSelect.Append(" OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");
                parameters.Add("@Offset", (options.PageNumber -1) * options.PageSize);
                parameters.Add("@PageSize", options.PageSize);
            }

            var finalSql = $"{sqlSelect}; {sqlCount}; {sqlFilteredCount};";
            using var multi = await connection.QueryMultipleAsync(finalSql, parameters);
            var data = (await multi.ReadAsync<Model.Models.TM_EMPLOYEE>()).ToList();
            var totalCount = await multi.ReadFirstAsync<long>();
            var totalFiltered = await multi.ReadFirstAsync<int>();
            return new PagedList<Model.Models.TM_EMPLOYEE>
            {
                Data = data,
                PageIndex = options.PageNumber,
                PageSize = options.PageSize,
                TotalCount = totalCount,
                TotalFilter = totalFiltered,
                TotalPages = (int)Math.Ceiling((decimal)totalFiltered / options.PageSize)
            };
        }

        // Lấy thông tin QLSC phê duyệt theo phòng ban
        public async Task<List<TM_EMPLOYEE_AGENTDTO>> GetQLSCApproverBySection(string? sectionCode)
        {
            using var connection = new SqlConnection(_AgentConnectionString);

            var baseSql = @"SELECT 
                  DISTINCT
                  CHR_EMPLOYEE_ID,
                  CHR_EMPLOYEE_NAME,
                  CHR_SEC_CODE,
                  CHR_SEC_NAME,
                  CHR_POSITION_GROUP,
                  CHR_POSITION_CODE,
                  CHR_POSITION_NAME,
                  CHR_EMPLOYEE_ADID
                  FROM [AGENTDB].[dbo].[TM_EMPLOYEE]
                  WHERE CHR_NOTE is null 
                  AND DTM_LEAVE_DATE is null
                  AND CHR_POSITION_GROUP IS NOT NULL
                  AND CHR_POSITION_NAME IS NOT NULL
                  AND (CHR_POSITION_GROUP in ('Chief','Expert') OR CHR_POSITION_NAME IN ('Engineer 1','Engineer 2','Engineer 3','Senior Engineer'))";

            var sql = baseSql;
            var parameters = new DynamicParameters();

            // Chỉ thêm điều kiện phòng ban nếu sectionCode không null hoặc empty
            if (!string.IsNullOrWhiteSpace(sectionCode))
            {
                sql += " AND RTRIM(CASE WHEN CHARINDEX(':', CHR_SECTION) > 0 THEN LEFT(CHR_SECTION, CHARINDEX(':', CHR_SECTION) - 1) ELSE CHR_SECTION END) = @sectionCode";
                parameters.Add("sectionCode", sectionCode);
            }

            sql += " ORDER BY CHR_EMPLOYEE_NAME";

            try
            {
                var result = await connection.QueryAsync<TM_EMPLOYEE_AGENTDTO>(sql, parameters);
                return result.AsList();
            }
            catch (Exception ex)
            {
                var a = ex.Message;
                return new List<TM_EMPLOYEE_AGENTDTO>();
            }
        }

        // Lấy thông tin QLTC phê duyệt theo phòng ban
        public async Task<List<TM_EMPLOYEE_AGENTDTO>> GetQLTCApproverBySection(string? sectionCode)
        {
            using var connection = new SqlConnection(_AgentConnectionString);

            var baseSql = @"SELECT 
                  DISTINCT
                  CHR_EMPLOYEE_ID,
                  CHR_EMPLOYEE_NAME,
                  CHR_SEC_CODE,
                  CHR_SEC_NAME,
                  CHR_POSITION_GROUP,
                  CHR_POSITION_CODE,
                  CHR_POSITION_NAME,
                  CHR_EMPLOYEE_ADID
                  FROM [AGENTDB].[dbo].[TM_EMPLOYEE]
                  WHERE CHR_NOTE is null 
                  AND DTM_LEAVE_DATE is null
                  AND CHR_POSITION_GROUP IS NOT NULL
                  AND CHR_POSITION_GROUP in ('Section Manager', 'Assistant Section Manager')";

            var sql = baseSql;
            var parameters = new DynamicParameters();

            // Chỉ thêm điều kiện phòng ban nếu sectionCode không null hoặc empty
            if (!string.IsNullOrWhiteSpace(sectionCode))
            {
                sql += " AND RTRIM(CASE WHEN CHARINDEX(':', CHR_SECTION) > 0 THEN LEFT(CHR_SECTION, CHARINDEX(':', CHR_SECTION) - 1) ELSE CHR_SECTION END) = @sectionCode";
                parameters.Add("sectionCode", sectionCode);
            }

            sql += " ORDER BY CHR_EMPLOYEE_NAME";

            try
            {
                var result = await connection.QueryAsync<TM_EMPLOYEE_AGENTDTO>(sql, parameters);
                return result.AsList();
            }
            catch (Exception ex)
            {
                var a = ex.Message;
                return new List<TM_EMPLOYEE_AGENTDTO>();
            }
        }
    }
}
