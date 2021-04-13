using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public interface IHandler
    {
        Task Run();
    }
}
