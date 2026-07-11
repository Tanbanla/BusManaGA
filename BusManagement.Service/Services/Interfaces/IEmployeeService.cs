using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;

namespace BusManagement.Service.Services.Interfaces;

public interface IEmployeeService : IBaseService<Model.Entity.TM_EMPLOYEE, int, TM_EMPLOYEEDTO>
{
    Task<Model.Models.TM_EMPLOYEE?> GetAgentEmployeeByAdidAsync(string adid);
    Task<Model.Models.TM_EMPLOYEE?> GetAgentEmployeeByEmployeeIdAsync(string employeeId);
    Task<Model.Entity.TM_EMPLOYEE> EnsureLocalEmployeeFromAgentAsync(Model.Models.TM_EMPLOYEE agentEmployee);
    Task<Model.Entity.TM_EMPLOYEE?> GetByEmployeeCodeAsync(string employeeCode);
}