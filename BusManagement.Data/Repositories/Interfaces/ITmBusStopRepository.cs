using BusManagement.Model.Entity;

namespace BusManagement.Data.Repositories.Interfaces;

public interface ITmBusStopRepository : IBaseRepository<TM_BUS_STOP, int>
{
    // get bus stop by Active status
    Task<List<TM_BUS_STOP>> GetActiveBusStopsAsync();
}
