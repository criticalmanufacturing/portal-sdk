using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public class NotFoundException : CmfFaultException
    {
        public NotFoundException()
        {
        }

        public NotFoundException(string action, CmfFaultCode code, CmfFaultReason reason, string message)
            : base(message)
        {
            Action = action;
            Code = code;
            Reason = reason;
        }

        public NotFoundException(string message)
            : base(message)
        {
        }
    }
}
