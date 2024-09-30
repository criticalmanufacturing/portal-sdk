using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IManifestsDownloaderHandler
    {
        Task Handle(string customerEnvironmentName, DirectoryInfo outputDir);
    }
}
