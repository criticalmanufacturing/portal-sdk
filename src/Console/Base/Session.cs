using Cmf.CustomerPortal.Sdk.Common;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.LightBusinessObjects.Infrastructure.Security.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Configuration;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    class Session : ISession
    {
        private const string _cmfPortalDirName = "cmfportal";
        private const string _loginTokenFileName = ".cmfportaltoken";
        private static readonly string _loginCredentialsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _cmfPortalDirName);
        private static readonly string _loginCredentialsFilePath = Path.Combine(_loginCredentialsDir, _loginTokenFileName);

        public Session(bool verbose)
        {
            LogLevel = LogLevel.Debug;
        }

        public LogLevel LogLevel
        {
            get; private set;
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

        private void ConfigureLBOs(string accessToken = null)
        {
            // Create the provider configuration function
            ClientConfigurationProvider.ConfigurationFactory = () =>
            {
                ClientConfiguration clientConfiguration = new ClientConfiguration()
                {
                    HostAddress = ConfigurationManager.AppSettings["ClientConfiguration:HostAddress"],
                    ClientTenantName = ConfigurationManager.AppSettings["ClientConfiguration:ClientTenantName"],
                    IsUsingLoadBalancer = bool.Parse(ConfigurationManager.AppSettings["ClientConfiguration:IsUsingLoadBalancer"]),
                    ClientId = ConfigurationManager.AppSettings["ClientConfiguration:ClientId"],
                    UseSsl = bool.Parse(ConfigurationManager.AppSettings["ClientConfiguration:UseSsl"]),
                    SecurityAccessToken = accessToken,
                    SecurityPortalBaseAddress = new Uri(ConfigurationManager.AppSettings["ClientConfiguration:SecurityPortalBaseAddress"])
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

        public void LogDebug(string message)
        {
            if (LogLevel <= LogLevel.Debug)
            {
                System.Console.Out.WriteLine(message);
            }
        }

        public void LogError(string message)
        {
            if (LogLevel <= LogLevel.Error)
            {
                System.Console.Error.WriteLine(message);
            }
        }

        public void LogError(Exception exception)
        {
            if (LogLevel <= LogLevel.Error)
            {
                System.Console.Error.WriteLine(exception.ToString());
            }
        }

        public void LogInformation(string message)
        {
            if (LogLevel <= LogLevel.Information)
            {
                System.Console.Out.WriteLine(message);
            }
        }

        public void LogPendingMessages()
        {
        }
    }
}
