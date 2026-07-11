using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmEmployeeRouteHistoryRepository : BaseRepository<TM_EMPLOYEE_ROUTE_HISTORY, int>, ITmEmployeeRouteHistoryRepository
{
    public TmEmployeeRouteHistoryRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
    }
}
