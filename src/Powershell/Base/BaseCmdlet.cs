using Cmf.CustomerPortal.Sdk.Common;
using System;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class BaseCmdlet<T> : AsyncCmdlet where T : IHandler
    {
        protected IServiceLocator ServiceLocator
        {
            get; private set;
        }

        public BaseCmdlet(bool requiresLogin = true) {
            ServiceLocator = new ServiceLocator(this);

            if (requiresLogin)
            {
                // ensure we have a session
                ServiceLocator.Get<ISession>().RestoreSession();
            }
        }
    }
}
