using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public interface ISession
    {
        IConfiguration Configuration { get; set; }
        LogLevel LogLevel { get; }
        void ConfigureSession(string token = null);
        void RestoreSession();

        /// <summary>
        /// Gets the configured token without contacting the portal or starting an interactive login.
        /// </summary>
        /// <returns>The token from the environment or the saved login.</returns>
        /// <exception cref="InvalidOperationException">No token is available.</exception>
        string GetToken();

        void LogInformation(string message);
        void LogError(string message);
        void LogError(Exception exception);
        void LogDebug(string message);
        void LogPendingMessages();
    }
}
