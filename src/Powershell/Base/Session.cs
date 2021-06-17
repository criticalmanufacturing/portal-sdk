﻿using Cmf.CustomerPortal.Sdk.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Threading;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class Session : CmfPortalSession
    {
        private readonly PSCmdlet _psCmdlet;
        private readonly string _mainThreadName;
        private readonly ConcurrentQueue<LogMessage> _logMessages;

        public Session(PSCmdlet powershellCmdlet, IServiceLocator serviceLocator)
        {
            _psCmdlet = powershellCmdlet;
            _mainThreadName = Thread.CurrentThread.Name;
            _logMessages = new ConcurrentQueue<LogMessage>();
            ServiceLocator = serviceLocator;
        }

        private bool IsRunningOnMainThread()
        {
            return Thread.CurrentThread.Name == _mainThreadName;
        }

        private void Log(LogMessage logMessage)
        {
            Log(logMessage.Message, logMessage.LogLevel);
        }

        private void Log(string message, LogLevel logLevel)
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

        public override void LogDebug(string message)
        {
            if (IsRunningOnMainThread())
            {
                _psCmdlet.WriteDebug(message);
            } else
            {
                _logMessages.Enqueue(new LogMessage(LogLevel.Debug, message));
            }
        }

        public override void LogError(string message)
        {
            if (IsRunningOnMainThread())
            {
                _psCmdlet.WriteError(new ErrorRecord(new Exception(message), "0001", ErrorCategory.NotSpecified, this));
            }
            else
            {
                _logMessages.Enqueue(new LogMessage(LogLevel.Error, message));
            }
        }

        public override void LogError(Exception exception)
        {
            if (IsRunningOnMainThread())
            {
                _psCmdlet.WriteError(new ErrorRecord(exception, "0001", ErrorCategory.NotSpecified, this));
            }
            else
            {
                _logMessages.Enqueue(new LogMessage(LogLevel.Error, exception.Message));
            }
        }

        public override void LogInformation(string message)
        {
            if (IsRunningOnMainThread())
            {
                _psCmdlet.WriteObject(message, false);
            }
            else
            {
                _logMessages.Enqueue(new LogMessage(LogLevel.Information, message));
            }
        }

        public override void LogPendingMessages()
        {
            if (IsRunningOnMainThread())
            {
                while (_logMessages.TryDequeue(out LogMessage logMessage))
                {
                    Log(logMessage);
                }
            }
        }
    }
}
