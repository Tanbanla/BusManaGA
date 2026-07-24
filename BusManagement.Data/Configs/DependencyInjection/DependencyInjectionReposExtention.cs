using BusManagement.Data.Repositories.Implementations;
using BusManagement.Data.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BusManagement.Data.Configs.DependencyInjection
{
    public static class DependencyInjectionReposExtention
    {
        public static IServiceCollection AddDependencyInjectionRepos(this IServiceCollection services)
        {
            services.AddScoped<IEmployeeRepository, EmployeeRepository>();
            services.AddScoped<IEmployeeAgentRepository, EmployeeAgentRepository>();
            services.AddScoped<IMasterBivnHolidayRepository, MasterBivnHolidayRepository>();
            services.AddScoped<IMasterPositionRepository, MasterPositionRepository>();
            services.AddScoped<ISectionRepository, SectionRepository>();
            services.AddScoped<ICenterRepository, CenterRepository>();
            services.AddScoped<ITmUserRepository, TmUserRepository>();
            services.AddScoped<ITmMenuRepository, TmMenuRepository>();
            services.AddScoped<ITmMenuRoleRepository, TmMenuRoleRepository>();
            services.AddScoped<ITmRoleRepository, TmRoleRepository>();
            services.AddScoped<ITmEmployeeRepository, TmEmployeeRepository>();
            services.AddScoped<ITmBusStopRepository, TmBusStopRepository>();
            services.AddScoped<ITmBusRouteRepository, TmBusRouteRepository>();
            services.AddScoped<ITmBusRouteStopRepository, TmBusRouteStopRepository>();
            services.AddScoped<ITmBusRepository, TmBusRepository>();
            services.AddScoped<ITmTaxiRepository, TmTaxiRepository>();
            services.AddScoped<ITmDriverRepository, TmDriverRepository>();
            services.AddScoped<ITmVendorRepository, TmVendorRepository>();
            services.AddScoped<ITmRequestTypeRepository, TmRequestTypeRepository>();
            services.AddScoped<ITmBusRequestRepository, TmBusRequestRepository>();
            services.AddScoped<ITmApprovalHistoryRepository, TmApprovalHistoryRepository>();
            services.AddScoped<ITmTripRequestRepository, TmTripRequestRepository>();
            services.AddScoped<ITmBusOtRepository, TmBusOtRepository>();
            services.AddScoped<ITmBusOtAssignRepository, TmBusOtAssignRepository>();
            services.AddScoped<ITmCardRepository, TmCardRepository>();
            services.AddScoped<ITmCardUsageRepository, TmCardUsageRepository>();
            services.AddScoped<ITmCardBorrowRepository, TmCardBorrowRepository>();
            services.AddScoped<ITmCardPrintQueueRepository, TmCardPrintQueueRepository>();
            services.AddScoped<ITmMasterMailRepository, TmMasterMailRepository>();
            services.AddScoped<ITmBusRequestFailedRepository, TmBusRequestFailedRepository>();
            services.AddScoped<ITmEmployeeRouteHistoryRepository, TmEmployeeRouteHistoryRepository>();
            services.AddScoped<ITmEmployeeRouteOTRepository, TmEmployeeRouteOTRepository>();
            services.AddScoped<ITmEmployeeRouteRepository, TmEmployeeRouteRepository>();
            services.AddScoped<ITmDriverViolationRepository, TmDriverViolationRepository>();
            services.AddScoped<ITmBusEvaluationRepository, TmBusEvaluationRepository>();
            services.AddScoped<ITmBusRouteAssignRepository, TmBusRouteAssignRepository>();
            services.AddScoped<ITmCityRepository, TmCityRepository>();
            services.AddScoped<ITmWardRepository, TmWardRepository>();

            return services;
        }
    }
}
