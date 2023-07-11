using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.New, "Infrastructure")]
    public class NewInfrastructure : BaseCmdlet<NewInfrastructureHandler>
    {
        [Parameter(HelpMessage = Resources.INFRASTRUCTURE_NAME_HELP)]
        public string Name { get; set; }

        [Parameter(HelpMessage = Resources.INFRASTRUCTURE_SITE_HELP)]
        public string SiteName { get; set; }

        [Parameter(HelpMessage = Resources.INFRASTRUCTURE_CUSTOMER_HELP)]
        public string CustomerName { get; set; }

        [Parameter(HelpMessage = Resources.INFRASTRUCTURE_PARAMETERSPATH_HELP)]
        public FileInfo ParametersPath { get; set; }

        [Parameter(HelpMessage = Resources.INFRASTRUCTURE_IGNORE_IF_EXISTS_HELP)]
        public SwitchParameter IgnoreIfExists;

        protected async override Task ProcessRecordAsync()
        {
            NewInfrastructureHandler handler = ServiceLocator.Get<NewInfrastructureHandler>();
            await handler.Run(Name, SiteName, CustomerName, IgnoreIfExists.ToBool(), ParametersPath);
        }
    }
}
