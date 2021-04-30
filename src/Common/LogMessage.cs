using Microsoft.Extensions.Logging;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public struct LogMessage
    {
        public LogLevel LogLevel { get; }
        public string Message { get; }

        public LogMessage(LogLevel logLevel, string message)
        {
            LogLevel = logLevel;
            Message = message;
        }
    }
}
