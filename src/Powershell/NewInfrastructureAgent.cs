using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using Cmf.CustomerPortal.Sdk.Powershell.Extensions;
using Cmf.Foundation.Common.Licenses.Enums;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.New, "InfrastructureAgent")]
    public class NewInfrastructureAgent : BaseCmdlet<NewEnvironmentHandler>
    {
        private ReplaceTokensParameterExtension ReplaceTokensExtension;
        private CommonParametersExtension CommonParametersExtension;

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_TIMEOUT_MINUTES,
            Mandatory = false
        )]
        public double? DeploymentTimeoutMinutes { get; set; }

        [Parameter(Position = 1)]
        public SwitchParameter Interactive;
        protected override IEnumerable<IParameterExtension> ExtendWithRange()
        {
            List <IParameterExtension> parameterExtensions = new List<IParameterExtension>();
            ReplaceTokensExtension = new ReplaceTokensParameterExtension();
            CommonParametersExtension = new CommonParametersExtension();
            parameterExtensions.Add(ReplaceTokensExtension);
            parameterExtensions.Add(CommonParametersExtension);
            return parameterExtensions;

        }
        protected async override Task ProcessRecordAsync()
        { 
            // get new environment handler and run it
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();
            await newEnvironmentHandler.Run((string)CommonParametersExtension.GetValue("Name"),(FileInfo)CommonParametersExtension.GetValue("ParametersPath"), 
                (EnvironmentType)CommonParametersExtension.GetValue("EnvironmentType"), null, null, null,
                (DeploymentTarget)CommonParametersExtension.GetValue("DeploymentTargetName"), (DirectoryInfo)CommonParametersExtension.GetValue("OutputDir"), ReplaceTokensExtension.GetTokens(), Interactive.ToBool(), 
                (string)CommonParametersExtension.GetValue("CustomerInfrastructureName") , (string)CommonParametersExtension.GetValue("Description"), 
                false, true, DeploymentTimeoutMinutes, false, false);
        }
    }
}
