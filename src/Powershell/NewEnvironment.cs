using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System;
using System.Management.Automation;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.New, "Environment")]
    public class NewEnvironmentCmdlet : BaseCmdlet<NewEnvironment>
    {
        [Parameter(
            HelpMessage = Common.Resources.DEPLOYMENT_PARAMETERSPATH_HELP,
            Mandatory = true
        )]
        public string ParametersPath
        {
            get; set;
        }

        [Parameter(
            HelpMessage = Common.Resources.DEPLOYMENT_ENVIRONMENTTYPE_HELP,
            Mandatory = true
        )]
        public Common.Enums.EnvironmentType EnvironmentType
        {
            get; set;
        }

        [Parameter(
            HelpMessage = Common.Resources.DEPLOYMENT_SITE_HELP
        )]
        public string SiteName
        {
            get; set;
        }

        [Parameter(
            HelpMessage = Common.Resources.DEPLOYMENT_LICENSE_HELP
        )]
        public string LicenseName
        {
            get; set;
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
        }
    }
}
