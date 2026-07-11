using BusManagement.Data.Repositories.Interfaces;
using BusManagement.Model.Common;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BusManagement.Data.Repositories.Implementations;

public class TmMenuRoleRepository : BaseRepository<TM_MENU_ROLE, int>, ITmMenuRoleRepository
{
    public TmMenuRoleRepository(BusManagementDBContext context, IOptions<ConnectionStringOptions> options, IConfiguration configuration)
        : base(context, options, configuration)
    {
    }
}
