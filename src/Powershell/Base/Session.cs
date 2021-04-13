using Cmf.CustomerPortal.Sdk.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class Session : ISession
    {
        private PSCmdlet PSCmdlet;

        public Session(PSCmdlet powershellCmdlet)
        {
            PSCmdlet = powershellCmdlet;
        }

        public LogLevel LogLevel
        {
            get; private set;
        }

        public void Log(string message, LogLevel logLevel)
        {
            switch(logLevel)
            {
                case LogLevel.Error:
                    LogError(new Exception(message));
                    break;
                case LogLevel.Debug:
                    LogDebug(message);
                    break;
                default:
                    LogInformation(message);
                    break;
            }
        }

        public void LogDebug(string message)
        {
            PSCmdlet.WriteDebug(message);
        }

        public void LogError(Exception exception)
        {
            PSCmdlet.WriteError(new ErrorRecord(exception, "0001", ErrorCategory.NotSpecified, this));
        }

        public void LogInformation(string message)
        {
            PSCmdlet.WriteInformation(message, null);
        }
    }
}
