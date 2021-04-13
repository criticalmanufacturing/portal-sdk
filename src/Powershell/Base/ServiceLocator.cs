using Microsoft.Extensions.DependencyInjection;
using Cmf.CustomerPortal.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class ServiceLocator : IServiceLocator
    {
        public ServiceLocator(PSCmdlet pscmdlet)
        {
            IServiceCollection builder = new ServiceCollection();
            builder.RegisterCommon();
            builder.AddSingleton<ISession>(new Session(pscmdlet));

            ServiceProvider = builder.BuildServiceProvider();
        }

        protected IServiceProvider ServiceProvider { get; }

        public TService Get<TService>() where TService : class
        {
            return ServiceProvider.GetService<TService>();
        }
    }

}
