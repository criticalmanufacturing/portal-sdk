using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public interface ISession
    {
        IConfiguration Configuration { get; set; }
        LogLevel LogLevel { get; }
        string AccessToken { get; }

        void ConfigureSession(string accessToken = null);
        void RestoreSession();

        void LogInformation(string message);
        void LogError(string message);
        void LogError(Exception exception);
        void LogDebug(string message);
        void LogPendingMessages();
    }
}
