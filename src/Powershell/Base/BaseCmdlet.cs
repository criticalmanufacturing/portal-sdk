using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Powershell.Extensions;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell.Base
{
    public class BaseCmdlet<T> : AsyncCmdlet, IDynamicParameters where T : IHandler
    {
        private struct ParameterExtensionData
        {
            public IParameterExtension ParameterExtension;
            public RuntimeDefinedParameter RuntimeDefinedParameter;
        }

        private readonly RuntimeDefinedParameterDictionary _runtimeDefinedParameterDictionary = new RuntimeDefinedParameterDictionary();
        private readonly IList<ParameterExtensionData> _parameterExtensions = new List<ParameterExtensionData>();

        protected IServiceLocator ServiceLocator
        {
            get; private set;
        }

        public BaseCmdlet()
        {
            Session session = new Session(this);
            ServiceLocator = new ServiceLocator(session);
        }

        private void UseExtensions(IEnumerable<IParameterExtension> extensions)
        {
            if (extensions != null)
            {
                foreach (IParameterExtension optionExtension in extensions)
                {
                    UseExtension(optionExtension);
                }
            }
        }

        private void UseExtension(IParameterExtension optionExtension)
        {
            if (optionExtension != null)
            {
                // get and save dynamic parameter
                IEnumerable<RuntimeDefinedParameter> parameters = optionExtension.GetParameters();
                foreach(RuntimeDefinedParameter param in parameters)
                {
                    _runtimeDefinedParameterDictionary.Add(param.Name, param);

                    // save extensions instance
                    _parameterExtensions.Add(new ParameterExtensionData { ParameterExtension = optionExtension, RuntimeDefinedParameter = param });
                }
            }
        }

        protected virtual IParameterExtension ExtendWith()
        {
            return null;
        }

        protected virtual IEnumerable<IParameterExtension> ExtendWithRange()
        {
            return null;
        }

        public object GetDynamicParameters()
        {
            UseExtension(ExtendWith());
            UseExtensions(ExtendWithRange());

            return _runtimeDefinedParameterDictionary;
        }

        protected sealed override Task BeginProcessingAsync()
        {
            // for all extensions, set their value from pipeline
            foreach (ParameterExtensionData parameterExtensionData in _parameterExtensions)
            {
                parameterExtensionData.ParameterExtension.ReadFromPipeline(parameterExtensionData.RuntimeDefinedParameter);
            }

            return Task.CompletedTask;
        }
    }
}
