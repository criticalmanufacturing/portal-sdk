﻿using Cmf.CustomerPortal.Sdk.Common;
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
        
        private LicensesParameterExtension LicensesParameterExtension;

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_SITE_HELP,
            Mandatory = true
        )]
        public string SiteName { get; set; }

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

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_TIMEOUT_MINUTES_TO_GET_SOME_MB_MESSAGE,
            Mandatory = false
        )]
        public double? DeploymentTimeoutMinutesToGetSomeMBMsg { get; set; }

        [Parameter(Position = 2, HelpMessage = Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_HELP)]
        public SwitchParameter TerminateOtherVersions;

        [Parameter(Position = 1)]
        public SwitchParameter Interactive;

        [Parameter(Position = 3, HelpMessage = Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_HELP)]
        public SwitchParameter TerminateOtherVersionsRemove;

        [Parameter(Position = 4, HelpMessage = Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_VOLUMES_HELP)]
        public SwitchParameter TerminateOtherVersionsRemoveVolumes;

        protected override IEnumerable<IParameterExtension> ExtendWithRange()
        {
            List<IParameterExtension> parameterExtensions = new List<IParameterExtension>();
            ReplaceTokensExtension = new ReplaceTokensParameterExtension();
            CommonParametersExtension = new CommonParametersExtension();
            LicensesParameterExtension = new LicensesParameterExtension();
            parameterExtensions.Add(ReplaceTokensExtension);
            parameterExtensions.Add(CommonParametersExtension);
            parameterExtensions.Add(LicensesParameterExtension);

            return parameterExtensions;
        }

        protected async override Task ProcessRecordAsync()
        {
            // get new environment handler and run it
            NewEnvironmentHandler newEnvironmentHandler = ServiceLocator.Get<NewEnvironmentHandler>();
            await newEnvironmentHandler.Run((string)CommonParametersExtension.GetValue("Name"), (FileInfo)CommonParametersExtension.GetValue("ParametersPath"),
                (EnvironmentType)CommonParametersExtension.GetValue("EnvironmentType"), SiteName, LicensesParameterExtension.GetTokens(), DeploymentPackageName,
               (DeploymentTarget)CommonParametersExtension.GetValue("DeploymentTargetName"), (DirectoryInfo)CommonParametersExtension.GetValue("OutputDir"),
                ReplaceTokensExtension.GetTokens(), Interactive.ToBool(), (string)CommonParametersExtension.GetValue("CustomerInfrastructureName"),
                (string)CommonParametersExtension.GetValue("Description"), TerminateOtherVersions.ToBool(), false,
                 DeploymentTimeoutMinutes, DeploymentTimeoutMinutesToGetSomeMBMsg, TerminateOtherVersionsRemove.ToBool(), TerminateOtherVersionsRemoveVolumes.ToBool());
        }
    }
}
