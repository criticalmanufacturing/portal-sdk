using Cmf.CustomerPortal.Sdk.Common;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class BaseCmdlet<T> : AsyncCmdlet where T : IHandler
    {
        protected IServiceLocator ServiceLocator
        {
            get; private set;
        }

        public BaseCmdlet()
        {
            Session session = new Session(this);
            ServiceLocator = new ServiceLocator(session);
        }
    }
}
