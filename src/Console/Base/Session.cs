using Cmf.CustomerPortal.Sdk.Common;
using System;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class Session : CmfPortalSession
    {
        public Session(bool verbose)
        {
            if (verbose)
            {
                LogLevel = Microsoft.Extensions.Logging.LogLevel.Debug;
            }
        }

        public override void LogDebug(string message)
        {
            if (LogLevel <= Microsoft.Extensions.Logging.LogLevel.Debug)
            {
                System.Console.Out.WriteLine(message);
            }
        }

        public override void LogInformation(string message)
        {
            if (LogLevel <= Microsoft.Extensions.Logging.LogLevel.Information)
            {
                System.Console.Out.WriteLine(message);
            }
        }

        public override void LogError(string message)
        {
            if (LogLevel <= Microsoft.Extensions.Logging.LogLevel.Error)
            {
                System.Console.Error.WriteLine(message);
            }
        }

        public override void LogError(Exception exception)
        {
            if (LogLevel <= Microsoft.Extensions.Logging.LogLevel.Error)
            {
                System.Console.Error.WriteLine(exception.ToString());
            }
        }

        public override void LogPendingMessages()
        {
        }
    }
}
