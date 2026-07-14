using AutoMapper;
using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Service.Services.Interfaces;

namespace BusManagement.Service.Services.Implementations;

public class TmEmployeeService : BaseService<TM_EMPLOYEE, int, TM_EMPLOYEEDTO>, ITmEmployeeService
{
    private readonly ITmEmployeeRepository _repo;

    public TmEmployeeService(ITmEmployeeRepository repository, IMapper mapper) : base(repository, mapper)
    {
        _repo = repository;
    }
    //Search Infor Empoloyee by code 
    public async Task<GenericResponse<TM_EMPLOYEEDTO?>> GetEmployeeByCodeAsync(string code)
    {
        var result = new GenericResponse<TM_EMPLOYEEDTO?>();

        try
        {
            var data = await _repo.GetEmployeeByCodeAsync(code);
            result.Data = _mapper.Map<TM_EMPLOYEEDTO>(data);
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }
}
