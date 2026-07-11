using Microsoft.AspNetCore.Http;
using BusManagement.Helpers;
using BusManagement.Models;

namespace BusManagement.Services.UserContext
{
    public class CurrentUserProvider : ICurrentUserProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private CurrentUser? _cached;

        public CurrentUserProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public CurrentUser? Current
        {
            get
            {
                if (_cached != null) return _cached;
                _cached = AccessTokenHelper.FromHttpContext(_httpContextAccessor.HttpContext!);
                return _cached;
            }
        }

        public CurrentUser? Refresh()
        {
            _cached = AccessTokenHelper.FromHttpContext(_httpContextAccessor.HttpContext!);
            return _cached;
        }
    }
}
