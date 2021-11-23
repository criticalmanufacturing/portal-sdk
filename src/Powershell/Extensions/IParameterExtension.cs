using System.Collections.Generic;
using System.Management.Automation;

namespace Cmf.CustomerPortal.Sdk.Powershell.Extensions
{
    public interface IParameterExtension
    {
        IEnumerable<RuntimeDefinedParameter> GetParameters();

        void ReadFromPipeline(RuntimeDefinedParameter value);
    }
}
