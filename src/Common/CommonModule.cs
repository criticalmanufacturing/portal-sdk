using Cmf.CustomerPortal.Sdk.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public static class CommonModule
    {
        public static void RegisterCommon(this IServiceCollection serviceCollection)
        {
            // Add Services
            serviceCollection.AddSingleton<ICustomerPortalClient, CustomerPortalClient>();
            serviceCollection.AddSingleton<INewEnvironmentUtilities, NewEnvironmentUtilities>();
            serviceCollection.AddTransient<IEnvironmentDeploymentHandler, EnvironmentDeploymentHandler>();
            serviceCollection.AddTransient<IAppInstallationHandler, AppInstallationHandler>();
            serviceCollection.AddTransient<IArtifactDownloaderHandler, ArtifactDownloaderHandler>();

            // Add Handlers
            serviceCollection.AddTransient<Handlers.NewEnvironmentHandler>();
            serviceCollection.AddTransient<Handlers.LoginHandler>();
            serviceCollection.AddTransient<Handlers.NewInfrastructureHandler>();
            serviceCollection.AddTransient<Handlers.GetAgentConnectionHandler>();
            serviceCollection.AddTransient<Handlers.AddManifestsHandler>();
            serviceCollection.AddTransient<Handlers.PublishPackageHandler>();
            serviceCollection.AddTransient<Handlers.InstallAppHandler>();
            serviceCollection.AddTransient<Handlers.DownloaderDeployArtifactHandler>();
        }
    }
}
