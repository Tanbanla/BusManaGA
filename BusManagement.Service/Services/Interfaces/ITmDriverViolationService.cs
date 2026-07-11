using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmDriverViolationService : IBaseService<TM_DRIVER_VIOLATION, int, TM_DRIVER_VIOLATIONDTO>
{
}
