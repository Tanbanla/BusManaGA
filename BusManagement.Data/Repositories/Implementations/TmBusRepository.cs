using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmBusRepository : BaseRepository<TM_BUS, int>, ITmBusRepository
{
    private readonly BusManagementDBContext _conn;
    public TmBusRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
        _conn = context;
    }    // get status Activition 
    public async Task<List<TM_BUS>> GetActiveBusesAsync()
    {
        return await _conn.TM_BUSs.Where(b => b.BIT_IS_ACTIVE == true)
            .OrderBy(c => c.CHR_BUS_CODE)
            .ToListAsync();
    }
}
