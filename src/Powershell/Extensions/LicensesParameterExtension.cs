using Cmf.CustomerPortal.Sdk.Common;
using System;
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
                Mandatory = true,
                HelpMessage = Resources.DEPLOYMENT_LICENSES_HELP
            };
        }

        public IEnumerable<RuntimeDefinedParameter> GetParameters()
        {
            List<RuntimeDefinedParameter> parameters = [];
            var param = new RuntimeDefinedParameter
            {
                IsSet = false,
                Name = "LicenseName",
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
            if (string.IsNullOrWhiteSpace(Licenses))
            {
                throw new ArgumentNullException("LicenseName");
            }

            var licenses = Licenses.Split([','], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim());

            if (!licenses.Any())
            {
                throw new ArgumentNullException("LicenseName");
            }

            return [.. licenses];
        }
    }
}
