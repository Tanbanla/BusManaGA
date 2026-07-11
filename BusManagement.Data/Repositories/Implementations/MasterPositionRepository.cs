using BusManagement.Model.Models;
using BusManagement.Data.Repositories.Implementations;
using BusManagement.Data.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using BusManagement.Model.Common;

namespace BusManagement.Data.Repositories.Implementations;

public class MasterPositionRepository : BaseRepository<TM_MASTER_POSITION, int>, IMasterPositionRepository
{
    // Use Agent DB connection for this repository
    public MasterPositionRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration, useAgent: true)
    {
    }
}
