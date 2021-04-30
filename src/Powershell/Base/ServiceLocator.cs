using Microsoft.Extensions.DependencyInjection;
using Cmf.CustomerPortal.Sdk.Common;
using System;
using System.Management.Automation;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class ServiceLocator : IServiceLocator
    {
        protected IServiceProvider ServiceProvider { get; }

        public ServiceLocator(PSCmdlet pscmdlet)
        {
            IServiceCollection builder = new ServiceCollection();
            
            // register common services
            builder.RegisterCommon();

            // register app configuration
            IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsettings.{Environment}.json", optional: true, reloadOnChange: true)
                    .Build();
            builder.AddSingleton<IConfiguration>(configuration);

            // register session service
            builder.AddSingleton<ISession>(new Session(pscmdlet, configuration));

            // build service provider
            ServiceProvider = builder.BuildServiceProvider();
        }

        public TService Get<TService>() where TService : class
        {
            return ServiceProvider.GetService<TService>();
        }
    }

}
