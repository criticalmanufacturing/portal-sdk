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

            // Add Handlers
            serviceCollection.AddTransient<Handlers.NewEnvironmentHandler>();
            serviceCollection.AddTransient<Handlers.NewEnvironmentForInfrastructureHandler>();
            serviceCollection.AddTransient<Handlers.LoginHandler>();
            serviceCollection.AddTransient<Handlers.NewInfrastructureHandler>();
            serviceCollection.AddTransient<Handlers.NewInfrastructureFromTemplateHandler>();
            serviceCollection.AddTransient<Handlers.GetAgentConnectionHandler>();
            serviceCollection.AddTransient<Handlers.AddManifestsHandler>();
            serviceCollection.AddTransient<Handlers.PublishPackageHandler>();
        }
    }
}
