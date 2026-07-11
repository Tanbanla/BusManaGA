using BusManagement.Service.Services.Implementations;
using BusManagement.Service.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BusManagement.Service.Configs.DependencyInjection
{
    public static class DependencyInjectionServicesExtention
    {
        public static IServiceCollection AddDependencyInjectionServices(this IServiceCollection services)
        {
            services.AddScoped<IEmployeeAgentService, EmployeeAgentService>();
            services.AddScoped<IEmployeeService, EmployeeService>();
            services.AddScoped<IAccountService, AccountService>();
            services.AddScoped<IMasterBivnHolidayService, MasterBivnHolidayService>();
            services.AddScoped<IMasterPositionService, MasterPositionService>();
            services.AddScoped<ISectionService, SectionService>();
            services.AddScoped<ITmUserService, TmUserService>();
            services.AddScoped<ITmMenuService, TmMenuService>();
            services.AddScoped<ITmMenuRoleService, TmMenuRoleService>();
            services.AddScoped<ITmRoleService, TmRoleService>();
            services.AddScoped<ITmEmployeeService, TmEmployeeService>();
            services.AddScoped<ITmBusStopService, TmBusStopService>();
            services.AddScoped<ITmBusRouteService, TmBusRouteService>();
            services.AddScoped<ITmBusRouteStopService, TmBusRouteStopService>();
            services.AddScoped<ITmBusService, TmBusService>();
            services.AddScoped<ITmTaxiService, TmTaxiService>();
            services.AddScoped<ITmDriverService, TmDriverService>();
            services.AddScoped<ITmVendorService, TmVendorService>();
            services.AddScoped<ITmRequestTypeService, TmRequestTypeService>();
            services.AddScoped<ITmBusRequestService, TmBusRequestService>();
            services.AddScoped<ITmApprovalHistoryService, TmApprovalHistoryService>();
            services.AddScoped<ITmTripRequestService, TmTripRequestService>();
            services.AddScoped<ITmBusOtService, TmBusOtService>();
            services.AddScoped<ITmCardService, TmCardService>();
            services.AddScoped<ITmCardUsageService, TmCardUsageService>();
            services.AddScoped<ITmCardBorrowService, TmCardBorrowService>();
            services.AddScoped<ITmCardPrintQueueService, TmCardPrintQueueService>();
            services.AddScoped<ITmMasterMailService, TmMasterMailService>();
            services.AddScoped<ITmBusRequestFailedService, TmBusRequestFailedService>();
            services.AddScoped<ITmEmployeeRouteHistoryService, TmEmployeeRouteHistoryService>();
            services.AddScoped<ITmEmployeeRouteService, TmEmployeeRouteService>();
            services.AddScoped<ITmDriverViolationService, TmDriverViolationService>();
            services.AddScoped<ITmBusEvaluationService, TmBusEvaluationService>();
            services.AddScoped<ITmBusRouteAssignService, TmBusRouteAssignService>();
            services.AddScoped<ITmCityService, TmCityService>();
            services.AddScoped<ITmWardService, TmWardService>();
            services.AddScoped<IActiveDirectoryService, ActiveDirectoryService>();

            return services;
        }
    }
}
