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

        void LogInformation(string message);
        void LogError(string message);
        void LogError(Exception exception);
        void LogDebug(string message);
        void LogPendingMessages();

        string AccessToken { get; set; }
    }
}
