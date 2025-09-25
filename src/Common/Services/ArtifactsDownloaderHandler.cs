using System.IO.Abstractions;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class ArtifactsDownloaderHandler(
        ISession session,
        IFileSystem fileSystem,
        ICustomerPortalClient customerPortalClient)
        : IArtifactsDownloaderHandler
    {
        public async Task<bool> Handle(EntityBase deployEntity, string outputPath)
        {
            var entityTypePrefix =
                deployEntity.GetType() == typeof(CustomerEnvironment) ? "CustomerEnvironment" : "App";

            // get the attachments of the current customer environment or app
            var attachments = await customerPortalClient.GetAttachmentsForEntity(deployEntity) ?? [];

            var attachmentToDownload = attachments
                .Where(e => e.Filename.StartsWith($"{entityTypePrefix}_{deployEntity.Name}"))
                .OrderByDescending(e => e.CreatedOn)
                .FirstOrDefault(); // default is null

            if (attachmentToDownload == null)
            {
                session.LogError("No attachment was found to download.");
                return false;
            }

            // Download the attachment
            session.LogDebug($"Downloading attachment {attachmentToDownload.Filename}");

            string archivePath = await customerPortalClient.DownloadAttachmentStreaming(attachmentToDownload.Id);

            var outDir = fileSystem.DirectoryInfo.New(outputPath);

            // ensure the output path exists
            outDir.Create();

            session.LogDebug($"Extracting attachment contents to {outDir.FullName}");
            using var zip = new ZipArchive(fileSystem.File.OpenRead(archivePath));
            foreach (var entry in zip.Entries)
            {
                // ZipArchiveEntry paths are always relative
                var subDir = fileSystem.Path.GetDirectoryName(entry.FullName);
                if (!string.IsNullOrWhiteSpace(subDir))
                {
                    outDir.CreateSubdirectory(subDir);
                }

                var fullPath = fileSystem.Path.Combine(outDir.FullName, entry.FullName);
                await using var outputStream = fileSystem.File.Create(fullPath);
                await using var entryStream = entry.Open();
                await entryStream.CopyToAsync(outputStream);
            }

            session.LogDebug($"Attachment successfully downloaded to {outDir.FullName}");
            return true;
        }
    }
}
