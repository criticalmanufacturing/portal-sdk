using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.Add, "Manifests")]
    public class AddManifests : BaseCmdlet<AddManifestsHandler>
    {
        [Parameter(
            HelpMessage = Resources.PUBLISHMANIFESTS_PATH_HELP,
            Mandatory = true
        )]
        public FileSystemInfo Path { get; set; }

        protected async override Task ProcessRecordAsync()
        {
            AddManifestsHandler addManifestsHandler = ServiceLocator.Get<AddManifestsHandler>();
            await addManifestsHandler.Run(Path, null);
        }
    }
}
