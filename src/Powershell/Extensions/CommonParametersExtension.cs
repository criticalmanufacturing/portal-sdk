using Cmf.CustomerPortal.Sdk.Common;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Powershell.Extensions
{
    class CommonParametersExtension : IParameterExtension
    {
        public readonly List<RuntimeDefinedParameter> parameters;
        public Dictionary<string, object> parametersValue;
        public CommonParametersExtension()
        {
            parameters = new List<RuntimeDefinedParameter>();
            parametersValue = new Dictionary<string, object>();
            CreateRuntimeParameter("CustomerInfrastructureAgent", Resources.INFRASTRUCTURE_EXISTING_NAME_HELP, typeof(string));
            CreateRuntimeParameter("Name", Resources.DEPLOYMENT_NAME_HELP, typeof(string));
            CreateRuntimeParameter("Description", Resources.DEPLOYMENT_DESCRIPTION_HELP, typeof(string));
            CreateRuntimeParameter("ParametersPath", Resources.DEPLOYMENT_PARAMETERSPATH_HELP, typeof(FileInfo));
            CreateRuntimeParameter("EnvironmentType", Resources.DEPLOYMENT_ENVIRONMENTTYPE_HELP, typeof(EnvironmentType));
            CreateRuntimeParameter("LicenseName", Resources.DEPLOYMENT_LICENSE_HELP, typeof(string));
            CreateRuntimeParameter("DeploymentTargetName", Resources.DEPLOYMENT_PACKAGE_HELP, typeof(string), true);
            CreateRuntimeParameter("TemplateName", Resources.INFRASTRUCTURE_EXISTING_ENVIRONMENT_TEMPLATE_NAME_HELP, typeof(string));
            CreateRuntimeParameter("OutputDir", Resources.DEPLOYMENT_OUTPUTDIR_HELP, typeof(DirectoryInfo));
        }

        public IEnumerable<RuntimeDefinedParameter> GetParameters()
        {   
            return parameters;
        }

        public void ReadFromPipeline(RuntimeDefinedParameter parameter)
        {
            parametersValue.Add(parameter.Name, parameter.Value);
        }

        public void CreateRuntimeParameter(string name, string helpMessage, Type type, bool mandatory = false)
        {
            ParameterAttribute parameterAttribute = new ParameterAttribute
            {
                HelpMessage = helpMessage,
                Mandatory = mandatory
            };
            RuntimeDefinedParameter runtimeParameter = new RuntimeDefinedParameter
            {
                IsSet = false,
                Name = name,
                ParameterType = type
            };
            runtimeParameter.Attributes.Add(parameterAttribute);
            parameters.Add(runtimeParameter);

        }
        public object GetValue(string key)
        {
            parametersValue.TryGetValue(key, out object value);
            return value;
        }
    }
}
