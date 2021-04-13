using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public interface IServiceLocator
    {
        TService Get<TService>() where TService : class;
    }

}
