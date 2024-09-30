using System.IO;
using System.Threading.Tasks;
using Cmf.Foundation.BusinessObjects;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IManifestsDownloaderHandler
    {
        Task<bool> Handle(EntityBase deployEntity, DirectoryInfo outputDir);
    }
}
