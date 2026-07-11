using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using BusManagement.Data.Repositories.Implementations;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;

namespace BusManagement.Data.Repositories.Implementations;

public class EmployeeRepository : BaseRepository<Model.Entity.TM_EMPLOYEE, int>, IEmployeeRepository
{
    protected readonly string _agentConnectionString;
    protected readonly string _workingControlConnectionString;
    protected readonly string _ezConnectionString;
    public EmployeeRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {

        _agentConnectionString = configuration.GetConnectionString("AgentConnection");
        _workingControlConnectionString = configuration.GetConnectionString("WorkingConnection");
        _ezConnectionString = configuration.GetConnectionString("EzConnection");
    }

    // Lấy dữ liệu Employee từ db AGENTDB cho login return
    public async Task<Model.Models.TM_EMPLOYEE> GetAgentEmployeeByEmployeeADIDAsync(string adId)
    {
        Model.Models.TM_EMPLOYEE result = new Model.Models.TM_EMPLOYEE();
        using (var connection = new SqlConnection(_agentConnectionString))
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CHR_EMPLOYEE_ADID", adId);

            var sql = $@"
                    SELECT TOP 1 *
	                FROM AGENTDB.dbo.{nameof(Model.Models.TM_EMPLOYEE)}
                    WHERE 
                        CHR_EMPLOYEE_ADID=@CHR_EMPLOYEE_ADID AND
                        DTM_LEAVE_DATE IS NULL AND
                        CHR_NOTE IS NULL
                        ORDER BY ID DESC
                ";

            result = await connection.QueryFirstOrDefaultAsync<Model.Models.TM_EMPLOYEE>(
                sql,
                parameters
            );
        }
        return result;
    }

    public async Task<Model.Models.TM_EMPLOYEE> GetAgentEmployeeByEmployeeIdAsync(string employeeId)
    {
        Model.Models.TM_EMPLOYEE result = new Model.Models.TM_EMPLOYEE();
        using (var connection = new SqlConnection(_agentConnectionString))
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CHR_EMPLOYEE_ID", employeeId);

            var sql = $@"
                    SELECT TOP 1 
		                *
	                FROM AGENTDB.dbo.{nameof(Model.Models.TM_EMPLOYEE)}
                    WHERE 
                        CHR_EMPLOYEE_ID = @CHR_EMPLOYEE_ID AND
                        DTM_LEAVE_DATE IS NULL AND
                        CHR_NOTE IS NULL
                ";

            result = await connection.QueryFirstOrDefaultAsync<Model.Models.TM_EMPLOYEE>(
                sql,
                parameters
            );
        }
        return result;
    }

    // Lấy dữ liệu Employee từ db Working_Control
    public async Task<TM_EMPLOYEE_WORKING> GetWorkingEmployeeByStaffIdAsync(string staffId)
    {
        TM_EMPLOYEE_WORKING result = new TM_EMPLOYEE_WORKING();
        using (var connection = new SqlConnection(_workingControlConnectionString))
        {
            var parameters = new DynamicParameters();
            parameters.Add("@CHR_STAFF_ID", staffId);

            var sql = $@"
                    SELECT TOP 1 
		                *
	                FROM Working_Control.dbo.TM_EMPLOYEE_NEW
                    WHERE 
                        CHR_STAFF_ID = @CHR_STAFF_ID
                ";

            result = await connection.QueryFirstOrDefaultAsync<TM_EMPLOYEE_WORKING>(sql, parameters);
        }
        return result;
    }
}
