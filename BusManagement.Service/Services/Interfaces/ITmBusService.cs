using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmBusService : IBaseService<TM_BUS, int, TM_BUSDTO>
{
    // get status Activition 
    Task<GenericResponse<List<TM_BUS>>> GetActiveBusesAsync();
}
