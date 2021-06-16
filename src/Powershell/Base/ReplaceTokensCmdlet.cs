using Cmf.CustomerPortal.Sdk.Common;
using System.Linq;
using System.Management.Automation;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class ReplaceTokensCmdlet<T> : BaseCmdlet<T> where T : IHandler
    {
        [Parameter(
            HelpMessage = Resources.REPLACETOKENS_HELP
        )]
        public string ReplaceTokens { get; set; }

        public string[] GetTokens()
        {
            return string.IsNullOrWhiteSpace(ReplaceTokens) ? null : ReplaceTokens.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        }
    }
}
