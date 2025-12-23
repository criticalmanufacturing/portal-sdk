using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.IO.Abstractions;
using System.Reflection;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public class ServiceLocator : IServiceLocator
    {
        protected IServiceProvider ServiceProvider { get; }

        public ServiceLocator(ISession session)
        {
            IServiceCollection builder = new ServiceCollection();

            // register common services
            builder.RegisterCommon();

            builder.AddTransient<IFileSystem, FileSystem>();

            // register app configuration
            IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            builder.AddSingleton<IConfiguration>(configuration);
            session.Configuration = configuration;

            // register session service
            builder.AddSingleton<ISession>(session);

            // build service provider
            ServiceProvider = builder.BuildServiceProvider();
        }

        public TService Get<TService>() where TService : class
        {
            return ServiceProvider.GetRequiredService<TService>();
        }
    }
}
