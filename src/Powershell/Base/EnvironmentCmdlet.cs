using Cmf.CustomerPortal.Sdk.Common;
using Microsoft.Extensions.Configuration;
using System.Management.Automation;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class EnvironmentCmdlet<T> : BaseCmdlet<T> where T : IHandler
    {
        [Parameter(
            HelpMessage = Resources.DESTINATIONENVIRONMENT_HELP
        )]
        public string Destination { get; set; }

        public EnvironmentCmdlet()
        {
            Session session = new Session(this);
            ServiceLocator = new ServiceLocator(session, Destination);
            session.Configuration = ServiceLocator.Get<IConfiguration>();
        }
    }
}
