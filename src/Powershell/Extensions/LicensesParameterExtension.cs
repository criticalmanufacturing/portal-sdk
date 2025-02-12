using Cmf.CustomerPortal.Sdk.Common;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Cmf.CustomerPortal.Sdk.Powershell.Extensions
{
    internal class LicensesParameterExtension : IParameterExtension
    {
        private readonly ParameterAttribute LicensesParamAttr;
        private string Licenses;

        public LicensesParameterExtension()
        {
            LicensesParamAttr = new ParameterAttribute
            {
                // Mandatory = false, // TODO: Enable when "LicenseName" is obsoleted
                HelpMessage = Resources.DEPLOYMENT_LICENSES_HELP
            };
        }

        public IEnumerable<RuntimeDefinedParameter> GetParameters()
        {
            List<RuntimeDefinedParameter> parameters = [];
            var param = new RuntimeDefinedParameter
            {
                IsSet = false,
                Name = "Licenses",
                ParameterType = typeof(string)
            };
            param.Attributes.Add(LicensesParamAttr);
            parameters.Add(param);
            return parameters;
        }

        public void ReadFromPipeline(RuntimeDefinedParameter parameter)
        {
            Licenses = parameter.Value as string;
        }

        public string[] GetTokens()
        {
            return string.IsNullOrWhiteSpace(Licenses) ? null : [.. Licenses.Split([','], System.StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim())];
        }
    }
}
