using System.Management.Automation;

namespace Cmf.CustomerPortal.Sdk.Powershell.Extensions
{
    public interface IParameterExtension
    {
        RuntimeDefinedParameter GetParameter();

        void ReadFromPipeline(object value);
    }
}
