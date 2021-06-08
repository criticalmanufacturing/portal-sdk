using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.LightBusinessObjects.Infrastructure.Security.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public abstract class CmfPortalSession : ISession
    {
        private const string _cmfPortalDirName = "cmfportal";
        private const string _loginTokenFileName = ".cmfportaltoken";
        private static readonly string _loginCredentialsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _cmfPortalDirName);
        private static readonly string _loginCredentialsFilePath = Path.Combine(_loginCredentialsDir, _loginTokenFileName);
        private string accessToken = null;

        public LogLevel LogLevel { get; protected set; } = LogLevel.Information;

        protected string AccessToken
        {
            get
            {
                if (accessToken == null)
                {
                    // see if file exists
                    if (File.Exists(_loginCredentialsFilePath))
                    {
                        // try to deserialize
                        try
                        {
                            accessToken = File.ReadAllText(_loginCredentialsFilePath);
                            LogDebug("Login Access Token restored from file");
                        }
                        catch (Exception ex)
                        {
                            LogError(ex);
                        }
                    }
                }
                return accessToken;
            }

            set
            {
                // write to file and set as hidden
                Directory.CreateDirectory(_loginCredentialsDir);
                File.WriteAllText(_loginCredentialsFilePath, value);
                File.SetAttributes(_loginCredentialsFilePath, FileAttributes.Hidden);
                accessToken = value;
                LogDebug("Login Access Token saved");
            }
        }

        public IConfiguration Configuration { get; set; }

        public void ConfigureSession(string accessToken = null)
        {
            // make sure that empty/whitespace values are set as null
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                // if the user provided a token, cache it
                AccessToken = accessToken;
            }

            ConfigureLBOs();
        }

        public void RestoreSession()
        {
            string accessToken = AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new Exception("Session not found. Have you tried to log in?");
            }

            ConfigureLBOs();
        }

        protected void ConfigureLBOs()
        {
            // Create the provider configuration function
            ClientConfigurationProvider.ConfigurationFactory = () =>
            {
                ClientConfiguration clientConfiguration = new ClientConfiguration()
                {
                    HostAddress = Configuration["ClientConfiguration:HostAddress"],
                    ClientTenantName = Configuration["ClientConfiguration:ClientTenantName"],
                    IsUsingLoadBalancer = bool.Parse(Configuration["ClientConfiguration:IsUsingLoadBalancer"]),
                    ClientId = Configuration["ClientConfiguration:ClientId"],
                    UseSsl = bool.Parse(Configuration["ClientConfiguration:UseSsl"]),
                    SecurityAccessToken = AccessToken,
                    SecurityPortalBaseAddress = new Uri(Configuration["ClientConfiguration:SecurityPortalBaseAddress"])
                };

                if (AccessToken == null)
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

        public abstract void LogDebug(string message);
        public abstract void LogError(string message);
        public abstract void LogError(Exception exception);
        public abstract void LogInformation(string message);
        public abstract void LogPendingMessages();
    }
}
