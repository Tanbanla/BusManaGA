using BusManagement.Model.Common;
using BusManagement.Model.Dtos;
using BusManagement.Model.Models;

namespace BusManagement.Service.Services.Interfaces;

public interface IAccountService
{
    Task<GenericResponse<bool>> ValidateCredentials(string adid, string password);

    // Login returns JWT access token
    Task<GenericResponse<string>> Login(string adid, string password);
}
