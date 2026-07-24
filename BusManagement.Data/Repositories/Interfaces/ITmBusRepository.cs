using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Data.Repositories.Interfaces;

public interface ITmBusRepository : IBaseRepository<TM_BUS, int>
{
    // get status Activition 
    Task<List<TM_BUS>> GetActiveBusesAsync();
}
