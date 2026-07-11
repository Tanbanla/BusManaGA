using AutoMapper;
using BusManagement.Model.Dtos;
using BusManagement.Model.Entity;
using BusManagement.Model.Models;

namespace BusManagement.Service.Configs.AutoMapper
{
    public class MappingProfile : Profile
    {
        public MappingProfile() 
        {
            // Employee
            CreateMap<Model.Models.TM_EMPLOYEE, TM_EMPLOYEE_AGENTDTO>().ReverseMap();

            // Center
            CreateMap<TM_CENTER, TM_CENTERDTO>().ReverseMap();

            // Master BIVN Holiday
            CreateMap<TM_MASTER_BIVN_HOLIDAY, TM_MASTER_BIVN_HOLIDAYDTO>().ReverseMap();

            // Master Position
            CreateMap<TM_MASTER_POSITION, TM_MASTER_POSITIONDTO>().ReverseMap();

            // Section
            CreateMap<TM_SECTION, TM_SECTIONDTO>().ReverseMap();

            // Bus Management entities
            CreateMap<TM_USER, TM_USERDTO>().ReverseMap();
            CreateMap<TM_MENU, TM_MENUDTO>().ReverseMap();
            CreateMap<TM_MENU_ROLE, TM_MENU_ROLEDTO>().ReverseMap();
            CreateMap<TM_ROLE, TM_ROLEDTO>().ReverseMap();
            CreateMap<Model.Entity.TM_EMPLOYEE, TM_EMPLOYEEDTO>().ReverseMap();
            CreateMap<TM_BUS_STOP, TM_BUS_STOPDTO>().ReverseMap();
            CreateMap<TM_BUS_ROUTE, TM_BUS_ROUTEDTO>().ReverseMap();
            CreateMap<TM_BUS_ROUTE_ASSIGN, TM_BUS_ROUTE_ASSIGNDTO>().ReverseMap();
            CreateMap<TM_BUS_ROUTE_STOP, TM_BUS_ROUTE_STOPDTO>().ReverseMap();
            CreateMap<TM_BUS, TM_BUSDTO>().ReverseMap();
            CreateMap<TM_TAXI, TM_TAXIDTO>().ReverseMap();
            CreateMap<TM_DRIVER, TM_DRIVERDTO>().ReverseMap();
            CreateMap<TM_VENDOR, TM_VENDORDTO>().ReverseMap();
            CreateMap<TM_REQUEST_TYPE, TM_REQUEST_TYPEDTO>().ReverseMap();
            CreateMap<TM_BUS_REQUEST, TM_BUS_REQUESTDTO>().ReverseMap();
            CreateMap<TM_APPROVAL_HISTORY, TM_APPROVAL_HISTORYDTO>().ReverseMap();
            CreateMap<TM_TRIP_REQUEST, TM_TRIP_REQUESTDTO>().ReverseMap();
            CreateMap<TM_BUS_OT, TM_BUS_OTDTO>().ReverseMap();
            CreateMap<TM_CARD, TM_CARDDTO>().ReverseMap();
            CreateMap<TM_CARD_USAGE, TM_CARD_USAGEDTO>().ReverseMap();
            CreateMap<TM_CARD_BORROW, TM_CARD_BORROWDTO>().ReverseMap();
            CreateMap<TM_CARD_PRINT_QUEUE, TM_CARD_PRINT_QUEUEDTO>().ReverseMap();
            CreateMap<TM_MASTER_MAIL, TM_MASTER_MAILDTO>().ReverseMap();
            CreateMap<TM_BUS_REQUEST_FAILED, TM_BUS_REQUEST_FAILEDDTO>().ReverseMap();
            CreateMap<TM_EMPLOYEE_ROUTE_HISTORY, TM_EMPLOYEE_ROUTE_HISTORYDTO>().ReverseMap();
            CreateMap<TM_EMPLOYEE_ROUTE, TM_EMPLOYEE_ROUTEDTO>().ReverseMap();
            CreateMap<TM_DRIVER_VIOLATION, TM_DRIVER_VIOLATIONDTO>().ReverseMap();
            CreateMap<TM_BUS_EVALUATION, TM_BUS_EVALUATIONDTO>().ReverseMap();
            CreateMap<TM_CITY, TM_CITYDTO>().ReverseMap();
            CreateMap<TM_WARD, TM_WARDDTO>().ReverseMap();


            // Agent TM_EMPLOYEE_AGENT -> local TM_EMPLOYEE mapping
            CreateMap<Model.Models.TM_EMPLOYEE, Model.Entity.TM_EMPLOYEE>()
                .ForMember(dest => dest.ID, opt => opt.Ignore())
                .ForMember(dest => dest.CHR_EMPLOYEE_ID, opt => opt.MapFrom(src => src.CHR_EMPLOYEE_ID))
                .ForMember(dest => dest.CHR_EMPLOYEE_ADID, opt => opt.MapFrom(src => src.CHR_EMPLOYEE_ADID))
                .ForMember(dest => dest.CHR_EMPLOYEE_MAIL, opt => opt.MapFrom(src => src.CHR_EMPLOYEE_MAIL))
                .ForMember(dest => dest.NVCHR_EMPLOYEE_NAME, opt => opt.MapFrom(src => src.CHR_EMPLOYEE_NAME))
                .ForMember(dest => dest.CHR_CODE_DEPT, opt => opt.MapFrom(src => src.CHR_DEPT_CODE ?? src.CHR_DEPT))
                .ForMember(dest => dest.CHR_CODE_SEC, opt => opt.MapFrom(src => src.CHR_SEC_CODE ?? src.CHR_SECTION))
                .ForMember(dest => dest.CHR_CODE_GROUP, opt => opt.MapFrom(src => src.CHR_DEPT_GROUP))
                .ForMember(dest => dest.CHR_POSITION, opt => opt.MapFrom(src => src.CHR_POSITION_NAME ?? src.CHR_POSITION))
                .ForMember(dest => dest.CHR_POSITION_GROUP, opt => opt.MapFrom(src => src.CHR_POSITION_GROUP))
                .ForMember(dest => dest.CHR_PHONE, opt => opt.MapFrom(src => src.CHR_PHONE_NO))
                .ForMember(dest => dest.DTM_BIRTHDAY, opt => opt.MapFrom(src => src.DTM_BIRTH_DATE))
                .ForMember(dest => dest.CHR_ADDRESS, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.CHR_TEMP_ADDRESS)
                        ? src.CHR_PERMANENT_ADDRESS
                        : new[] { "KTX", "Nhà riêng", "Ở trọ" }
                            .Contains(src.CHR_TEMP_ADDRESS.Trim(), StringComparer.OrdinalIgnoreCase)
                                ? src.CHR_PERMANENT_ADDRESS
                                : src.CHR_TEMP_ADDRESS
                ))
                .ForMember(dest => dest.NVCHR_CITY, opt => opt.MapFrom(src => src.CHR_PROVINCE))
                .ForMember(dest => dest.BIT_IS_ACTIVE, opt => opt.MapFrom(_ => true))
                .ForMember(dest => dest.BIT_GENDER, opt => opt.MapFrom(src => src.CHR_GENDER == "F" ? false : true))
                .ForMember(dest => dest.CHR_NOTE, opt => opt.MapFrom(src => src.CHR_NOTE));
        }
    }
}
