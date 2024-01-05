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
        private const string _loginTokenFileName = "cmfportaltoken";
        private const string _tokenEnvVar = "CM_PORTAL_TOKEN";
        private static readonly string _loginCredentialsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), _cmfPortalDirName);
        private static readonly string _loginCredentialsFilePath = Path.Combine(_loginCredentialsDir, _loginTokenFileName);
        
        private string token = null;

        public LogLevel LogLevel { get; protected set; } = LogLevel.Information;

        public IConfiguration Configuration { get; set; }

        private string Token
        {
            get
            {
                if (token == null)
                {
                    // read token from:
                    //  1. Env var
                    //  2. File
                    
                    // see if env var exists
                    string value = Environment.GetEnvironmentVariable(_tokenEnvVar);
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        token = value;
                        LogDebug("Login Access Token restored from environment variable");
                    }
                    // see if file exists
                    else if (File.Exists(_loginCredentialsFilePath))
                    {
                        // try to deserialize
                        try
                        {
                            token = File.ReadAllText(_loginCredentialsFilePath);
                            LogDebug("Login Access Token restored from file");
                        }
                        catch (Exception ex)
                        {
                            LogError(ex);
                        }
                    }
                }

                return token;
            }

            set
            {
                // write to file
                Directory.CreateDirectory(_loginCredentialsDir);
                File.WriteAllText(_loginCredentialsFilePath, value);
                token = value;
                LogDebug("Login Access Token saved");
            }
        }

        /// <summary>
        /// Configures the LBO client with the configuration read from the appsettings file.
        /// Registers a callback to cache the token if one wasn't provided.
        /// </summary>
        /// <param name="token">Token to configure the LBOs client with.</param>
        private void ConfigureLBOs(string token)
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
                    SecurityAccessToken = token,
                    SecurityPortalBaseAddress = new Uri(Configuration["ClientConfiguration:SecurityPortalBaseAddress"])
                };

                clientConfiguration.TokenProviderUpdated += (object sender, IAuthProvider authProvider) =>
                {
                    // save access token in the session
                    if (token == null)
                    {
                        Token = authProvider.RefreshToken;
                    }
                };

                return clientConfiguration;
            };
        }

        /// <summary>
        /// Configures the Session by delegating <see cref="ConfigureLBOs(string)"/> and pre-caching the token if one was passed.
        /// </summary>
        /// <param name="token">CmfPortal Token to cache and be provided to the LBOs client configuration.</param>
        public void ConfigureSession(string token = null)
        {
            // make sure that empty/whitespace values are set as null
            if (!string.IsNullOrWhiteSpace(token))
            {
                Token = token;
            }

            ConfigureLBOs(token);
        }

        /// <summary>
        /// Restores a session by ensuring the CmfPortal Token is configured.
        /// This method searches the CmfPortal by two methods, in order from the most priority to the least:
        ///     1. Read from an environment variable "CM_PORTAL_TOKEN".
        ///     2. Read from a cached file in the host file system "<AppDataDir>/cmfportal/cmfportaltoken".
        /// </summary>
        /// <exception cref="Exception">If no CmfPortal Token was found.</exception>
        public void RestoreSession()
        {
            string token = Token;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new Exception("Session not found. Have you tried to log in?");
            }

            ConfigureLBOs(token);
        }

        public abstract void LogDebug(string message);
        public abstract void LogError(string message);
        public abstract void LogError(Exception exception);
        public abstract void LogInformation(string message);
        public abstract void LogPendingMessages();
    }
}
