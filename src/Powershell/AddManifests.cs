using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.IO;
using System.Linq;
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
        public string Path { get; set; }

        [Parameter(
            HelpMessage = Resources.REPLACETOKENS_HELP
        )]
        public string ReplaceTokens { get; set; }

        protected async override Task ProcessRecordAsync()
        {
            AddManifestsHandler addManifestsHandler = ServiceLocator.Get<AddManifestsHandler>();
            string[] tokens = string.IsNullOrWhiteSpace(ReplaceTokens) ? null : ReplaceTokens.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            await addManifestsHandler.Run(new DirectoryInfo(Path), tokens);
        }
    }
}
