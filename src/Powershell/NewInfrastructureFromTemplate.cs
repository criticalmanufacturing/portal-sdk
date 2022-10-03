using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.New, "InfrastructureFromTemplate")]
    public class NewInfrastructureFromTemplate : BaseCmdlet<NewInfrastructureFromTemplateHandler>
    {
        [Parameter(
            HelpMessage = Resources.INFRASTRUCTUREFROMTEMPLATE_NAME_HELP
        )]
        public string Name { get; set; }

        [Parameter(
            HelpMessage = Resources.INFRASTRUCTUREFROMTEMPLATE_TEMPLATENAME_HELP,
            Mandatory = true
        )]
        public string TemplateName { get; set; }

        protected async override Task ProcessRecordAsync()
        {
            NewInfrastructureFromTemplateHandler newInfrastructureFromTemplateHandler = ServiceLocator.Get<NewInfrastructureFromTemplateHandler>();
            await newInfrastructureFromTemplateHandler.Run(Name, TemplateName);
        }
    }
}
