using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public abstract class AbstractHandler : IHandler
    {
        protected ISession Session;

        public AbstractHandler(ISession session)
        {
            Session = session;
        }

        public virtual Task Run()
        {
            throw new NotImplementedException("Method not implemented");
        }
    }
}
