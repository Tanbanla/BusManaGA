using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmBusOtAssignRepository : BaseRepository<TM_BUS_OT_ASSIGN, int>, ITmBusOtAssignRepository
{
    public TmBusOtAssignRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
    }
}
