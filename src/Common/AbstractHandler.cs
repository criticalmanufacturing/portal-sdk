using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public abstract class AbstractHandler : IHandler
    {
        protected ISession Session;
        protected readonly bool RequiresLogin;

        public AbstractHandler(ISession session, bool requiresLogin)
        {
            Session = session;
            RequiresLogin = requiresLogin;
        }

        public virtual Task Run()
        {
            throw new NotImplementedException("Method not implemented");
        }

        protected async Task LoginIfRequired()
        {
            if (this.RequiresLogin)
            {
                // ensure we have a session
                await Task.Run(() => this.Session.RestoreSession());
            }
        }
    }
}
