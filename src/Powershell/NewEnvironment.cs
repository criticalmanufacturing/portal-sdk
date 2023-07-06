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
    [Cmdlet(VerbsCommon.New, "Environment")]
    public class NewEnvironment : BaseCmdlet<NewEnvironmentHandler>
    {
        private ReplaceTokensParameterExtension ReplaceTokensExtension;

        private CommonParametersExtension CommonParametersExtension;

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_SITE_HELP,
            Mandatory = true
        )]
        public string SiteName { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_LICENSE_HELP,
            Mandatory = true
        )]
        public string LicenseName { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_PACKAGE_HELP,
            Mandatory = true
        )]
        public string DeploymentPackageName { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_TIMEOUT_MINUTES,
            Mandatory = false
        )]
        public double? DeploymentTimeoutMinutes { get; set; }

        [Parameter(Position = 2, HelpMessage = Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_HELP)]
        public SwitchParameter TerminateOtherVersions;

        [Parameter(Position = 1)]
        public SwitchParameter Interactive;

        protected override IEnumerable<IParameterExtension> ExtendWithRange()
        {
            List<IParameterExtension> parameterExtensions = new List<IParameterExtension>();
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
            await newEnvironmentHandler.Run((string)CommonParametersExtension.GetValue("Name"), (FileInfo)CommonParametersExtension.GetValue("ParametersPath"),
                (EnvironmentType)CommonParametersExtension.GetValue("EnvironmentType"), SiteName, LicenseName, DeploymentPackageName,
               (DeploymentTarget)CommonParametersExtension.GetValue("DeploymentTargetName"), (DirectoryInfo)CommonParametersExtension.GetValue("OutputDir"),
                ReplaceTokensExtension.GetTokens(), Interactive.ToBool(), (string)CommonParametersExtension.GetValue("CustomerInfrastructureName"),
                (string)CommonParametersExtension.GetValue("Description"), TerminateOtherVersions.ToBool(), false,
                 DeploymentTimeoutMinutes);
        }
    }
}
