using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.New, "InfrastructureFromTemplate")]
    public class NewInfrastructureFromTemplate : Cmdlet
    {
        [Parameter(
            HelpMessage = Common.Resources.INFRASTRUCTUREFROMTEMPLATE_HELP,
            Mandatory = true
        )]
        public string InfrastructureName
        {
            get;
            set;
        }

    }
}
