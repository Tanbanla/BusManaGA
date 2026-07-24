using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmBusStopRepository : BaseRepository<TM_BUS_STOP, int>, ITmBusStopRepository
{
    private readonly BusManagementDBContext _conn;
    public TmBusStopRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
        _conn = context;
    }
    // get bus stop by Active status
    public async Task<List<TM_BUS_STOP>> GetActiveBusStopsAsync()
    {
        return await _conn.TM_BUS_STOPs.Where(x => x.BIT_IS_ACTIVE == true)
            .OrderBy(c => c.NVCHR_STOP_NAME)
            .ToListAsync();
    }
}
