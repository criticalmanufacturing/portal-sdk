using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using Cmf.Foundation.Common.Licenses.Enums;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.New, "Environment")]
    public class NewEnvironment : ReplaceTokensBaseCmdlet<NewEnvironmentHandler>
    {
        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_NAME_HELP
        )]
        public string Name { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_PARAMETERSPATH_HELP
        )]
        public FileInfo ParametersPath { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_ENVIRONMENTTYPE_HELP,
            Mandatory = true
        )]
        public EnvironmentType EnvironmentType { get; set; }

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
            HelpMessage = Resources.DEPLOYMENT_TARGET_HELP,
            Mandatory = true
        )]
        public string DeploymentTargetName { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_OUTPUTDIR_HELP
        )]
        public DirectoryInfo OutputDir { get; set; }

        [Parameter(Position = 1)]
        public SwitchParameter Interactive;

        protected async override Task ProcessRecordAsync()
        {
            // get new environment handler and run it
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();
            await newEnvironmentHandler.Run(Name, ParametersPath, EnvironmentType, SiteName, LicenseName, DeploymentPackageName, DeploymentTargetName, OutputDir, GetTokens(), Interactive.ToBool());
        }
    }
}
