using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using Cmf.Foundation.Common.Licenses.Enums;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.New, "EnvironmentForInfrastructure")]
    public class NewEnvironmentForInfrastructure : ReplaceTokensBaseCmdlet<NewEnvironmentForInfrastructureHandler>
    {
        [Parameter(HelpMessage = Resources.INFRASTRUCTURE_EXISTING_NAME_HELP)]
        public string CustomerInfrastructureName { get; set; }

        [Parameter(HelpMessage = Resources.DEPLOYMENT_NAME_HELP)]
        public string Name { get; set; }

        [Parameter(HelpMessage = Resources.DEPLOYMENT_DESCRIPTION_HELP)]
        public string Description { get; set; }

        [Parameter(HelpMessage = Resources.DEPLOYMENT_PARAMETERSPATH_HELP)]
        public FileInfo ParametersPath { get; set; }

        [Parameter(HelpMessage = Resources.DEPLOYMENT_ENVIRONMENTTYPE_HELP)]
        public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.Development;

        [Parameter(HelpMessage = Resources.DEPLOYMENT_LICENSE_HELP)]
        public string LicenseName { get; set; }

        [Parameter(HelpMessage = Resources.DEPLOYMENT_PACKAGE_HELP)]
        public string DeploymentPackageName { get; set; }

        [Parameter(HelpMessage = Resources.DEPLOYMENT_TARGET_HELP)]
        public string DeploymentTargetName { get; set; }

        [Parameter(HelpMessage = Resources.DEPLOYMENT_OUTPUTDIR_HELP)]
        public DirectoryInfo OutputDir { get; set; }

        [Parameter(Position = 1)]
        public SwitchParameter Interactive;

        protected async override Task ProcessRecordAsync()
        {
            // get new environment handler and run it
            NewEnvironmentForInfrastructureHandler newEnvironmentForInfrastructureHandler = ServiceLocator.Get<NewEnvironmentForInfrastructureHandler>();
            await newEnvironmentForInfrastructureHandler
                .Run(CustomerInfrastructureName, Name, Description, ParametersPath, EnvironmentType, LicenseName,
                    DeploymentPackageName, DeploymentTargetName, OutputDir, GetTokens(), Interactive.ToBool());
        }
    }
}
