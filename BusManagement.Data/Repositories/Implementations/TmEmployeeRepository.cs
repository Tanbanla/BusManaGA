using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmEmployeeRepository : BaseRepository<Model.Entity.TM_EMPLOYEE, int>, ITmEmployeeRepository
{
    private readonly BusManagementDBContext _context;
    public TmEmployeeRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration) : base(context, options, configuration)
    {
        _context = context;
    }
    //Search Infor Empoloyee by code 
    public async Task<Model.Entity.TM_EMPLOYEE?> GetEmployeeByCodeAsync(string code)
    {
        var employee = await _context.TM_EMPLOYEEs
            .FirstOrDefaultAsync(e => e.CHR_EMPLOYEE_ID == code);

        return employee;
    }

}
