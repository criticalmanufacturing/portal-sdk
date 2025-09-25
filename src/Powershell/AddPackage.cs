using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.Add, "Package")]
    public class AddPackage : BaseCmdlet<PublishPackageHandler>
    {
        [Parameter(
            HelpMessage = Resources.PUBLISHPACKAGE_PATH_HELP,
            Mandatory = true
        )]
        public string Path { get; set; }

        [Parameter(
            HelpMessage = Resources.PUBLISHPACKAGE_DATAGROUP_HELP,
            Mandatory = false
        )]
        public string Datagroup { get; set; }

        protected async override Task ProcessRecordAsync()
        {
            PublishPackageHandler publishPackageHandler = ServiceLocator.Get<PublishPackageHandler>();
            await publishPackageHandler.Run(Path, Datagroup);
        }
    }
}
