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
        [Parameter(HelpMessage = Resources.InfrastructureNameHelp)]
        public string Name { get; set; }

        [Parameter(HelpMessage = Resources.InfrastructureSiteHelp)]
        public string SiteName { get; set; }

        [Parameter(HelpMessage = Resources.InfrastructureCustomerHelp)]
        public string CustomerName { get; set; }

        [Parameter(HelpMessage = Resources.InfrastructureParametersPathHelp)]
        public FileInfo ParametersPath { get; set; }

        [Parameter(HelpMessage = Resources.InfrastructureIgnoreIfExistsHelp)]
        public SwitchParameter IgnoreIfExists;

        protected async override Task ProcessRecordAsync()
        {
            NewInfrastructureHandler handler = ServiceLocator.Get<NewInfrastructureHandler>();
            await handler.Run(Name, SiteName, CustomerName, IgnoreIfExists.ToBool(), ParametersPath);
        }
    }
}
