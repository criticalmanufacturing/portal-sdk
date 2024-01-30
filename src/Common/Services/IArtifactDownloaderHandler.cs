using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IArtifactDownloaderHandler
    {
        Task Handle(string customerEnvironmentName, DirectoryInfo outputDir);
    }
}
