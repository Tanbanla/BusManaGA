using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;

namespace BusManagement.Service.Services.Interfaces;

public interface ITmEmployeeService : IBaseService<TM_EMPLOYEE, int, TM_EMPLOYEEDTO>
{

    //Search Infor Empoloyee by code 
    Task<GenericResponse<TM_EMPLOYEEDTO?>> GetEmployeeByCodeAsync(string code);
}
