using Cmf.Foundation.Common.Base;
using Cmf.MessageBus.Client;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    public interface ICustomerPortalClient
    {
        Task<Transport> GetMessageBusTransport();
        Task<T> GetObjectByName<T>(string name, int levelsToLoad = 0) where T : CoreBase, new();
        Task<T> LoadObjectRelations<T>(T obj, Collection<string> relationsNames) where T : CoreBase, new();
    }
}