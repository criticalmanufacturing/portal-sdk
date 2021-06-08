using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public class ServiceLocator : IServiceLocator
    {
        protected IServiceProvider ServiceProvider { get; }

        public ServiceLocator(ISession session, string environment = null)
        {
            IServiceCollection builder = new ServiceCollection();

            // register common services
            builder.RegisterCommon();

            // register app configuration
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            if (!string.IsNullOrWhiteSpace(environment))
            {
                configurationBuilder.AddJsonFile($"appsettings.{environment}.json", optional: false, reloadOnChange: true);
            }
            IConfigurationRoot configuration = configurationBuilder.Build();
            builder.AddSingleton<IConfiguration>(configuration);

            // register session service
            builder.AddSingleton<ISession>(session);

            // build service provider
            ServiceProvider = builder.BuildServiceProvider();
        }

        public TService Get<TService>() where TService : class
        {
            return ServiceProvider.GetService<TService>();
        }
    }
}
