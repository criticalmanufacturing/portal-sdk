using Cmf.CustomerPortal.Sdk.Common;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.LightBusinessObjects.Infrastructure.Security.Providers;
using System;
using System.Configuration;

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

        protected override void ConfigureLBOs()
        {
            // Create the provider configuration function
            ClientConfigurationProvider.ConfigurationFactory = () =>
            {
                ClientConfiguration clientConfiguration = new ClientConfiguration()
                {
                    HostAddress = ConfigurationManager.AppSettings["HostAddress"],
                    ClientTenantName = ConfigurationManager.AppSettings["ClientTenantName"],
                    IsUsingLoadBalancer = bool.Parse(ConfigurationManager.AppSettings["IsUsingLoadBalancer"]),
                    ClientId = ConfigurationManager.AppSettings["ClientId"],
                    UseSsl = bool.Parse(ConfigurationManager.AppSettings["UseSsl"]),
                    SecurityAccessToken = AccessToken,
                    SecurityPortalBaseAddress = new Uri(ConfigurationManager.AppSettings["SecurityPortalBaseAddress"])
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
    }
}
