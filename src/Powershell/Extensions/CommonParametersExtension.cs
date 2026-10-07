using Cmf.CustomerPortal.Sdk.Common;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;

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
            CreateRuntimeParameter("CustomerInfrastructureName", Resources.InfrastructureExistingNameHelp, typeof(string), true);
            CreateRuntimeParameter("Name", Resources.DeploymentNameHelp, typeof(string), true);
            CreateRuntimeParameter("Description", Resources.DeploymentDescriptionHelp, typeof(string));
            CreateRuntimeParameter("ParametersPath", Resources.DeploymentParametersPathHelp, typeof(FileInfo));
            CreateRuntimeParameter("EnvironmentType", Resources.DeploymentEnvironmentTypeHelp, typeof(EnvironmentType), defaultValue: EnvironmentType.Development);
            CreateRuntimeParameter("DeploymentTargetName", Resources.DeploymentTargetHelp, typeof(DeploymentTarget), mandatory: true);
            CreateRuntimeParameter("OutputDir", Resources.DeploymentOutputDirHelp, typeof(DirectoryInfo));
        }

        public IEnumerable<RuntimeDefinedParameter> GetParameters()
        {   
            return parameters;
        }

        public void ReadFromPipeline(RuntimeDefinedParameter parameter)
        {
            parametersValue.Add(parameter.Name, parameter.Value);
        }

        public void CreateRuntimeParameter(string name, string helpMessage, Type type, bool mandatory = false, object defaultValue = null)
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
                ParameterType = type, 
                Value = defaultValue
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
