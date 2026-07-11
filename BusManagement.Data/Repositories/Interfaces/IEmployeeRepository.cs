using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;

namespace BusManagement.Data.Repositories.Interfaces;

public interface IEmployeeRepository : IBaseRepository<Model.Entity.TM_EMPLOYEE, int>
{
    Task<Model.Models.TM_EMPLOYEE> GetAgentEmployeeByEmployeeADIDAsync(string adId);
    Task<Model.Models.TM_EMPLOYEE> GetAgentEmployeeByEmployeeIdAsync(string employeeId);
    Task<TM_EMPLOYEE_WORKING> GetWorkingEmployeeByStaffIdAsync(string staffId);
}
