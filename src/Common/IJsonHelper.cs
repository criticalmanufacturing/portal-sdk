using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public interface IJsonHelper
    {
        T Deserialize<T>(string json);
        T DeserializeAnonymousType<T>(string value, T anonymousTypeObject);
    }
}
