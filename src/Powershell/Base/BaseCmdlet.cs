using Cmf.CustomerPortal.Sdk.Common;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class BaseCmdlet<T> : PSCmdlet where T : Common.IHandler
    {
        protected IServiceLocator ServiceLocator
        {
            get;
        }

        public BaseCmdlet() {
            this.ServiceLocator = new ServiceLocator(this);
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();
        }
    }
}
