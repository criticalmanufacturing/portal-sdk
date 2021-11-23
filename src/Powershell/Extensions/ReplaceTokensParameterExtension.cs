using Cmf.CustomerPortal.Sdk.Common;
using System.Collections.Generic;
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

        public IEnumerable<RuntimeDefinedParameter> GetParameters()
        {
            List<RuntimeDefinedParameter> parameters = new List<RuntimeDefinedParameter>();
            var param = new RuntimeDefinedParameter
            {
                IsSet = false,
                Name = "ReplaceTokens",
                ParameterType = typeof(string)
            };
            param.Attributes.Add(ReplaceTokensParamAttr);
            parameters.Add(param);
            return parameters;
        }

        public void ReadFromPipeline(RuntimeDefinedParameter parameter)
        {
            ReplaceTokens = parameter.Value as string;
        }

        public string[] GetTokens()
        {
            return string.IsNullOrWhiteSpace(ReplaceTokens) ? null : ReplaceTokens.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        }
    }
}
