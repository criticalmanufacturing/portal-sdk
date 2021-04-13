using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public interface ISession
    {
        LogLevel LogLevel
        {
            get;
        }

        void Log(string message, LogLevel logLevel);
        void LogInformation(string message);
        void LogError(Exception exception);
        void LogDebug(string message);
    }
}
