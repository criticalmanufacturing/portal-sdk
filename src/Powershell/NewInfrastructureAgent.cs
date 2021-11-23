using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using Cmf.CustomerPortal.Sdk.Powershell.Extensions;
using Cmf.Foundation.Common.Licenses.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.New, "InfrastructureAgent")]
    public class NewInfrastructureAgent : BaseCmdlet<NewEnvironmentHandler>
    {
        private ReplaceTokensParameterExtension ReplaceTokensExtension;

        [Parameter(HelpMessage = Resources.INFRASTRUCTURE_EXISTING_NAME_HELP)]
        public string CustomerInfrastructureName { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_NAME_HELP
        )]
        public string Name { get; set; }

        [Parameter(HelpMessage = Resources.DEPLOYMENT_DESCRIPTION_HELP)]
        public string Description { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_PARAMETERSPATH_HELP
        )]
        public FileInfo ParametersPath { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_ENVIRONMENTTYPE_HELP
        )]
        public EnvironmentType EnvironmentType { get; set; } = EnvironmentType.Development;

        public string LicenseName { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_PACKAGE_HELP,
            Mandatory = true
        )]
        public string DeploymentTargetName { get; set; }

        [Parameter(
            HelpMessage = Resources.INFRASTRUCTURE_EXISTING_ENVIRONMENT_TEMPLATE_NAME_HELP
        )]
        public string TemplateName { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_OUTPUTDIR_HELP
        )]
        public DirectoryInfo OutputDir { get; set; }

        [Parameter(Position = 1)]
        public SwitchParameter Interactive;

        protected override IParameterExtension ExtendWith()
        {
            ReplaceTokensExtension = new ReplaceTokensParameterExtension();
            return ReplaceTokensExtension;
        }

        protected async override Task ProcessRecordAsync()
        {
            // get new environment handler and run it
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();
            await newEnvironmentHandler.Run(Name, ParametersPath, EnvironmentType, null, LicenseName, null, DeploymentTargetName, OutputDir,
                ReplaceTokensExtension.GetTokens(), Interactive.ToBool(), CustomerInfrastructureName, Description, TemplateName, true);
        }
    }
}
