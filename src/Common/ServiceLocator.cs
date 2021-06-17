﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public class ServiceLocator : IServiceLocator
    {
        protected IServiceProvider ServiceProvider { get; }

        public ServiceLocator()
        {
            IServiceCollection builder = new ServiceCollection();

            // register common services
            builder.RegisterCommon();

            // register app configuration
            IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
            builder.AddSingleton<IConfiguration>(configuration);

            // build service provider
            ServiceProvider = builder.BuildServiceProvider();
        }

        public TService Get<TService>() where TService : class
        {
            return ServiceProvider.GetService<TService>();
        }
    }
}
