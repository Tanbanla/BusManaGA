using BusManagement.Model.Dtos;
using BusManagement.Model.Models;
using BusManagement.Service.Services.Implementations;
using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class MasterBivnHolidayService : BaseService<TM_MASTER_BIVN_HOLIDAY, decimal, TM_MASTER_BIVN_HOLIDAYDTO>, IMasterBivnHolidayService
{
    private readonly IMasterBivnHolidayRepository _repo;
    public MasterBivnHolidayService(IMasterBivnHolidayRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
}
