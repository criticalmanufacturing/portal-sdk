using Cmf.CustomerPortal.Sdk.Common;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.LightBusinessObjects.Infrastructure.Security.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Management.Automation;
using System.Threading;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class Session : ISession
    {
        private const string _cmfPortalDirName = "cmfportal";
        private const string _loginTokenFileName = ".cmfportaltoken";
        private static readonly string _loginCredentialsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _cmfPortalDirName);
        private static readonly string _loginCredentialsFilePath = Path.Combine(_loginCredentialsDir, _loginTokenFileName);

        private readonly PSCmdlet _psCmdlet;
        private readonly IConfiguration _configuration;
        private readonly string _mainThreadName;
        private readonly ConcurrentQueue<LogMessage> _logMessages;

        public LogLevel LogLevel
        {
            get; private set;
        }

        private string AccessToken
        {
            get
            {
                // see if file exists
                if (File.Exists(_loginCredentialsFilePath))
                {
                    // try to deserialize
                    try
                    {
                        return File.ReadAllText(_loginCredentialsFilePath);
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                    }
                }

                return null;
            }

            set
            {
                // write to file and set as hidden
                Directory.CreateDirectory(_loginCredentialsDir);
                File.WriteAllText(_loginCredentialsFilePath, value);
                File.SetAttributes(_loginCredentialsFilePath, FileAttributes.Hidden);
            }
        }

        public Session(PSCmdlet powershellCmdlet, IConfiguration configuration)
        {
            _psCmdlet = powershellCmdlet;
            _mainThreadName = Thread.CurrentThread.Name;
            _logMessages = new ConcurrentQueue<LogMessage>();
            _configuration = configuration;
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

        private void ConfigureLBOs(string accessToken = null)
        {
            // Create the provider configuration function
            ClientConfigurationProvider.ConfigurationFactory = () =>
            {
                ClientConfiguration clientConfiguration = new ClientConfiguration()
                {
                    HostAddress = _configuration["ClientConfiguration:HostAddress"],
                    ClientTenantName = _configuration["ClientConfiguration:ClientTenantName"],
                    IsUsingLoadBalancer = bool.Parse(_configuration["ClientConfiguration:IsUsingLoadBalancer"]),
                    ClientId = _configuration["ClientConfiguration:ClientId"],
                    UseSsl = bool.Parse(_configuration["ClientConfiguration:UseSsl"]),
                    SecurityAccessToken = accessToken,
                    SecurityPortalBaseAddress = new Uri(_configuration["ClientConfiguration:SecurityPortalBaseAddress"])
                };

                if (accessToken == null)
                {
                    clientConfiguration.TokenProviderUpdated += (object sender, IAuthProvider authProvider) =>
                    {
                        // save access token in the session
                        AccessToken = authProvider.RefreshToken;
                    };
                }

                return clientConfiguration;
            };
        }

        public void ConfigureSession(string accessToken = null)
        {
            // make sure that empty/whitespace values are set as null
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                accessToken = null;
            }
            else
            {
                // if the user provided a token, cache it
                AccessToken = accessToken;
            }

            ConfigureLBOs(accessToken);
        }

        public void RestoreSession()
        {
            string accessToken = AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception("Session not found. Have you tried to log in?");
            }

            ConfigureLBOs(accessToken);
        }

        public void LogDebug(string message)
        {
            if (IsRunningOnMainThread())
            {
                _psCmdlet.WriteDebug(message);
            } else
            {
                _logMessages.Enqueue(new LogMessage(LogLevel.Debug, message));
            }
        }

        public void LogError(string message)
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

        public void LogError(Exception exception)
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

        public void LogInformation(string message)
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

        public void LogPendingMessages()
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
