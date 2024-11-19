using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class ArtifactsDownloaderHandler(ISession session, ICustomerPortalClient customerPortalClient) : IArtifactsDownloaderHandler
    {
        private readonly ISession _session = session;
        private readonly ICustomerPortalClient _customerPortalClient = customerPortalClient;

        public async Task<bool> Handle(EntityBase deployEntity, string outputPath)
        {
            string entityType = deployEntity.GetType() == typeof(CustomerEnvironment) ? "Customer Environment" : "App";

            // get the attachments of the current customer environment or app
            await Task.Delay(TimeSpan.FromSeconds(1));
            var output = await _customerPortalClient.GetAttachmentsForEntity(deployEntity);
            EntityDocumentation attachmentToDownload = null;
            if (output?.Count > 0)
            {
                string prefix = entityType.Replace(" ", "");
                output.Sort((a, b) => DateTime.Compare(b.CreatedOn, a.CreatedOn));
                attachmentToDownload = output.Where(x => x.Filename.StartsWith($"{prefix}_{deployEntity.Name}")).FirstOrDefault();
            }
            
            if (attachmentToDownload == null)
            {
                _session.LogError("No attachment was found to download.");
                return false;
            }
            else
            {
                // Download the attachment
                _session.LogDebug($"Downloading attachment {attachmentToDownload.Filename}");

                string outputFile = await _customerPortalClient.DownloadAttachmentStreaming(attachmentToDownload.Id);

                // create the dir to extract to
                string extractionTarget = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(extractionTarget);

                _session.LogDebug($"Extracting attachment contents to {extractionTarget}");

                // extract the zip to the previously created dir
                ZipFile.ExtractToDirectory(outputFile, extractionTarget);

                // get target full dir
                if (string.IsNullOrEmpty(outputPath))
                {
                    outputPath = Path.Combine(Directory.GetCurrentDirectory(), "out");
                }

                // ensure the output path exists
                Directory.CreateDirectory(outputPath);

                _session.LogDebug($"Moving attachment contents from {extractionTarget} to {outputPath}");

                // create all of the directories
                foreach (string dirPath in Directory.GetDirectories(extractionTarget, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(extractionTarget, outputPath));
                }

                // copy all the files & Replaces any files with the same name
                foreach (string newPath in Directory.GetFiles(extractionTarget, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(extractionTarget, outputPath), true);
                }

                _session.LogDebug($"Attachment successfully downloaded to {outputPath}");
            }
            return true;
        }
    }
}
