using Cmf.CustomerPortal.Sdk.Common;
using System.Linq;
using System.Management.Automation;

namespace Cmf.CustomerPortal.Sdk.Powershell.Extensions
{
    internal class ReplaceTokensParameterExtension : IParameterExtension
    {
        private readonly ParameterAttribute ReplaceTokensParamAttr;
        private string ReplaceTokens;

        public ReplaceTokensParameterExtension()
        {
            ReplaceTokensParamAttr = new ParameterAttribute
            {
                Mandatory = false,
                HelpMessage = Resources.REPLACETOKENS_HELP
            };
        }

        public RuntimeDefinedParameter GetParameter()
        {
            var param = new RuntimeDefinedParameter
            {
                IsSet = false,
                Name = "ReplaceTokens",
                ParameterType = typeof(string)
            };
            param.Attributes.Add(ReplaceTokensParamAttr);

            return param;
        }

        public void ReadFromPipeline(object value)
        {
            ReplaceTokens = value as string;
        }

        public string[] GetTokens()
        {
            return string.IsNullOrWhiteSpace(ReplaceTokens) ? null : ReplaceTokens.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        }
    }
}
