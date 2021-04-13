using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public abstract class AbstractHandler : IHandler
    {
        protected ISession Session;

        public AbstractHandler(ISession session)
        {
            this.Session = session;
        }

        public virtual async Task Run()
        {
            if (Session == null)
            {
                throw new Exception("No session specified. Have you tried to create a session first?");
            }
        }
    }
}
