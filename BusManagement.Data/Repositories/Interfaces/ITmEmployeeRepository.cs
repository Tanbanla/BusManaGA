using BusManagement.Model.Entity;

namespace BusManagement.Data.Repositories.Interfaces;

public interface ITmEmployeeRepository : IBaseRepository<TM_EMPLOYEE, int>
{
    //Search Infor Empoloyee by code 
    Task<TM_EMPLOYEE?> GetEmployeeByCodeAsync(string code);
}
