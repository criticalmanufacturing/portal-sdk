using Cmf.CustomerPortal.Sdk.Common;
using Microsoft.Extensions.Configuration;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class BaseCmdlet<T> : AsyncCmdlet where T : IHandler
    {
        protected IServiceLocator ServiceLocator
        {
            get; set;
        }

        public BaseCmdlet()
        {
            Session session = new Session(this);
            ServiceLocator = new ServiceLocator(session);
            session.Configuration = ServiceLocator.Get<IConfiguration>();
        }
    }
}
