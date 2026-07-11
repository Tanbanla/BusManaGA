using BusManagement.Models;

namespace BusManagement.Services.UserContext
{
    public interface ICurrentUserProvider
    {
        /// <summary>
        /// Returns cached CurrentUser for current scope, or null if not authenticated.
        /// </summary>
        CurrentUser? Current { get; }

        /// <summary>
        /// Force refresh from HTTP context/token.
        /// </summary>
        CurrentUser? Refresh();
    }
}
