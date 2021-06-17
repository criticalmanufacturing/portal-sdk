using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.Set, "Login")]
    public class SetLogin : BaseCmdlet<LoginHandler>
    {
        [Parameter(
            HelpMessage = Resources.LOGIN_PAT_HELP
        )]
        public string PAT
        {
            get; set;
        }

        protected override async Task ProcessRecordAsync()
        {
            // use login handler to save login information
            LoginHandler loginHandler = ServiceLocator.Get<LoginHandler>();
            await loginHandler.Run(PAT);
        }
    }
}
