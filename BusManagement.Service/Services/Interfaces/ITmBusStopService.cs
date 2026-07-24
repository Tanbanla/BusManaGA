using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmBusStopService : IBaseService<TM_BUS_STOP, int, TM_BUS_STOPDTO>
{
    // get bus stop by Active status
    Task<GenericResponse<List<TM_BUS_STOP>>> GetActiveBusStopsAsync();
}
