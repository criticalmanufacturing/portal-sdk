using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public static class CommonModule
    {
        public static void RegisterCommon(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ICustomerPortalClient, CustomerPortalClient>();

            // Add Handlers
            serviceCollection.AddTransient<Handlers.NewEnvironment>();
        }
    }
}
