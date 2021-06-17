﻿using Microsoft.Extensions.DependencyInjection;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public static class CommonModule
    {
        public static void RegisterCommon(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ICustomerPortalClient, CustomerPortalClient>();

            // Add Handlers
            serviceCollection.AddTransient<Handlers.NewEnvironmentHandler>();
            serviceCollection.AddTransient<Handlers.LoginHandler>();
            serviceCollection.AddTransient<Handlers.NewInfrastructureHandler>();
            serviceCollection.AddTransient<Handlers.NewInfrastructureFromTemplateHandler>();
            serviceCollection.AddTransient<Handlers.GetAgentConnectionHandler>();
            serviceCollection.AddTransient<Handlers.AddManifestsHandler>();
        }
    }
}
